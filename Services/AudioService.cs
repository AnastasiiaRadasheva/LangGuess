using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Background music + tap SFX.
///
/// Android MediaPlayer needs a real file path — MemoryStream won't work.
/// We copy both assets to the app's cache directory once, then use CreatePlayer(path).
/// </summary>
public class AudioService : IDisposable
{
    private readonly IAudioManager _mgr;

    private IAudioPlayer? _bgPlayer;
    private bool          _disposed;

    // Paths to cached audio files (written once to CacheDirectory)
    private string? _pongPath;
    private string? _bgPath;

    private double _vol;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey      = "sfx_enabled";

    public AudioService(IAudioManager audioManager)
    {
        _mgr = audioManager;
        _vol = Preferences.Get(MusicVolKey, 0.5);
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    public bool MusicEnabled
    {
        get => Preferences.Get(MusicKey, true);
        set
        {
            Preferences.Set(MusicKey, value);
            if (!value) MainThread.BeginInvokeOnMainThread(StopBackgroundMusic);
            else        _ = StartBackgroundMusicAsync();
        }
    }

    private CancellationTokenSource? _volSaveCts;

    public double MusicVolume
    {
        get => _vol;
        set
        {
            _vol = Math.Clamp(value, 0.0, 1.0);

            if (_bgPlayer != null)
            {
                try   { _bgPlayer.Volume = _vol; }
                catch { _ = RestartMusicAsync(); }
            }

            _volSaveCts?.Cancel();
            _volSaveCts = new CancellationTokenSource();
            var token = _volSaveCts.Token;
            _ = Task.Delay(400, token).ContinueWith(t =>
            {
                if (!t.IsCanceled) Preferences.Set(MusicVolKey, _vol);
            }, TaskScheduler.Default);
        }
    }

    public bool SfxEnabled
    {
        get => Preferences.Get(SfxKey, true);
        set => Preferences.Set(SfxKey, value);
    }

    public bool IsEnabled { get => SfxEnabled; set => SfxEnabled = value; }

    // ── Cache audio files to disk (called once from HomePage.OnAppearing) ─────

    public async Task PreloadAsync()
    {
        var cache = FileSystem.CacheDirectory;
        _pongPath = Path.Combine(cache, "pong.mp3");
        _bgPath   = Path.Combine(cache, "background.mp3");

        if (!File.Exists(_pongPath))
        {
            try
            {
                using var src  = await FileSystem.OpenAppPackageFileAsync("pong.mp3");
                await using var dst = File.Create(_pongPath);
                await src.CopyToAsync(dst);
            }
            catch { _pongPath = null; }
        }

        if (!File.Exists(_bgPath))
        {
            try
            {
                using var src  = await FileSystem.OpenAppPackageFileAsync("background.mp3");
                await using var dst = File.Create(_bgPath);
                await src.CopyToAsync(dst);
            }
            catch { _bgPath = null; }
        }
    }

    // ── Background music ─────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled) return;

        if (_bgPlayer != null)
        {
            try   { if (_bgPlayer.IsPlaying) return; }
            catch { }
            StopBackgroundMusic();
        }

        // Ensure cached file exists
        if (_bgPath == null || !File.Exists(_bgPath))
            await PreloadAsync();

        if (_bgPath == null) return;

        try
        {
            _bgPlayer = _mgr.CreatePlayer(_bgPath);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = _vol;
            _bgPlayer.Play();
        }
        catch
        {
            StopBackgroundMusic();
        }
    }

    private async Task RestartMusicAsync()
    {
        StopBackgroundMusic();
        await Task.Delay(100);
        await StartBackgroundMusicAsync();
    }

    public void StopBackgroundMusic()
    {
        var p = _bgPlayer;
        _bgPlayer = null;
        try { p?.Stop(); }    catch { }
        try { p?.Dispose(); } catch { }
    }

    // ── SFX ──────────────────────────────────────────────────────────────────
    // Deferred disposal off the PlaybackEnded Java callback thread.

    public void PlayTap()
    {
        if (!SfxEnabled || _pongPath == null || !File.Exists(_pongPath)) return;
        try
        {
            var player = _mgr.CreatePlayer(_pongPath);
            player.Volume = 1.0;
            player.Play();
            player.PlaybackEnded += (_, _) =>
                _ = Task.Run(async () =>
                {
                    await Task.Delay(400);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { player.Dispose(); } catch { }
                    });
                });
        }
        catch { }
    }

    public Task PlayTapAsync()     { PlayTap(); return Task.CompletedTask; }
    public Task PlayCorrectAsync() { PlayTap(); return Task.CompletedTask; }
    public Task PlayWrongAsync()   { PlayTap(); return Task.CompletedTask; }
    public Task PlayWinAsync()     { PlayTap(); return Task.CompletedTask; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _volSaveCts?.Cancel();
        StopBackgroundMusic();
    }
}
