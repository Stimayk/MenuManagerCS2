using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace MenuManager;

internal static class Misc
{
    private static string _defaultMenu = "ButtonMenu";
    private static MenuType _defaultMenuType = MenuType.ButtonMenu;

    public static List<CCSPlayerController> GetValidPlayers()
    {
        var players = new List<CCSPlayerController>();
        foreach (var player in Utilities.GetPlayers())
            if (player is
                { IsValid: true, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected })
                players.Add(player);
        return players;
    }

    public static void SetDefaultMenu(string defaultMenuStr)
    {
        var menuTypes = new List<string>(["ButtonMenu", "CenterMenu", "ConsoleMenu", "ChatMenu", "MetamodMenu"]);
        if (menuTypes.Contains(defaultMenuStr))
        {
            _defaultMenu = defaultMenuStr;
            if (Enum.TryParse(defaultMenuStr, out MenuType result)) _defaultMenuType = result;
        }
        else
        {
            Control.GetPlugin()
                ?.Logger
                .LogInformation(
                    "Invalid menu type: {DefaultMenuStr}. Using default menu {DefaultMenu}", defaultMenuStr,
                    _defaultMenu);
        }
    }

    private static PlayerSettings GetOrAddSettings(ulong steamId)
    {
        if (MenuManagerCore.PlayerSettingsCache.TryGetValue(steamId, out var value)) return value;
        value = new PlayerSettings();
        MenuManagerCore.PlayerSettingsCache[steamId] = value;
        return value;
    }

    public static MenuType GetCurrentPlayerMenu(CCSPlayerController player)
    {
        if (!IsValidPlayer(player)) return _defaultMenuType;
        var steamId = player.AuthorizedSteamID?.SteamId64;
        if (steamId == null) return _defaultMenuType;

        if (MenuManagerCore.PlayerSettingsCache.TryGetValue(steamId.Value, out var settings))
            return settings.MenuType == MenuType.ButtonMenu ? _defaultMenuType : settings.MenuType;
        return _defaultMenuType;
    }

    public static bool GetPlayerPagination(CCSPlayerController player)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null)
            return Control.GetPlugin()!.Config.Pagination;

        if (MenuManagerCore.PlayerSettingsCache.TryGetValue(player.AuthorizedSteamID.SteamId64, out var settings))
            return settings.UsePagination ?? Control.GetPlugin()!.Config.Pagination;
        return Control.GetPlugin()!.Config.Pagination;
    }

    public static bool GetPlayerSoundsEnabled(CCSPlayerController player)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return true;

        if (MenuManagerCore.PlayerSettingsCache.TryGetValue(player.AuthorizedSteamID.SteamId64, out var settings))
            return settings.SoundsEnabled ?? true;
        return true;
    }

    public static float GetPlayerVolume(CCSPlayerController player)
    {
        var configVol = Control.GetPlugin()!.Config.SoundVolume;
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return configVol;

        if (MenuManagerCore.PlayerSettingsCache.TryGetValue(player.AuthorizedSteamID.SteamId64, out var settings))
            return settings.Volume ?? configVol;
        return configVol;
    }

    public static void SelectPlayerMenu(CCSPlayerController player, MenuType type)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return;
        var steamId = player.AuthorizedSteamID.SteamId64;

        var settings = GetOrAddSettings(steamId);
        settings.MenuType = type;

        MenuManagerCore.PlayerSettingsCache[steamId] = settings;

        player.PrintToChat($"{Control.GetPlugin()?.Localizer["menumanager.selected_type"]} {GetMenuTypeName(type)}");
        SaveSettingsAsync(steamId, settings);
        CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);
    }

    public static void SetPlayerPagination(CCSPlayerController player, bool pagination)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return;
        var steamId = player.AuthorizedSteamID.SteamId64;

        var settings = GetOrAddSettings(steamId);
        settings.UsePagination = pagination;
        MenuManagerCore.PlayerSettingsCache[steamId] = settings;

        SaveSettingsAsync(steamId, settings);
    }

    public static void SetPlayerSoundsEnabled(CCSPlayerController player, bool enabled)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return;
        var steamId = player.AuthorizedSteamID.SteamId64;

        var settings = GetOrAddSettings(steamId);
        settings.SoundsEnabled = enabled;
        MenuManagerCore.PlayerSettingsCache[steamId] = settings;

        SaveSettingsAsync(steamId, settings);
    }

    public static void SetPlayerVolume(CCSPlayerController player, float volume)
    {
        if (!IsValidPlayer(player) || player.AuthorizedSteamID == null) return;
        var steamId = player.AuthorizedSteamID.SteamId64;

        volume = Math.Clamp(volume, 0.0f, 0.9f);

        var settings = GetOrAddSettings(steamId);
        settings.Volume = volume;
        MenuManagerCore.PlayerSettingsCache[steamId] = settings;

        SaveSettingsAsync(steamId, settings);
    }

    private static void SaveSettingsAsync(ulong steamId, PlayerSettings settings)
    {
        Task.Run(async () =>
        {
            if (MenuManagerCore.DataBaseService != null)
                await MenuManagerCore.DataBaseService.SaveMenuSetting(steamId, settings);
        });
    }

    private static string GetMenuTypeName(MenuType type)
    {
        return type switch
        {
            MenuType.ChatMenu => Control.GetPlugin()?.Localizer["menumanager.chat"] ??
                                 throw new InvalidOperationException(),
            MenuType.ConsoleMenu => Control.GetPlugin()?.Localizer["menumanager.console"] ??
                                    throw new InvalidOperationException(),
            MenuType.CenterMenu => Control.GetPlugin()?.Localizer["menumanager.center"] ??
                                   throw new InvalidOperationException(),
            MenuType.ButtonMenu => Control.GetPlugin()?.Localizer["menumanager.control"] ??
                                   throw new InvalidOperationException(),
            MenuType.MetamodMenu => Control.GetPlugin()?.Localizer["menumanager.metamod"] ??
                                    throw new InvalidOperationException(),
            _ => "Undefined"
        };
    }

    public static bool IsValidPlayer(CCSPlayerController player)
    {
        return player is
            { IsValid: true, Connected: PlayerConnectedState.PlayerConnected, IsBot: false };
    }

    internal static string ColorText(string text, bool needColors = true)
    {
        var newText = text;
        var colors = new List<string>([
            "Default", "White", "Darkred", "Green", "Lightyellow", "Lightblue", "Olive", "Lime", "Red", "Lightpurple",
            "Purple", "Grey", "Yellow", "Gold", "Silver", "Blue", "Darkblue", "Bluegrey", "Magenta", "Lightred",
            "Orange"
        ]);

        if (needColors)
        {
            foreach (var color0 in colors)
            {
                var color = "[color:" + color0 + "]";
                var colorOld = "{" + color0 + "}";
                var rep = $"<font color='{color0.ToLower()}'>";
                newText = newText.Replace(color, rep, StringComparison.CurrentCultureIgnoreCase);
                newText = newText.Replace(colorOld, rep, StringComparison.CurrentCultureIgnoreCase);
            }
        }
        else
        {
            foreach (var color0 in colors)
            {
                var color = "[color:" + color0 + "]";
                var colorOld = "{" + color0 + "}";
                newText = newText.Replace(color, "", StringComparison.CurrentCultureIgnoreCase);
                newText = newText.Replace(colorOld, "", StringComparison.CurrentCultureIgnoreCase);
            }

            newText = newText.Replace("</font>", "");
        }

        return newText;
    }
}