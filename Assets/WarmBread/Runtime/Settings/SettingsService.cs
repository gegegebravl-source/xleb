using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace WarmBread
{
    public sealed class SettingsService
    {
        private readonly string path;
        public SettingsService(string directory) { path = Path.Combine(directory, "warm-bread-settings.json"); }
        public GameSettings Current { get; private set; } = new GameSettings();
        public event Action<GameSettings> Changed;

        public void Load()
        {
            try
            {
                if (File.Exists(path)) Current = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText(path)) ?? new GameSettings();
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException || exception is UnauthorizedAccessException)
            {
                Current = new GameSettings();
            }
            Current.Clamp();
            Changed?.Invoke(Current);
        }

        public void Apply(GameSettings settings)
        {
            Current = settings ?? new GameSettings();
            Current.Clamp();
            Changed?.Invoke(Current);
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(Current, Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        public void RestoreDefaults() { Apply(new GameSettings()); }
    }
}
