using Plugin.Maui.Audio;

namespace LangGuess.Services;

public class AudioService : IDisposable
{
    private readonly IAudioManager _mgr;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IAudioPlayer? _bgPlayer;
    private bool          _disposed;
    private bool          _preloaded;
    private string?       _bgPath;

    private const string MusicKey    = "music_enabled";
    private const string MusicVolKey = "music_volume";

    public bool MusicEnabled
    {
        get => Preferences.Get(MusicKey, true);
        set
        {
            Preferences.Set(MusicKey, value);
            if (value) _ = StartBackgroundMusicAsync();
            else       StopBackgroundMusic();
        }
    }

    public double MusicVolume
    {
        get => Preferences.Get(MusicVolKey, 0.5);
        set
        {
            Preferences.Set(MusicVolKey, value);
            try { if (_bgPlayer != null) _bgPlayer.Volume = value; } catch { }
        }
    }

    public AudioService(IAudioManager audioManager) => _mgr = audioManager;

    // ── Preload ───────────────────────────────────────────────────────────────

    public async Task PreloadAsync()
    {
        if (_preloaded) return;
        _preloaded = true;
        _bgPath = Path.Combine(FileSystem.CacheDirectory, "bg_music.mp3");
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

    // ── Music — called only from GamePage and StreakPage ──────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (!MusicEnabled) return;
        if (!_preloaded) await PreloadAsync();
        if (_bgPath == null || !File.Exists(_bgPath)) return;

        if (!await _lock.WaitAsync(50)) return;
        try
        {
            if (_bgPlayer != null)
            {
                bool playing = false;
                try { playing = _bgPlayer.IsPlaying; } catch { }
                if (playing) return;
                KillUnsafe();
            }

            var p = _mgr.CreatePlayer(_bgPath);
            p.Volume = MusicVolume;
            p.Loop   = true;
            p.PlaybackEnded += OnEnded;
            _bgPlayer = p;
            p.Play();
        }
        catch { KillUnsafe(); }
        finally { _lock.Release(); }
    }

    public void StopBackgroundMusic()
    {
        if (!_lock.Wait(200)) return;
        try   { KillUnsafe(); }
        finally { _lock.Release(); }
    }

    private void OnEnded(object? sender, EventArgs e)
    {
        var old = _bgPlayer;
        _bgPlayer = null;
        if (old != null) try { old.PlaybackEnded -= OnEnded; } catch { }
        _ = Task.Run(() => { try { old?.Stop(); old?.Dispose(); } catch { } });

        if (!MusicEnabled) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(250);
            await StartBackgroundMusicAsync();
        });
    }

    private void KillUnsafe()
    {
        var p = _bgPlayer;
        _bgPlayer = null;
        if (p == null) return;
        try { p.PlaybackEnded -= OnEnded; } catch { }
        try { p.Stop(); }    catch { }
        try { p.Dispose(); } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lock.Dispose();
        KillUnsafe();
    }
}
