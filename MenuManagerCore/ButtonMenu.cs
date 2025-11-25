using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace MenuManager;

public class ButtonMenu : IMenu
{
    public Action<CCSPlayerController>? BackAction = null;
    public bool Metamod = false;
    public Action<CCSPlayerController>? ResetAction = null;

    public ButtonMenu(string title, bool metamod = false)
    {
        MenuOptions = [];
        Title = title;
        Metamod = metamod;
    }

    public string Title { get; set; }

    public List<ChatMenuOption> MenuOptions { get; }

    public bool ExitButton { get; set; }

    public PostSelectAction PostSelectAction { get; set; } = PostSelectAction.Nothing;

    public ChatMenuOption AddMenuOption(string display, Action<CCSPlayerController, ChatMenuOption> onSelect,
        bool disabled = false)
    {
        var option = new ChatMenuOption(display, disabled, onSelect);
        MenuOptions.Add(option);
        return option;
    }

    public void Open(CCSPlayerController player)
    {
        Control.AddMenu(player, this);
    }

    public void OpenToAll()
    {
        Control.AddMenuAll(this);
    }
}