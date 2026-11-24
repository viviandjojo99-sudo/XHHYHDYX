Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Drawing.Drawing2D

$out = "C:\Users\YXY\JOJO\XHHYHDYX\_svg_preview.png"
$src = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$size = 512
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::White)
$scale = $size / 100.0
$g.ScaleTransform($scale, $scale)
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)

function Parse-And-Fill([System.Drawing.Graphics]$g, [System.Drawing.Brush]$brush, [string]$d) {
  $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
  $gp.FillMode = [System.Drawing.Drawing2D.FillMode]::Alternate
  $tokens = [regex]::Split($d.Trim(), '\s+') | Where-Object { $_ -ne '' }
  $i = 0
  $cx = 0.0; $cy = 0.0
  $startX = 0.0; $startY = 0.0
  $poly = New-Object System.Collections.Generic.List[System.Drawing.PointF]

  while ($i -lt $tokens.Count) {
    $t = $tokens[$i]
    if ($t -match '^[MLHVCZ]$') {
      $cmd = $t
      $i++
      switch ($cmd) {
        'M' {
          if ($poly.Count -ge 3) { $gp.AddPolygon($poly.ToArray()) }
          $poly = New-Object System.Collections.Generic.List[System.Drawing.PointF]
          $cx = [double]$tokens[$i]; $cy = [double]$tokens[$i+1]; $i += 2
          $startX = $cx; $startY = $cy
          $poly.Add([System.Drawing.PointF]::new([float]$cx, [float]$cy))
        }
        'L' {
          $cx = [double]$tokens[$i]; $cy = [double]$tokens[$i+1]; $i += 2
          $poly.Add([System.Drawing.PointF]::new([float]$cx, [float]$cy))
        }
        'H' {
          $cx = [double]$tokens[$i]; $i++
          $poly.Add([System.Drawing.PointF]::new([float]$cx, [float]$cy))
        }
        'V' {
          $cy = [double]$tokens[$i]; $i++
          $poly.Add([System.Drawing.PointF]::new([float]$cx, [float]$cy))
        }
        'C' {
          $x1=[double]$tokens[$i]; $y1=[double]$tokens[$i+1]
          $x2=[double]$tokens[$i+2]; $y2=[double]$tokens[$i+3]
          $x3=[double]$tokens[$i+4]; $y3=[double]$tokens[$i+5]
          $i += 6
          $p0x=$cx; $p0y=$cy
          for ($s=1; $s -le 20; $s++) {
            $tt = $s/20.0
            $u = 1-$tt
            $px = $u*$u*$u*$p0x + 3*$u*$u*$tt*$x1 + 3*$u*$tt*$tt*$x2 + $tt*$tt*$tt*$x3
            $py = $u*$u*$u*$p0y + 3*$u*$u*$tt*$y1 + 3*$u*$tt*$tt*$y2 + $tt*$tt*$tt*$y3
            $poly.Add([System.Drawing.PointF]::new([float]$px, [float]$py))
          }
          $cx=$x3; $cy=$y3
        }
        'Z' {
          $poly.Add([System.Drawing.PointF]::new([float]$startX, [float]$startY))
          if ($poly.Count -ge 3) { $gp.AddPolygon($poly.ToArray()) }
          $poly = New-Object System.Collections.Generic.List[System.Drawing.PointF]
          $cx=$startX; $cy=$startY
        }
      }
    } else { $i++ }
  }
  if ($poly.Count -ge 3) { $gp.AddPolygon($poly.ToArray()) }
  $g.FillPath($brush, $gp)
  $gp.Dispose()
}

$pin = @"
M 50 9.5
C 27.8 9.5 11.5 26.2 11.5 48
C 11.5 61.2 20.2 72.8 34.2 85
L 47.1 96.4
C 48.6 97.7 51.4 97.7 52.9 96.4
L 65.8 85
C 79.8 72.8 88.5 61.2 88.5 48
C 88.5 26.2 72.2 9.5 50 9.5
Z
M 50 18.5
C 67 18.5 79.5 31.8 79.5 48
C 79.5 58.2 73 67.5 62.8 77
L 50 89
L 37.2 77
C 27 67.5 20.5 58.2 20.5 48
C 20.5 31.8 33 18.5 50 18.5
Z
"@
Parse-And-Fill $g $brush $pin

$g.FillRectangle($brush, 24.5, 51, 47, 5.8)
$g.FillRectangle($brush, 24.5, 51, 6, 32)
$g.FillRectangle($brush, 65.5, 56.8, 6, 28.7)

$house = @"
M 35.5 70.5
L 49.5 54.5
L 63.5 70.5
L 63.5 84
L 35.5 84
Z
M 43.5 72.5
H 55.5
V 84
H 43.5
Z
"@
Parse-And-Fill $g $brush $house

$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "Wrote $out"

$cmp = New-Object System.Drawing.Bitmap 1100, 560
$cg = [System.Drawing.Graphics]::FromImage($cmp)
$cg.Clear([System.Drawing.Color]::LightGray)
$srcImg = [System.Drawing.Image]::FromFile($src)
$svgImg = [System.Drawing.Image]::FromFile($out)
$black = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)
$cg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$cg.DrawImage($srcImg, 40, 40, 480, 480)
$cg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$cg.DrawImage($svgImg, 580, 40, 480, 480)
$font = New-Object System.Drawing.Font "Segoe UI", 14
$cg.DrawString("Original PNG", $font, $black, 40, 10)
$cg.DrawString("SVG vector", $font, $black, 580, 10)
$cmpOut = "C:\Users\YXY\JOJO\XHHYHDYX\_compare_preview.png"
$cmp.Save($cmpOut, [System.Drawing.Imaging.ImageFormat]::Png)
$cg.Dispose(); $cmp.Dispose(); $srcImg.Dispose(); $svgImg.Dispose()
Write-Output "Wrote $cmpOut"
