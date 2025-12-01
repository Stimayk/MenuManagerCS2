using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace MenuManager;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DatabaseHost")] public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabasePort")] public int DatabasePort { get; set; } = 3306;
    [JsonPropertyName("DatabaseUser")] public string DatabaseUser { get; set; } = "";
    [JsonPropertyName("DatabasePassword")] public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabaseName")] public string DatabaseName { get; set; } = "";
    [JsonPropertyName("DefaultMenu")] public string DefaultMenu { get; set; } = "ButtonMenu";
    [JsonPropertyName("MenuFlashFix")] public bool MenuFlashFix { get; set; } = false;
    [JsonPropertyName("TrimMenuOptions")] public bool TrimMenuOptions { get; set; } = true;
    [JsonPropertyName("TrimSelectedOption")] public int TrimSelectedOption { get; set; } = 24;
    [JsonPropertyName("TrimNonSelectedOption")] public int TrimNonSelectedOption { get; set; } = 28;
    [JsonPropertyName("MenuSettingsCommand")] public List<string> MenuSettingsCommand { get; set; } = ["menu", "menus"];
    [JsonPropertyName("MenuAddon")] public int MenuAddon { get; set; } = 0;
    [JsonPropertyName("MenuAddonUrl")] public string MenuAddonUrl { get; set; } = "pisex.online/module/buttons";
    [JsonPropertyName("MenuTime")] public float MenuTime { get; set; } = 120.0f;
    [JsonPropertyName("Pagination")] public bool Pagination { get; set; } = false;
    [JsonPropertyName("SoundVolume")] public float SoundVolume { get; set; } = 0.7f;
    [JsonPropertyName("SoundScroll")] public string SoundScroll { get; set; } = "";
    [JsonPropertyName("SoundClick")] public string SoundClick { get; set; } = "";
    [JsonPropertyName("SoundDisabled")] public string SoundDisabled { get; set; } = "";
    [JsonPropertyName("SoundBack")] public string SoundBack { get; set; } = "";
    [JsonPropertyName("SoundExit")] public string SoundExit { get; set; } = "";
    [JsonPropertyName("StopingUser")] public bool StopingUser { get; set; } = true;
    [JsonPropertyName("ButtonsConfig")] public ButtonsConfig ButtonsConfig { get; set; } = new();
    [JsonPropertyName("IgnoreErrors")] public bool IgnoreErrors { get; set; } = true;
    [JsonPropertyName("MenuLinesCount")] public int MenuLinesCount { get; set; } = 5;
    [JsonPropertyName("UseMetamodMenu")] public bool UseMetamodMenu { get; set; } = false;
    [JsonPropertyName("UseMetamodMenuReplace")] public bool UseMetamodMenuReplace { get; set; } = false;
}

public class MenuManagerCore : BasePlugin, IPluginConfig<PluginConfig>
{
    internal static DataBaseService? DataBaseService;
    internal static ConcurrentDictionary<ulong, PlayerSettings> PlayerSettingsCache = new();
    private readonly PluginCapability<IMenuApi?> _pluginCapability = new("menu:nfcore");

    private IMenuApi? _api;
    public CCSGameRules? GameRules;
    public override string ModuleName => "[FORK] MenuManager";
    public override string ModuleVersion => "v1.1.0";
    public override string ModuleAuthor => "E!N (base by Nick Fox)";
    public override string ModuleDescription => "";
    public required PluginConfig Config { get; set; }

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        if (Config.MenuLinesCount is > 5 or < 1)
        {
            Config.MenuLinesCount = 5;
            Logger.LogWarning("MenuLinesCount is incorrect. Setting it to default (= 5)");
        }

        Misc.SetDefaultMenu(Config.DefaultMenu);

        DataBaseService = new DataBaseService(config, ModuleDirectory);

        Task.Run(async () =>
        {
            await DataBaseService.TestAndCheckDataBaseTableAsync();
            PlayerSettingsCache = await DataBaseService.LoadAllMenuSettings();
            Logger.LogInformation("Database initialized and cache loaded.");
        });
    }

    public override void Load(bool hotReload)
    {
        _api = new CMenuApi(this);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => _api);

        Control.Init(this);
        RegisterListener<OnTick>(Control.OnPluginTick);
        RegisterListener<OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerDisconnect>((@event, _) =>
        {
            if (Config.UseMetamodMenu && @event.Userid != null)
                MenusMm.ClearCallbackInfo(@event.Userid.Slot);
            return HookResult.Continue;
        }, HookMode.Pre);
        Config.MenuSettingsCommand.ForEach(c =>
        {
            AddCommand($"css_{c}", "Menu settings", (player, _) => OnCommand(player));
        });
        if (hotReload) MenusMm.Init();
    }

    private void OnMapStart(string map)
    {
        if (Config.MenuFlashFix) GameRules = null;

        MenusMm.Init();
    }

    public void InitializeGameRules()
    {
        var gameRulesProxy =
            Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        GameRules = gameRulesProxy?.GameRules;
    }

    public override void Unload(bool hotReload)
    {
        Control.Clear();
    }

    private void OnCommand(CCSPlayerController? player)
    {
        if (player == null || _api == null) return;
        player.ExecuteClientCommandFromServer("snd_toolvolume 0.1");
        var menu = _api.GetMenu(Localizer["menumanager.settings_title"]);

        menu.AddMenuOption(Localizer["menumanager.select_type"], (p, _) => OpenMenuTypeSettings(p));

        var currentType = Misc.GetCurrentPlayerMenu(player);
        var isButtonBased = currentType is MenuType.ButtonMenu or MenuType.MetamodMenu;

        if (isButtonBased)
        {
            menu.AddMenuOption(Localizer["menumanager.nav_mode"], (p, _) => OpenNavigationSettings(p));

            menu.AddMenuOption(Localizer["menumanager.sound_settings"], (p, _) => OpenSoundSettings(p));
        }

        menu.Open(player);
    }

    private void OpenMenuTypeSettings(CCSPlayerController player)
    {
        if (_api == null) return;
        var menu = _api.GetMenu(Localizer["menumanager.select_type"], OnCommand);

        menu.AddMenuOption(Localizer["menumanager.console"], (p, _) => Misc.SelectPlayerMenu(p, MenuType.ConsoleMenu));
        menu.AddMenuOption(Localizer["menumanager.chat"], (p, _) => Misc.SelectPlayerMenu(p, MenuType.ChatMenu));
        menu.AddMenuOption(Localizer["menumanager.center"], (p, _) => Misc.SelectPlayerMenu(p, MenuType.CenterMenu));
        menu.AddMenuOption(Localizer["menumanager.control"], (p, _) => Misc.SelectPlayerMenu(p, MenuType.ButtonMenu));

        if (Config is { UseMetamodMenu: true, UseMetamodMenuReplace: false })
            menu.AddMenuOption(Localizer["menumanager.metamod"],
                (p, _) => Misc.SelectPlayerMenu(p, MenuType.MetamodMenu));

        menu.Open(player);
    }

    private void OpenNavigationSettings(CCSPlayerController player)
    {
        if (_api == null) return;

        var currentPagination = Misc.GetPlayerPagination(player);
        string statusText = currentPagination ? Localizer["menumanager.pagination"] : Localizer["menumanager.scroll"];

        var menu = _api.GetMenu($"{Localizer["menumanager.nav_mode"]}: {statusText}", OnCommand);

        menu.AddMenuOption($"{Localizer["menumanager.pagination"]}", (p, _) =>
        {
            Misc.SetPlayerPagination(p, true);
            OpenNavigationSettings(p);
        }, currentPagination);

        menu.AddMenuOption($"{Localizer["menumanager.scroll"]}", (p, _) =>
        {
            Misc.SetPlayerPagination(p, false);
            OpenNavigationSettings(p);
        }, !currentPagination);

        menu.Open(player);
    }

    private void OpenSoundSettings(CCSPlayerController player)
    {
        if (_api == null) return;
        var menu = _api.GetMenu(Localizer["menumanager.sound_settings"], OnCommand);

        var areSoundsEnabled = Misc.GetPlayerSoundsEnabled(player);
        string toggleText = areSoundsEnabled
            ? Localizer["menumanager.disable_sounds"]
            : Localizer["menumanager.enable_sounds"];

        menu.AddMenuOption(toggleText, (p, _) =>
        {
            Misc.SetPlayerSoundsEnabled(p, !areSoundsEnabled);
            OpenSoundSettings(p);
        });

        if (areSoundsEnabled)
            menu.AddMenuOption(Localizer["menumanager.volume_settings"], (p, _) => OpenVolumeSettings(p));

        menu.Open(player);
    }

    private void OpenVolumeSettings(CCSPlayerController player)
    {
        if (_api == null) return;
        var currentVol = Misc.GetPlayerVolume(player);
        var menu = _api.GetMenu($"{Localizer["menumanager.volume"]}: {currentVol:0.0}", OpenSoundSettings);

        menu.AddMenuOption(Localizer["menumanager.volume_up"], (p, _) =>
        {
            Misc.SetPlayerVolume(p, currentVol + 0.1f);
            OpenVolumeSettings(p);
        }, currentVol >= 0.9f);

        menu.AddMenuOption(Localizer["menumanager.volume_down"], (p, _) =>
        {
            Misc.SetPlayerVolume(p, currentVol - 0.1f);
            OpenVolumeSettings(p);
        }, currentVol <= 0.0f);

        menu.Open(player);
    }
}