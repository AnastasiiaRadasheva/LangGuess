using Plugin.Maui.Audio;

namespace LangGuess.Services;

public class AudioService : IDisposable
{
    private readonly IAudioManager? _audio;
    private IAudioPlayer?           _bgPlayer;
    private Stream?                 _bgStream;
    private System.Threading.Timer? _fadeTimer;
    private bool                    _fadingOut;
    private bool                    _disposed;

    private const string MusicKey   = "music_enabled";
    private const string MusicVolKey = "music_volume";
    private const string SfxKey     = "sfx_enabled";

    public bool MusicEnabled
    {
        get => Preferences.Get(MusicKey, true);
        set
        {
            Preferences.Set(MusicKey, value);
            if (!value) StopBackgroundMusic();
            else        _ = StartBackgroundMusicAsync();
        }
    }

    public double MusicVolume
    {
        get => Preferences.Get(MusicVolKey, 0.5);
        set
        {
            Preferences.Set(MusicVolKey, value);
            if (_bgPlayer != null) _bgPlayer.Volume = value;
        }
    }

    public bool SfxEnabled
    {
        get => Preferences.Get(SfxKey, true);
        set => Preferences.Set(SfxKey, value);
    }

    // Legacy property so SettingsService doesn't break
    public bool IsEnabled
    {
        get => SfxEnabled;
        set => SfxEnabled = value;
    }

    public AudioService(IAudioManager? audio = null) => _audio = audio;

    // ── Background music ────────────────────────────────────────────────────

    public async Task StartBackgroundMusicAsync()
    {
        if (_audio == null || !MusicEnabled) return;
        try
        {
            StopBackgroundMusic();
            _bgStream = await FileSystem.OpenAppPackageFileAsync("background.mp3");
            _bgPlayer = _audio.CreatePlayer(_bgStream);
            _bgPlayer.Volume = 0;
            _bgPlayer.Loop   = false; // we handle loop manually for crossfade
            _bgPlayer.PlaybackEnded += OnBgEnded;
            _bgPlayer.Play();
            _fadingOut = false;
            StartFadeTimer();
            await FadeInAsync();
        }
        catch { /* file missing — silently skip */ }
    }

    public void StopBackgroundMusic()
    {
        _fadeTimer?.Dispose();
        _fadeTimer = null;
        if (_bgPlayer != null)
        {
            _bgPlayer.PlaybackEnded -= OnBgEnded;
            _bgPlayer.Stop();
            _bgPlayer.Dispose();
            _bgPlayer = null;
        }
        _bgStream?.Dispose();
        _bgStream = null;
    }

    // ── Crossfade loop ──────────────────────────────────────────────────────

    private void StartFadeTimer()
    {
        _fadeTimer?.Dispose();
        _fadeTimer = new System.Threading.Timer(CheckFade, null, 500, 500);
    }

    private async void CheckFade(object? _)
    {
        if (_bgPlayer == null || _fadingOut) return;
        try
        {
            double remaining = _bgPlayer.Duration - _bgPlayer.CurrentPosition;
            if (remaining > 0 && remaining < 3.0)
            {
                _fadingOut = true;
                await FadeOutAsync();
            }
        }
        catch { }
    }

    private async Task FadeInAsync()
    {
        double target = MusicVolume;
        if (_bgPlayer == null) return;
        while (_bgPlayer != null && _bgPlayer.Volume < target - 0.01)
        {
            _bgPlayer.Volume = Math.Min(target, _bgPlayer.Volume + 0.04);
            await Task.Delay(60);
        }
        if (_bgPlayer != null) _bgPlayer.Volume = target;
    }

    private async Task FadeOutAsync()
    {
        if (_bgPlayer == null) return;
        while (_bgPlayer != null && _bgPlayer.Volume > 0.01)
        {
            _bgPlayer.Volume = Math.Max(0, _bgPlayer.Volume - 0.04);
            await Task.Delay(60);
        }
        if (_bgPlayer != null) _bgPlayer.Volume = 0;
    }

    private async void OnBgEnded(object? sender, EventArgs e)
    {
        _fadeTimer?.Dispose();
        _fadeTimer = null;
        if (!MusicEnabled) return;
        // Brief pause before restarting so the loop feels intentional
        await Task.Delay(200);
        await StartBackgroundMusicAsync();
    }

    // ── SFX ────────────────────────────────────────────────────────────────

    private async Task PlaySfxAsync(string fileName)
    {
        if (!SfxEnabled || _audio == null) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var player = _audio.CreatePlayer(stream);
            player.Volume = 1.0;
            player.Play();
            player.PlaybackEnded += (_, _) => { player.Dispose(); stream.Dispose(); };
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
