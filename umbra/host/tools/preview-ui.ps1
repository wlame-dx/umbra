# Umbra - arayuz onizleme araci
# MainWindow.xaml dosyasini ayristirir ve PNG olarak render eder.

param(
  [string]$Xaml = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "ui\MainWindow.xaml"),
  [string]$Out  = (Join-Path $env:TEMP "umbra-ui.png"),
  [double]$W = 940,
  [double]$H = 648
)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$src  = [System.IO.File]::ReadAllText($Xaml)
$root = [System.Windows.Markup.XamlReader]::Parse($src)
Write-Host "XAML ayristirildi." -ForegroundColor Green

$root.Measure([System.Windows.Size]::new($W, $H))
$root.Arrange([System.Windows.Rect]::new(0, 0, $W, $H))
$root.UpdateLayout()

$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$W, [int]$H, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32))
$rtb.Render($root)

$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
$enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
$fs = [System.IO.File]::Create($Out)
$enc.Save($fs)
$fs.Close()

Write-Host ("Render edildi: " + $Out) -ForegroundColor Green
