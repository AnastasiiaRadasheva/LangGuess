using Plugin.Maui.Audio;

namespace LangGuess.Services;

public class AudioService
{
    private readonly IAudioManager? _audio;
    private const string SoundKey = "sound_enabled";

    public bool IsEnabled
    {
        get => Preferences.Get(SoundKey, true);
        set => Preferences.Set(SoundKey, value);
    }

    // IAudioManager может быть null если Plugin не инициализирован — всё равно работаем
    public AudioService(IAudioManager? audio = null) => _audio = audio;

    public async Task PlayAsync(string fileName)
    {
        if (!IsEnabled || _audio == null) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var player = _audio.CreatePlayer(stream);
            player.Play();
            await Task.Delay(800); // ждём конца звука до GC
        }
        catch { /* файл не найден — молча пропускаем */ }
    }

    public Task PlayCorrectAsync() => PlayAsync("correct.wav");
    public Task PlayWrongAsync()   => PlayAsync("wrong.wav");
    public Task PlayWinAsync()     => PlayAsync("win.wav");
}
