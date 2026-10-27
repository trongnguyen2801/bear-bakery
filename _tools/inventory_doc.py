#!/usr/bin/env python3
"""Writes Docs/asset_inventory.md (what exists in the export) and Docs/anim_timings.md (frame timings read from .anim)."""
import os, json, collections
SRC = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets"
ROOT = "/Users/trongnp/BearBakery"
idx = json.load(open(f"{ROOT}/_derived/sprites_index.json"))
lines = ["# Kiểm kê bộ asset (tự động)", "",
         f"Nguồn: `{SRC}` (AssetRipper export, Unity 6000.2). Nguồn **không bị sửa**; mọi bản trích/xử lý nằm ở `_derived/` và `Assets/BearBakery/Resources/`.", "",
         "## Thư mục và số file (không tính .meta)", "", "| Thư mục | Số file | Dung lượng | Nhóm |", "|---|---|---|---|"]
groups = {"AnimationClip": "animation metadata (.anim)", "AnimatorController": "animator", "AudioClip": "âm thanh (.ogg)", "Font": "font (.ttf)",
          "GameObject": "prefab UI/VFX", "Material": "material", "Resources": "data bảng (txt), prefab màn chơi/nội thất/UI, font SDF", "Scenes": "GameScene.unity",
          "Scripts": "mã đã decompile (không dùng)", "Shader": "shader TMP/UI", "Sprite": "Sprite (.asset) tham chiếu atlas", "Texture2D": "atlas PNG + texture nền",
          "Plugins": "plugin bên thứ ba (không dùng)", "AssetBundles": "bundle (không dùng)", "MonoBehaviour": "localization settings"}
for d in sorted(os.listdir(SRC)):
    p = os.path.join(SRC, d)
    if not os.path.isdir(p): continue
    n = sz = 0
    for r, _, fs in os.walk(p):
        for f in fs:
            if f.endswith(".meta"): continue
            n += 1; sz += os.path.getsize(os.path.join(r, f))
    lines.append(f"| {d} | {n} | {sz / 1e6:.1f} MB | {groups.get(d, '')} |")
lines += ["", "## Atlas (sprite đã trích theo metadata Sprite.asset: m_Rect/textureRect/offset/pivot/border/settingsRaw)", "",
          "| Atlas trang | Số sprite | Nội dung chính (xem contact sheet) |", "|---|---|---|"]
desc = {"Atlas_Bread": "bánh thành phẩm cỡ nhỏ (icon kệ/kho)", "Atlas_BreadBig": "bánh cỡ lớn (sổ công thức)", "Atlas_Dough": "bột theo từng loại bánh",
        "Atlas_Characters": "khách (Idle/Hold/Walk/Pickwalk), nhân viên (Idle/Work/Fast/Rest), bubble, khay, túi", "Atlas_Furniture_Normal": "nội thất các mùa",
        "Atlas_SpritesBG": "kiến trúc: tường, bàn bếp, lò, kệ, quầy, cửa tiệm", "Atlas_CanvasUI": "UI: nút, icon, khung popup, minh họa nhân vật", "Atlas_ShopUI": "UI cửa hàng IAP", "Atlas_GachaUI": "UI gacha"}
c = collections.Counter(e["group"] for e in idx if "out" in e)
for g, n in sorted(c.items()):
    base = g.rsplit("_p", 1)[0]
    lines.append(f"| {g} | {n} | {desc.get(base, 'texture rời (nền/tường/sàn theo mùa)')} |")
lines += ["", f"Tổng: **{len(idx)} sprite**, 0 lỗi trích, rotation đóng gói = 0 cho mọi sprite (đã kiểm tra settingsRaw).", "",
          "## Âm thanh", ""]
aud = sorted(f for f in os.listdir(f"{SRC}/AudioClip") if f.endswith(".ogg"))
lines.append(", ".join(f"`{a}`" for a in aud))
lines += ["", "## Font", "", "- `KATURI.ttf` (ONE Mobile POP): **thiếu 134 ký tự tiếng Việt có dấu** → chỉ dùng cho số (tiền, cấp).",
          "- `LiberationSans.ttf`: đủ toàn bộ dấu tiếng Việt → dùng cho mọi chữ tiếng Việt.", "",
          "## Dữ liệu bảng gốc đã đọc để lấy *thứ tự* (không lấy thông số)", "",
          "- `Resources/data/Bread.txt`: chuỗi bánh mùa chính 40001–40006 = DinnerRolls → Bagle → Croissant → Pretzel → Muffin → StrawberryJamPie (dùng làm chuỗi 6 cấp).",
          "- `Character.txt` + `String.txt`: tên khách (Cheese, Giffie, Piggy, Bunnie, Penny, Symba) và nhân viên (Mr. Bear, Amber, Tweetie).",
          "- `Furniture.txt`: danh sách nội thất mùa chính (Bed, Sofa, BeanBagChair, CoffeeMachine, FloorLamp, LawnRug ...).",
          "- `prefabs/ingameui/OvenSlot|MergeTray|StandSlot.prefab`: vị trí cửa lò/lửa/khói/bong bóng, bột trên khay, 5 vị trí bánh trên kệ.",
          "- `prefabs/Field_Merge|Field_Store|Field_Sale.prefab`: bố cục tương đối gấu–bàn bếp, quầy–thu ngân, khách–khay/túi.", ""]
open(f"{ROOT}/Docs/asset_inventory.md", "w").write("\n".join(lines))

meta = json.load(open(f"{ROOT}/_derived/anim_metadata.json"))
sel = ["Bear_Idle", "Bear_Work", "Bear_Fast", "Bear_Rest", "LesserPanda_Idle", "LesserPanda_Work", "LesserPanda_Fast", "LesserPanda_Rest",
       "Sparrow_Idle", "Sparrow_Work", "Sparrow_Fast", "Sparrow_Rest", "Walk", "Chracters_Pickwalk", "Chracters_Pickhold", "Chracters_Idle",
       "Oven_On", "Oven_Off", "Oven_Complete", "Furniture", "Earn_Money", "Racco_Idle", "Racco_PickWalk"]
out = ["# Animation metadata đọc từ AnimationClip gốc", "",
       "Nhân vật trong bộ asset là **animation theo frame** (mỗi frame là một GameObject con bật/tắt bằng curve `m_IsActive`). Bảng dưới là các mốc bật/tắt đã đọc; game dùng đúng các mốc này (FrameAnim.cs).",
       "Chỉ có **một hướng nhìn** (chính diện, hơi nghiêng trái) → game dùng `flipX` khi đi sang phải; không có frame đi lùi/quay lưng.", ""]
for n in sel:
    m = meta.get(n)
    if not m: continue
    out.append(f"## {n}  (`{m['file']}`, dài {m['length']:.3f}s, loop={m['loop']}, sampleRate {m['sampleRate']:.0f})")
    for cv in m["curves"]:
        ks = ", ".join(f"{k[0]:.3f}s→{k[1] if not isinstance(k[1], list) else '(' + ','.join(f'{x:.2f}' for x in k[1][:2]) + ')'}" for k in cv["keys"][:9])
        out.append(f"- `{cv['path']}` {cv['attr'] or cv['type']}: {ks}")
    out.append("")
open(f"{ROOT}/Docs/anim_timings.md", "w").write("\n".join(out))
print("ok")
