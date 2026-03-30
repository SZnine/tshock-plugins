using System.Text.Json;
using System.Text.Json.Serialization;

namespace PermanentPotionBuffs;

public sealed class PermanentPotionBuffsConfig
{
    private const int CurrentConfigVersion = 2;

    public int ConfigVersion { get; set; } = CurrentConfigVersion;

    public int ScanIntervalTicks { get; set; } = 15;

    public int RequiredStack { get; set; } = 30;

    public int PotionDurationMultiplier { get; set; } = 2;

    public bool EnableAllPotions { get; set; } = true;

    public bool EnableAllFoods { get; set; } = true;

    [JsonIgnore]
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static PermanentPotionBuffsConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var config = new PermanentPotionBuffsConfig();
            config.Save(path);
            return config;
        }

        var text = File.ReadAllText(path);
        var configFile = JsonSerializer.Deserialize<PermanentPotionBuffsConfig>(text, JsonOptions);
        if (configFile is null)
        {
            throw new InvalidOperationException("Failed to deserialize PermanentPotionBuffs config.");
        }

        configFile.Normalize();
        configFile.Save(path);
        return configFile;
    }

    public void Save(string path)
    {
        Normalize();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    private void Normalize()
    {
        if (ConfigVersion < 2)
        {
            MigrateToVersion2();
        }

        ScanIntervalTicks = Math.Max(1, ScanIntervalTicks);
        RequiredStack = Math.Max(1, RequiredStack);
        PotionDurationMultiplier = Math.Max(1, PotionDurationMultiplier);
        ConfigVersion = CurrentConfigVersion;
    }

    private void MigrateToVersion2()
    {
        if (RequiredStack == 5)
        {
            RequiredStack = 30;
        }

        if (PotionDurationMultiplier < 2)
        {
            PotionDurationMultiplier = 2;
        }

        if (ScanIntervalTicks == 60)
        {
            ScanIntervalTicks = 15;
        }
    }
}
