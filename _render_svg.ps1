# Rasterize SVG via a simple path fill using System.Drawing is hard.
# Instead, create a large preview PNG from the original alpha for side-by-side,
# and also write a HTML preview that embeds both.
$src = "C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
$svg = "C:\Users\YXY\JOJO\XHHYHDYX\PositionIcon_House.svg"
$html = "C:\Users\YXY\JOJO\XHHYHDYX\_preview_icon.html"

# Copy source to workspace for relative preview
Copy-Item $src "C:\Users\YXY\JOJO\XHHYHDYX\_src_icon.png" -Force
$svgText = Get-Content $svg -Raw

$htmlContent = @"
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Icon Preview</title>
<style>
  body { font-family: sans-serif; background: #ddd; padding: 24px; }
  .row { display: flex; gap: 32px; align-items: flex-start; }
  .card { background: #fff; padding: 16px; border-radius: 8px; }
  .card.dark { background: #333; }
  img, object, svg { width: 256px; height: 256px; image-rendering: pixelated; }
  .card svg { display: block; }
  h3 { margin: 0 0 12px; font-size: 14px; }
  .dark h3 { color: #fff; }
</style></head><body>
<h2>Original vs SVG</h2>
<div class="row">
  <div class="card"><h3>Original PNG (scaled)</h3><img src="_src_icon.png" width="256" height="256"></div>
  <div class="card"><h3>SVG on white</h3>$svgText</div>
  <div class="card dark"><h3>SVG on dark</h3>$svgText</div>
</div>
</body></html>
"@
[System.IO.File]::WriteAllText($html, $htmlContent, [System.Text.UTF8Encoding]::new($false))
Write-Output "Wrote $html"
