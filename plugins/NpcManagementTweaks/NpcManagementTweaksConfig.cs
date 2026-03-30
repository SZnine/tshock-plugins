using System.Text.Json;
using System.Text.Json.Serialization;

namespace NpcManagementTweaks;

public sealed class NpcManagementTweaksConfig
{
    private const int CurrentConfigVersion = 4;

    public int ConfigVersion { get; set; } = CurrentConfigVersion;

    public TownNpcTweaks TownNpcs { get; set; } = new();

    public TravelingMerchantTweaks TravelingMerchant { get; set; } = new();

    public AnglerTweaks Angler { get; set; } = new();

    [JsonIgnore]
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static NpcManagementTweaksConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var config = new NpcManagementTweaksConfig();
            config.Save(path);
            return config;
        }

        var text = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<NpcManagementTweaksConfig>(text, JsonOptions);
        if (loaded is null)
        {
            throw new InvalidOperationException("Failed to deserialize NpcManagementTweaks config.");
        }

        loaded.Normalize();
        loaded.Save(path);
        return loaded;
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
        TownNpcs ??= new TownNpcTweaks();
        TravelingMerchant ??= new TravelingMerchantTweaks();
        Angler ??= new AnglerTweaks();

        if (ConfigVersion < CurrentConfigVersion)
        {
            Migrate();
        }

        TownNpcs.Normalize();
        TravelingMerchant.Normalize();
        Angler.Normalize();
        ConfigVersion = CurrentConfigVersion;
    }

    private void Migrate()
    {
        if (TownNpcs.ScanIntervalTicks > 1)
        {
            TownNpcs.ScanIntervalTicks = 1;
        }

        if (TownNpcs.TeleportDistanceTiles > 3)
        {
            TownNpcs.TeleportDistanceTiles = 3;
        }

        if (Angler.DailyTurnInLimit <= 0)
        {
            Angler.DailyTurnInLimit = 5;
        }
    }
}

public sealed class TownNpcTweaks
{
    public bool Enabled { get; set; } = true;

    public bool AllowDaytimeGoHome { get; set; } = true;

    public bool TeleportDirectlyHome { get; set; } = true;

    public int ScanIntervalTicks { get; set; } = 1;

    public int TeleportDistanceTiles { get; set; } = 3;

    public void Normalize()
    {
        ScanIntervalTicks = Math.Max(1, ScanIntervalTicks);
        TeleportDistanceTiles = Math.Max(0, TeleportDistanceTiles);
    }
}

public sealed class TravelingMerchantTweaks
{
    public bool Enabled { get; set; } = true;

    public int ArrivalChanceNumerator { get; set; } = 1;

    public int ArrivalChanceDenominator { get; set; } = 3;

    public int ExtraShopSlots { get; set; } = 5;

    public int MergeShopRerolls { get; set; } = 12;

    public void Normalize()
    {
        ArrivalChanceDenominator = Math.Max(1, ArrivalChanceDenominator);
        ArrivalChanceNumerator = Math.Clamp(ArrivalChanceNumerator, 0, ArrivalChanceDenominator);
        ExtraShopSlots = Math.Clamp(ExtraShopSlots, 0, 20);
        MergeShopRerolls = Math.Clamp(MergeShopRerolls, 1, 100);
    }
}

public sealed class AnglerTweaks
{
    public bool Enabled { get; set; } = true;

    public int ScanIntervalTicks { get; set; } = 1;

    public int DailyTurnInLimit { get; set; } = 5;

    public void Normalize()
    {
        ScanIntervalTicks = Math.Max(1, ScanIntervalTicks);
        DailyTurnInLimit = Math.Clamp(DailyTurnInLimit, 1, 100);
    }
}
