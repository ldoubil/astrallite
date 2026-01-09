using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AstralLite.Services
{
    public static class UserSettingsService
    {
        private const string SettingsFileName = "settings.json";
        private const string AppFolderName = "AstralLite";

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);

        private sealed class UserSettings
        {
            public string? PlayerName { get; set; }
        }

        public static string? LoadPlayerName()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return null;
                }

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                return settings?.PlayerName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettings] Failed to load settings: {ex.Message}");
                return null;
            }
        }

        public static void SavePlayerName(string? playerName)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);

                var settings = new UserSettings
                {
                    PlayerName = playerName
                };

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettings] Failed to save settings: {ex.Message}");
            }
        }
    }
}
