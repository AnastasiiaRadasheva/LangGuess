using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Background music + tap SFX.
///
/// Music lifecycle:
///   App.OnResume  → StartBackgroundMusicAsync()   (initial launch + return from background)
///   App.OnSleep   → StopBackgroundMusic()
///   Settings      → MusicEnabled / MusicVolume
///   Pages         → only call PreloadAsync() for SFX files, never touch music
///
/// Files are copied to CacheDirectory once (Android MediaPlayer needs real file paths).
/// A SemaphoreSlim stops concurrent callers from spawning multiple players.
/// </summary>
public class AudioService : IDisposable
{
    private readonly IAudioManager _mgr;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IAudioPlayer? _bgPlayer;
    private bool          _disposed;
    private bool          _preloaded;

    private string? _pongPath;
    private string? _bgPath;
    private double  _vol;

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

    private CancellationTokenSource? _volCts;

    public double MusicVolume
    {
        get => _vol;
        set
        {
            _vol = Math.Clamp(value, 0.0, 1.0);
            if (_bgPlayer != null)
                try { _bgPlayer.Volume = _vol; } catch { _ = RestartMusicAsync(); }

            _volCts?.Cancel();
            _volCts = new CancellationTokenSource();
            var tok = _volCts.Token;
            _ = Task.Delay(400, tok).ContinueWith(t =>
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

    // ── Copy assets to CacheDirectory (safe to call multiple times) ───────────

    public async Task PreloadAsync()
    {
        if (_preloaded) return;
        _preloaded = true;

        var cache = FileSystem.CacheDirectory;
        _pongPath = Path.Combine(cache, "sfx_pong.mp3");
        _bgPath   = Path.Combine(cache, "bg_music.mp3");

        if (!File.Exists(_pongPath))
        {
            try
            {
                using var src = await FileSystem.OpenAppPackageFileAsync("pong.mp3");
                using var dst = File.Create(_pongPath);
                await src.CopyToAsync(dst);
            }
            catch { _pongPath = null; }
        }

        if (!File.Exists(_bgPath))
        {
            try
            {
                using var src = await FileSystem.OpenAppPackageFileAsync("background.mp3");
                using var dst = File.Create(_bgPath);
                await src.CopyToAsync(dst);
            }
            catch { _bgPath = null; }
        }
    }

    // ── Background music ─────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled) return;

        // Ensure files are on disk before creating player
        if (!_preloaded) await PreloadAsync();
        if (_bgPath == null) return;

        // Block concurrent calls — skip if someone is already starting
        if (!await _lock.WaitAsync(0)) return;
        try
        {
            // Already playing → nothing to do
            if (_bgPlayer != null)
            {
                bool playing = false;
                try { playing = _bgPlayer.IsPlaying; } catch { }
                if (playing) return;
                StopBackgroundMusic();
            }

            _bgPlayer = _mgr.CreatePlayer(_bgPath);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = _vol;
            _bgPlayer.Play();
        }
        catch { StopBackgroundMusic(); }
        finally { _lock.Release(); }
    }

    private async Task RestartMusicAsync()
    {
        StopBackgroundMusic();
        await Task.Delay(150);
        await StartBackgroundMusicAsync();
    }

    public void StopBackgroundMusic()
    {
        var p = _bgPlayer;
        _bgPlayer = null;
        try { p?.Stop(); }    catch { }
        try { p?.Dispose(); } catch { }
    }

    // ── SFX — short pong click, completely separate from music ───────────────

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
        _volCts?.Cancel();
        _lock.Dispose();
        StopBackgroundMusic();
    }
}
