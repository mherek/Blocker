using System;
using System.IO;
using System.Text.Json;

namespace Blocker
{
    public class ConfigManager
    {
    private readonly string configPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "config.json"
);

        public AppConfig Load()
        {
            if (!File.Exists(configPath))
            {
                AppConfig defaultConfig = new AppConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            string json = File.ReadAllText(configPath);

            AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json);

            return config ?? new AppConfig();
        }

        public void Save(AppConfig config)
        {
            string json = JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(configPath, json);
        }
    }
}