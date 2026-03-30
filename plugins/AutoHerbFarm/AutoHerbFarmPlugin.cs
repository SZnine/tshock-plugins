using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AutoHerbFarm;

[ApiVersion(2, 1)]
public sealed class AutoHerbFarmPlugin : TerrariaPlugin
{
    private const string AdminPermission = "autoherbfarm.admin";
    private static readonly Version PluginVersion = new(0, 3, 0);

    private readonly Dictionary<int, HerbDropDefinition> _dropsByStyle = new()
    {
        [0] = new HerbDropDefinition(ItemID.Daybloom, ItemID.DaybloomSeeds),
        [1] = new HerbDropDefinition(ItemID.Moonglow, ItemID.MoonglowSeeds),
        [2] = new HerbDropDefinition(ItemID.Blinkroot, ItemID.BlinkrootSeeds),
        [3] = new HerbDropDefinition(ItemID.Deathweed, ItemID.DeathweedSeeds),
        [4] = new HerbDropDefinition(ItemID.Waterleaf, ItemID.WaterleafSeeds),
        [5] = new HerbDropDefinition(ItemID.Fireblossom, ItemID.FireblossomSeeds),
        [6] = new HerbDropDefinition(ItemID.Shiverthorn, ItemID.ShiverthornSeeds)
    };

    private readonly Dictionary<int, FarmSetupState> _setupByPlayer = [];
    private readonly Dictionary<string, long> _regrowReadyScanByTileKey = [];

    private AutoHerbFarmConfig _config = new();
    private string _configPath = string.Empty;
    private int _ticksUntilScan;
    private int _lastHarvestCount;
    private int _lastPlantCount;
    private long _scanCounter;

    public AutoHerbFarmPlugin(Main game)
        : base(game)
    {
    }

    public override string Author => "鱼仔仔面";

    public override string Description => "Automates herb growth, harvest, chest storage, and replanting inside configured farm regions.";

    public override string Name => "AutoHerbFarm";

    public override Version Version => PluginVersion;

    public override void Initialize()
    {
        _configPath = Path.Combine(TShock.SavePath, "AutoHerbFarm.json");
        ReloadConfig();

        ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
        GeneralHooks.ReloadEvent += OnReload;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            GeneralHooks.ReloadEvent -= OnReload;
        }

        base.Dispose(disposing);
    }

    private void OnGameInitialize(EventArgs args)
    {
        Commands.ChatCommands.Add(new Command(AdminPermission, HandleCommand, "ahf", "autoherbfarm")
        {
            HelpText = "Usage: /ahf reload | /ahf status | /ahf list | /ahf pos1 [x y] | /ahf pos2 [x y] | /ahf chest [x y] | /ahf add <name> | /ahf set <name> <growthchance|growthpasses|regrowscans|ignorerules> <value> | /ahf enable <name> [true|false] | /ahf remove <name>"
        });
    }

    private void OnReload(ReloadEventArgs args)
    {
        ReloadConfig();
        args.Player?.SendSuccessMessage("[AutoHerbFarm] Config reloaded.");
    }

    private void OnServerLeave(LeaveEventArgs args)
    {
        _setupByPlayer.Remove(args.Who);
    }

    private void OnGameUpdate(EventArgs args)
    {
        if (_ticksUntilScan > 0)
        {
            _ticksUntilScan--;
            return;
        }

        _ticksUntilScan = _config.ScanIntervalTicks;
        _scanCounter++;
        _lastHarvestCount = 0;
        _lastPlantCount = 0;

        foreach (var farm in _config.Farms)
        {
            if (!farm.Enabled)
            {
                continue;
            }

            ProcessFarm(farm);
        }
    }

    private void HandleCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("Usage: /ahf reload | /ahf status | /ahf list | /ahf pos1 [x y] | /ahf pos2 [x y] | /ahf chest [x y] | /ahf add <name> | /ahf set <name> <growthchance|growthpasses|regrowscans|ignorerules> <value> | /ahf enable <name> [true|false] | /ahf remove <name>");
            return;
        }

        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "reload":
                ReloadConfig();
                args.Player.SendSuccessMessage("[AutoHerbFarm] Config reloaded.");
                break;

            case "status":
                ShowStatus(args.Player);
                break;

            case "list":
                ListFarms(args.Player);
                break;

            case "pos1":
                SetSelectionCorner(args.Player, args.Parameters, true);
                break;

            case "pos2":
                SetSelectionCorner(args.Player, args.Parameters, false);
                break;

            case "chest":
                SetSelectionChest(args.Player, args.Parameters);
                break;

            case "add":
                AddFarmFromSelection(args.Player, args.Parameters);
                break;

            case "set":
                SetFarmValue(args.Player, args.Parameters);
                break;

            case "remove":
                RemoveFarm(args.Player, args.Parameters);
                break;

            case "enable":
                SetFarmEnabled(args.Player, args.Parameters);
                break;

            default:
                args.Player.SendErrorMessage("Unknown subcommand. Usage: /ahf reload | /ahf status | /ahf list | /ahf pos1 [x y] | /ahf pos2 [x y] | /ahf chest [x y] | /ahf add <name> | /ahf set <name> <growthchance|growthpasses|regrowscans|ignorerules> <value> | /ahf enable <name> [true|false] | /ahf remove <name>");
                break;
        }
    }

    private void ShowStatus(TSPlayer player)
    {
        player.SendInfoMessage($"[AutoHerbFarm] Farms={_config.Farms.Count}, Enabled={_config.Farms.Count(static f => f.Enabled)}, Harvested(last)={_lastHarvestCount}, Replanted(last)={_lastPlantCount}, Scan={_config.ScanIntervalTicks} ticks, DefaultChance={_config.MatureChancePercent}%, DefaultPasses={_config.GrowthPassesPerScan}, DefaultRegrowScans={_config.MinimumRegrowScans}");

        if (player.RealPlayer && _setupByPlayer.TryGetValue(player.Index, out var setup))
        {
            var pos1 = setup.Pos1X.HasValue ? $"{setup.Pos1X},{setup.Pos1Y}" : "unset";
            var pos2 = setup.Pos2X.HasValue ? $"{setup.Pos2X},{setup.Pos2Y}" : "unset";
            var chest = setup.ChestTileX.HasValue ? $"{setup.ChestTileX},{setup.ChestTileY}" : "unset";
            player.SendInfoMessage($"[AutoHerbFarm] Selection pos1={pos1}, pos2={pos2}, chest={chest}");
        }
    }

    private void ListFarms(TSPlayer player)
    {
        if (_config.Farms.Count == 0)
        {
            player.SendInfoMessage("[AutoHerbFarm] No farms configured.");
            return;
        }

        foreach (var farm in _config.Farms.OrderBy(static f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            player.SendInfoMessage($"[AutoHerbFarm] {farm.Name}: enabled={farm.Enabled}, area=({farm.Left},{farm.Top})-({farm.Right},{farm.Bottom}), chest=({farm.ChestTileX},{farm.ChestTileY}), ignoreGrowthRules={farm.IgnoreNaturalGrowthRules}, chance={DescribeIntOverride(farm.MatureChancePercent, _config.MatureChancePercent)}%, passes={DescribeIntOverride(farm.GrowthPassesPerScan, _config.GrowthPassesPerScan)}, regrowScans={DescribeIntOverride(farm.MinimumRegrowScans, _config.MinimumRegrowScans)}");
        }
    }

    private void SetSelectionCorner(TSPlayer player, List<string> parameters, bool isFirstCorner)
    {
        if (!player.RealPlayer)
        {
            player.SendErrorMessage("Only in-game players can set selection points.");
            return;
        }

        if (!TryResolveTileCoordinates(player, parameters, 1, out var x, out var y))
        {
            player.SendErrorMessage("Usage: /ahf pos1 [x y] or /ahf pos2 [x y]");
            return;
        }

        var setup = GetOrCreateSetup(player.Index);
        if (isFirstCorner)
        {
            setup.Pos1X = x;
            setup.Pos1Y = y;
            player.SendSuccessMessage($"[AutoHerbFarm] pos1 set to ({x},{y}).");
            return;
        }

        setup.Pos2X = x;
        setup.Pos2Y = y;
        player.SendSuccessMessage($"[AutoHerbFarm] pos2 set to ({x},{y}).");
    }

    private void SetSelectionChest(TSPlayer player, List<string> parameters)
    {
        if (!player.RealPlayer)
        {
            player.SendErrorMessage("Only in-game players can set chest locations.");
            return;
        }

        int chestTileX;
        int chestTileY;

        if (parameters.Count >= 3 && int.TryParse(parameters[1], out chestTileX) && int.TryParse(parameters[2], out chestTileY))
        {
            if (ResolveChestIndex(chestTileX, chestTileY) < 0)
            {
                player.SendErrorMessage("No chest was found at those coordinates.");
                return;
            }
        }
        else
        {
            var playerTileX = GetPlayerTileX(player);
            var playerTileY = GetPlayerTileY(player);
            if (!TryFindNearbyChest(playerTileX, playerTileY, out chestTileX, out chestTileY))
            {
                player.SendErrorMessage("No nearby chest found. Stand near a chest or use /ahf chest <x> <y>.");
                return;
            }
        }

        var setup = GetOrCreateSetup(player.Index);
        setup.ChestTileX = chestTileX;
        setup.ChestTileY = chestTileY;
        player.SendSuccessMessage($"[AutoHerbFarm] chest set to ({chestTileX},{chestTileY}).");
    }

    private void AddFarmFromSelection(TSPlayer player, List<string> parameters)
    {
        if (!player.RealPlayer)
        {
            player.SendErrorMessage("Only in-game players can create farms from selections.");
            return;
        }

        if (parameters.Count < 2)
        {
            player.SendErrorMessage("Usage: /ahf add <name>");
            return;
        }

        if (!_setupByPlayer.TryGetValue(player.Index, out var setup) || !setup.IsReady)
        {
            player.SendErrorMessage("Set /ahf pos1, /ahf pos2, and /ahf chest first.");
            return;
        }

        var name = string.Join(' ', parameters.Skip(1)).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            player.SendErrorMessage("Farm name cannot be empty.");
            return;
        }

        var existing = FindFarm(name);
        var farm = existing ?? new HerbFarmDefinition();
        farm.Name = name;
        farm.Enabled = true;
        farm.Left = Math.Min(setup.Pos1X!.Value, setup.Pos2X!.Value);
        farm.Top = Math.Min(setup.Pos1Y!.Value, setup.Pos2Y!.Value);
        farm.Right = Math.Max(setup.Pos1X.Value, setup.Pos2X.Value);
        farm.Bottom = Math.Max(setup.Pos1Y.Value, setup.Pos2Y.Value);
        farm.ChestTileX = setup.ChestTileX!.Value;
        farm.ChestTileY = setup.ChestTileY!.Value;
        farm.Normalize();

        if (existing is null)
        {
            _config.Farms.Add(farm);
        }

        SaveConfig();
        player.SendSuccessMessage($"[AutoHerbFarm] Farm '{farm.Name}' saved: area=({farm.Left},{farm.Top})-({farm.Right},{farm.Bottom}), chest=({farm.ChestTileX},{farm.ChestTileY}).");
    }

    private void SetFarmValue(TSPlayer player, List<string> parameters)
    {
        if (parameters.Count < 4)
        {
            player.SendErrorMessage("Usage: /ahf set <name> <growthchance|growthpasses|regrowscans|ignorerules> <value>");
            return;
        }

        var field = parameters[^2].ToLowerInvariant();
        var value = parameters[^1];
        var name = string.Join(' ', parameters.Skip(1).Take(parameters.Count - 3)).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            player.SendErrorMessage("Farm name cannot be empty.");
            return;
        }

        var farm = FindFarm(name);
        if (farm is null)
        {
            player.SendErrorMessage("Farm not found.");
            return;
        }

        switch (field)
        {
            case "growthchance":
                if (!TryParseOverrideInt(value, 0, 100, out var growthChance))
                {
                    player.SendErrorMessage("growthchance must be 0-100 or default.");
                    return;
                }

                farm.MatureChancePercent = growthChance;
                break;

            case "growthpasses":
                if (!TryParseOverrideInt(value, 1, 20, out var growthPasses))
                {
                    player.SendErrorMessage("growthpasses must be 1-20 or default.");
                    return;
                }

                farm.GrowthPassesPerScan = growthPasses;
                break;

            case "regrowscans":
                if (!TryParseOverrideInt(value, 0, 100000, out var regrowScans))
                {
                    player.SendErrorMessage("regrowscans must be >= 0 or default.");
                    return;
                }

                farm.MinimumRegrowScans = regrowScans;
                break;

            case "ignorerules":
            case "ignorenaturalgrowthrules":
                if (!bool.TryParse(value, out var ignoreRules))
                {
                    player.SendErrorMessage("ignorerules must be true or false.");
                    return;
                }

                farm.IgnoreNaturalGrowthRules = ignoreRules;
                break;

            default:
                player.SendErrorMessage("Supported fields: growthchance, growthpasses, regrowscans, ignorerules");
                return;
        }

        farm.Normalize();
        SaveConfig();
        player.SendSuccessMessage($"[AutoHerbFarm] Farm '{farm.Name}' updated: {field}={value}.");
    }

    private void RemoveFarm(TSPlayer player, List<string> parameters)
    {
        if (parameters.Count < 2)
        {
            player.SendErrorMessage("Usage: /ahf remove <name>");
            return;
        }

        var name = string.Join(' ', parameters.Skip(1)).Trim();
        var farm = FindFarm(name);
        if (farm is null)
        {
            player.SendErrorMessage("Farm not found.");
            return;
        }

        _config.Farms.Remove(farm);
        SaveConfig();
        player.SendSuccessMessage($"[AutoHerbFarm] Removed farm '{farm.Name}'.");
    }

    private void SetFarmEnabled(TSPlayer player, List<string> parameters)
    {
        if (parameters.Count < 2)
        {
            player.SendErrorMessage("Usage: /ahf enable <name> [true|false]");
            return;
        }

        var enabled = true;
        if (parameters.Count >= 3 && !bool.TryParse(parameters[^1], out enabled))
        {
            player.SendErrorMessage("Enable flag must be true or false.");
            return;
        }

        var nameParts = parameters.Count >= 3 ? parameters.Skip(1).Take(parameters.Count - 2) : parameters.Skip(1);
        var name = string.Join(' ', nameParts).Trim();
        var farm = FindFarm(name);
        if (farm is null)
        {
            player.SendErrorMessage("Farm not found.");
            return;
        }

        farm.Enabled = enabled;
        SaveConfig();
        player.SendSuccessMessage($"[AutoHerbFarm] Farm '{farm.Name}' enabled={farm.Enabled}.");
    }

    private void ProcessFarm(HerbFarmDefinition farm)
    {
        var chestIndex = ResolveChestIndex(farm.ChestTileX, farm.ChestTileY);
        if (chestIndex < 0)
        {
            return;
        }

        var chest = Main.chest[chestIndex];
        if (chest is null)
        {
            return;
        }

        for (var y = farm.Top; y <= farm.Bottom; y++)
        {
            for (var x = farm.Left; x <= farm.Right; x++)
            {
                var tile = Main.tile[x, y];
                if (tile is null)
                {
                    continue;
                }

                TryGrowHerb(tile, x, y, farm);
                TryHarvestHerb(tile, x, y, farm, chest);
            }
        }
    }

    private void TryGrowHerb(ITile tile, int x, int y, HerbFarmDefinition farm)
    {
        if (!farm.IgnoreNaturalGrowthRules || tile.type != TileID.ImmatureHerbs)
        {
            return;
        }

        if (!HasAllowedSupportTile(x, y, farm) || !IsRegrowReady(farm, x, y))
        {
            return;
        }

        var growthPasses = farm.GetEffectiveGrowthPassesPerScan(_config);
        var matureChancePercent = farm.GetEffectiveMatureChancePercent(_config);

        for (var pass = 0; pass < growthPasses; pass++)
        {
            if (WorldGen.genRand.Next(100) >= matureChancePercent)
            {
                continue;
            }

            tile.type = TileID.MatureHerbs;
            WorldGen.SquareTileFrame(x, y, true);
            NetMessage.SendTileSquare(-1, x, y, 1);
            break;
        }
    }

    private void TryHarvestHerb(ITile tile, int x, int y, HerbFarmDefinition farm, Chest chest)
    {
        if (tile.type != TileID.MatureHerbs && tile.type != TileID.BloomingHerbs)
        {
            return;
        }

        if (!HasAllowedSupportTile(x, y, farm))
        {
            return;
        }

        var style = GetHerbStyle(tile);
        if (!_dropsByStyle.TryGetValue(style, out var drop))
        {
            return;
        }

        var herbAmount = _config.HerbYield;
        var seedAmount = WorldGen.genRand.Next(_config.SeedYieldMin, _config.SeedYieldMax + 1);

        if (_config.RequireChestSpaceForHarvest && !CanInsertAll(chest, drop.HerbItemId, herbAmount, drop.SeedItemId, seedAmount))
        {
            return;
        }

        if (!InsertItem(chest, drop.HerbItemId, herbAmount))
        {
            return;
        }

        if (seedAmount > 0)
        {
            InsertItem(chest, drop.SeedItemId, seedAmount);
        }

        tile.type = TileID.ImmatureHerbs;
        tile.frameY = 0;
        SetRegrowCooldown(farm, x, y);
        WorldGen.SquareTileFrame(x, y, true);
        NetMessage.SendTileSquare(-1, x, y, 1);

        _lastHarvestCount++;
        _lastPlantCount++;
    }

    private bool HasAllowedSupportTile(int x, int y, HerbFarmDefinition farm)
    {
        if (y + 1 >= Main.maxTilesY)
        {
            return false;
        }

        var support = Main.tile[x, y + 1];
        if (support is null || !support.active())
        {
            return false;
        }

        return farm.AllowedSupportTiles.Contains(support.type);
    }

    private bool IsRegrowReady(HerbFarmDefinition farm, int x, int y)
    {
        var key = GetTileKey(farm, x, y);
        return !_regrowReadyScanByTileKey.TryGetValue(key, out var readyScan) || _scanCounter >= readyScan;
    }

    private void SetRegrowCooldown(HerbFarmDefinition farm, int x, int y)
    {
        var key = GetTileKey(farm, x, y);
        _regrowReadyScanByTileKey[key] = _scanCounter + farm.GetEffectiveMinimumRegrowScans(_config);
    }

    private static string GetTileKey(HerbFarmDefinition farm, int x, int y)
    {
        return $"{farm.Name}:{x}:{y}";
    }

    private static int GetHerbStyle(ITile tile)
    {
        return tile.frameX / 18;
    }

    private static int ResolveChestIndex(int chestTileX, int chestTileY)
    {
        var direct = Chest.FindChest(chestTileX, chestTileY);
        if (direct >= 0)
        {
            return direct;
        }

        var left = Chest.FindChest(chestTileX - 1, chestTileY);
        if (left >= 0)
        {
            return left;
        }

        var up = Chest.FindChest(chestTileX, chestTileY - 1);
        if (up >= 0)
        {
            return up;
        }

        return Chest.FindChest(chestTileX - 1, chestTileY - 1);
    }

    private bool TryFindNearbyChest(int tileX, int tileY, out int chestTileX, out int chestTileY)
    {
        for (var offsetY = -2; offsetY <= 2; offsetY++)
        {
            for (var offsetX = -2; offsetX <= 2; offsetX++)
            {
                var x = tileX + offsetX;
                var y = tileY + offsetY;
                if (ResolveChestIndex(x, y) < 0)
                {
                    continue;
                }

                chestTileX = x;
                chestTileY = y;
                return true;
            }
        }

        chestTileX = 0;
        chestTileY = 0;
        return false;
    }

    private FarmSetupState GetOrCreateSetup(int playerIndex)
    {
        if (!_setupByPlayer.TryGetValue(playerIndex, out var setup))
        {
            setup = new FarmSetupState();
            _setupByPlayer[playerIndex] = setup;
        }

        return setup;
    }

    private HerbFarmDefinition? FindFarm(string name)
    {
        return _config.Farms.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private void SaveConfig()
    {
        _config.Save(_configPath);
        _ticksUntilScan = 0;
    }

    private static bool TryResolveTileCoordinates(TSPlayer player, List<string> parameters, int startIndex, out int x, out int y)
    {
        if (parameters.Count >= startIndex + 2 && int.TryParse(parameters[startIndex], out x) && int.TryParse(parameters[startIndex + 1], out y))
        {
            return true;
        }

        x = GetPlayerTileX(player);
        y = GetPlayerTileY(player);
        return true;
    }

    private static bool TryParseOverrideInt(string value, int min, int max, out int parsedValue)
    {
        if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase))
        {
            parsedValue = -1;
            return true;
        }

        if (int.TryParse(value, out parsedValue) && parsedValue >= min && parsedValue <= max)
        {
            return true;
        }

        parsedValue = 0;
        return false;
    }

    private static string DescribeIntOverride(int overrideValue, int defaultValue)
    {
        return overrideValue >= 0 ? overrideValue.ToString() : $"default({defaultValue})";
    }

    private static int GetPlayerTileX(TSPlayer player)
    {
        return (int)(player.TPlayer.position.X / 16f);
    }

    private static int GetPlayerTileY(TSPlayer player)
    {
        return (int)(player.TPlayer.position.Y / 16f);
    }

    private static bool CanInsertAll(Chest chest, int herbItemId, int herbAmount, int seedItemId, int seedAmount)
    {
        var snapshot = chest.item.Select(static item => item.Clone()).ToArray();
        return InsertItem(snapshot, herbItemId, herbAmount) && InsertItem(snapshot, seedItemId, seedAmount);
    }

    private static bool InsertItem(Chest chest, int itemId, int stack)
    {
        return InsertItem(chest.item, itemId, stack);
    }

    private static bool InsertItem(Item[] slots, int itemId, int stack)
    {
        if (stack <= 0)
        {
            return true;
        }

        var template = new Item();
        template.SetDefaults(itemId);
        var remaining = stack;

        foreach (var slot in slots)
        {
            if (slot is null || slot.IsAir || slot.type != itemId || slot.stack >= slot.maxStack)
            {
                continue;
            }

            var canMove = Math.Min(slot.maxStack - slot.stack, remaining);
            slot.stack += canMove;
            remaining -= canMove;
            if (remaining <= 0)
            {
                return true;
            }
        }

        foreach (var slot in slots)
        {
            if (slot is null || !slot.IsAir)
            {
                continue;
            }

            slot.SetDefaults(itemId);
            slot.stack = Math.Min(template.maxStack, remaining);
            remaining -= slot.stack;
            if (remaining <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ReloadConfig()
    {
        _config = AutoHerbFarmConfig.Load(_configPath);
        _ticksUntilScan = 0;
    }

    private sealed class FarmSetupState
    {
        public int? Pos1X { get; set; }
        public int? Pos1Y { get; set; }
        public int? Pos2X { get; set; }
        public int? Pos2Y { get; set; }
        public int? ChestTileX { get; set; }
        public int? ChestTileY { get; set; }

        public bool IsReady => Pos1X.HasValue && Pos1Y.HasValue && Pos2X.HasValue && Pos2Y.HasValue && ChestTileX.HasValue && ChestTileY.HasValue;
    }

    private readonly record struct HerbDropDefinition(int HerbItemId, int SeedItemId);
}

