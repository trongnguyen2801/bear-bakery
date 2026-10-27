#!/usr/bin/env python3
"""Side-by-side of the same state at 360x800 / 390x844 / 430x932 -> Docs/screens/compare_<state>.png"""
import sys, os, glob
from PIL import Image, ImageDraw, ImageFont
D = "/Users/trongnp/BearBakery/Docs/screens"
F = ImageFont.truetype("/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets/Font/LiberationSans.ttf", 18)
for state in sys.argv[1:]:
    ims = []
    for r in ["360x800", "390x844", "430x932"]:
        g = glob.glob(f"{D}/{r}_*_{state}.png")
        if g: ims.append((r, Image.open(g[0]).convert("RGB")))
    if not ims: continue
    H = max(i.height for _, i in ims) + 30
    W = sum(i.width for _, i in ims) + 20 * (len(ims) - 1)
    out = Image.new("RGB", (W, H), (40, 30, 25))
    d = ImageDraw.Draw(out); x = 0
    for r, im in ims:
        out.paste(im, (x, 30)); d.text((x + 6, 5), r, fill=(255, 255, 255), font=F); x += im.width + 20
    out.save(f"{D}/compare_{state}.png")
    print(f"{D}/compare_{state}.png")
