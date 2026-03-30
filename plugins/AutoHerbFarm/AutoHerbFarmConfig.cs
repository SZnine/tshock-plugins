using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria.ID;

namespace AutoHerbFarm;

public sealed class AutoHerbFarmConfig
{
    public int ScanIntervalTicks { get; set; } = 300;

    public int MatureChancePercent { get; set; } = 3;

    public int GrowthPassesPerScan { get; set; } = 1;

    public int MinimumRegrowScans { get; set; } = 120;

    public int HerbYield { get; set; } = 1;

    public int SeedYieldMin { get; set; } = 1;

    public int SeedYieldMax { get; set; } = 3;

    public bool RequireChestSpaceForHarvest { get; set; } = true;

    public List<HerbFarmDefinition> Farms { get; set; } =
    [
        HerbFarmDefinition.CreateExample()
    ];

    [JsonIgnore]
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static AutoHerbFarmConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var config = new AutoHerbFarmConfig();
            config.Save(path);
            return config;
        }

        var text = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<AutoHerbFarmConfig>(text, JsonOptions);
        if (loaded is null)
        {
            throw new InvalidOperationException("Failed to deserialize AutoHerbFarm config.");
        }

        loaded.Normalize();
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

    public void Normalize()
    {
        ScanIntervalTicks = Math.Max(1, ScanIntervalTicks);
        MatureChancePercent = Math.Clamp(MatureChancePercent, 0, 100);
        GrowthPassesPerScan = Math.Clamp(GrowthPassesPerScan, 1, 20);
        MinimumRegrowScans = Math.Max(0, MinimumRegrowScans);
        HerbYield = Math.Max(1, HerbYield);
        SeedYieldMin = Math.Max(0, SeedYieldMin);
        SeedYieldMax = Math.Max(SeedYieldMin, SeedYieldMax);

        Farms ??= [];
        foreach (var farm in Farms)
        {
            farm.Normalize();
        }
    }
}

public sealed class HerbFarmDefinition
{
    public string Name { get; set; } = "ExampleHerbFarm";

    public bool Enabled { get; set; } = false;

    public int Left { get; set; } = 100;

    public int Top { get; set; } = 200;

    public int Right { get; set; } = 120;

    public int Bottom { get; set; } = 220;

    public int ChestTileX { get; set; } = 121;

    public int ChestTileY { get; set; } = 220;

    public bool IgnoreNaturalGrowthRules { get; set; } = true;

    public int MatureChancePercent { get; set; } = -1;

    public int GrowthPassesPerScan { get; set; } = -1;

    public int MinimumRegrowScans { get; set; } = -1;

    public List<int> AllowedSupportTiles { get; set; } =
    [
        TileID.ClayPot,
        TileID.PlanterBox
    ];

    public void Normalize()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "HerbFarm" : Name.Trim();

        if (Left > Right)
        {
            (Left, Right) = (Right, Left);
        }

        if (Top > Bottom)
        {
            (Top, Bottom) = (Bottom, Top);
        }

        MatureChancePercent = MatureChancePercent < 0 ? -1 : Math.Clamp(MatureChancePercent, 0, 100);
        GrowthPassesPerScan = GrowthPassesPerScan < 0 ? -1 : Math.Clamp(GrowthPassesPerScan, 1, 20);
        MinimumRegrowScans = Math.Max(-1, MinimumRegrowScans);

        AllowedSupportTiles ??= [];
        AllowedSupportTiles = AllowedSupportTiles.Distinct().ToList();
    }

    public int GetEffectiveMatureChancePercent(AutoHerbFarmConfig config)
    {
        return MatureChancePercent >= 0 ? MatureChancePercent : config.MatureChancePercent;
    }

    public int GetEffectiveGrowthPassesPerScan(AutoHerbFarmConfig config)
    {
        return GrowthPassesPerScan >= 0 ? GrowthPassesPerScan : config.GrowthPassesPerScan;
    }

    public int GetEffectiveMinimumRegrowScans(AutoHerbFarmConfig config)
    {
        return MinimumRegrowScans >= 0 ? MinimumRegrowScans : config.MinimumRegrowScans;
    }

    public static HerbFarmDefinition CreateExample() => new();
}
