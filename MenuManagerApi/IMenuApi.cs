using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace MenuManager;

public interface IMenuApi
{
    public IMenu GetMenu(string title, Action<CCSPlayerController> backAction = null,
        Action<CCSPlayerController> resetAction = null);

    // Deprecated, only for backward compatibility
    [Obsolete("Method used only for backward compatibility, not for develope.", true)]
    public IMenu NewMenu(string title, Action<CCSPlayerController> backAction = null);
    //

    public IMenu GetMenuForcetype(string title, MenuType type, Action<CCSPlayerController> backAction = null,
        Action<CCSPlayerController> resetAction = null);

    // Deprecated, only for backward compatibility
    [Obsolete("Method used only for backward compatibility, not for develope.", true)]
    public IMenu NewMenuForcetype(string title, MenuType type, Action<CCSPlayerController> backAction = null);
    // 

    public void CloseMenu(CCSPlayerController player);
    public MenuType GetMenuType(CCSPlayerController player);
    public bool HasOpenedMenu(CCSPlayerController player);
}

public enum MenuType
{
    Default = -1,
    ChatMenu = 0,
    ConsoleMenu = 1,
    CenterMenu = 2,
    ButtonMenu = 3,
    MetamodMenu = 4
}