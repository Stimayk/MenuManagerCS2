using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace MenuManager;

internal static class Control
{
    private static readonly List<PlayerInfo> Menus = [];
    private static MenuManagerCore? _hPlugin;

    public static void AddMenu(CCSPlayerController player, ButtonMenu inst)
    {
        var oldSelected = 0;
        var oldTitle = "";
        var oldOffset = 0;
        for (var i = 0; i < Menus.Count; i++)
            if (Menus[i].GetPlayer() == player)
            {
                oldSelected = Menus[i].Selected();
                oldTitle = Menus[i].Menu.Title;
                oldOffset = Menus[i].Offset();
                Menus.Remove(Menus[i]);
                i++;
            }

        var menu = new PlayerInfo(player, inst, oldSelected, oldOffset, oldTitle);
        Menus.Add(menu);
    }

    public static void AddMenuAll(ButtonMenu inst)
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players.OfType<CCSPlayerController?>().Where(player =>
                     player is
                     {
                         IsValid: true, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected
                     }))
            if (player != null)
                AddMenu(player, inst);
    }

    public static void Clear()
    {
        foreach (var player in Menus.Select(menu => menu.GetPlayer()))
            if (Misc.IsValidPlayer(player) &&
                _hPlugin is { Config.StopingUser: true } &&
                player.PawnIsAlive &&
                player.Pawn.Value != null &&
                player.Pawn.Value.MoveType == MoveType_t.MOVETYPE_NONE)
            {
                player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
            }

        Menus.RemoveAll(_ => true);
    }

    public static void OnPluginTick()
    {
        if (_hPlugin is { Config.MenuFlashFix: true })
        {
            if (_hPlugin.GameRules == null)
                _hPlugin.InitializeGameRules();
            else
                _hPlugin.GameRules.GameRestart = _hPlugin.GameRules.RestartRoundTime < Server.CurrentTime;
        }

        if (Menus.Count <= 0) return;
        for (var i = 0; i < Menus.Count; i++)
        {
            var menu = Menus[i];
            var player = menu.GetPlayer();
            if (!Misc.IsValidPlayer(player))
            {
                Menus.RemoveAt(i);
                i--;
                continue;
            }

            if (_hPlugin != null && Server.CurrentTime - menu.StartTime > _hPlugin.Config.MenuTime)
            {
                if (_hPlugin.Config.StopingUser &&
                    player.PawnIsAlive &&
                    player.Pawn.Value != null &&
                    player.Pawn.Value.MoveType == MoveType_t.MOVETYPE_NONE)
                {
                    player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                    Schema.SetSchemaValue(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                    Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
                }

                menu.Close(true);
                Menus.RemoveAt(i);
                i--;
                continue;
            }

            var buttons = player.Buttons;
            if (_hPlugin is { Config.StopingUser: true } && player.PawnIsAlive && player.Pawn.Value != null &&
                player.Pawn.Value.MoveType == MoveType_t.MOVETYPE_WALK)
            {
                player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;
                Schema.SetSchemaValue(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
            }

            if (!menu.IsEqualButtons(buttons.ToString()))
            {
                if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.UpButton))
                    menu.MoveUp();
                else if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.DownButton))
                    menu.MoveDown();
                else if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.LeftButton))
                    menu.MoveUp(GetPlugin()!.Config.MenuLinesCount);
                else if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.RightButton))
                    menu.MoveDown(GetPlugin()!.Config.MenuLinesCount);
                else if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.SelectButton))
                    menu.OnSelect();
                else if (_hPlugin != null && buttons.HasFlag(_hPlugin.Config.ButtonsConfig.BackButton))
                    menu.Menu.BackAction?.Invoke(player);

                if (_hPlugin != null && (buttons.HasFlag(_hPlugin.Config.ButtonsConfig.ExitButton) || menu.Closed()))
                {
                    menu.Close(true);
                    if (_hPlugin.Config.StopingUser && player.PawnIsAlive && player.Pawn.Value != null &&
                        player.Pawn.Value.MoveType == MoveType_t.MOVETYPE_NONE)
                    {
                        player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                        Schema.SetSchemaValue(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                        Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
                    }

                    Menus.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            menu.GetPlayer().PrintToCenterHtml(menu.GetText(), 1);
        }
    }

    public static void PlaySound(CCSPlayerController player, string sound)
    {
        if (string.IsNullOrEmpty(sound)) return;

        if (!Misc.GetPlayerSoundsEnabled(player)) return;

        if (sound.StartsWith("sounds/"))
        {
            player.ExecuteClientCommand("play " + sound);
        }
        else
        {
            if (_hPlugin == null) return;
            var vol = Misc.GetPlayerVolume(player);
            player.EmitSound(sound, player, vol);
        }
    }

    public static void CloseMenu(CCSPlayerController player)
    {
        CounterStrikeSharp.API.Modules.Menu.MenuManager.CloseActiveMenu(player);

        var menuInfo = Menus.FirstOrDefault(t => t.GetPlayer() == player);
        if (menuInfo != null)
        {
            menuInfo.Close();

            if (_hPlugin is { Config.StopingUser: true } &&
                player.PawnIsAlive &&
                player.Pawn.Value != null &&
                player.Pawn.Value.MoveType == MoveType_t.MOVETYPE_NONE)
            {
                player.Pawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(player.Pawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_MoveType");
            }
        }

        if (menuInfo != null) Menus.Remove(menuInfo);

        MenusMm.ClosePlayerMenu(player.Slot);
    }

    internal static bool HasOpenedMenu(CCSPlayerController player, PlayerInfo? info = null)
    {
        if (Menus.Any(menu => menu.GetPlayer() == player && !menu.Closed() && menu != info)) return true;

        return info == null && MenusMm.IsMenuOpen(player.Slot);
    }

    internal static void Init(MenuManagerCore hPlugin)
    {
        _hPlugin = hPlugin;
    }

    internal static MenuManagerCore? GetPlugin()
    {
        return _hPlugin;
    }
}