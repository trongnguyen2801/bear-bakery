#!/usr/bin/env python3
"""Contact sheet of every sprite integrated in the game, labeled with semantic ID + source sprite asset path.
Pages grouped by category -> Docs/contact_sheets/integrated_<cat>.png"""
import csv, os, math
from PIL import Image, ImageDraw, ImageFont
ROOT = "/Users/trongnp/BearBakery"
FONT = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets/Font/LiberationSans.ttf"
fb = ImageFont.truetype(FONT, 12); fs = ImageFont.truetype(FONT, 10); fh = ImageFont.truetype(FONT, 20)
rows = list(csv.DictReader(open(f"{ROOT}/Docs/asset_manifest.csv")))
out = f"{ROOT}/Docs/contact_sheets"; os.makedirs(out, exist_ok=True)
cats = {}
for r in rows:
    cat = r["unity_resource_png"].split("/")[1]
    cats.setdefault(cat, []).append(r)
CELL, LAB, COLS = 170, 46, 6
def checker(w, h):
    im = Image.new("RGBA", (w, h), (236, 236, 236, 255)); d = ImageDraw.Draw(im)
    for y in range(0, h, 10):
        for x in range(0, w, 10):
            if (x // 10 + y // 10) % 2: d.rectangle([x, y, x + 9, y + 9], fill=(214, 214, 214, 255))
    return im
made = []
for cat, items in sorted(cats.items()):
    items.sort(key=lambda r: r["semantic_id"])
    per = COLS * 6
    for p in range(math.ceil(len(items) / per)):
        chunk = items[p * per:(p + 1) * per]
        rws = math.ceil(len(chunk) / COLS)
        sheet = Image.new("RGBA", (COLS * CELL, 34 + rws * (CELL + LAB)), (255, 255, 255, 255))
        d = ImageDraw.Draw(sheet)
        d.text((8, 7), f"Integrated sprites — category '{cat}' (page {p + 1})  ·  label: semantic id / source .asset", fill=(0, 0, 0), font=fh)
        for i, r in enumerate(chunk):
            cx, cy = (i % COLS) * CELL, 34 + (i // COLS) * (CELL + LAB)
            sheet.paste(checker(CELL - 6, CELL - 6), (cx + 3, cy + 3))
            im = Image.open(f"{ROOT}/Assets/BearBakery/Resources/{r['unity_resource_png']}").convert("RGBA")
            sc = min((CELL - 12) / im.width, (CELL - 12) / im.height, 1.0)
            t = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))))
            sheet.alpha_composite(t, (cx + (CELL - t.width) // 2, cy + (CELL - t.height) // 2))
            # pivot marker
            px = cx + (CELL - t.width) // 2 + float(r["pivot_x"]) * t.width
            py = cy + (CELL - t.height) // 2 + (1 - float(r["pivot_y"])) * t.height
            d.ellipse([px - 3, py - 3, px + 3, py + 3], outline=(220, 30, 30), width=2)
            d.text((cx + 3, cy + CELL), r["semantic_id"][:26], fill=(0, 0, 0), font=fb)
            src = r["source_sprite_asset"]
            d.text((cx + 3, cy + CELL + 15), src[:30], fill=(70, 70, 70), font=fs)
            d.text((cx + 3, cy + CELL + 27), (src[30:58] + f"  {int(float(r['tex_w']))}x{int(float(r['tex_h']))}"), fill=(70, 70, 70), font=fs)
        fn = f"{out}/integrated_{cat}_{p + 1}.png"
        sheet.convert("RGB").save(fn, optimize=True); made.append(fn)
print("\n".join(made))
