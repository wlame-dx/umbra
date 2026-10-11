# Umbra - arayuz denetim araci
# XAML'i ayristirir, yerlestirir ve adlandirilmis ogelerin gercek boyutlarini listeler.

param(
  [string]$Xaml = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "ui\MainWindow.xaml"),
  [double]$Width = 940,
  [double]$Height = 648
)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$src  = [System.IO.File]::ReadAllText($Xaml)
$root = [System.Windows.Markup.XamlReader]::Parse($src)
$root.Measure([System.Windows.Size]::new($Width, $Height))
$root.Arrange([System.Windows.Rect]::new(0, 0, $Width, $Height))
$root.UpdateLayout()

Write-Host ("Kok: " + $root.GetType().Name + "  " + [math]::Round($root.ActualWidth) + "x" + [math]::Round($root.ActualHeight)) -ForegroundColor Cyan
Write-Host "Adlandirilmis ogeler:" -ForegroundColor Cyan

$queue = New-Object System.Collections.Queue
$queue.Enqueue($root)
$missing = 0
while ($queue.Count -gt 0) {
  $el = $queue.Dequeue()
  if ($el -is [System.Windows.FrameworkElement]) {
    $n = $el.Name
    if ($n -and $n.Length -gt 0) {
      $ew = [math]::Round($el.ActualWidth); $eh = [math]::Round($el.ActualHeight)
      $flag = ""
      if ($ew -le 0 -or $eh -le 0) { $flag = "  <-- BOYUT YOK (bekleniyor olabilir)"; $missing++ }
      Write-Host ("  {0,-16} {1,-14} {2,5} x {3,-5}{4}" -f $n, $el.GetType().Name, $ew, $eh, $flag)
    }
    $vc = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($el)
    for ($ci = 0; $ci -lt $vc; $ci++) { $queue.Enqueue([System.Windows.Media.VisualTreeHelper]::GetChild($el, $ci)) }
  }
}

$pw = [int]$Width; $ph = [int]$Height
$total = $pw * $ph
$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($pw, $ph, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32))
$rtb.Render($root)
$stride = $pw * 4
$buf = New-Object byte[] ($stride * $ph)
$rtb.CopyPixels($buf, $stride, 0)
$opaque = 0
for ($i = 3; $i -lt $buf.Length; $i += 4) { if ($buf[$i] -gt 10) { $opaque++ } }
$pct = [math]::Round(100.0 * $opaque / $total, 1)
Write-Host ("Opak piksel: {0:N0} / {1:N0}  ({2}%)" -f $opaque, $total, $pct) -ForegroundColor Cyan
if ($missing -gt 0) { Write-Host ("Not: {0} oge su an sifir boyutlu." -f $missing) -ForegroundColor Yellow }
