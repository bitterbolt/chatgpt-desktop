using System;
using System.IO;
using System.Text.Json;

namespace ChatGPT
{
    /// <summary>
    /// Пользовательские настройки, сохраняются в %AppData%\ChatGPT\settings.json.
    /// </summary>
    public class UserSettings
    {
        // Общие
        public bool RememberLast { get; set; } = false;        // запоминать последний открытый сервис
        public bool RememberWindowSize { get; set; } = false;  // запоминать размер окна
        // Специфика ChatGPT
        public bool BypassOnStartup { get; set; } = false;     // включать обход блокировок при запуске

        // Сохраняемое состояние
        public string? LastUrl { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatGPT", "settings.json");

        public static UserSettings Load()
        {
            try
            {
                string path = SettingsPath;
                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));
                    if (loaded != null) return loaded;
                }
            }
            catch { }
            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                string path = SettingsPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(this,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}
