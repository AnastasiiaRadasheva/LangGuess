using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Handles background music (looping) and tap SFX.
/// Uses AudioManager.Current lazily so it works regardless of DI init order.
/// </summary>
public class AudioService : IDisposable
{
    private IAudioPlayer? _bgPlayer;
    private Stream?       _bgStream;
    private bool          _disposed;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey      = "sfx_enabled";

    // ── Settings ────────────────────────────────────────────────────────────

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

    public double MusicVolume
    {
        get => Preferences.Get(MusicVolKey, 0.5);
        set
        {
            var v = Math.Clamp(value, 0.0, 1.0);
            Preferences.Set(MusicVolKey, v);
            if (_bgPlayer != null) try { _bgPlayer.Volume = v; } catch { }
        }
    }

    public bool SfxEnabled
    {
        get => Preferences.Get(SfxKey, true);
        set => Preferences.Set(SfxKey, value);
    }

    public bool IsEnabled { get => SfxEnabled; set => SfxEnabled = value; }

    // ── Lazy audio manager — no DI dependency, works on all platforms ────────
    private static IAudioManager? GetManager()
    {
        try { return AudioManager.Current; }
        catch { return null; }
    }

    // ── Background music ────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled || _bgPlayer != null) return;
        var mgr = GetManager();
        if (mgr == null) return;
        try
        {
            _bgStream = await FileSystem.OpenAppPackageFileAsync("background.mp3");
            _bgPlayer = mgr.CreatePlayer(_bgStream);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = MusicVolume;
            _bgPlayer.Play();
        }
        catch { }
    }

    public void StopBackgroundMusic()
    {
        var p = _bgPlayer; var s = _bgStream;
        _bgPlayer = null;  _bgStream = null;
        try { p?.Stop(); }    catch { }
        try { p?.Dispose(); } catch { }
        try { s?.Dispose(); } catch { }
    }

    // ── SFX ────────────────────────────────────────────────────────────────
    // Disposal is deferred off the PlaybackEnded callback thread to avoid
    // ObjectDisposedException on Android (Java audio thread cannot dispose
    // Mono objects directly).

    private async Task PlaySfxAsync(string fileName)
    {
        if (!SfxEnabled) return;
        var mgr = GetManager();
        if (mgr == null) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = mgr.CreatePlayer(stream);
            player.Volume = 1.0;
            player.Play();
            player.PlaybackEnded += (_, _) =>
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { player.Dispose(); } catch { }
                        try { stream.Dispose(); } catch { }
                    });
                });
        }
        catch { }
    }

    public Task PlayTapAsync()     => PlaySfxAsync("pong.mp3");
    public Task PlayCorrectAsync() => PlaySfxAsync("pong.mp3");
    public Task PlayWrongAsync()   => PlaySfxAsync("pong.mp3");
    public Task PlayWinAsync()     => PlaySfxAsync("pong.mp3");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopBackgroundMusic();
    }
}
