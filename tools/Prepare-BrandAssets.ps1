[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\newlogo.png'),
    [string]$AssetDirectory = (Join-Path $PSScriptRoot '..\src\Registry.App\Assets'),
    [string]$InstallerDirectory = (Join-Path $PSScriptRoot '..\installer\assets')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-Canvas([int]$Width, [int]$Height, [double]$Fill) {
    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $scale = [Math]::Min($Width / $script:Logo.Width, $Height / $script:Logo.Height) * $Fill
    $drawWidth = [int][Math]::Round($script:Logo.Width * $scale)
    $drawHeight = [int][Math]::Round($script:Logo.Height * $scale)
    $graphics.DrawImage($script:Logo, [int](($Width - $drawWidth) / 2), [int](($Height - $drawHeight) / 2), $drawWidth, $drawHeight)
    $graphics.Dispose()
    return $bitmap
}

function Save-Png([System.Drawing.Bitmap]$Bitmap, [string]$Path) {
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Bitmap.Dispose()
}

function Write-Icon([string]$Path) {
    $sizes = 16, 24, 32, 48, 64, 128, 256
    $images = foreach ($size in $sizes) {
        $bitmap = New-Canvas $size $size 0.88
        $stream = [System.IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        ,$stream.ToArray()
        $stream.Dispose()
    }
    $stream = [System.IO.File]::Create($Path)
    $writer = [System.IO.BinaryWriter]::new($stream)
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$sizes.Count)
    $offset = 6 + (16 * $sizes.Count)
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$index].Length); $writer.Write([UInt32]$offset)
        $offset += $images[$index].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
    $writer.Dispose(); $stream.Dispose()
}

New-Item -ItemType Directory -Force $AssetDirectory, $InstallerDirectory | Out-Null
$raw = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source))
$bounds = [System.Drawing.Rectangle]::new($raw.Width, $raw.Height, 0, 0)
for ($y = 0; $y -lt $raw.Height; $y++) { for ($x = 0; $x -lt $raw.Width; $x++) { if ($raw.GetPixel($x, $y).A -gt 8) { $bounds = [System.Drawing.Rectangle]::Union($bounds, [System.Drawing.Rectangle]::new($x, $y, 1, 1)) } } }
if ($bounds.Width -le 0 -or $bounds.Height -le 0) { throw 'The supplied logo has no visible pixels.' }
$script:Logo = [System.Drawing.Bitmap]::new($bounds.Width, $bounds.Height)
$graphics = [System.Drawing.Graphics]::FromImage($script:Logo); $graphics.DrawImage($raw, [System.Drawing.Rectangle]::new(0, 0, $bounds.Width, $bounds.Height), $bounds, [System.Drawing.GraphicsUnit]::Pixel); $graphics.Dispose(); $raw.Dispose()

$outputs = @{
    'RegistryLogo.png' = @(1024, 1024, 0.90); 'Square150x150Logo.scale-200.png' = @(300, 300, 0.84)
    'Square44x44Logo.scale-200.png' = @(88, 88, 0.88); 'Square44x44Logo.targetsize-24_altform-unplated.png' = @(24, 24, 0.90)
    'Square44x44Logo.targetsize-48_altform-lightunplated.png' = @(48, 48, 0.90); 'StoreLogo.png' = @(50, 50, 0.86)
    'LockScreenLogo.scale-200.png' = @(48, 48, 0.90); 'Wide310x150Logo.scale-200.png' = @(620, 300, 0.72)
    'SplashScreen.scale-200.png' = @(1240, 600, 0.46)
}
foreach ($entry in $outputs.GetEnumerator()) { Save-Png (New-Canvas $entry.Value[0] $entry.Value[1] $entry.Value[2]) (Join-Path $AssetDirectory $entry.Key) }
Write-Icon (Join-Path $AssetDirectory 'AppIcon.ico')
Copy-Item (Join-Path $AssetDirectory 'AppIcon.ico') (Join-Path $InstallerDirectory 'app.ico') -Force
$script:Logo.Dispose()
Write-Host "Prepared Registry branding assets from $Source"
