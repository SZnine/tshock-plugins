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
    private static readonly Version PluginVersion = new(0, 2, 1);

    private readonly Dictionary<int, int> _anglerTurnInsToday = [];
    private readonly HashSet<int> _playersWithFinishedFlag = [];

    private NpcManagementTweaksConfig _config = new();
    private string _configPath = string.Empty;
    private int _anglerTicks;
    private bool _dailyResetHandled;

    public NpcManagementTweaksPlugin(Main game)
        : base(game)
    {
    }

    public override string Author => "鱼仔仔面";

    public override string Description => "NPC-focused tweaks for town NPC housing, traveling merchant arrival/shop size, and angler turn-in limits.";

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
            _anglerTurnInsToday.Clear();
            _playersWithFinishedFlag.Clear();
            Log("daily angler counters reset");
            return;
        }

        if (!Main.dayTime)
        {
            _dailyResetHandled = false;
        }
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
                args.Player.SendInfoMessage($"[NpcManagementTweaks] AnglerEnabled={_config.Angler.Enabled}, ScanIntervalTicks={_config.Angler.ScanIntervalTicks}, DailyTurnInLimit={_config.Angler.DailyTurnInLimit}, ActiveCounters={_anglerTurnInsToday.Count}");
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
        _anglerTurnInsToday.Clear();
        _playersWithFinishedFlag.Clear();
        Log($"config reloaded anglerEnabled={_config.Angler.Enabled} scanTicks={_config.Angler.ScanIntervalTicks} dailyTurnInLimit={_config.Angler.DailyTurnInLimit}");
    }

    private static void Log(string message)
    {
        TShock.Log.ConsoleInfo($"[NpcManagementTweaks:AnglerLimit] {message}");
    }
}



