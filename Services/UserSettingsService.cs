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
            public bool? WfpEnabled { get; set; }
        }

        public static string? LoadPlayerName()
        {
            var settings = LoadSettings();
            return settings.PlayerName;
        }

        public static bool LoadWfpEnabled(bool defaultValue = true)
        {
            var settings = LoadSettings();
            return settings.WfpEnabled ?? defaultValue;
        }

        public static void SaveWfpEnabled(bool enabled)
        {
            var settings = LoadSettings();
            settings.WfpEnabled = enabled;
            SaveSettings(settings);
        }

        public static void SavePlayerName(string? playerName)
        {
            var settings = LoadSettings();
            settings.PlayerName = playerName;
            SaveSettings(settings);
        }

        private static UserSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new UserSettings();
                }

                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettings] Failed to load settings: {ex.Message}");
                return new UserSettings();
            }
        }

        private static void SaveSettings(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);

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
