param(
    [string]$Source = (Join-Path $PSScriptRoot '..\Assets\windowtab-logo.png'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\windowtab.ico')
)

# Package the unchanged logo into the image sizes used by Windows shell icons.
Add-Type -AssemblyName System.Drawing
$image = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $Source).Path)
$sizes = @(16, 20, 24, 32, 48, 64, 128, 256)
$entries = [System.Collections.Generic.List[byte[]]]::new()
try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $stream = [System.IO.MemoryStream]::new()
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.DrawImage($image, 0, 0, $size, $size)
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $entries.Add($stream.ToArray())
        }
        finally { $graphics.Dispose(); $bitmap.Dispose(); $stream.Dispose() }
    }
    $output = [System.IO.File]::Create([System.IO.Path]::GetFullPath($Destination))
    $writer = [System.IO.BinaryWriter]::new($output)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$entries[$i].Length); $writer.Write([uint32]$offset)
            $offset += $entries[$i].Length
        }
        foreach ($entry in $entries) { $writer.Write([byte[]]$entry) }
    }
    finally { $writer.Dispose(); $output.Dispose() }
}
finally { $image.Dispose() }
