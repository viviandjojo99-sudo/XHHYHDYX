Add-Type -AssemblyName System.Drawing
$src = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$out = "C:\Users\YXY\JOJO\XHHYHDYX\PositionIcon_House.svg"
$bmp = New-Object System.Drawing.Bitmap $src
$w = $bmp.Width
$h = $bmp.Height
$threshold = 100

$mask = New-Object 'bool[,]' ($w + 2), ($h + 2)
for ($y = 0; $y -lt $h; $y++) {
  for ($x = 0; $x -lt $w; $x++) {
    $mask[($x + 1), ($y + 1)] = ($bmp.GetPixel($x, $y).A -ge $threshold)
  }
}
$bmp.Dispose()

# Marching squares / boundary edge collection into rings
# For each filled pixel, emit unit edges on sides adjacent to empty
$edgeSet = @{} # key "x1,y1,x2,y2" in grid coords (pixel corners), value = 1

function Add-Edge([int]$x1, [int]$y1, [int]$x2, [int]$y2) {
  $k = "$x1,$y1,$x2,$y2"
  $rk = "$x2,$y2,$x1,$y1"
  if ($script:edgeSet.ContainsKey($rk)) {
    $script:edgeSet.Remove($rk)
  } else {
    $script:edgeSet[$k] = $true
  }
}

for ($y = 0; $y -le ($h + 1); $y++) {
  for ($x = 0; $x -le ($w + 1); $x++) {
    if ($mask[$x, $y]) {
      # pixel occupies cell (x-1..x) x (y-1..y) in corner space? 
      # Our mask is 1-indexed content; pixel (x,y) in mask covers corners (x-1,y-1)-(x,y)
      # Actually mask[x,y] corresponds to image pixel (x-1,y-1), corners: (x-1,y-1), (x,y-1), (x,y), (x-1,y)
      $c0x = $x - 1; $c0y = $y - 1
      $c1x = $x;     $c1y = $y - 1
      $c2x = $x;     $c2y = $y
      $c3x = $x - 1; $c3y = $y
      if (-not $mask[($x), ($y - 1)]) { Add-Edge $c0x $c0y $c1x $c1y } # top, L->R
      if (-not $mask[($x + 1), ($y)]) { Add-Edge $c1x $c1y $c2x $c2y } # right, T->B
      if (-not $mask[($x), ($y + 1)]) { Add-Edge $c2x $c2y $c3x $c3y } # bottom, R->L
      if (-not $mask[($x - 1), ($y)]) { Add-Edge $c3x $c3y $c0x $c0y } # left, B->T
    }
  }
}

# Build adjacency from remaining edges
$next = @{}
foreach ($k in @($edgeSet.Keys)) {
  $p = $k.Split(',')
  $a = "{0},{1}" -f $p[0], $p[1]
  $b = "{0},{1}" -f $p[2], $p[3]
  $next[$a] = $b
}

# Trace all rings
$visited = @{}
$rings = New-Object System.Collections.Generic.List[object]
foreach ($start in @($next.Keys)) {
  if ($visited.ContainsKey($start)) { continue }
  $ring = New-Object System.Collections.Generic.List[string]
  $cur = $start
  $guard = 0
  while (-not $visited.ContainsKey($cur) -and $guard -lt 20000) {
    $visited[$cur] = $true
    $ring.Add($cur)
    if (-not $next.ContainsKey($cur)) { break }
    $cur = $next[$cur]
    $guard++
    if ($cur -eq $start) { break }
  }
  if ($ring.Count -ge 4) { $rings.Add($ring) }
}

Write-Output ("Rings: {0}" -f $rings.Count)
foreach ($ring in $rings) { Write-Output ("  len={0}" -f $ring.Count) }

# Simplify: remove colinear points
function Simplify-Ring($ring) {
  $pts = New-Object System.Collections.Generic.List[object]
  foreach ($s in $ring) {
    $p = $s.Split(',')
    $pts.Add(@([int]$p[0], [int]$p[1]))
  }
  if ($pts.Count -lt 3) { return $pts }
  $simp = New-Object System.Collections.Generic.List[object]
  $n = $pts.Count
  for ($i = 0; $i -lt $n; $i++) {
    $prev = $pts[(($i - 1 + $n) % $n)]
    $cur = $pts[$i]
    $nxt = $pts[(($i + 1) % $n)]
    $cross = ($cur[0] - $prev[0]) * ($nxt[1] - $cur[1]) - ($cur[1] - $prev[1]) * ($nxt[0] - $cur[0])
    if ($cross -ne 0) { $simp.Add($cur) }
  }
  return $simp
}

# Build SVG path (mask coords are already in image pixel corner space: 0..100)
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="512" height="512">')
[void]$sb.Append('  <path fill="#000000" fill-rule="evenodd" d="')

# Sort rings by bbox area descending so outer comes first
$ringMeta = @()
foreach ($ring in $rings) {
  $simp = Simplify-Ring $ring
  if ($simp.Count -lt 3) { continue }
  $minx = 999; $miny = 999; $maxx = -999; $maxy = -999
  foreach ($p in $simp) {
    if ($p[0] -lt $minx) { $minx = $p[0] }
    if ($p[1] -lt $miny) { $miny = $p[1] }
    if ($p[0] -gt $maxx) { $maxx = $p[0] }
    if ($p[1] -gt $maxy) { $maxy = $p[1] }
  }
  $area = ($maxx - $minx) * ($maxy - $miny)
  $ringMeta += [pscustomobject]@{ Area = $area; Pts = $simp }
}
$ringMeta = $ringMeta | Sort-Object Area -Descending

foreach ($rm in $ringMeta) {
  $pts = $rm.Pts
  $first = $true
  foreach ($p in $pts) {
    # corner coords are already image-space (0..100); mask padding means corner (0,0) is top-left of image
    if ($first) {
      [void]$sb.AppendFormat("M{0} {1} ", $p[0], $p[1])
      $first = $false
    } else {
      [void]$sb.AppendFormat("L{0} {1} ", $p[0], $p[1])
    }
  }
  [void]$sb.Append("Z ")
}

[void]$sb.AppendLine('"/>')
[void]$sb.AppendLine('</svg>')
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Output ("Wrote {0}" -f $out)
Write-Output ("Path rings used: {0}" -f $ringMeta.Count)
