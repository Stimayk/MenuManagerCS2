using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

namespace MenuManager;

internal class PlayerInfo
{
    private readonly CCSPlayerController _player;

    public readonly ButtonMenu Menu;
    private bool _aPressed = false;
    private bool _closed;
    private bool _dPressed = false;
    private bool _ePressed = false;
    private int _offset;
    private string _prevButtons;
    private int _selected;
    private bool _sPressed = false;

    private bool _wPressed = false;

    public float StartTime;

    public PlayerInfo(CCSPlayerController player, ButtonMenu menu, int newSelected = 0,
        int newOffset = 0, string oldTitle = "")
    {
        _player = player;
        Menu = menu;

        StartTime = Server.CurrentTime;

        _closed = false;
        _offset = 0;
        if (menu.Title == oldTitle)
        {
            _selected = menu.MenuOptions.Count > newSelected ? newSelected : Math.Max(menu.MenuOptions.Count - 1, 0);
            _offset = newOffset;
        }
        else
        {
            _selected = 0;
        }

        _prevButtons = _player.Buttons.ToString();

        var usePagination = Misc.GetPlayerPagination(_player);

        if (!usePagination) return;

        var menuLinesCount = Control.GetPlugin()!.Config.MenuLinesCount;
        var pageIndex = _selected / menuLinesCount;
        _offset = pageIndex * menuLinesCount;
    }

    public int Offset()
    {
        return _offset;
    }

    public CCSPlayerController GetPlayer()
    {
        return _player;
    }

    public string GetText()
    {
        var config = Control.GetPlugin()?.Config;
        if (config == null) throw new InvalidOperationException();
        var usePagination = Misc.GetPlayerPagination(_player);
        var menuLinesCount = config.MenuLinesCount;

        string text = Control.GetPlugin()?.Localizer["menumanager.title", Menu.Title] ??
                      throw new InvalidOperationException();

        if (Menu.MenuOptions.Count > 0)
            for (var i = _offset;
                 i < Math.Min(_offset + menuLinesCount, Menu.MenuOptions.Count);
                 i++)
            {
                var line = Menu.MenuOptions[i].Text;

                if (config.TrimMenuOptions)
                {
                    if (i == _selected)
                    {
                        if (line.Length > config.TrimSelectedOption)
                            line = line[..config.TrimSelectedOption];
                    }
                    else
                    {
                        if (line.Length > config.TrimNonSelectedOption)
                            line = line[..config.TrimNonSelectedOption];
                    }
                }

                if (Menu.MenuOptions[i].Disabled)
                    line = Control.GetPlugin()?.Localizer["menumanager.disabled_item"].Value
                        .Replace("[MENUITEM]", line);

                if (i == _selected)
                {
                    line = Control.GetPlugin()?.Localizer["menumanager.selected_item"].Value
                        .Replace("[MENUITEM]", line);

                    var eBtnKey = config.MenuAddon == 1
                        ? $"menumanager.html_{(_ePressed ? "e_p" : "e")}_button"
                        : $"menumanager.html_{(_ePressed ? "e_p" : "e")}_button_web";

                    if (config.MenuAddon == 1)
                        line += Control.GetPlugin()?.Localizer[eBtnKey] ?? throw new InvalidOperationException();
                    else if (config.MenuAddonUrl != string.Empty)
                        line += Control.GetPlugin()?.Localizer[eBtnKey, config.MenuAddonUrl] ??
                                throw new InvalidOperationException();
                }

                text = text + "<br>" + line;
            }
        else
            text = $"{text}<br>{Control.GetPlugin()?.Localizer["menumanager.empty"]}";

        text += "<br>";

        var isFirstItemOnPage = usePagination && _selected == _offset;

        if (_selected > 0 && !isFirstItemOnPage)
        {
            if (config.MenuAddon == 1)
                text += Control.GetPlugin()?.Localizer[$"menumanager.html_{(_wPressed ? "w_p" : "w")}_button"] ??
                        throw new InvalidOperationException();
            else if (config.MenuAddonUrl != string.Empty)
                text += Control.GetPlugin()
                    ?.Localizer[$"menumanager.html_{(_wPressed ? "w_p" : "w")}_button_web",
                        config.MenuAddonUrl] ?? throw new InvalidOperationException();
        }
        else
        {
            if (config.MenuAddon == 1)
                text += Control.GetPlugin()?.Localizer["menumanager.html_space_short_button"] ??
                        throw new InvalidOperationException();
            else if (config.MenuAddonUrl != string.Empty)
                text += Control.GetPlugin()
                            ?.Localizer["menumanager.html_space_short_button_web", config.MenuAddonUrl] ??
                        throw new InvalidOperationException();
        }

        var isLastItemOnPage = usePagination && _selected - _offset >= menuLinesCount - 1;

        if (_selected < Menu.MenuOptions.Count - 1 && !isLastItemOnPage)
        {
            if (config.MenuAddon == 1)
                text += Control.GetPlugin()?.Localizer[$"menumanager.html_{(_sPressed ? "s_p" : "s")}_button"] ??
                        throw new InvalidOperationException();
            else if (config.MenuAddonUrl != string.Empty)
                text += Control.GetPlugin()
                    ?.Localizer[$"menumanager.html_{(_sPressed ? "s_p" : "s")}_button_web",
                        config.MenuAddonUrl] ?? throw new InvalidOperationException();
        }
        else
        {
            if (config.MenuAddon == 1)
                text += Control.GetPlugin()?.Localizer["menumanager.html_space_short_button"] ??
                        throw new InvalidOperationException();
            else if (config.MenuAddonUrl != string.Empty)
                text += Control.GetPlugin()
                            ?.Localizer["menumanager.html_space_short_button_web", config.MenuAddonUrl] ??
                        throw new InvalidOperationException();
        }

        var hasMultiplePages = Menu.MenuOptions.Count > menuLinesCount;

        if (hasMultiplePages)
        {
            var isFirstPage = _offset == 0;

            if (isFirstPage)
            {
                if (config.MenuAddon == 1)
                    text += Control.GetPlugin()?.Localizer["menumanager.html_space_short_button"] ??
                            throw new InvalidOperationException();
                else if (config.MenuAddonUrl != string.Empty)
                    text += Control.GetPlugin()
                                ?.Localizer["menumanager.html_space_short_button_web", config.MenuAddonUrl] ??
                            throw new InvalidOperationException();
            }
            else
            {
                if (config.MenuAddon == 1)
                    text += Control.GetPlugin()?.Localizer[$"menumanager.html_{(_aPressed ? "a_p" : "a")}_button"] ??
                            throw new InvalidOperationException();
                else if (config.MenuAddonUrl != string.Empty)
                    text += Control.GetPlugin()
                        ?.Localizer[$"menumanager.html_{(_aPressed ? "a_p" : "a")}_button_web",
                            config.MenuAddonUrl] ?? throw new InvalidOperationException();
            }

            var isLastPage = _offset + menuLinesCount >= Menu.MenuOptions.Count;

            if (isLastPage)
            {
                if (config.MenuAddon == 1)
                    text += Control.GetPlugin()?.Localizer["menumanager.html_space_short_button"] ??
                            throw new InvalidOperationException();
                else if (config.MenuAddonUrl != string.Empty)
                    text += Control.GetPlugin()
                                ?.Localizer["menumanager.html_space_short_button_web", config.MenuAddonUrl] ??
                            throw new InvalidOperationException();
            }
            else
            {
                if (config.MenuAddon == 1)
                    text += Control.GetPlugin()?.Localizer[$"menumanager.html_{(_dPressed ? "d_p" : "d")}_button"] ??
                            throw new InvalidOperationException();
                else if (config.MenuAddonUrl != string.Empty)
                    text += Control.GetPlugin()
                        ?.Localizer[$"menumanager.html_{(_dPressed ? "d_p" : "d")}_button_web",
                            config.MenuAddonUrl] ?? throw new InvalidOperationException();
            }
        }
        else
        {
            if (Menu.ExitButton)
            {
                if (config.MenuAddon == 1)
                    text += Control.GetPlugin()?.Localizer["menumanager.html_space_button"] ??
                            throw new InvalidOperationException();
                else if (config.MenuAddonUrl != string.Empty)
                    text += Control.GetPlugin()?.Localizer["menumanager.html_space_button_web", config.MenuAddonUrl] ??
                            throw new InvalidOperationException();
            }
        }

        if (!Menu.ExitButton) return text;
        if (config.MenuAddon == 1)
            text += Control.GetPlugin()?.Localizer["menumanager.html_f_button"] ??
                    throw new InvalidOperationException();
        else if (config.MenuAddonUrl != string.Empty)
            text += Control.GetPlugin()?.Localizer["menumanager.html_f_button_web", config.MenuAddonUrl] ??
                    throw new InvalidOperationException();

        return text;
    }

    public bool MoveDown(int lines = 1)
    {
        var menuLinesCount = Control.GetPlugin()!.Config.MenuLinesCount;
        var isPagination = Misc.GetPlayerPagination(_player);
        var canMove = true;

        if (lines > 1)
        {
            if (_offset + menuLinesCount >= Menu.MenuOptions.Count)
                canMove = false;
        }
        else
        {
            if (_selected >= Menu.MenuOptions.Count - 1)
                canMove = false;

            if (isPagination && _selected - _offset >= menuLinesCount - 1)
                canMove = false;
        }

        if (!canMove) return false;

        {
            StartTime = Server.CurrentTime;
            var configSoundScroll = Control.GetPlugin()?.Config.SoundScroll;
            if (configSoundScroll != null)
                Control.PlaySound(_player, configSoundScroll);

            if (_selected == Menu.MenuOptions.Count - 1) return false;

            if (isPagination && lines == 1)
                if (_selected + 1 >= _offset + Control.GetPlugin()!.Config.MenuLinesCount)
                    return false;

            var previousVisualIndex = _selected - _offset;

            _selected = Math.Min(_selected + lines, Menu.MenuOptions.Count - 1);

            if (isPagination)
            {
                var pageIndex = _selected / menuLinesCount;
                _offset = pageIndex * menuLinesCount;
            }
            else
            {
                if (lines > 1)
                {
                    _offset = Math.Max(0, _selected - previousVisualIndex);
                }
                else
                {
                    if (_selected - _offset > menuLinesCount - 1)
                        _offset = _selected - menuLinesCount + 1;
                }
            }

            return true;
        }
    }

    public bool MoveUp(int lines = 1)
    {
        var menuLinesCount = Control.GetPlugin()!.Config.MenuLinesCount;
        var isPagination = Misc.GetPlayerPagination(_player);

        var canMove = true;
        if (lines > 1)
        {
            if (_offset <= 0)
                canMove = false;
        }
        else
        {
            if (_selected <= 0)
                canMove = false;

            if (isPagination && _selected == _offset)
                canMove = false;
        }

        if (!canMove) return false;

        StartTime = Server.CurrentTime;
        var configSoundScroll = Control.GetPlugin()?.Config.SoundScroll;
        if (configSoundScroll != null)
            Control.PlaySound(_player, configSoundScroll);

        if (_selected < 1)
        {
            _selected = 0;
            return false;
        }

        if (isPagination && lines == 1)
            if (_selected - 1 < _offset)
                return false;

        var previousVisualIndex = _selected - _offset;

        _selected = Math.Max(_selected - lines, 0);

        if (isPagination)
        {
            var pageIndex = _selected / menuLinesCount;
            _offset = pageIndex * menuLinesCount;
        }
        else
        {
            if (lines > 1)
            {
                _offset = Math.Max(0, _selected - previousVisualIndex);
            }
            else
            {
                if (_selected < _offset)
                    _offset = _selected;
            }
        }

        return true;
    }

    public bool IsEqualButtons(string buttons)
    {
        var flag = _prevButtons.Equals(buttons);

        _wPressed = buttons.Contains(Control.GetPlugin()?.Config.ButtonsConfig.UpButton.ToString() ??
                                     throw new InvalidOperationException());
        _aPressed = buttons.Contains(Control.GetPlugin()?.Config.ButtonsConfig.LeftButton.ToString() ??
                                     throw new InvalidOperationException());
        _sPressed = buttons.Contains(Control.GetPlugin()?.Config.ButtonsConfig.DownButton.ToString() ??
                                     throw new InvalidOperationException());
        _dPressed = buttons.Contains(Control.GetPlugin()?.Config.ButtonsConfig.RightButton.ToString() ??
                                     throw new InvalidOperationException());
        _ePressed = buttons.Contains(Control.GetPlugin()?.Config.ButtonsConfig.SelectButton.ToString() ??
                                     throw new InvalidOperationException());

        _prevButtons = buttons;
        return flag;
    }

    public int Selected()
    {
        return _selected;
    }

    public void OnSelect()
    {
        if (_selected <= -1 || _selected >= Menu.MenuOptions.Count) return;
        if (Menu.MenuOptions[_selected].Disabled)
        {
            var configSoundDisabled = Control.GetPlugin()?.Config.SoundDisabled;
            if (configSoundDisabled != null)
                Control.PlaySound(_player, configSoundDisabled);
        }
        else
        {
            var configSoundClick = Control.GetPlugin()?.Config.SoundClick;
            if (configSoundClick != null)
                Control.PlaySound(_player, configSoundClick);

            if (Control.GetPlugin()!.Config.IgnoreErrors)
                try
                {
                    Menu.MenuOptions[_selected].OnSelect(_player, Menu.MenuOptions[_selected]);
                }
                catch (Exception e)
                {
                    Control.GetPlugin()
                        ?.Logger
                        .LogInformation(
                            "Error was caused in calling plugin: {EMessage}\n=============== STACKTRACE ===============\n{EStackTrace}\n==========================================",
                            e.Message, e.StackTrace);
                }
            else
                Menu.MenuOptions[_selected].OnSelect(_player, Menu.MenuOptions[_selected]);

            switch (Menu.PostSelectAction)
            {
                case PostSelectAction.Close: Close(); break;
                //case PostSelectAction.Reset: Close(false); Control.GetPlugin().AddTimer(0.2f, () => { if (menu.ResetAction != null && !Control.HasOpenedMenu(player, this)) menu.ResetAction(player); }); break;
                case PostSelectAction.Reset:
                    if (!Control.HasOpenedMenu(_player, this)) Menu.ResetAction?.Invoke(_player);
                    break;
                case PostSelectAction.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void Close(bool withsound = false)
    {
        if (_closed) return;
        if (withsound)
        {
            var configSoundExit = Control.GetPlugin()?.Config.SoundExit;
            if (configSoundExit != null)
                Control.PlaySound(_player, configSoundExit);
        }

        _closed = true;
    }

    public bool Closed()
    {
        return _closed;
    }
}