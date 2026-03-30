using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace NpcManagementTweaks;

[ApiVersion(2, 1)]
public sealed class NpcManagementTweaksPlugin : TerrariaPlugin
{
    private const string AdminPermission = "npcmanagementtweaks.admin";
    private static readonly Version PluginVersion = new(0, 2, 4);
    private static readonly MethodInfo? TownNpcTeleportHomeMethod = typeof(NPC).GetMethod(
        "AI_007_TownEntities_TeleportToHome",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null,
        types: [typeof(int), typeof(int)],
        modifiers: null);

    private readonly Dictionary<int, int> _anglerTurnInsToday = [];
    private readonly HashSet<int> _playersWithFinishedFlag = [];
    private readonly Dictionary<int, TownNpcTeleportState> _townNpcTeleportStates = [];

    private NpcManagementTweaksConfig _config = new();
    private string _configPath = string.Empty;
    private int _anglerTicks;
    private int _townNpcTicks;
    private int _daySequence;
    private bool _dailyResetHandled;
    private bool _travelingMerchantHandledForDay;
    private bool _loggedMissingTownNpcTeleportMethod;

    public NpcManagementTweaksPlugin(Main game)
        : base(game)
    {
    }

    public override string Author => "鱼仔仔面";

    public override string Description => "NPC-focused tweaks for town NPC housing, traveling merchant arrival, and angler turn-in limits.";

    public override string Name => "NpcManagementTweaks";

    public override Version Version => PluginVersion;

    public override void Initialize()
    {
        _configPath = Path.Combine(TShock.SavePath, "NpcManagementTweaks.json");
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
        Commands.ChatCommands.Add(new Command(AdminPermission, HandleCommand, "nmt", "npcmanagementtweaks")
        {
            HelpText = "Usage: /nmt reload | /nmt status"
        });
    }

    private void OnReload(ReloadEventArgs args)
    {
        ReloadConfig();
        args.Player?.SendSuccessMessage("[NpcManagementTweaks] Config reloaded.");
    }

    private void OnServerLeave(LeaveEventArgs args)
    {
        _anglerTurnInsToday.Remove(args.Who);
        _playersWithFinishedFlag.Remove(args.Who);
    }

    private void OnGameUpdate(EventArgs args)
    {
        HandleDailyReset();
        HandleTownNpcTweaks();
        HandleTravelingMerchant();
        HandleAnglerTurnInLimit();
    }

    private void HandleDailyReset()
    {
        if (Main.dayTime && Main.time < 60.0)
        {
            if (_dailyResetHandled)
            {
                return;
            }

            _dailyResetHandled = true;
            _daySequence++;
            _travelingMerchantHandledForDay = false;
            _anglerTurnInsToday.Clear();
            _playersWithFinishedFlag.Clear();
            Log($"daily reset townEnabled={_config.TownNpcs.Enabled} merchantEnabled={_config.TravelingMerchant.Enabled} anglerEnabled={_config.Angler.Enabled}");
            return;
        }

        if (!Main.dayTime)
        {
            _dailyResetHandled = false;
            _travelingMerchantHandledForDay = false;
        }
    }

    private void HandleTownNpcTweaks()
    {
        if (!_config.TownNpcs.Enabled || !_config.TownNpcs.AllowDaytimeGoHome || !Main.dayTime)
        {
            return;
        }

        if (_townNpcTicks++ < _config.TownNpcs.ScanIntervalTicks)
        {
            return;
        }

        _townNpcTicks = 0;

        for (var i = 0; i < Main.npc.Length; i++)
        {
            var npc = Main.npc[i];
            if (npc is null || !npc.active || !npc.townNPC || npc.type == NPCID.TravellingMerchant || npc.homeless)
            {
                continue;
            }

            if (npc.homeTileX <= 0 || npc.homeTileY <= 0)
            {
                continue;
            }

            if (!ShouldTeleportTownNpcHome(npc))
            {
                continue;
            }

            TryTeleportTownNpcHome(npc);
        }
    }

    private bool ShouldTeleportTownNpcHome(NPC npc)
    {
        if (!_config.TownNpcs.TeleportDirectlyHome)
        {
            return false;
        }

        var homePosition = GetTownNpcHomePosition(npc);
        var distanceTiles = Vector2.Distance(npc.position, homePosition) / 16f;
        if (distanceTiles <= _config.TownNpcs.TeleportDistanceTiles)
        {
            return false;
        }

        return !_townNpcTeleportStates.TryGetValue(npc.whoAmI, out var state)
            || state.DaySequence != _daySequence
            || state.HomeTileX != npc.homeTileX
            || state.HomeTileY != npc.homeTileY;
    }

    private void TryTeleportTownNpcHome(NPC npc)
    {
        if (TownNpcTeleportHomeMethod is null)
        {
            if (!_loggedMissingTownNpcTeleportMethod)
            {
                _loggedMissingTownNpcTeleportMethod = true;
                Log("town NPC teleport-home method was not found; daytime go-home teleport is unavailable on this server build");
            }

            return;
        }

        try
        {
            TownNpcTeleportHomeMethod.Invoke(npc, [npc.homeTileX, npc.homeTileY]);
            _townNpcTeleportStates[npc.whoAmI] = new TownNpcTeleportState(_daySequence, npc.homeTileX, npc.homeTileY);
            npc.netUpdate = true;
            NetMessage.SendData((int)MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
        }
        catch (TargetInvocationException ex)
        {
            Log($"failed to teleport town NPC {npc.FullName} ({npc.whoAmI}) home: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private void HandleTravelingMerchant()
    {
        if (!_config.TravelingMerchant.Enabled || !Main.dayTime || Main.time >= 60.0 || _travelingMerchantHandledForDay)
        {
            return;
        }

        _travelingMerchantHandledForDay = true;

        if (IsTravelingMerchantActive())
        {
            Log("traveling merchant already active for today");
            return;
        }

        var numerator = _config.TravelingMerchant.ArrivalChanceNumerator;
        var denominator = _config.TravelingMerchant.ArrivalChanceDenominator;
        if (numerator <= 0 || denominator <= 0)
        {
            Log("traveling merchant chance disabled by config");
            return;
        }

        var roll = Main.rand.Next(denominator);
        if (roll >= numerator)
        {
            Log($"traveling merchant skipped today roll={roll} chance={numerator}/{denominator}");
            return;
        }

        WorldGen.SpawnTravelNPC();
        Log($"traveling merchant spawned roll={roll} chance={numerator}/{denominator}");
    }

    private void HandleAnglerTurnInLimit()
    {
        if (!_config.Angler.Enabled)
        {
            return;
        }

        if (_anglerTicks++ < _config.Angler.ScanIntervalTicks)
        {
            return;
        }

        _anglerTicks = 0;

        foreach (var tsPlayer in TShock.Players)
        {
            if (tsPlayer?.Active != true)
            {
                continue;
            }

            var finishedToday = Main.anglerWhoFinishedToday.Contains(tsPlayer.Name);
            if (!finishedToday)
            {
                _playersWithFinishedFlag.Remove(tsPlayer.Index);
                continue;
            }

            if (!_playersWithFinishedFlag.Add(tsPlayer.Index))
            {
                continue;
            }

            var count = _anglerTurnInsToday.GetValueOrDefault(tsPlayer.Index) + 1;
            _anglerTurnInsToday[tsPlayer.Index] = count;

            if (count < _config.Angler.DailyTurnInLimit)
            {
                Main.anglerWhoFinishedToday.Remove(tsPlayer.Name);
                tsPlayer.SendData(PacketTypes.AnglerQuest, "");
                _playersWithFinishedFlag.Remove(tsPlayer.Index);
                tsPlayer.SendInfoMessage($"[NpcManagementTweaks] Today's angler turn-ins: {count}/{_config.Angler.DailyTurnInLimit}. You can submit again.");
                Log($"player={tsPlayer.Name} angler turn-in {count}/{_config.Angler.DailyTurnInLimit}, cleared finished-today state");
            }
            else
            {
                tsPlayer.SendInfoMessage($"[NpcManagementTweaks] Today's angler turn-ins: {count}/{_config.Angler.DailyTurnInLimit}. Today's quota is complete.");
                Log($"player={tsPlayer.Name} angler turn-in {count}/{_config.Angler.DailyTurnInLimit}, limit reached");
            }
        }
    }

    private void HandleCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("Usage: /nmt reload | /nmt status");
            return;
        }

        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "reload":
                ReloadConfig();
                args.Player.SendSuccessMessage("[NpcManagementTweaks] Config reloaded.");
                break;

            case "status":
                args.Player.SendInfoMessage($"[NpcManagementTweaks] TownNpcs={_config.TownNpcs.Enabled}, Merchant={_config.TravelingMerchant.Enabled} ({_config.TravelingMerchant.ArrivalChanceNumerator}/{_config.TravelingMerchant.ArrivalChanceDenominator}), Angler={_config.Angler.Enabled}, DailyTurnInLimit={_config.Angler.DailyTurnInLimit}");
                break;

            default:
                args.Player.SendErrorMessage("Unknown subcommand. Usage: /nmt reload | /nmt status");
                break;
        }
    }

    private void ReloadConfig()
    {
        _config = NpcManagementTweaksConfig.Load(_configPath);
        _anglerTicks = 0;
        _townNpcTicks = 0;
        _anglerTurnInsToday.Clear();
        _playersWithFinishedFlag.Clear();
        _townNpcTeleportStates.Clear();
        _loggedMissingTownNpcTeleportMethod = false;
        Log($"config reloaded townEnabled={_config.TownNpcs.Enabled} merchantEnabled={_config.TravelingMerchant.Enabled} merchantChance={_config.TravelingMerchant.ArrivalChanceNumerator}/{_config.TravelingMerchant.ArrivalChanceDenominator} anglerEnabled={_config.Angler.Enabled} dailyTurnInLimit={_config.Angler.DailyTurnInLimit}");
    }

    private static bool IsTravelingMerchantActive()
    {
        for (var i = 0; i < Main.npc.Length; i++)
        {
            var npc = Main.npc[i];
            if (npc is not null && npc.active && npc.type == NPCID.TravellingMerchant)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 GetTownNpcHomePosition(NPC npc)
    {
        var x = (npc.homeTileX * 16f) + 8f - (npc.width / 2f);
        var y = (npc.homeTileY * 16f) - npc.height;
        return new Vector2(x, y);
    }

    private static void Log(string message)
    {
        TShock.Log.ConsoleInfo($"[NpcManagementTweaks] {message}");
    }

    private readonly record struct TownNpcTeleportState(int DaySequence, int HomeTileX, int HomeTileY);
}
