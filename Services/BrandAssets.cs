using System.Reflection;

namespace WindowTab.Services;

internal static class BrandAssets
{
    public static readonly Image Logo = LoadLogo();
    public static readonly Icon AppIcon = LoadIcon();

    private static Image LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowTab.Assets.windowtab-logo.png")
            ?? throw new InvalidOperationException("Missing WindowTab logo resource.");
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowTab.Assets.windowtab.ico")
            ?? throw new InvalidOperationException("Missing WindowTab icon resource.");
        using var icon = new Icon(stream, new Size(32, 32));
        return (Icon)icon.Clone();
    }
}
