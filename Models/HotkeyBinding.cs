using System.Windows.Forms;

namespace WindowTab.Models;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4
}

public sealed class HotkeyBinding
{
    public Keys Key { get; set; } = Keys.None;
    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.None;

    public HotkeyBinding Clone() => new() { Key = Key, Modifiers = Modifiers };

    public bool IsValid => Key != Keys.None &&
                           Key != Keys.ControlKey &&
                           Key != Keys.ShiftKey &&
                           Key != Keys.Menu &&
                           Key != Keys.F12 &&
                           Modifiers != HotkeyModifiers.None;

    public string ToDisplayString()
    {
        var parts = new List<string>(4);
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(Key == Keys.Space ? "Space" : Key.ToString());
        return string.Join(" + ", parts);
    }

    public override string ToString() => ToDisplayString();

    public static HotkeyBinding CreateDefaultLeft() => new()
    {
        Key = Keys.F8,
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift
    };

    public static HotkeyBinding CreateDefaultRight() => new()
    {
        Key = Keys.F10,
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift
    };
}
