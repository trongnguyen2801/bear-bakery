#!/usr/bin/env python3
"""usage: sheet_by_names.py out.png regex [cols] [cell]  -> labeled contact sheet of sprites whose name matches regex"""
import json, sys, re, math
from PIL import Image, ImageDraw, ImageFont
idx = [e for e in json.load(open("/Users/trongnp/BearBakery/_derived/sprites_index.json")) if "out" in e]
out, pat = sys.argv[1], re.compile(sys.argv[2])
COLS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
CELL = int(sys.argv[4]) if len(sys.argv) > 4 else 120
FONT = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets/Font/LiberationSans.ttf"
f = ImageFont.truetype(FONT, 10)
items = sorted([e for e in idx if pat.search(e["name"])], key=lambda e: e["name"])
seen = set(); uniq = []
for e in items:
    if e["name"] in seen: continue
    seen.add(e["name"]); uniq.append(e)
items = uniq
LAB = 26
rows = math.ceil(len(items) / COLS)
sheet = Image.new("RGBA", (COLS * CELL, rows * (CELL + LAB)), (255, 255, 255, 255))
d = ImageDraw.Draw(sheet)
for i, e in enumerate(items):
    cx, cy = (i % COLS) * CELL, (i // COLS) * (CELL + LAB)
    d.rectangle([cx + 2, cy + 2, cx + CELL - 3, cy + CELL - 3], fill=(225, 225, 225, 255))
    im = Image.open(e["out"])
    sc = min((CELL - 8) / im.width, (CELL - 8) / im.height, 1.0)
    t = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))))
    sheet.alpha_composite(t, (cx + (CELL - t.width) // 2, cy + (CELL - t.height) // 2))
    n = e["name"]
    d.text((cx + 2, cy + CELL), n[:24], fill=(0, 0, 0), font=f)
    d.text((cx + 2, cy + CELL + 12), (n[24:44] + " " if len(n) > 24 else "") + f"{e['size'][0]}x{e['size'][1]}", fill=(80, 80, 80), font=f)
sheet.convert("RGB").save(out)
print(len(items), "items ->", out)
