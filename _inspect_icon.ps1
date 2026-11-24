Add-Type -AssemblyName System.Drawing
$path = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$img = [System.Drawing.Image]::FromFile($path)
$bmp = New-Object System.Drawing.Bitmap $img
Write-Output ("Size: {0}x{1}" -f $bmp.Width, $bmp.Height)
Write-Output ("PixelFormat: {0}" -f $bmp.PixelFormat)

$colors = @{}
$minX = $bmp.Width; $minY = $bmp.Height; $maxX = -1; $maxY = -1
$nonempty = 0
for ($y = 0; $y -lt $bmp.Height; $y++) {
  for ($x = 0; $x -lt $bmp.Width; $x++) {
    $c = $bmp.GetPixel($x, $y)
    $key = "{0:X2}{1:X2}{2:X2}A{3:X2}" -f $c.R, $c.G, $c.B, $c.A
    if (-not $colors.ContainsKey($key)) { $colors[$key] = 0 }
    $colors[$key]++
    if ($c.A -gt 10) {
      $nonempty++
      if ($x -lt $minX) { $minX = $x }
      if ($y -lt $minY) { $minY = $y }
      if ($x -gt $maxX) { $maxX = $x }
      if ($y -gt $maxY) { $maxY = $y }
    }
  }
}
Write-Output ("NonEmpty: {0}" -f $nonempty)
if ($maxX -ge 0) { Write-Output ("BBox: {0},{1} -> {2},{3}" -f $minX, $minY, $maxX, $maxY) }
$colors.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 20 | ForEach-Object {
  Write-Output ("{0} {1}" -f $_.Key, $_.Value)
}

$step = [Math]::Max(1, [Math]::Floor($bmp.Width / 48))
for ($y = 0; $y -lt $bmp.Height; $y += $step) {
  $row = New-Object System.Text.StringBuilder
  for ($x = 0; $x -lt $bmp.Width; $x += $step) {
    $a = $bmp.GetPixel($x, $y).A
    if ($a -gt 128) { [void]$row.Append("#") }
    elseif ($a -gt 20) { [void]$row.Append(".") }
    else { [void]$row.Append(" ") }
  }
  Write-Output $row.ToString()
}
$bmp.Dispose()
$img.Dispose()
