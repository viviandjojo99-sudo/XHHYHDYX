Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$svgPath = "C:\Users\YXY\JOJO\XHHYHDYX\PositionIcon_House.svg"
$previewPath = "C:\Users\YXY\JOJO\XHHYHDYX\_original_on_white.png"
$threshold = 100

$bmp = New-Object System.Drawing.Bitmap $srcPath
$w = $bmp.Width
$h = $bmp.Height

# White-background preview at 8x for inspection
$scale = 8
$prev = New-Object System.Drawing.Bitmap ($w * $scale), ($h * $scale)
$pg = [System.Drawing.Graphics]::FromImage($prev)
$pg.Clear([System.Drawing.Color]::White)
$pg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$pg.DrawImage($bmp, 0, 0, $w * $scale, $h * $scale)
$pg.Dispose()
$prev.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$prev.Dispose()
Write-Output "Wrote $previewPath"

# Build SVG from horizontal runs (exact pixel geometry)
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="512" height="512" shape-rendering="geometricPrecision">')
[void]$sb.AppendLine('  <g fill="#000000">')

for ($y = 0; $y -lt $h; $y++) {
  $x = 0
  while ($x -lt $w) {
    while ($x -lt $w -and $bmp.GetPixel($x, $y).A -lt $threshold) { $x++ }
    if ($x -ge $w) { break }
    $start = $x
    while ($x -lt $w -and $bmp.GetPixel($x, $y).A -ge $threshold) { $x++ }
    $len = $x - $start
    [void]$sb.AppendLine(("    <rect x=`"{0}`" y=`"{1}`" width=`"{2}`" height=`"1`"/>" -f $start, $y, $len))
  }
}

[void]$sb.AppendLine('  </g>')
[void]$sb.AppendLine('</svg>')
[System.IO.File]::WriteAllText($svgPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
$bmp.Dispose()
Write-Output "Wrote $svgPath"
Write-Output ("SVG bytes: {0}" -f (Get-Item $svgPath).Length)
