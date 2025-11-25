using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace MenuManager;

internal class CMenuApi : IMenuApi
{
    private MenuManagerCore _plugin;

    //public CApi(BasePlugin _plugin)
    public CMenuApi(MenuManagerCore plugin)
    {
        _plugin = plugin;
    }

    public IMenu GetMenu(string title, Action<CCSPlayerController> backAction = null,
        Action<CCSPlayerController> resetAction = null)
    {
        return new MenuInstance(title, backAction, resetAction);
    }

    public IMenu NewMenu(string title, Action<CCSPlayerController> backAction = null)
    {
        return new MenuInstance(title, backAction);
    }

    public IMenu GetMenuForcetype(string title, MenuType type, Action<CCSPlayerController> backAction = null,
        Action<CCSPlayerController> resetAction = null)
    {
        return new MenuInstance(title, backAction, resetAction, type);
    }

    public IMenu NewMenuForcetype(string title, MenuType type, Action<CCSPlayerController> backAction = null)
    {
        return new MenuInstance(title, backAction, null, type);
    }

    public void CloseMenu(CCSPlayerController player)
    {
        Control.CloseMenu(player);
    }

    public MenuType GetMenuType(CCSPlayerController player)
    {
        return Misc.GetCurrentPlayerMenu(player);
    }

    public bool HasOpenedMenu(CCSPlayerController player)
    {
        return Control.HasOpenedMenu(player);
    }
}