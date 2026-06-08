using Plugin.Maui.Audio;

namespace LangGuess.Services;

public class AudioService : IDisposable
{
    private readonly IAudioManager? _audio;
    private IAudioPlayer?           _bgPlayer;
    private Stream?                 _bgStream;
    private bool                    _disposed;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey      = "sfx_enabled";

    public bool MusicEnabled
    {
        get => Preferences.Get(MusicKey, true);
        set
        {
            Preferences.Set(MusicKey, value);
            if (!value)
                MainThread.BeginInvokeOnMainThread(StopBackgroundMusic);
            else
                _ = StartBackgroundMusicAsync();
        }
    }

    public double MusicVolume
    {
        get => Preferences.Get(MusicVolKey, 0.5);
        set
        {
            var v = Math.Clamp(value, 0.0, 1.0);
            Preferences.Set(MusicVolKey, v);
            if (_bgPlayer != null)
                try { _bgPlayer.Volume = v; } catch { }
        }
    }

    public bool SfxEnabled
    {
        get => Preferences.Get(SfxKey, true);
        set => Preferences.Set(SfxKey, value);
    }

    // Legacy alias so nothing else breaks
    public bool IsEnabled
    {
        get => SfxEnabled;
        set => SfxEnabled = value;
    }

    public AudioService(IAudioManager? audio = null) => _audio = audio;

    // ── Background music ────────────────────────────────────────────────────
    // Uses Loop = true — seamless looping without any timer or event hackery.

    public async Task StartBackgroundMusicAsync()
    {
        if (_audio == null || !MusicEnabled) return;
        if (_bgPlayer != null) return; // already running

        try
        {
            _bgStream = await FileSystem.OpenAppPackageFileAsync("background.mp3");
            _bgPlayer = _audio.CreatePlayer(_bgStream);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = MusicVolume;
            _bgPlayer.Play();
        }
        catch { /* file missing or audio not available */ }
    }

    public void StopBackgroundMusic()
    {
        var player = _bgPlayer;
        var stream = _bgStream;
        _bgPlayer = null;
        _bgStream = null;

        try { player?.Stop(); }    catch { }
        try { player?.Dispose(); } catch { }
        try { stream?.Dispose(); } catch { }
    }

    // ── SFX ────────────────────────────────────────────────────────────────
    // IMPORTANT: Never call player.Dispose() inside PlaybackEnded on Android —
    // that fires on the Java audio thread and causes ObjectDisposedException.
    // Instead we wait a short moment on a background thread, then dispose on
    // the main thread after the callback chain is fully unwound.

    private async Task PlaySfxAsync(string fileName)
    {
        if (!SfxEnabled || _audio == null) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = _audio.CreatePlayer(stream);
            player.Volume = 1.0;
            player.Play();

            // Defer cleanup: wait until the sound is definitely done,
            // then dispose safely on the main thread.
            player.PlaybackEnded += (_, _) =>
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { player.Dispose(); } catch { }
                        try { stream.Dispose(); } catch { }
                    });
                });
            };
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
