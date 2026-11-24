Add-Type -AssemblyName System.Drawing
$path = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$bmp = New-Object System.Drawing.Bitmap $path
$w = $bmp.Width
$h = $bmp.Height
$threshold = 128

# Build binary mask
$mask = New-Object 'bool[,]' $w, $h
for ($y = 0; $y -lt $h; $y++) {
  for ($x = 0; $x -lt $w; $x++) {
    $mask[$x, $y] = ($bmp.GetPixel($x, $y).A -ge $threshold)
  }
}

# Find outer contour starting from leftmost solid pixel of topmost row with content
$startX = -1; $startY = -1
for ($y = 0; $y -lt $h; $y++) {
  for ($x = 0; $x -lt $w; $x++) {
    if ($mask[$x, $y]) { $startX = $x; $startY = $y; break }
  }
  if ($startX -ge 0) { break }
}

# Moore neighborhood clockwise from west=0
# dirs: 0E 1SE 2S 3SW 4W 5NW 6N 7NE
$dx = @(1,1,0,-1,-1,-1,0,1)
$dy = @(0,1,1,1,0,-1,-1,-1)

function Test-Solid([int]$x, [int]$y) {
  if ($x -lt 0 -or $y -lt 0 -or $x -ge $script:w -or $y -ge $script:h) { return $false }
  return $script:mask[$x, $y]
}

$points = New-Object System.Collections.Generic.List[string]
$cx = $startX; $cy = $startY
$dir = 4 # came from west conceptually, start looking from N-ish
$points.Add("$cx,$cy")
$maxSteps = 5000
$steps = 0
do {
  # start search from (dir + 6) mod 8 = turn left relative to incoming
  $startDir = ($dir + 6) % 8
  $found = $false
  for ($i = 0; $i -lt 8; $i++) {
    $nd = ($startDir + $i) % 8
    $nx = $cx + $dx[$nd]
    $ny = $cy + $dy[$nd]
    if (Test-Solid $nx $ny) {
      $cx = $nx; $cy = $ny; $dir = $nd
      $points.Add("$cx,$cy")
      $found = $true
      break
    }
  }
  if (-not $found) { break }
  $steps++
} while (-not ($cx -eq $startX -and $cy -eq $startY) -and $steps -lt $maxSteps)

Write-Output ("OuterPoints: {0}" -f $points.Count)
# downsample every Nth for SVG path preview
$stepN = 2
$ds = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $points.Count; $i += $stepN) { $ds.Add($points[$i]) }
Write-Output ("Downsampled: {0}" -f $ds.Count)
Write-Output ($ds -join " ")

# Also dump a higher-res ASCII of the filled region for manual pathing
Write-Output "---GRID---"
for ($y = 0; $y -lt $h; $y++) {
  $row = New-Object System.Text.StringBuilder
  for ($x = 0; $x -lt $w; $x++) {
    if ($mask[$x, $y]) { [void]$row.Append("#") } else { [void]$row.Append(".") }
  }
  Write-Output $row.ToString()
}
$bmp.Dispose()
