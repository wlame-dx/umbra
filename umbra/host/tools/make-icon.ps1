# Umbra - "U" logosunu uretir: assets\umbra.ico (16..256, PNG tabanli) + assets\umbra.png
# Kullanim:  powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root  = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root "assets"
if (-not (Test-Path $assets)) { New-Item -ItemType Directory -Path $assets | Out-Null }

# --- 256px ana cizim ---
function New-UmbraBitmap([int]$size) {
  $bmp = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

  $s = [double]$size

  # Koyu yuvarlak zemin (gradyan)
  $x0 = [single](0.04 * $s); $y0 = [single](0.04 * $s)
  $w  = [single](0.92 * $s); $h  = [single](0.92 * $s)
  $rect = [System.Drawing.RectangleF]::new($x0, $y0, $w, $h)
  $r = [single](0.24 * $s)
  $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
  $path.AddArc($rect.X, $rect.Y, $r, $r, 180, 90)
  $path.AddArc($rect.Right - $r, $rect.Y, $r, $r, 270, 90)
  $path.AddArc($rect.Right - $r, $rect.Bottom - $r, $r, $r, 0, 90)
  $path.AddArc($rect.X, $rect.Bottom - $r, $r, $r, 90, 90)
  $path.CloseFigure()

  $c1 = [System.Drawing.Color]::FromArgb(255, 0x12, 0x15, 0x1D)
  $c2 = [System.Drawing.Color]::FromArgb(255, 0x07, 0x08, 0x0B)
  $grad = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, $c1, $c2, 45.0)
  $g.FillPath($grad, $path)

  # Hafif cerceve
  $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(40, 255, 255, 255), [Math]::Max(1, [single](0.012 * $s)))
  $g.DrawPath($pen, $path)
  $pen.Dispose()

  # "U" harfi: acik gradyan
  $fontSize = [single](0.58 * $s)
  $font = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
  $tf = [System.Drawing.StringFormat]::new()
  $tf.Alignment = [System.Drawing.StringAlignment]::Center
  $tf.LineAlignment = [System.Drawing.StringAlignment]::Center

  $drawRect = [System.Drawing.RectangleF]::new(0, [single](0.03 * $s), $s, [single](0.92 * $s))
  $lg = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    $drawRect,
    [System.Drawing.Color]::FromArgb(255, 0xFF, 0xFF, 0xFF),
    [System.Drawing.Color]::FromArgb(255, 0xB8, 0xBE, 0xC8),
    90.0)
  $g.DrawString("U", $font, $lg, $drawRect, $tf)

  $lg.Dispose(); $tf.Dispose(); $font.Dispose(); $grad.Dispose(); $path.Dispose()
  $g.Dispose()
  return $bmp
}

# --- ico dosyasini elle yaz (PNG tabanli, Vista+ destekler) ---
$sizes = @(256, 128, 64, 48, 32, 24, 16)
$pngs = @()
foreach ($sz in $sizes) {
  $bmp = New-UmbraBitmap $sz
  $ms = [System.IO.MemoryStream]::new()
  $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $pngs += ,($ms.ToArray())
  $bmp.Dispose(); $ms.Dispose()

  if ($sz -eq 256) {
    $bmp256 = New-UmbraBitmap 256
    $bmp256.Save((Join-Path $assets "umbra.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp256.Dispose()
  }
}

$count = $sizes.Count
$ico = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($ico)

$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$count)

$offset = 6 + 16 * $count
for ($i = 0; $i -lt $count; $i++) {
  $sz = $sizes[$i]
  $dim = [Byte]($(if ($sz -ge 256) { 0 } else { $sz }))
  $writer.Write($dim)
  $writer.Write($dim)
  $writer.Write([Byte]0)
  $writer.Write([Byte]0)
  $writer.Write([UInt16]1)
  $writer.Write([UInt16]32)
  $writer.Write([UInt32]$pngs[$i].Length)
  $writer.Write([UInt32]$offset)
  $offset += $pngs[$i].Length
}
foreach ($png in $pngs) { $writer.Write($png) }
$writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets "umbra.ico"), $ico.ToArray())
$writer.Dispose(); $ico.Dispose()

Write-Host ("Olusturuldu: " + (Join-Path $assets "umbra.ico") + "  (" + (Get-Item (Join-Path $assets 'umbra.ico')).Length + " bayt)")
Write-Host ("Olusturuldu: " + (Join-Path $assets "umbra.png"))