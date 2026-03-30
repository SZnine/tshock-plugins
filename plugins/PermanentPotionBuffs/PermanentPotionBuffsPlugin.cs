using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace PermanentPotionBuffs;

[ApiVersion(2, 1)]
public sealed class PermanentPotionBuffsPlugin : TerrariaPlugin
{
    private const string AdminPermission = "permanentpotionbuffs.admin";
    private static readonly Version PluginVersion = new(0, 5, 3);
    private static readonly int[] FoodBuffIds =
    [
        BuffID.WellFed,
        BuffID.WellFed2,
        BuffID.WellFed3
    ];

    private readonly Dictionary<int, PlayerBuffMemory> _buffMemoryByPlayer = [];
    private readonly Dictionary<int, int> _basePotionDurationByBuffId = [];

    private PermanentPotionBuffsConfig _config = new();
    private string _configPath = string.Empty;
    private int _ticksUntilScan;
    private bool _potionCatalogReady;
    private long _gameTickCounter;

    public PermanentPotionBuffsPlugin(Main game)
        : base(game)
    {
    }

    public override string Author => "鱼仔仔面";

    public override string Description => "Grant persistent buffs when enough potions or foods are stored in player-accessible inventories.";

    public override string Name => "PermanentPotionBuffs";

    public override Version Version => PluginVersion;

    public override void Initialize()
    {
        _configPath = Path.Combine(TShock.SavePath, "PermanentPotionBuffs.json");
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
        Commands.ChatCommands.Add(new Command(AdminPermission, HandleCommand, "ppb", "permanentpotionbuffs")
        {
            HelpText = "Usage: /ppb reload | /ppb status [player]"
        });
    }

    private void OnReload(ReloadEventArgs args)
    {
        ReloadConfig();
        args.Player?.SendSuccessMessage("[PermanentPotionBuffs] Config reloaded.");
    }

    private void OnServerLeave(LeaveEventArgs args)
    {
        _buffMemoryByPlayer.Remove(args.Who);
    }

    private void OnGameUpdate(EventArgs args)
    {
        _gameTickCounter++;

        if (!_potionCatalogReady)
        {
            TryBuildPotionCatalog();
        }

        if (_ticksUntilScan > 0)
        {
            _ticksUntilScan--;
            return;
        }

        _ticksUntilScan = _config.ScanIntervalTicks;
        var scanTick = _gameTickCounter;

        foreach (var tsPlayer in TShock.Players)
        {
            if (tsPlayer?.Active != true)
            {
                continue;
            }

            ApplyConfiguredBuffs(tsPlayer, scanTick);
        }
    }

    private void HandleCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("Usage: /ppb reload | /ppb status [player]");
            return;
        }

        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "reload":
                ReloadConfig();
                args.Player.SendSuccessMessage("[PermanentPotionBuffs] Config reloaded.");
                break;

            case "status":
                var target = ResolvePlayer(args);
                if (target is null)
                {
                    return;
                }

                ShowStatus(args.Player, target);
                break;

            default:
                args.Player.SendErrorMessage("Unknown subcommand. Usage: /ppb reload | /ppb status [player]");
                break;
        }
    }

    private TSPlayer? ResolvePlayer(CommandArgs args)
    {
        if (args.Parameters.Count == 1)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("Console must specify a player name.");
                return null;
            }

            return args.Player;
        }

        var matches = TSPlayer.FindByNameOrID(args.Parameters[1]);
        if (matches.Count == 0)
        {
            args.Player.SendErrorMessage("Player not found.");
            return null;
        }

        if (matches.Count > 1)
        {
            args.Player.SendMultipleMatchError(matches.Select(static p => p.Name));
            return null;
        }

        return matches[0];
    }

    private void ShowStatus(TSPlayer viewer, TSPlayer target)
    {
        var summary = BuildInventorySummary(target.TPlayer);
        var qualifiedPotions = summary.Potions.Values
            .Where(static p => p.Stack >= p.RequiredStack)
            .OrderByDescending(static p => p.Stack)
            .ThenBy(static p => Lang.GetItemNameValue(p.ItemId))
            .ToList();

        viewer.SendInfoMessage($"[PermanentPotionBuffs] {target.Name}:");
        viewer.SendInfoMessage($"- config => required stack {_config.RequiredStack}, normal potion duration x{_config.PotionDurationMultiplier}, scan {_config.ScanIntervalTicks} ticks, catalog {(_potionCatalogReady ? "ready" : "warming")}");
        viewer.SendInfoMessage($"- infinite potions => {qualifiedPotions.Count}");

        if (qualifiedPotions.Count == 0)
        {
            viewer.SendInfoMessage("- infinite potion details => none qualified");
        }
        else
        {
            foreach (var potion in qualifiedPotions.Take(8))
            {
                viewer.SendInfoMessage($"- {Lang.GetItemNameValue(potion.ItemId)} => {potion.Stack}/{potion.RequiredStack}, buff {potion.BuffId}, infinite refresh {potion.DurationTicks}");
            }
        }

        if (summary.Food.BuffId > 0)
        {
            viewer.SendInfoMessage($"- infinite food => {Lang.GetItemNameValue(summary.Food.ItemId)} {summary.Food.Stack}/{_config.RequiredStack}, buff {summary.Food.BuffId}, refresh {summary.Food.DurationTicks}");
        }
        else
        {
            viewer.SendInfoMessage($"- infinite food => none qualified, highest stack {summary.HighestFoodStack}/{_config.RequiredStack}");
        }

        if (_buffMemoryByPlayer.TryGetValue(target.Index, out var memory) && memory.TransientPotionBuffs.Count > 0)
        {
            foreach (var entry in memory.TransientPotionBuffs.OrderBy(static e => e.Key).Take(8))
            {
                viewer.SendInfoMessage($"- normal potion buff {entry.Key} => remaining {entry.Value.RemainingTime}, boosted {entry.Value.BoostedDuration}, restore grace {entry.Value.RestoreGraceScans}");
            }
        }
    }

    private void ReloadConfig()
    {
        _config = PermanentPotionBuffsConfig.Load(_configPath);
        _ticksUntilScan = 0;
        _potionCatalogReady = false;
        _basePotionDurationByBuffId.Clear();
    }

    private void TryBuildPotionCatalog()
    {
        try
        {
            _basePotionDurationByBuffId.Clear();

            for (var itemId = 1; itemId < ItemID.Count; itemId++)
            {
                var item = new Item();
                item.SetDefaults(itemId);

                if (!IsSupportedPotion(item) || item.buffTime <= 0)
                {
                    continue;
                }

                if (_basePotionDurationByBuffId.TryGetValue(item.buffType, out var current) && current >= item.buffTime)
                {
                    continue;
                }

                _basePotionDurationByBuffId[item.buffType] = item.buffTime;
            }

            _potionCatalogReady = _basePotionDurationByBuffId.Count > 0;
        }
        catch
        {
            _basePotionDurationByBuffId.Clear();
            _potionCatalogReady = false;
        }
    }

    private void ApplyConfiguredBuffs(TSPlayer tsPlayer, long scanTick)
    {
        var player = tsPlayer.TPlayer;
        if (player is null || !player.active)
        {
            return;
        }

        if (!_buffMemoryByPlayer.TryGetValue(tsPlayer.Index, out var memory))
        {
            memory = new PlayerBuffMemory();
            _buffMemoryByPlayer[tsPlayer.Index] = memory;
        }

        var summary = BuildInventorySummary(player);

        if (player.dead)
        {
            memory.WasDeadLastScan = true;
            return;
        }

        var restoringAfterDeath = memory.WasDeadLastScan;
        memory.WasDeadLastScan = false;

        var qualifiedInfinitePotionBuffs = _config.EnableAllPotions
            ? ApplyInfinitePotionBuffRules(tsPlayer, summary, memory)
            : [];

        if (_config.EnableAllFoods)
        {
            ApplyInfiniteFoodBuffRule(tsPlayer, summary.Food, memory);
        }

        ApplyTransientPotionRules(tsPlayer, qualifiedInfinitePotionBuffs, memory, restoringAfterDeath, scanTick);
    }

    private HashSet<int> ApplyInfinitePotionBuffRules(TSPlayer tsPlayer, InventorySummary summary, PlayerBuffMemory memory)
    {
        var player = tsPlayer.TPlayer;
        var qualifiedPotionBuffs = new HashSet<int>();

        foreach (var potion in summary.Potions.Values)
        {
            if (potion.Stack < potion.RequiredStack)
            {
                continue;
            }

            qualifiedPotionBuffs.Add(potion.BuffId);
            EnsureInfiniteBuff(tsPlayer, player, potion.BuffId, potion.DurationTicks);
            memory.ManagedInfinitePotionBuffs.Add(potion.BuffId);
            memory.TransientPotionBuffs.Remove(potion.BuffId);
        }

        foreach (var buffId in memory.ManagedInfinitePotionBuffs.ToArray())
        {
            if (qualifiedPotionBuffs.Contains(buffId))
            {
                continue;
            }

            RemoveBuffIfPresent(player, buffId);
            memory.ManagedInfinitePotionBuffs.Remove(buffId);
        }

        return qualifiedPotionBuffs;
    }

    private void ApplyInfiniteFoodBuffRule(TSPlayer tsPlayer, FoodState state, PlayerBuffMemory memory)
    {
        var player = tsPlayer.TPlayer;

        if (state.BuffId <= 0)
        {
            foreach (var buffId in memory.ManagedInfiniteFoodBuffs.ToArray())
            {
                RemoveBuffIfPresent(player, buffId);
                memory.ManagedInfiniteFoodBuffs.Remove(buffId);
            }

            return;
        }

        RemoveAllFoodBuffsExcept(player, state.BuffId);
        EnsureInfiniteBuff(tsPlayer, player, state.BuffId, state.DurationTicks);

        memory.ManagedInfiniteFoodBuffs.Clear();
        memory.ManagedInfiniteFoodBuffs.Add(state.BuffId);
    }

    private void ApplyTransientPotionRules(TSPlayer tsPlayer, HashSet<int> qualifiedInfinitePotionBuffs, PlayerBuffMemory memory, bool restoringAfterDeath, long scanTick)
    {
        var player = tsPlayer.TPlayer;

        if (restoringAfterDeath)
        {
            foreach (var pair in memory.TransientPotionBuffs.ToArray())
            {
                if (qualifiedInfinitePotionBuffs.Contains(pair.Key))
                {
                    memory.TransientPotionBuffs.Remove(pair.Key);
                    continue;
                }

                var state = pair.Value;
                state = state with { RestoreGraceScans = 3, LastSeenTick = scanTick };
                memory.TransientPotionBuffs[pair.Key] = state;

                if (state.RemainingTime <= 0)
                {
                    continue;
                }

                if (!TryGetCurrentBuffTime(player, pair.Key, out var currentTime) || currentTime < state.RemainingTime)
                {
                    tsPlayer.SetBuff(pair.Key, state.RemainingTime, true);
                }
            }
        }

        var seenTransientBuffs = new HashSet<int>();

        for (var i = 0; i < player.buffType.Length; i++)
        {
            var buffId = player.buffType[i];
            var currentTime = player.buffTime[i];
            if (buffId <= 0 || currentTime <= 0)
            {
                continue;
            }

            if (qualifiedInfinitePotionBuffs.Contains(buffId) || !_basePotionDurationByBuffId.TryGetValue(buffId, out var baseDuration))
            {
                continue;
            }

            seenTransientBuffs.Add(buffId);
            var boostedDuration = GetPotionDurationTicks(baseDuration);

            if (!memory.TransientPotionBuffs.TryGetValue(buffId, out var state))
            {
                var initialRemaining = Math.Max(currentTime, boostedDuration);
                if (!restoringAfterDeath && currentTime < boostedDuration)
                {
                    tsPlayer.SetBuff(buffId, boostedDuration, true);
                    initialRemaining = boostedDuration;
                }

                memory.TransientPotionBuffs[buffId] = new TransientPotionState(initialRemaining, boostedDuration, 0, scanTick);
                continue;
            }

            var elapsedTicks = (int)Math.Max(0, scanTick - state.LastSeenTick);
            var estimatedRemaining = Math.Max(0, state.RemainingTime - elapsedTicks);

            if (state.RestoreGraceScans > 0 && state.RemainingTime > currentTime)
            {
                tsPlayer.SetBuff(buffId, state.RemainingTime, true);
                currentTime = state.RemainingTime;
                estimatedRemaining = state.RemainingTime;
            }

            var bestKnownRemaining = Math.Max(currentTime, estimatedRemaining);
            memory.TransientPotionBuffs[buffId] = state with
            {
                RemainingTime = bestKnownRemaining,
                BoostedDuration = Math.Max(state.BoostedDuration, boostedDuration),
                RestoreGraceScans = 0,
                LastSeenTick = scanTick
            };
        }

        foreach (var buffId in memory.TransientPotionBuffs.Keys.ToArray())
        {
            if (qualifiedInfinitePotionBuffs.Contains(buffId))
            {
                memory.TransientPotionBuffs.Remove(buffId);
                continue;
            }

            if (seenTransientBuffs.Contains(buffId))
            {
                continue;
            }

            var state = memory.TransientPotionBuffs[buffId];
            if (state.RestoreGraceScans > 0)
            {
                memory.TransientPotionBuffs[buffId] = state with { RestoreGraceScans = state.RestoreGraceScans - 1, LastSeenTick = scanTick };
                continue;
            }

            memory.TransientPotionBuffs.Remove(buffId);
        }
    }

    private InventorySummary BuildInventorySummary(Player player)
    {
        var potions = new Dictionary<int, PotionState>();
        var foods = new Dictionary<int, FoodState>();
        var highestFoodStack = 0;

        foreach (var item in EnumerateSupportedStorages(player))
        {
            if (item is null || item.IsAir || item.stack <= 0 || item.buffType <= 0 || !item.consumable)
            {
                continue;
            }

            if (_config.EnableAllFoods && IsSupportedFood(item))
            {
                highestFoodStack = Math.Max(highestFoodStack, item.stack);

                if (!foods.TryGetValue(item.type, out var currentFood))
                {
                    foods[item.type] = new FoodState(item.type, item.buffType, item.stack, item.buffTime);
                }
                else
                {
                    foods[item.type] = currentFood with { Stack = currentFood.Stack + item.stack };
                }

                continue;
            }

            if (!_config.EnableAllPotions || !IsSupportedPotion(item))
            {
                continue;
            }

            if (!potions.TryGetValue(item.type, out var currentPotion))
            {
                potions[item.type] = new PotionState(item.type, item.buffType, item.stack, GetPotionDurationTicks(item.buffTime), _config.RequiredStack);
            }
            else
            {
                potions[item.type] = currentPotion with { Stack = currentPotion.Stack + item.stack };
            }
        }

        var bestFood = new FoodState(0, 0, 0, 0);
        foreach (var state in foods.Values)
        {
            if (state.Stack < _config.RequiredStack)
            {
                continue;
            }

            var compare = CompareFoodBuffPriority(state.BuffId, bestFood.BuffId);
            if (compare > 0 || (compare == 0 && state.Stack > bestFood.Stack))
            {
                bestFood = state;
            }
        }

        return new InventorySummary(potions, bestFood, highestFoodStack);
    }

    private int GetPotionDurationTicks(int baseDuration)
    {
        return Math.Max(2, baseDuration * _config.PotionDurationMultiplier);
    }

    private static IEnumerable<Item> EnumerateSupportedStorages(Player player)
    {
        foreach (var item in player.inventory)
        {
            yield return item;
        }

        foreach (var item in player.bank.item)
        {
            yield return item;
        }

        foreach (var item in player.bank2.item)
        {
            yield return item;
        }

        foreach (var item in player.bank3.item)
        {
            yield return item;
        }

        foreach (var item in player.bank4.item)
        {
            yield return item;
        }
    }

    private static bool TryGetCurrentBuffTime(Player player, int buffId, out int time)
    {
        for (var i = 0; i < player.buffType.Length; i++)
        {
            if (player.buffType[i] != buffId)
            {
                continue;
            }

            time = player.buffTime[i];
            return true;
        }

        time = 0;
        return false;
    }

    private static void EnsureInfiniteBuff(TSPlayer tsPlayer, Player player, int buffId, int refreshDuration)
    {
        if (TryGetCurrentBuffTime(player, buffId, out var currentTime) && currentTime > GetInfiniteRefreshThreshold(refreshDuration))
        {
            return;
        }

        tsPlayer.SetBuff(buffId, refreshDuration, true);
    }

    private static int GetInfiniteRefreshThreshold(int refreshDuration)
    {
        return Math.Max(120, refreshDuration / 6);
    }

    private static void RemoveBuffIfPresent(Player player, int buffId)
    {
        for (var i = 0; i < player.buffType.Length; i++)
        {
            if (player.buffType[i] != buffId)
            {
                continue;
            }

            player.DelBuff(i);
            i--;
        }
    }

    private static void RemoveAllFoodBuffsExcept(Player player, int keepBuffId)
    {
        foreach (var foodBuffId in FoodBuffIds)
        {
            if (foodBuffId == keepBuffId)
            {
                continue;
            }

            RemoveBuffIfPresent(player, foodBuffId);
        }
    }

    private static bool IsSupportedPotion(Item item)
    {
        return item.consumable && item.buffType > 0 && !IsSupportedFood(item);
    }

    private static bool IsSupportedFood(Item item)
    {
        return item.buffType is BuffID.WellFed or BuffID.WellFed2 or BuffID.WellFed3;
    }

    private static int CompareFoodBuffPriority(int left, int right)
    {
        return GetFoodPriority(left).CompareTo(GetFoodPriority(right));
    }

    private static int GetFoodPriority(int buffId)
    {
        return buffId switch
        {
            BuffID.WellFed3 => 3,
            BuffID.WellFed2 => 2,
            BuffID.WellFed => 1,
            _ => 0
        };
    }

    private sealed class PlayerBuffMemory
    {
        public bool WasDeadLastScan { get; set; }

        public Dictionary<int, TransientPotionState> TransientPotionBuffs { get; } = [];

        public HashSet<int> ManagedInfinitePotionBuffs { get; } = [];

        public HashSet<int> ManagedInfiniteFoodBuffs { get; } = [];
    }

    private readonly record struct TransientPotionState(int RemainingTime, int BoostedDuration, int RestoreGraceScans, long LastSeenTick);
    private readonly record struct PotionState(int ItemId, int BuffId, int Stack, int DurationTicks, int RequiredStack);
    private readonly record struct FoodState(int ItemId, int BuffId, int Stack, int DurationTicks);
    private readonly record struct InventorySummary(Dictionary<int, PotionState> Potions, FoodState Food, int HighestFoodStack);
}

