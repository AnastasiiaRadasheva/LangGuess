using Plugin.Maui.Audio;

namespace LangGuess.Services;

/// <summary>
/// Background music (looping) + tap SFX.
/// pong.mp3 bytes are pre-loaded once so every tap is instant (no file I/O per tap).
/// </summary>
public class AudioService : IDisposable
{
    private IAudioPlayer? _bgPlayer;
    private Stream?       _bgStream;
    private bool          _disposed;

    // Pre-loaded SFX bytes – populated once in PreloadAsync()
    private byte[]? _pongBytes;

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

    // kept for SettingsViewModel compatibility
    public bool IsEnabled { get => SfxEnabled; set => SfxEnabled = value; }

    // ── Lazy audio manager ────────────────────────────────────────────────────
    private static IAudioManager? GetManager()
    {
        try { return AudioManager.Current; }
        catch { return null; }
    }

    // ── Pre-load SFX bytes ───────────────────────────────────────────────────
    /// <summary>Call once from HomePage.OnAppearing — loads pong.mp3 into RAM.</summary>
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
        catch { /* silent – SFX just won't play if file missing */ }
    }

    // ── Background music ─────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled) return;

        // If player exists and is still running – just ensure volume is right
        if (_bgPlayer != null)
        {
            try
            {
                if (_bgPlayer.IsPlaying)
                {
                    _bgPlayer.Volume = MusicVolume;
                    return;
                }
                // Player exists but stopped — dispose and recreate below
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
            _bgPlayer.Volume = MusicVolume;
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
    // Uses pre-loaded bytes → no async file I/O on the hot path.
    // Disposal deferred off the PlaybackEnded Java thread to avoid
    // ObjectDisposedException on Android.

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

    // Async wrappers kept so ViewModels compile unchanged
    public Task PlayTapAsync()     { PlayTap(); return Task.CompletedTask; }
    public Task PlayCorrectAsync() { PlayTap(); return Task.CompletedTask; }
    public Task PlayWrongAsync()   { PlayTap(); return Task.CompletedTask; }
    public Task PlayWinAsync()     { PlayTap(); return Task.CompletedTask; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopBackgroundMusic();
    }
}
