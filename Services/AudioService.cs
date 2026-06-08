using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Background music (looping) + tap SFX.
///
/// IAudioManager is injected via DI with a lazy factory (resolved on first access,
/// after Android platform is fully ready — not during CreateMauiApp).
///
/// pong.mp3 bytes pre-loaded once into RAM so every tap is instant.
/// MusicVolume: applied to live player immediately; Preferences write debounced 400ms.
/// </summary>
public class AudioService : IDisposable
{
    private readonly IAudioManager _mgr;

    private IAudioPlayer? _bgPlayer;
    private Stream?       _bgStream;
    private bool          _disposed;

    private byte[]? _pongBytes;

    // Cached volume — avoids Preferences.Get on every slider tick
    private double _vol;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey      = "sfx_enabled";

    // Diagnostic string — visible on HomePage in DEBUG builds
    public string LastError { get; private set; } = "";

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

            // Apply immediately to the live player
            if (_bgPlayer != null)
            {
                try
                {
                    _bgPlayer.Volume = _vol;
                }
                catch (Exception ex)
                {
                    LastError = $"setVolume: {ex.Message}";
                    // Player died — restart it
                    _ = Task.Run(async () =>
                    {
                        MainThread.BeginInvokeOnMainThread(StopBackgroundMusic);
                        await Task.Delay(150);
                        await StartBackgroundMusicAsync();
                    });
                }
            }

            // Debounce Preferences write — don't hit disk 60x/sec while slider moves
            _volSaveCts?.Cancel();
            _volSaveCts = new CancellationTokenSource();
            var token = _volSaveCts.Token;
            _ = Task.Delay(400, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    Preferences.Set(MusicVolKey, _vol);
            }, TaskScheduler.Default);
        }
    }

    public bool SfxEnabled
    {
        get => Preferences.Get(SfxKey, true);
        set => Preferences.Set(SfxKey, value);
    }

    public bool IsEnabled { get => SfxEnabled; set => SfxEnabled = value; }

    // ── Pre-load SFX bytes ────────────────────────────────────────────────────
    public async Task PreloadAsync()
    {
        if (_pongBytes != null) return;
        try
        {
            using var s  = await FileSystem.OpenAppPackageFileAsync("pong.mp3");
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            _pongBytes = ms.ToArray();
            LastError = $"pong loaded {_pongBytes.Length} bytes";
        }
        catch (Exception ex)
        {
            LastError = $"PreloadAsync: {ex.Message}";
        }
    }

    // ── Background music ─────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled) return;

        if (_bgPlayer != null)
        {
            try
            {
                if (_bgPlayer.IsPlaying) return;
                StopBackgroundMusic();
            }
            catch
            {
                StopBackgroundMusic();
            }
        }

        try
        {
            _bgStream = await FileSystem.OpenAppPackageFileAsync("background.mp3");
            _bgPlayer = _mgr.CreatePlayer(_bgStream);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = _vol;
            _bgPlayer.Play();
            LastError = "music started";
        }
        catch (Exception ex)
        {
            LastError = $"StartMusic: {ex.Message}";
            StopBackgroundMusic();
        }
    }

    public void StopBackgroundMusic()
    {
        var p = _bgPlayer; var s = _bgStream;
        _bgPlayer = null;  _bgStream = null;
        try { p?.Stop(); }    catch { }
        try { p?.Dispose(); } catch { }
        try { s?.Dispose(); } catch { }
    }

    // ── SFX ──────────────────────────────────────────────────────────────────
    // Deferred disposal off the PlaybackEnded Java thread (Android safety).

    public void PlayTap()
    {
        if (!SfxEnabled) return;

        try
        {
            IAudioPlayer player;
            Stream       stream;

            if (_pongBytes != null)
            {
                // Fast path — pre-loaded bytes, no file I/O
                stream = new MemoryStream(_pongBytes);
                player = _mgr.CreatePlayer(stream);
            }
            else
            {
                // Fallback: try to load the file synchronously
                // (happens if PreloadAsync hasn't run yet)
                var t = FileSystem.OpenAppPackageFileAsync("pong.mp3");
                t.Wait(1000);
                if (!t.IsCompletedSuccessfully) return;
                stream = t.Result;
                player = _mgr.CreatePlayer(stream);
            }

            player.Volume = 1.0;
            player.Play();
            player.PlaybackEnded += (_, _) =>
                _ = Task.Run(async () =>
                {
                    await Task.Delay(400);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { player.Dispose(); } catch { }
                        try { stream.Dispose(); } catch { }
                    });
                });
        }
        catch (Exception ex)
        {
            LastError = $"PlayTap: {ex.Message}";
        }
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
