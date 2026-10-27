namespace WindowTab.Models;

public sealed class AppSettings
{
    public HotkeyBinding SwipeLeft { get; set; } = HotkeyBinding.CreateDefaultLeft();
    public HotkeyBinding SwipeRight { get; set; } = HotkeyBinding.CreateDefaultRight();
    public int CooldownMs { get; set; } = 250;
    public int AnimationMs { get; set; } = 600;
    public bool StartWithWindows { get; set; }
    public bool IncludeDesktop { get; set; }

    public AppSettings Clone() => new()
    {
        SwipeLeft = SwipeLeft.Clone(),
        SwipeRight = SwipeRight.Clone(),
        CooldownMs = CooldownMs,
        AnimationMs = AnimationMs,
        StartWithWindows = StartWithWindows,
        IncludeDesktop = IncludeDesktop
    };

    public void Normalize()
    {
        if (SwipeLeft is null || !SwipeLeft.IsValid) SwipeLeft = HotkeyBinding.CreateDefaultLeft();
        if (SwipeRight is null || !SwipeRight.IsValid) SwipeRight = HotkeyBinding.CreateDefaultRight();
        CooldownMs = Math.Clamp(CooldownMs, 0, 1000);
        AnimationMs = Math.Clamp(AnimationMs, 100, 600);
    }
}
