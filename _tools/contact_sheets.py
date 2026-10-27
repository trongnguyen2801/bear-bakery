#!/usr/bin/env python3
"""Labeled contact sheets per atlas group. Label = sprite name; header = group + derived dir.
Checkerboard background shows transparency."""
import json, os, math
from PIL import Image, ImageDraw, ImageFont
IDX = "/Users/trongnp/BearBakery/_derived/sprites_index.json"
OUT = "/Users/trongnp/BearBakery/_derived/contact_sheets"
os.makedirs(OUT, exist_ok=True)
FONT = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets/Font/LiberationSans.ttf"
f_small = ImageFont.truetype(FONT, 11)
f_head = ImageFont.truetype(FONT, 18)
CELL, LAB, COLS, ROWS = 150, 28, 8, 7
idx = [e for e in json.load(open(IDX)) if "out" in e]
groups = {}
for e in idx:
    groups.setdefault(e["group"], []).append(e)

def checker(w, h):
    im = Image.new("RGBA", (w, h), (235, 235, 235, 255))
    d = ImageDraw.Draw(im)
    for yy in range(0, h, 10):
        for xx in range(0, w, 10):
            if (xx // 10 + yy // 10) % 2: d.rectangle([xx, yy, xx + 9, yy + 9], fill=(210, 210, 210, 255))
    return im

sheets = []
for g, items in sorted(groups.items()):
    items.sort(key=lambda e: e["name"].lower())
    per = COLS * ROWS
    for p in range(math.ceil(len(items) / per)):
        chunk = items[p * per:(p + 1) * per]
        rows = math.ceil(len(chunk) / COLS)
        W, H = COLS * CELL, 30 + rows * (CELL + LAB)
        sheet = Image.new("RGBA", (W, H), (255, 255, 255, 255))
        d = ImageDraw.Draw(sheet)
        d.text((6, 5), f"{g}  page {p+1}  (_derived/sprites/{g}/<name>.png)", fill=(0, 0, 0), font=f_head)
        for i, e in enumerate(chunk):
            cx, cy = (i % COLS) * CELL, 30 + (i // COLS) * (CELL + LAB)
            sheet.paste(checker(CELL - 6, CELL - 6), (cx + 3, cy + 3))
            im = Image.open(e["out"])
            sc = min((CELL - 10) / im.width, (CELL - 10) / im.height, 1.0)
            t = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))))
            sheet.alpha_composite(t, (cx + (CELL - t.width) // 2, cy + (CELL - t.height) // 2))
            nm = e["name"]
            l1, l2 = nm[:22], nm[22:44]
            d.text((cx + 3, cy + CELL), l1, fill=(0, 0, 0), font=f_small)
            d.text((cx + 3, cy + CELL + 13), (l2 + f" {int(e['size'][0])}x{int(e['size'][1])}") if l2 else f"{int(e['size'][0])}x{int(e['size'][1])}", fill=(90, 90, 90), font=f_small)
        fn = os.path.join(OUT, f"{g}_{p+1:02d}.png")
        sheet.convert("RGB").save(fn, optimize=True)
        sheets.append(fn)
print(len(sheets))
for s in sheets: print(os.path.basename(s))
