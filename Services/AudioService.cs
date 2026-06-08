using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Background music (looping) + tap SFX.
/// pong.mp3 bytes pre-loaded once so every tap is instant.
/// MusicVolume: applied to live player immediately; Preferences write debounced 400ms.
/// </summary>
public class AudioService : IDisposable
{
    private IAudioPlayer? _bgPlayer;
    private Stream?       _bgStream;
    private bool          _disposed;

    private byte[]? _pongBytes;

    // Cached volume — avoids Preferences.Get on every slider tick
    private double _vol;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey      = "sfx_enabled";

    public AudioService()
    {
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
                catch
                {
                    // If setVolume killed the player, restart it
                    _ = Task.Run(async () =>
                    {
                        MainThread.BeginInvokeOnMainThread(StopBackgroundMusic);
                        await Task.Delay(100);
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

    // ── Lazy audio manager ────────────────────────────────────────────────────
    private static IAudioManager? GetManager()
    {
        try { return AudioManager.Current; }
        catch { return null; }
    }

    // ── Pre-load SFX bytes ────────────────────────────────────────────────────
    public async Task PreloadAsync()
    {
        if (_pongBytes != null) return;
        try
        {
            await using var s = await FileSystem.OpenAppPackageFileAsync("pong.mp3");
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            _pongBytes = ms.ToArray();
        }
        catch { }
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

        var mgr = GetManager();
        if (mgr == null) return;

        try
        {
            _bgStream = await FileSystem.OpenAppPackageFileAsync("background.mp3");
            _bgPlayer = mgr.CreatePlayer(_bgStream);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = _vol;
            _bgPlayer.Play();
        }
        catch
        {
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

    public void PlayTap()
    {
        if (!SfxEnabled || _pongBytes == null) return;
        var mgr = GetManager();
        if (mgr == null) return;
        try
        {
            var ms     = new MemoryStream(_pongBytes);
            var player = mgr.CreatePlayer(ms);
            player.Volume = 1.0;
            player.Play();
            player.PlaybackEnded += (_, _) =>
                _ = Task.Run(async () =>
                {
                    await Task.Delay(400);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { player.Dispose(); } catch { }
                        try { ms.Dispose();     } catch { }
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
