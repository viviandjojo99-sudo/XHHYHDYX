from PIL import Image
import collections

path = r"C:\Users\YXY\.cursor\projects\c-Users-YXY-JOJO-XHHYHDYX\assets\PositionIcon_House-02eed68b-a5a0-4641-b274-7844771225cd.png"
im = Image.open(path)
print("size", im.size, "mode", im.mode)
im2 = im.convert("RGBA")
px = list(im2.getdata())
print("pixels", len(px))
c = collections.Counter((p[0], p[1], p[2], p[3]) for p in px)
print("unique", len(c))
print("top", c.most_common(25))
nonempty = [(i, p) for i, p in enumerate(px) if p[3] > 10]
print("nonempty", len(nonempty))
if nonempty:
    xs = [i % im2.size[0] for i, _ in nonempty]
    ys = [i // im2.size[0] for i, _ in nonempty]
    print("bbox", min(xs), min(ys), max(xs), max(ys))
    # sample a grid of alpha values to understand shape
    w, h = im2.size
    step = max(1, w // 40)
    for y in range(0, h, step):
        row = []
        for x in range(0, w, step):
            a = im2.getpixel((x, y))[3]
            row.append("#" if a > 128 else ("." if a > 20 else " "))
        print("".join(row))
