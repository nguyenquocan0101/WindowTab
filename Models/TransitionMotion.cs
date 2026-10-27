using WindowTab.Services;

namespace WindowTab.Models;

internal readonly record struct TransitionFrame(Rectangle Outgoing, Rectangle Incoming);

internal static class TransitionMotion
{
    internal const double PreviewScale = 0.84;
    internal const double EdgeTravel = 0.20;

    public static TransitionFrame Calculate(Rectangle outgoing, Rectangle incoming, int viewportWidth,
        GestureDirection direction, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var shrink = Ease(progress / 0.22);
        var slide = Ease((progress - 0.18) / 0.64);
        var zoom = Ease((progress - 0.78) / 0.22);
        var travel = direction == GestureDirection.Left ? -viewportWidth : viewportWidth;

        return new TransitionFrame(
            Offset(Scale(outgoing, 1 - (1 - PreviewScale) * shrink), travel * slide),
            Offset(Scale(incoming, PreviewScale + (1 - PreviewScale) * zoom), -travel * (1 - slide)));
    }

    public static Rectangle Boundary(Rectangle home, int viewportWidth, GestureDirection direction, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var displacement = progress < 0.38
            ? Ease(progress / 0.38)
            : SpringReturn((progress - 0.38) / 0.62);
        var travel = viewportWidth * EdgeTravel * (direction == GestureDirection.Left ? -1 : 1);
        return Offset(Scale(home, 1 - 0.04 * Math.Abs(displacement)), travel * displacement);
    }

    public static double Ease(double value)
    {
        var t = Math.Clamp(value, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static double SpringReturn(double t)
    {
        static double Spring(double p) => Math.Exp(-8 * p) * (Math.Cos(10 * p) + 0.8 * Math.Sin(10 * p));
        var end = Spring(1);
        return (Spring(t) - end) / (1 - end);
    }

    private static Rectangle Scale(Rectangle rect, double scale)
    {
        var width = (int)Math.Round(rect.Width * scale);
        var height = (int)Math.Round(rect.Height * scale);
        return new Rectangle(rect.Left + (rect.Width - width) / 2, rect.Top + (rect.Height - height) / 2, width, height);
    }

    private static Rectangle Offset(Rectangle rect, double x) =>
        new(rect.Left + (int)Math.Round(x), rect.Top, rect.Width, rect.Height);
}
