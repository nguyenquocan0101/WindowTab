using System.Drawing;
using WindowTab.Models;
using WindowTab.Services;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}

foreach (var viewport in new[] { new Size(1920, 1040), new Size(2560, 1400), new Size(1280, 720) })
{
    var background = new Rectangle(-8, -8, viewport.Width + 16, viewport.Height + 16);
    var floating = new Rectangle(viewport.Width / 6, viewport.Height / 10,
        viewport.Width * 3 / 5, viewport.Height * 4 / 5);
    var reveal = TransitionMotion.SelectMode(background, floating, viewport, 16);
    var dismiss = TransitionMotion.SelectMode(floating, background, viewport, 16);
    Check(reveal == TransitionMode.RevealWindow, "A floating target must enter over a stationary background.");
    Check(dismiss == TransitionMode.DismissWindow, "A floating source must leave above a stationary background.");
    Check(TransitionMotion.SelectMode(background, background, viewport, 16) == TransitionMode.SlideBoth,
        "Two full-size windows must retain the existing transition.");
    Check(TransitionMotion.SelectMode(floating, floating, viewport, 16) == TransitionMode.SlideBoth,
        "Two floating windows must retain the existing transition.");
    Check(TransitionMotion.SelectMode(new Rectangle(8, 8, viewport.Width - 16, viewport.Height - 16),
        floating, viewport, 16) == TransitionMode.RevealWindow, "Allow normal work-area frame insets.");

    foreach (var direction in new[] { GestureDirection.Left, GestureDirection.Right })
    {
        for (var sample = 0; sample <= 1000; sample++)
        {
            var progress = sample / 1000d;
            var entering = TransitionMotion.CalculateLayers(background, floating, viewport.Width, direction, progress, reveal);
            var leaving = TransitionMotion.CalculateLayers(floating, background, viewport.Width, direction, progress, dismiss);
            Check(entering.Outgoing == background, "Zalo must not move or resize while Docker enters.");
            Check(leaving.Incoming == background, "Zalo must not move or resize while Docker leaves.");
            Check(entering.Incoming.Width > 0 && entering.Incoming.Height > 0 &&
                leaving.Outgoing.Width > 0 && leaving.Outgoing.Height > 0, "Floating previews must remain drawable.");
        }
        var start = TransitionMotion.CalculateLayers(floating, background, viewport.Width, direction, 0, dismiss);
        var finish = TransitionMotion.CalculateLayers(background, floating, viewport.Width, direction, 1, reveal);
        Check(start.Outgoing == floating && finish.Incoming == floating, "Handoffs must use the actual floating-window bounds.");
        var offscreen = TransitionMotion.CalculateLayers(background, floating, viewport.Width, direction, 0, reveal).Incoming;
        Check(direction == GestureDirection.Left ? offscreen.Left >= viewport.Width : offscreen.Right <= 0,
            "Entering preview must begin outside the viewport.");
        foreach (var progress in new[] { 0d, .25, .5, .75, 1d })
            Check(TransitionMotion.CalculateLayers(background, background, viewport.Width, direction, progress, TransitionMode.SlideBoth)
                == TransitionMotion.Calculate(background, background, viewport.Width, direction, progress),
                "Existing full-size animation must remain unchanged.");
    }
}
Console.WriteLine($"PASS: {checks} floating-window motion regression checks.");
