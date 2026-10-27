#!/usr/bin/env python3
"""Extract every Sprite (.asset, AssetRipper export) into individual PNGs.
Source files are read-only; output goes to /Users/trongnp/BearBakery/_derived.
Records rect, textureRect, offset, pivot, rotation (settingsRaw), PPU into sprites_index.json
"""
import os, re, json, sys
from PIL import Image

SRC = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets"
OUT = "/Users/trongnp/BearBakery/_derived/sprites"
os.makedirs(OUT, exist_ok=True)

# guid -> texture path
tex_by_guid = {}
for root, _, files in os.walk(SRC):
    for f in files:
        if f.endswith(".png.meta") or f.endswith(".jpg.meta") or f.endswith(".tga.meta"):
            p = os.path.join(root, f)
            with open(p, encoding="utf-8", errors="ignore") as fh:
                for line in fh:
                    if line.startswith("guid:"):
                        tex_by_guid[line.split()[1]] = p[:-5]
                        break

def atlas_name(texpath):
    b = os.path.basename(texpath)
    m = re.search(r"Atlas_[A-Za-z_]+?-[0-9a-f]{8}", b)
    if m:
        page = re.match(r"sactx-(\d+)", b).group(1)
        return m.group(0).rsplit("-", 1)[0] + "_p" + page
    return "Tex_" + os.path.splitext(b)[0]

num = r"(-?[0-9.eE+-]+)"
def grab(block, key):
    m = re.search(key + r":\s*\n\s*serializedVersion: 2\s*\n\s*x: " + num + r"\s*\n\s*y: " + num + r"\s*\n\s*width: " + num + r"\s*\n\s*height: " + num, block)
    return tuple(float(v) for v in m.groups()) if m else None

img_cache = {}
index = []
sprite_files = []
for root, _, files in os.walk(SRC):
    if "/Scripts" in root:
        continue
    for f in files:
        if f.endswith(".asset") and not f.endswith(".meta"):
            sprite_files.append(os.path.join(root, f))

for sp in sorted(sprite_files):
    with open(sp, encoding="utf-8", errors="ignore") as fh:
        txt = fh.read()
    if "\nSprite:" not in txt:
        continue
    name = (re.search(r"m_Name:[ ]?(.*)", txt).group(1).strip() or os.path.splitext(os.path.basename(sp))[0])
    rect = grab(txt, "m_Rect")
    rd = txt.split("m_RD:")[1].split("m_AtlasRD:")[0]
    guid = re.search(r"texture: \{fileID: \d+, guid: ([0-9a-f]+)", rd)
    if not guid:
        index.append({"name": name, "asset": os.path.relpath(sp, SRC), "error": "no texture"}); continue
    guid = guid.group(1)
    tex = tex_by_guid.get(guid)
    trect = grab(rd, "textureRect")
    toff = re.search(r"textureRectOffset: \{x: " + num + ", y: " + num + r"\}", rd)
    toff = (float(toff.group(1)), float(toff.group(2))) if toff else (0, 0)
    settings = int(re.search(r"settingsRaw: (\d+)", rd).group(1))
    pivot = re.search(r"m_Pivot: \{x: " + num + ", y: " + num + r"\}", txt)
    pivot = (float(pivot.group(1)), float(pivot.group(2)))
    ppu = float(re.search(r"m_PixelsToUnits: " + num, txt).group(1))
    border = re.search(r"m_Border: \{x: " + num + ", y: " + num + ", z: " + num + ", w: " + num + r"\}", txt)
    border = [float(b) for b in border.groups()] if border else [0, 0, 0, 0]
    packed = settings & 1
    rotation = (settings >> 2) & 0xF
    entry = {"name": name, "asset": os.path.relpath(sp, SRC), "texture": os.path.relpath(tex, SRC) if tex else None,
             "rect": rect, "textureRect": trect, "textureRectOffset": toff, "pivot": pivot, "ppu": ppu,
             "border": border, "settingsRaw": settings, "packed": packed, "rotation": rotation}
    if not tex or not trect or trect[2] < 1 or trect[3] < 1:
        entry["error"] = "missing texture or empty rect"; index.append(entry); continue
    if tex not in img_cache:
        img_cache[tex] = Image.open(tex).convert("RGBA")
    im = img_cache[tex]
    W, H = im.size
    x, y, w, h = trect
    # Unity texture coords: origin bottom-left
    box = (int(round(x)), int(round(H - y - h)), int(round(x + w)), int(round(H - y)))
    crop = im.crop(box)
    if rotation == 1: crop = crop.transpose(Image.FLIP_LEFT_RIGHT)
    elif rotation == 2: crop = crop.transpose(Image.FLIP_TOP_BOTTOM)
    elif rotation == 3: crop = crop.transpose(Image.ROTATE_180)
    elif rotation == 4: crop = crop.transpose(Image.ROTATE_270)
    # place trimmed crop into full sprite rect canvas (source size)
    fw, fh = int(round(rect[2])), int(round(rect[3]))
    if (fw, fh) != crop.size and fw >= crop.size[0] and fh >= crop.size[1]:
        canvas = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
        ox, oy = int(round(toff[0])), int(round(toff[1]))
        canvas.paste(crop, (ox, fh - oy - crop.size[1]))
        crop = canvas
        entry["untrimmed"] = True
    group = atlas_name(tex)
    d = os.path.join(OUT, group)
    os.makedirs(d, exist_ok=True)
    safe = re.sub(r"[^A-Za-z0-9_.\-]", "_", name)
    outp = os.path.join(d, safe + ".png")
    n = 1
    while os.path.exists(outp) and outp in {e.get("out") for e in index}:
        outp = os.path.join(d, f"{safe}__{n}.png"); n += 1
    crop.save(outp)
    entry["out"] = outp
    entry["group"] = group
    entry["size"] = crop.size
    index.append(entry)

with open("/Users/trongnp/BearBakery/_derived/sprites_index.json", "w") as fh:
    json.dump(index, fh, indent=1)
from collections import Counter
print("sprites:", len(index), "errors:", sum(1 for e in index if "error" in e))
print("rotations:", Counter(e.get("rotation") for e in index))
print(Counter(e.get("group") for e in index))
