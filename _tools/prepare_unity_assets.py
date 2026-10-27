#!/usr/bin/env python3
"""Semantic ID -> real sprite/frame mapping. Copies only the sprites the game uses into the
Unity project (derived copies; sources untouched) and writes:
  Assets/BearBakery/Resources/asset_manifest.json  (runtime: id -> Resources path)
  Assets/BearBakery/Editor/import_settings.json     (pivot/border per copied PNG, original metadata)
  Docs/asset_manifest.csv                           (traceability: id -> source .asset + texture + rect)
"""
import json, os, shutil, csv, re
ROOT = "/Users/trongnp/BearBakery"
SRC = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets"
idx = [e for e in json.load(open(f"{ROOT}/_derived/sprites_index.json")) if "out" in e]

def find(name, group_prefix=None, nth=0):
    c = [e for e in idx if e["name"] == name and (group_prefix is None or e["group"].startswith(group_prefix))]
    if not c:
        raise SystemExit(f"NOT FOUND: {name} {group_prefix}")
    return c[nth]

M = {}  # id -> (name, groupPrefix, category, borderOverride)
def add(i, name, gp=None, cat="misc", border=None):
    M[i] = (name, gp, cat, border)

# ---------- backgrounds / architecture ----------
for i, n in {
    "bg.kitchen.floor": "Img_Pattern001", "bg.shop.floor": "Img_Pattern002", "bg.break.floor": "Sng_Main_Tile001",
    "bg.break.wall": "Wallpaper002", "bg.shop.wall": "Img_CounterWall_Main001", "bg.kitchen.wall": "Img_CounterWall_Main006",
    "bg.white": "Img_White10x10", "bg.line": "Img_Line",
    "bg.shop.facade": "Img_Entry_Main001", "bg.shop.facadeSign": "Img_Entry_Main001_1",
    "bg.kitchen.window": "Img_KitchenWindow_Main001", "bg.kitchen.airpan": "Img_AirPan_Main001",
    "bg.kitchen.fridge": "Img_Refrigerator_Main001", "bg.kitchen.sink": "Img_Simker_Main001",
    "bg.kitchen.table": "Img_KitchenTable_Main001", "bg.kitchen.storage": "Img_KitchenWheeltray_Main001",
    "bg.kitchen.sunlight": "Img_Sunlight",
    "bg.shop.counter": "Img_CounterTable_Main001", "bg.shop.menu": "Img_CounterMenu_Main001",
    "bg.shop.light": "Img_CounterIight_Main001", "bg.shop.calculator": "Img_Calculator001",
    "bg.shop.menuSmall": "Img_Menu001", "bg.shop.milkfridge": "Img_Countermilk_Main001",
    "bg.shop.milk": "Img_Milk_Main001", "bg.street.chair": "Img_EntryChair_Main001",
    "bg.street.bread1": "Img_EntryBread1", "bg.street.bread2": "Img_EntryBread2",
    "bg.break.door": "Img_Loading_Door01", "bg.shop.gradient": "Img_Gradation_basic",
}.items():
    add(i, n, None, "bg")
# ---------- equipment ----------
for i, n in {"equip.oven": "Img_Oven001", "equip.ovenDoorClosed": "Img_OvenDoor001", "equip.ovenDoorOpen": "Img_OvenDoor002",
             "equip.ovenFire": "Img_OvenFire001", "equip.ovenSmoke": "Img_OvenSmoke001",
             "equip.boardSlot": "Img_Table_Merge801", "equip.tablePlain": "Img_Table801", "equip.shelf": "Img_Table_Sell801", "equip.lockSign": "Sng_LockSign"}.items():
    add(i, n, None, "equip")
# ---------- recipes (6-level chain = original main-season order 40001..40006) ----------
CHAIN = ["DinnerRolls", "Bagle", "Croissant", "Pretzel", "Muffin", "StrawberryJamPie"]
for lv, n in enumerate(CHAIN, 1):
    add(f"dough.{lv}", n + "_Dough", "Atlas_Dough", "item")
    add(f"bread.{lv}", n, "Atlas_Bread_", "item")
    add(f"breadBig.{lv}", n, "Atlas_BreadBig", "item")
# ---------- staff ----------
for f in ["Idle01", "Idle02", "Work01", "Work02", "Work03", "Fast01", "Fast02", "Rest01", "Rest02"]:
    add(f"chef.{f}", f"Man_Main_Bear_{f}", None, "char")
for f in ["Idle01", "Idle02", "Work01", "Work02", "Work03", "Work04", "Fast01", "Fast02", "Fast03", "Rest01", "Rest02", "Bread"]:
    add(f"cashier.{f}", f"Man_Main_Lesserpanda_{f}", None, "char")
for f in ["Idle01", "Idle02", "Work01", "Work02", "Fast01", "Fast02", "Rest01", "Rest02"]:
    add(f"promoter.{f}", f"Man_Main_Sparrow_{f}", None, "char")
# ---------- delivery dog + raccoon cleaner (NPC staff) ----------
for f in ["Idle01", "Idle02", "Walk01", "Walk02"]:
    add(f"npc.dog.{f}", f"Man_Main_Dog_{f}", None, "char")
    add(f"npc.raccoon.{f}", f"Man_Main_Raccoon_{f}", None, "char")
for k, n in enumerate(["Img_Trash_Banana", "Img_Trash_IronCan", "Img_Trash_Leaflet", "Img_Trash_PaperCup"]):
    add(f"trash.{k}", n, None, "bg")
add("bg.shop.deliverySign", "Image_DeliverySign", None, "bg")
add("bg.shop.deliveryDog", "Img_Delivery_Dog", None, "bg")
# ---------- customers (6 of 10 available main-theme animals) ----------
CUST = ["Cat", "Giraffe", "Pig", "Rabbit", "Penguin", "Lion"]
for c in CUST:
    for f in ["Idle01", "Hold01", "Pickwalk01", "Pickwalk02", "Walk01", "Walk02"]:
        add(f"cust.{c}.{f}", f"Cha_Main_{c}_{f}", None, "char")
for i, n in {"char.shadow": "Cha_Shadow", "prop.trayBread": "Cha_Tray_Bread01", "prop.trayBreadOutline": "Cha_Tray_Bread_Outline",
             "prop.trayEmpty": "Cha_Tray_Empty01", "prop.bag": "Cha_Breadbag01", "prop.bagOutline": "Cha_Breadbag_Outline",
             "prop.receipt": "Cha_Print01", "bubble.heart": "Cha_Bubble_Heart", "bubble.issue": "Cha_Bubble_Issue", "bubble.sad": "Cha_Bubble_Sad"}.items():
    add(i, n, None, "char")
# ---------- furniture (break room) ----------
for i, n in {"furn.beanbag": "BeanBagChair", "furn.sofa": "Sofa", "furn.bed": "Bed", "furn.coffee": "CoffeeMachine",
             "furn.lamp": "FloorLamp", "furn.rug": "LawnRug", "furn.plant": "WallPlanter", "furn.table": "WoodenTable"}.items():
    add(i, n, "Atlas_Furniture", "furniture")
# ---------- UI ----------
ui = {"ui.coin": "UI_Coin", "ui.coinSmall": "UI_Coin_Small", "ui.btnBasic": "UI_Btn_Basic", "ui.popupIn": "UI_Popup_InColor",
      "ui.popupLineOut": "UI_PopupLine_OutColor", "ui.popupLineMid": "UI_PopupLine_MidColor", "ui.popupLineIn": "UI_PopupLine_InColor",
      "ui.btnLine": "UI_BtnLine", "ui.close": "UI_Btn_Close", "ui.check": "UI_Btn_Check", "ui.lock": "UI_Btn_Locked",
      "ui.tab.kitchen": "UI_Btn_Kitchen", "ui.tab.shop": "UI_Btn_Shop", "ui.tab.furniture": "Icon_FurnitureBuff",
      "ui.tab.book": "UI_Btn_Manager", "ui.quest": "UI_Btn_DailyQuest", "ui.option": "UI_Btn_Option", "ui.level": "UI_Btn_Level",
      "ui.heart": "UI_Heart", "ui.heartSmall": "UI_Heart_Small1", "ui.bubble": "UI_Bubble1", "ui.bubbleRound": "UI_Bubble_Round1",
      "ui.gaugeFill": "UI_Bubble_Guage1", "ui.gaugeFill2": "UI_Bubble_Guage2", "ui.gaugeLine": "UI_Bubble_Line1",
      "ui.hand1": "Image_TutoHand1", "ui.hand2": "Image_TutoHand2", "ui.hand3": "Image_TutoHand3",
      "ui.full": "Img_Full", "ui.new": "UI_New", "ui.clear": "UI_Clear1", "ui.arrowUp": "Icon_BuffArrow", "ui.auto": "Icon_Bubble_Auto",
      "ui.construction": "UI_Construction", "ui.hammer": "UI_Hammer", "ui.piggyEmpty": "UI_Icon_Piggy_Empty",
      "ui.piggyIng": "UI_Icon_Piggy_Ing", "ui.piggyFull": "UI_Icon_Piggy_Full", "ui.sound": "Icon_Option_Sound",
      "ui.music": "Icon_Option_Music", "ui.label": "UI_Popup_LabelColor1", "ui.postit": "UI_Postit", "ui.logo": "UI_Popup_Logo1",
      "ui.redDot": "UI_Reddot01", "ui.issue": "UI_Issue", "ui.plus": "UI_Plus1", "ui.question": "UI_Bubble_Question",
      "ui.inventory": "UI_QuestIcon_Inventory", "ui.qOven": "UI_QuestIcon_Oven", "ui.qStand": "UI_QuestIcon_Stand",
      "ui.qTray": "UI_QuestIcon_Tray", "ui.qBear": "UI_QuestIcon_Bear", "ui.qLesser": "UI_QuestIcon_Lesserpanda",
      "ui.qSparrow": "UI_QuestIcon_Sparrow", "ui.buff.dough": "80275", "ui.buff.oven": "80289", "ui.buff.coin": "80273",
      "ui.buff.furniture": "80269", "ui.buff.cashier": "80271", "ui.buff.sparrow": "80299", "ui.store": "UI_Btn_Store",
      "ui.stamp": "UI_Image_Stamp02", "ui.patternCoin": "Pattern_Coin", "ui.gaugeCircle": "UI_Gauge_Circle",
      "ui.lineColor": "UI_Line_Color", "ui.labelSide": "UI_Label_SideColor", "ui.invenLabel": "UI_Popup_InvenLabel_Color",
      "ui.sameIcon": "UI_Same1", "ui.arrow": "UI_Arrow1", "ui.arrowBlock": "UI_Arrow_Small1", "ui.clearLabel": "UI_Clear1",
      "ui.boost.chef": "UI_IconBoost_Bear01", "ui.boost.chef2": "UI_IconBoost_Bear02", "ui.boost.cashier": "UI_IconBoost_Lessa01",
      "ui.boost.cashier2": "UI_IconBoost_Lessa02", "ui.boost.promoter": "UI_IconBoost_Sparrow01", "ui.boost.promoter2": "UI_IconBoost_Sparrow02",
      "ui.qDog": "UI_QuestIcon_Dog", "ui.qRaccoon": "UI_QuestIcon_Raccoon", "ui.box": "Icon_Shop_Box", "ui.treasure": "UI_Delivery_TresureBox",
      "ui.btnShop": "UI_Btn_Shop", "ui.whiteCircle": "UI_White_Circle100x100", "ui.whiteRound": "UI_White_Round10X10", "ui.gaugeIn": "UI_Gauge_InCircle", "ui.dotLine": "UI_DotLine", "ui.signX": "UI_Sign_X", "ui.signO": "UI_Sign_O",
      "fx.mergeDust": "Img_MergeDust01", "fx.star": "Image_StarEffect", "fx.twinkle1": "Img_Twinkle01", "fx.twinkle2": "Img_Twinkle02",
      "fx.spin": "Img_SpinEffect01", "fx.zzz": "Image_RouletteBBangda03", "fx.glow": "Img_Gradation"}
for i, n in ui.items():
    add(i, n, "Atlas_CanvasUI" if not i.startswith("ui.questIcon") else None, "ui")
# questIcons for customers
for c in CUST:
    add(f"ui.qc.{c}", f"UI_QuestIcon_{c}", "Atlas_CanvasUI", "ui")
for c, n in {"Bear": "Illust_Bear01", "Lesserpanda": "Illust_Lesserpanda01", "Sparrow": "Illust_Sparrow01", "Cat": "Illust_Cat01",
             "Giraffe": "Illust_Giraffe01", "Pig": "Illust_Pig01", "Rabbit": "Illust_Rabbit01", "Penguin": "Illust_Penguin01",
             "Lion": "Illust_Lion01", "NewBread": "Illust_NewBread1"}.items():
    add(f"illust.{c}", n, "Atlas_CanvasUI", "ui")

# ---------- write ----------
ART = f"{ROOT}/Assets/BearBakery/Resources/Art"
if os.path.isdir(ART):
    for r, ds, fs in os.walk(ART):
        for f in fs:
            if f.endswith(".png"): os.remove(os.path.join(r, f))
os.makedirs(ART, exist_ok=True)
manifest, imports, rows = [], {}, []
for i, (name, gp, cat, border) in M.items():
    gp2 = gp
    if gp == "Atlas_CanvasUI":
        c = [e for e in idx if e["name"] == name and e["group"].startswith("Atlas_CanvasUI")]
        if not c: gp2 = None
    e = find(name, gp2)
    rel_dir = f"{cat}"
    os.makedirs(f"{ART}/{rel_dir}", exist_ok=True)
    fname = re.sub(r"[^A-Za-z0-9_.\-]", "_", f"{e['name']}__{e['group']}") + ".png"
    dst = f"{ART}/{rel_dir}/{fname}"
    shutil.copyfile(e["out"], dst)
    res = f"Art/{rel_dir}/{fname[:-4]}"
    bd = border if border else e["border"]
    manifest.append({"id": i, "res": res, "w": e["size"][0], "h": e["size"][1], "pivot": e["pivot"], "border": bd})
    imports[f"Assets/BearBakery/Resources/{res}.png"] = {"pivot": e["pivot"], "border": bd, "ppu": e["ppu"]}
    rows.append([i, res + ".png", e["name"], e["asset"], e["texture"], *[round(v, 2) for v in e["textureRect"]], *e["pivot"], e["ppu"], "|".join(str(b) for b in bd)])
os.makedirs(f"{ROOT}/Assets/BearBakery/Editor", exist_ok=True)
json.dump({"items": manifest}, open(f"{ROOT}/Assets/BearBakery/Resources/asset_manifest.json", "w"), indent=1)
json.dump({"items": [dict(path=k, **v) for k, v in imports.items()]}, open(f"{ROOT}/Assets/BearBakery/Editor/import_settings.json", "w"), indent=1)
os.makedirs(f"{ROOT}/Docs", exist_ok=True)
with open(f"{ROOT}/Docs/asset_manifest.csv", "w", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(["semantic_id", "unity_resource_png", "source_sprite_name", "source_sprite_asset", "source_texture", "tex_x", "tex_y", "tex_w", "tex_h", "pivot_x", "pivot_y", "ppu", "border_LBRT"])
    w.writerows(sorted(rows))
# audio + fonts (copied verbatim)
AUD = f"{ROOT}/Assets/BearBakery/Resources/Audio"; os.makedirs(AUD, exist_ok=True)
for f in ["bgm_Main", "se_Etc_Bubble", "se_Etc_Click", "se_Etc_GetCoin", "se_Etc_GetReward", "se_Etc_SetFurniture", "se_Etc_Speak_Bear",
          "se_Etc_Speak_Lesserpanda", "se_Etc_Speak_Sparrow", "se_Etc_Upgrade", "se_Etc_UseItem", "se_Kitchen_Dough", "se_Kitchen_DoughSave",
          "se_Kitchen_OvenConstructionCompletion", "se_Kitchen_OvenDone", "se_Kitchen_OvenStart", "se_Kitchen_TrayConstructionCompletion",
          "se_Shop_OpenInventory", "se_Shop_Sell", "se_Shop_SetBreadToStand", "se_Store_StandConstructionCompletion", "se_Store_Purchase", "se_Store_Fanfare", "se_Etc_GetTicket"]:
    shutil.copyfile(f"{SRC}/AudioClip/{f}.ogg", f"{AUD}/{f}.ogg")
FNT = f"{ROOT}/Assets/BearBakery/Resources/Fonts"; os.makedirs(FNT, exist_ok=True)
for f in ["LiberationSans.ttf", "KATURI.ttf"]:
    shutil.copyfile(f"{SRC}/Font/{f}", f"{FNT}/{f}")
print("sprites:", len(manifest))
