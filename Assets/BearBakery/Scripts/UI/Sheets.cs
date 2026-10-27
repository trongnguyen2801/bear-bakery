// Bottom sheets (upgrades, shop, furniture, notebook, shelf picker), popups, tutorial hand.
using System;
using System.Collections.Generic;
using System.Linq;
using BB.Model;
using BB.View;
using UnityEngine;
using UnityEngine.UI;

namespace BB.UI
{
    public partial class UIRoot
    {
        RectTransform sheetRoot, sheetPanel, sheetContent;
        SheetKind sheetKind = SheetKind.None; int sheetArg = -1; string sheetSArg; int notebookTab;
        readonly List<Action> refreshers = new List<Action>();
        public bool SheetOpen => sheetKind != SheetKind.None;
        public bool ModalOpen => modal != null;
        RectTransform modal;
        bool resetArmed;

        public void OpenSheet(SheetKind kind, int arg = -1, string sarg = null)
        {
            bool wasOpen = SheetOpen;
            DestroySheet();
            sheetKind = kind; sheetArg = arg; sheetSArg = sarg;
            if (kind == SheetKind.Notebook && arg >= 0) notebookTab = arg;
            BuildSheet(!wasOpen);
        }

        public void CloseSheet()
        {
            if (!SheetOpen) return;
            var p = sheetPanel; var r = sheetRoot;
            sheetKind = SheetKind.None;
            refreshers.Clear();
            if (p != null)
            {
                float h = p.sizeDelta.y;
                Tw.Run(p, 0.18f, t => { if (p) p.anchoredPosition = new Vector2(0, -h * t); }, Ease.InQuad, () => { if (r) Destroy(r.gameObject); });
            }
            sheetRoot = null; sheetPanel = null; resetArmed = false;
        }

        void DestroySheet()
        {
            if (sheetRoot != null) Destroy(sheetRoot.gameObject);
            sheetRoot = null; sheetPanel = null; refreshers.Clear();
        }

        public void RebuildSheet()
        {
            if (!SheetOpen) return;
            float scroll = sheetContent != null ? sheetContent.anchoredPosition.y : 0;
            var k = sheetKind; DestroySheet(); sheetKind = k; BuildSheet(false);
            if (sheetContent != null) sheetContent.anchoredPosition = new Vector2(0, scroll);
        }

        void RefreshSheet() { foreach (var r in refreshers) r(); }

        void BuildSheet(bool animate)
        {
            sheetRoot = K.Stretch(SheetLayer, "Sheet");
            var dim = K.Stretch(sheetRoot, "Dim");
            var dimImg = K.Img(dim, "bg.white", new Color(0.15f, 0.08f, 0.03f, 0.35f), false, true, false);
            var dimBtn = dim.gameObject.AddComponent<Button>(); dimBtn.transition = Selectable.Transition.None; dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener(CloseSheet);
            float frameH = Frame.rect.height;
            float h = Mathf.Min(frameH * 0.6f, 560f);
            sheetPanel = K.Rect(sheetRoot, "Panel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, h + BottomInsetPx / Scale - BarH));
            var fill = K.Stretch(sheetPanel, "Fill", 4, 4, 4, 0);
            K.Img(fill, "bg.white", Pal.Cream, false, true, false);
            var outline = K.Stretch(sheetPanel, "Outline", -2, -2, -2, -40);
            K.Img(outline, "ui.popupLineOut", Pal.BrownSoft, true);
            // header
            var head = K.Rect(sheetPanel, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-16, 48));
            var title = K.Txt(head, "Title", SheetTitle(), 20, Pal.Brown, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(150, 0), new Vector2(280, 40), true);
            var close = K.At(head, "Close", new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(46, 46));
            K.Btn(close, "ui.close", CloseSheet);
            float top = 56;
            if (sheetKind == SheetKind.Notebook)
            {
                string[] tabNames = { "Nhiệm vụ", "Công thức", "Khách quen" };
                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    var tb = K.Rect(sheetPanel, "NbTab" + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10 + i * 122, -54), new Vector2(116, 40));
                    K.Btn(tb, "ui.btnBasic", () => { notebookTab = idx; RebuildSheet(); }, i == notebookTab ? Pal.Orange : Pal.CreamDark, true);
                    K.Txt(tb, "T", tabNames[i], 14, i == notebookTab ? Color.white : Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(116, 36), true);
                }
                top = 100;
            }
            // scroll list
            var view = K.Rect(sheetPanel, "View", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            view.offsetMin = new Vector2(10, BottomInsetPx / Scale - BarH + 8); view.offsetMax = new Vector2(-10, -top);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = K.Img(view, "bg.white", new Color(1, 1, 1, 0.001f), false, true, false);
            sheetContent = K.Rect(view, "Content", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0));
            var vlg = sheetContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.childControlHeight = false; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(0, 0, 4, 12);
            var fit = sheetContent.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = sheetContent; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30;
            sr.viewport = view;
            switch (sheetKind)
            {
                case SheetKind.Kitchen: BuildKitchenSheet(); break;
                case SheetKind.Shop: BuildShopSheet(); break;
                case SheetKind.Furniture: BuildFurnitureSheet(); break;
                case SheetKind.Notebook: BuildNotebook(); break;
                case SheetKind.ShelfPick: BuildShelfPick(); break;
                case SheetKind.Inventory: BuildInventory(); break;
                case SheetKind.Settings: BuildSettings(); break;
                case SheetKind.Delivery: BuildDeliverySheet(); break;
                case SheetKind.Cleaner: BuildCleanerSheet(); break;
            }
            RefreshSheet();
            if (animate)
            {
                var p = sheetPanel; float hh = p.sizeDelta.y;
                p.anchoredPosition = new Vector2(0, -hh);
                Tw.Run(p, 0.22f, t => { if (p) p.anchoredPosition = new Vector2(0, -hh * (1 - t)); }, Ease.OutCubic);
            }
        }

        string SheetTitle()
        {
            switch (sheetKind)
            {
                case SheetKind.Kitchen: return "Nâng cấp Bếp";
                case SheetKind.Shop: return "Nâng cấp Tiệm";
                case SheetKind.Furniture: return "Nội thất & Nhân viên";
                case SheetKind.Notebook: return "Sổ tay";
                case SheetKind.ShelfPick: return $"Bày bánh lên kệ {sheetArg + 1}";
                case SheetKind.Inventory: return "Kho bánh";
                case SheetKind.Settings: return "Cài đặt";
                case SheetKind.Delivery: return "Giao hàng";
                case SheetKind.Cleaner: return "Dọn rác";
            }
            return "";
        }

        // ------------------------------------------------------------------ row builder
        const float RowRightPad = 114;
        class RowRefs { public RectTransform row; public Text title, desc, btnText; public Button btn; public Image btnImg, icon, coin; }

        RowRefs Row(string icon, string title, string desc, float height = 86)
        {
            var r = new RowRefs();
            r.row = K.Rect(sheetContent, "Row", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height));
            K.Img(r.row, "ui.btnBasic", Color.white, true);
            if (icon != null) r.icon = K.Pic(r.row, "Icon", icon, new Vector2(0, 0.5f), new Vector2(40, 2), new Vector2(60, 58));
            float tx = icon != null ? 78 : 14;
            r.title = K.Txt(r.row, "Title", title, 16, Pal.Brown, TextAnchor.UpperLeft, new Vector2(0, 1), Vector2.zero, Vector2.zero, true);
            var trt = r.title.rectTransform; trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
            trt.offsetMin = new Vector2(tx, -32); trt.offsetMax = new Vector2(-RowRightPad, -9);
            r.desc = K.Txt(r.row, "Desc", desc, 13, Pal.BrownSoft, TextAnchor.UpperLeft, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var drt = r.desc.rectTransform; drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(1, 1); drt.pivot = new Vector2(0, 1);
            drt.offsetMin = new Vector2(tx, 6); drt.offsetMax = new Vector2(-RowRightPad, -33);
            r.desc.verticalOverflow = VerticalWrapMode.Truncate;
            r.desc.resizeTextForBestFit = true; r.desc.resizeTextMinSize = 10; r.desc.resizeTextMaxSize = 13;
            r.title.horizontalOverflow = HorizontalWrapMode.Overflow;
            r.title.resizeTextForBestFit = true; r.title.resizeTextMinSize = 12; r.title.resizeTextMaxSize = 16; r.title.verticalOverflow = VerticalWrapMode.Truncate;
            r.title.horizontalOverflow = HorizontalWrapMode.Wrap;
            return r;
        }

        void RowButton(RowRefs r, Action onClick, float width = 96)
        {
            var b = K.At(r.row, "Btn", new Vector2(1, 0.5f), new Vector2(-width / 2 - 8, 0), new Vector2(width, 48));
            r.btn = K.Btn(b, "ui.btnBasic", onClick, Pal.Green, true);
            r.btnImg = (Image)r.btn.targetGraphic;
            r.coin = K.Pic(b, "Coin", "ui.coin", new Vector2(0, 0.5f), new Vector2(18, 1), new Vector2(24, 24));
            r.btnText = K.Txt(b, "T", "", 15, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(10, 1), new Vector2(width - 24, 44), true, false, true);
        }

        /// <summary>Configure a buy button: cost < 0 => maxed/hidden; level gate shows lock.</summary>
        void SetBuy(RowRefs r, int cost, int levelReq, string maxText = "Tối đa", bool available = true)
        {
            if (cost < 0 || !available)
            {
                r.btnText.text = maxText; r.coin.enabled = false; r.btn.interactable = false; r.btnImg.color = Pal.Grey;
                r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                return;
            }
            r.btnText.rectTransform.anchoredPosition = new Vector2(10, 1);
            if (M.ShopLevel < levelReq)
            {
                r.coin.enabled = true; r.coin.sprite = Art.S("ui.lock");
                r.btnText.text = "Cấp " + levelReq; r.btn.interactable = true; r.btnImg.color = Pal.Grey;
                return;
            }
            r.coin.enabled = true; r.coin.sprite = Art.S("ui.coin");
            r.btnText.text = cost == 0 ? "Miễn phí" : Fmt(cost);
            bool can = M.S.currency >= cost;
            r.btn.interactable = true;
            r.btnImg.color = can ? Pal.Green : new Color(0.75f, 0.68f, 0.6f);
        }

        // ------------------------------------------------------------------ kitchen
        void BuildKitchenSheet()
        {
            var chef = Row("ui.buff.dough", "", "");
            RowButton(chef, () => M.BuyUpgrade("chef"));
            refreshers.Add(() =>
            {
                int lv = M.UpLevel("chef"); var u = M.C.Upgrade("chef");
                chef.title.text = $"{u.nameVi} · Cấp {lv}";
                float cur = M.C.chef.intervalByLevel[lv - 1];
                string nx = lv < u.maxLevel ? $" → {M.C.chef.intervalByLevel[lv]:0.#}s" : "";
                float bc = M.C.chef.bonusChanceByLevel[lv - 1];
                chef.desc.text = $"1 bột mỗi {cur:0.#}s{nx}" + (bc > 0 ? $" · {bc * 100:0}% ra bột cấp 2" : lv < u.maxLevel && M.C.chef.bonusChanceByLevel[lv] > 0 ? " · mở bột cấp 2 ngẫu nhiên" : "");
                SetBuy(chef, M.UpgradeCost("chef"), 1);
            });
            var oven = Row("ui.buff.oven", "", "");
            RowButton(oven, () => M.BuyUpgrade("oven"));
            refreshers.Add(() =>
            {
                int lv = M.UpLevel("oven"); var u = M.C.Upgrade("oven");
                oven.title.text = $"{u.nameVi} · Cấp {lv}";
                oven.desc.text = $"Thời gian nướng ×{u.values[lv - 1]:0.00}" + (lv < u.maxLevel ? $" → ×{u.values[lv]:0.00}" : "") + " (áp dụng cho mẻ mới)";
                SetBuy(oven, M.UpgradeCost("oven"), 1);
            });
            var newOven = Row("ui.qOven", "", "Nướng nhiều mẻ cùng lúc. Lò mới xuất hiện trong bếp.");
            RowButton(newOven, () => { if (M.BuyExpansion(1)) { CloseSheet(); CameraRig.I.Focus(WL.FocusKitchen); } });
            refreshers.Add(() =>
            {
                newOven.title.text = $"Lò nướng mới ({M.OwnedOvens}/{M.C.ovens.max})";
                int c = M.ExpansionCost(1, out int req); SetBuy(newOven, c, req, "Đủ lò");
            });
            var slot = Row("ui.qTray", "", "Thêm chỗ đặt bột trên bàn chuẩn bị.");
            RowButton(slot, () => M.BuyExpansion(0));
            refreshers.Add(() =>
            {
                slot.title.text = $"Ô bàn chuẩn bị ({M.S.boardUnlocked}/{M.C.board.max})";
                int c = M.ExpansionCost(0, out int req); SetBuy(slot, c, req, "Đủ ô");
            });
        }

        // ------------------------------------------------------------------ shop
        void BuildShopSheet()
        {
            var cap = Row("ui.qStand", "", "");
            RowButton(cap, () => M.BuyUpgrade("capacity"));
            refreshers.Add(() =>
            {
                int lv = M.UpLevel("capacity"); var u = M.C.Upgrade("capacity");
                cap.title.text = $"{u.nameVi} · Cấp {lv}";
                cap.desc.text = $"Kệ {u.values[lv - 1]:0}" + (lv < u.maxLevel ? $"→{u.values[lv]:0}" : "") + $" chiếc · Kho {u.values2[lv - 1]:0}" + (lv < u.maxLevel ? $"→{u.values2[lv]:0}" : "");
                SetBuy(cap, M.UpgradeCost("capacity"), 1);
            });
            var shelf = Row("equip.shelf", "", "Thêm kệ trưng bày: khách có nhiều lựa chọn hơn.");
            RowButton(shelf, () => { if (M.BuyExpansion(2)) { CloseSheet(); CameraRig.I.Focus(WL.FocusShop); } });
            refreshers.Add(() =>
            {
                shelf.title.text = $"Kệ trưng bày ({M.OwnedShelves}/{M.C.shelves.max})";
                int c = M.ExpansionCost(2, out int req); SetBuy(shelf, c, req, "Đủ kệ");
            });
            var auto = Row("ui.auto", "Tự bổ sung kệ", "Kệ tự lấy bánh cùng loại từ kho khi vơi. Không tạo bánh mới.");
            RowButton(auto, () => M.UnlockAutoRestock());
            refreshers.Add(() =>
            {
                if (M.S.autoRestockUnlocked) { SetBuy(auto, -1, 1, "Đã mở"); auto.desc.text = "Bật/tắt riêng từng kệ bên dưới (hoặc chạm kệ)."; }
                else if (!M.CanUnlockAutoRestock) { SetBuy(auto, -1, 1, "Bán 1 bánh"); }
                else SetBuy(auto, M.C.economy.autoRestockCost, 1);
            });
            if (M.S.autoRestockUnlocked)
                for (int i = 0; i < GameModel.ShelfSlots; i++)
                {
                    if (!M.S.shelfStock[i].owned) continue;
                    int idx = i;
                    var r = Row(null, "", "", 64);
                    RowButton(r, () => M.SetAuto(idx, !M.S.shelfStock[idx].auto), 110);
                    refreshers.Add(() =>
                    {
                        var s = M.S.shelfStock[idx];
                        r.title.text = $"Kệ {idx + 1}: " + (s.recipe > 0 ? M.C.Recipe(s.recipe).nameVi : "chưa bày");
                        r.desc.text = $"Đang có {s.stock} chiếc";
                        r.coin.enabled = false; r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                        r.btnText.text = s.auto ? "Tự động: Bật" : "Tự động: Tắt";
                        r.btnImg.color = s.auto ? Pal.Green : Pal.Grey;
                    });
                }
        }

        // ------------------------------------------------------------------ furniture + staff
        void BuildFurnitureSheet()
        {
            foreach (var sid in GameModel.StaffIds)
            {
                string id = sid;
                string ill = id == "chef" ? "illust.Bear" : id == "cashier" ? "illust.Lesserpanda" : "illust.Sparrow";
                string nm = id == "chef" ? "Gấu (đầu bếp)" : id == "cashier" ? "Amber (thu ngân)" : "Tweetie (mời khách)";
                var r = Row(ill, nm, "", 70);
                var bar = Bar.Make(r.row, "Energy", new Vector2(0, 0), new Vector2(78 + 80, 12), new Vector2(160, 8), Pal.Green);
                RowButton(r, () => { if (!M.SendToRest(id)) Toast("Không còn chỗ nghỉ trống hoặc nhân viên chưa mệt."); }, 90);
                refreshers.Add(() =>
                {
                    var st = M.Staff(id);
                    var s = (StaffState)st.state;
                    string state = s == StaffState.Resting ? $"Đang nghỉ ở {M.C.Furniture(st.restSpot)?.nameVi}" : s == StaffState.RestInPlace ? "Ngủ gật tại chỗ (thiếu chỗ nghỉ)" :
                        s == StaffState.ToRest ? "Đang đi nghỉ" : s == StaffState.Returning ? "Quay lại làm việc" : M.IsTired(st) ? "Mệt — làm chậm hơn" : "Đang làm việc";
                    r.desc.text = $"Năng lượng {Mathf.RoundToInt(st.energy)}% · {state}";
                    bar.Set(st.energy / 100f);
                    bar.fill.color = st.energy < M.C.energy.tiredThreshold ? Pal.Red : st.energy < 60 ? Pal.Orange : Pal.Green;
                    r.coin.enabled = false; r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                    bool can = !M.IsAway(st) && st.energy < M.C.energy.restDoneAt - 5;
                    r.btnText.text = "Cho nghỉ"; r.btn.interactable = can; r.btnImg.color = can ? Pal.Orange : Pal.Grey;
                });
            }
            foreach (var f in M.C.furniture)
            {
                var cfg = f;
                var r = Row("furn." + f.id, "", "", 100);
                RowButton(r, () => M.BuyFurniture(cfg.id));
                if (sheetSArg == f.id) Tw.Delay(r.row, 0.25f, () => { if (r.row) Tw.Squash(r.row, 0.05f, 0.3f, Vector3.one); });
                refreshers.Add(() =>
                {
                    var d = M.Furn(cfg.id);
                    r.title.text = cfg.nameVi + (d.owned ? $" · Cấp {d.level}" : "");
                    r.desc.text = cfg.descVi + "\n" + EffectText(cfg, d.owned ? d.level : 0);
                    SetBuy(r, M.FurnitureCost(cfg.id), d.owned ? 1 : cfg.levelReq, "Tối đa");
                    if (d.owned && M.FurnitureCost(cfg.id) >= 0) r.btnText.text = "↑ " + Fmt(M.FurnitureCost(cfg.id));
                });
            }
        }

        string EffectText(FurnitureCfg f, int lv)
        {
            string Val(int l)
            {
                float v = f.values[Mathf.Clamp(l, 1, f.values.Length) - 1];
                switch (f.effect)
                {
                    case "rest": return $"+{v:0.#} năng lượng/giây";
                    case "drain": return $"-{v * 100:0}% hao năng lượng";
                    case "restBoost": return $"+{v * 100:0}% tốc độ hồi";
                    case "tips": return $"+{v * 100:0}% tip khách quen";
                    case "spawn": return $"khách đến nhanh hơn {v * 100:0}%";
                }
                return "";
            }
            if (lv <= 0) return "Hiệu quả: " + Val(1);
            return lv < f.values.Length ? $"Hiện tại: {Val(lv)} → {Val(lv + 1)}" : $"Hiện tại: {Val(lv)} (tối đa)";
        }

        // ------------------------------------------------------------------ notebook
        void BuildNotebook()
        {
            if (notebookTab == 0)
            {
                for (int i = 0; i < M.C.quests.Length; i++)
                {
                    int idx = i; var q = M.C.quests[i];
                    var r = Row(q.tutorial ? "ui.quest" : "ui.stamp", "", "", 72);
                    RowButton(r, () => M.ClaimQuest(q.id), 100);
                    refreshers.Add(() =>
                    {
                        var d = M.S.quests[idx];
                        bool active = M.ActiveQuestIndex == idx;
                        r.title.text = (q.tutorial ? "Hướng dẫn: " : "Mục tiêu: ") + q.textVi;
                        r.desc.text = d.completed ? (d.claimed ? "Đã nhận thưởng" : "Hoàn thành! Nhận thưởng") : active ? $"Đang làm {d.progress}/{q.target} · {q.hintVi}" : "Chưa mở";
                        r.btnText.rectTransform.anchoredPosition = new Vector2(10, 1);
                        r.coin.enabled = true; r.coin.sprite = Art.S("ui.coin");
                        r.btnText.text = "+" + q.reward;
                        bool can = d.completed && !d.claimed;
                        r.btn.interactable = can; r.btnImg.color = can ? Pal.Green : d.claimed ? new Color(0.8f, 0.78f, 0.72f) : Pal.Grey;
                        if (d.claimed) { r.coin.enabled = false; r.btnText.text = "Đã nhận"; r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); }
                    });
                }
            }
            else if (notebookTab == 1)
            {
                foreach (var rc in M.C.recipes)
                {
                    var recipe = rc;
                    bool un = M.IsUnlocked(rc.level);
                    var r = Row(Art.BreadBig(rc.level), un ? $"{rc.nameVi} · cấp {rc.level}" : $"??? · cấp {rc.level}", "", 92);
                    r.icon.color = un ? Color.white : new Color(0.15f, 0.1f, 0.08f, 0.8f);
                    int inputs = 1 << (rc.level - 1);
                    r.desc.text = un ? $"Bán {rc.price} xu/chiếc · {rc.pieces} chiếc/mẻ · nướng {rc.bakeSeconds * M.OvenMult:0.#}s\nCần {inputs} bột cấp 1 · giá trị mẻ {rc.price * rc.pieces} xu"
                                     : $"Ghép 2 bột cấp {rc.level - 1} để mở khóa.";
                    var dough = K.Pic(r.row, "Dough", un ? Art.Dough(rc.level) : null, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(48, 40));
                    if (!un) dough.enabled = false;
                }
            }
            else
            {
                foreach (var cc in M.C.customers)
                {
                    var c = cc; var a = M.Aff(c.id);
                    var r = Row("illust." + c.id, a.met ? c.nameVi : "Chưa gặp", "", 96);
                    r.icon.color = a.met ? Color.white : new Color(0.15f, 0.1f, 0.08f, 0.75f);
                    int fav = M.FavoriteOf(c.id); int h = M.Hearts(c.id);
                    r.desc.text = a.met ? $"{c.descVi}\nĐã mua {a.purchases} · Thích: {M.C.Recipe(fav).nameVi}" : (M.ShopLevel < c.unlockLevel ? $"Ghé tiệm từ cấp {c.unlockLevel}." : "Sắp ghé tiệm...");
                    if (a.met)
                    {
                        K.Pic(r.row, "Fav", Art.Bread(fav), new Vector2(1, 0.5f), new Vector2(-36, 12), new Vector2(40, 34));
                        for (int k = 0; k < 3; k++) K.Pic(r.row, "H" + k, "ui.heart", new Vector2(1, 0.5f), new Vector2(-66 + k * 22, -22), new Vector2(20, 16)).color = k < h ? Color.white : new Color(0.6f, 0.55f, 0.5f, 0.5f);
                        int next = h < M.C.flow.heartThresholds.Length ? M.C.flow.heartThresholds[h] : -1;
                        if (next > 0) K.Txt(r.row, "Aff", $"{a.affection}/{next}", 11, Pal.BrownSoft, TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(-44, -38), new Vector2(70, 14));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ shelf picker
        void BuildShelfPick()
        {
            int si = sheetArg;
            var s = M.S.shelfStock[si];
            var info = Row(s.recipe > 0 ? Art.Bread(s.recipe) : "equip.shelf", "", "", 70);
            refreshers.Add(() =>
            {
                var sh = M.S.shelfStock[si];
                info.title.text = sh.stock > 0 ? $"Đang bày: {M.C.Recipe(sh.recipe).nameVi}" : "Kệ đang trống";
                info.desc.text = $"{sh.stock}/{M.ShelfCap} chiếc · giữ chỗ cho khách: {M.Reserved[si]}";
            });
            if (M.S.autoRestockUnlocked)
            {
                var r = Row("ui.auto", "Tự bổ sung từ kho", "Lấy bánh cùng loại từ kho khi kệ vơi.", 70);
                RowButton(r, () => M.SetAuto(si, !M.S.shelfStock[si].auto), 100);
                refreshers.Add(() => { var sh = M.S.shelfStock[si]; r.coin.enabled = false; r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); r.btnText.text = sh.auto ? "Bật" : "Tắt"; r.btnImg.color = sh.auto ? Pal.Green : Pal.Grey; });
            }
            bool any = false;
            for (int lv = 1; lv <= M.C.MaxRecipe; lv++)
            {
                if (M.S.inventory[lv - 1] <= 0) continue;
                any = true;
                int level = lv;
                var r = Row(Art.Bread(lv), M.C.Recipe(lv).nameVi, "", 76);
                RowButton(r, () => { int n = M.StockShelf(si, level); if (n > 0) { CloseSheet(); } }, 100);
                refreshers.Add(() =>
                {
                    var sh = M.S.shelfStock[si];
                    int inv = M.S.inventory[level - 1];
                    r.desc.text = $"Trong kho: {inv} · {M.C.Recipe(level).price} xu/chiếc";
                    bool ok = inv > 0 && (sh.stock == 0 || sh.recipe == level) && sh.stock < M.ShelfCap;
                    r.coin.enabled = false; r.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                    r.btnText.text = ok ? $"Bày {Mathf.Min(inv, M.ShelfCap - sh.stock)}" : sh.stock >= M.ShelfCap ? "Kệ đầy" : "Khác loại";
                    r.btn.interactable = ok; r.btnImg.color = ok ? Pal.Green : Pal.Grey;
                });
            }
            if (!any) Row("ui.inventory", "Kho trống", "Lấy bánh từ lò (chạm lò khi bánh chín) rồi quay lại đây.", 76);
        }

        void BuildInventory()
        {
            var head = Row("ui.inventory", "", "Bánh nướng xong nằm ở đây cho tới khi bày lên kệ.", 76);
            refreshers.Add(() => head.title.text = $"Sức chứa {M.StorageUsed}/{M.StorageCap}");
            for (int lv = 1; lv <= M.C.MaxRecipe; lv++)
            {
                if (!M.IsUnlocked(lv)) continue;
                int level = lv;
                var r = Row(Art.Bread(lv), M.C.Recipe(lv).nameVi, "", 64);
                refreshers.Add(() => r.desc.text = $"{M.S.inventory[level - 1]} chiếc · {M.C.Recipe(level).price} xu/chiếc");
            }
        }

        // ------------------------------------------------------------------ settings
        void BuildSettings()
        {
            var snd = Row("ui.sound", "Âm thanh", "Nhạc nền và hiệu ứng (bật sau lần chạm đầu tiên).", 76);
            RowButton(snd, () => { M.S.soundOn = !M.S.soundOn; AudioMan.SetMuted(!M.S.soundOn); }, 90);
            refreshers.Add(() => { snd.coin.enabled = false; snd.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); snd.btnText.text = M.S.soundOn ? "Bật" : "Tắt"; snd.btnImg.color = M.S.soundOn ? Pal.Green : Pal.Grey; });
            var hand = Row("ui.hand1", "Bàn tay gợi ý", "Hiện sau vài giây không thao tác trong phần hướng dẫn.", 76);
            RowButton(hand, () => M.S.handHints = !M.S.handHints, 90);
            refreshers.Add(() => { hand.coin.enabled = false; hand.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); hand.btnText.text = M.S.handHints ? "Bật" : "Tắt"; hand.btnImg.color = M.S.handHints ? Pal.Green : Pal.Grey; });
            var burn = Row("equip.ovenFire", "Chế độ bánh cháy", "", 76);
            burn.desc.text = M.C.burn.burnEnabled ? $"Đang BẬT (config): bánh để quá {M.C.burn.burnGraceSeconds:0}s khi đang chơi sẽ cháy." : "Đang TẮT (mặc định prototype). Bật bằng burnEnabled trong config.json.";
            var save = Row("ui.postit", "Lưu tiến trình", "", 92);
            refreshers.Add(() =>
            {
                save.desc.text = (SaveStore.ReadOnly ? "Chế độ chỉ đọc (bản lưu từ phiên bản mới hơn). " : "Lưu cục bộ trên thiết bị này, tự lưu định kỳ. ")
                    + $"Vắng mặt tối đa tính {M.C.offline.maxSeconds / 3600:0.#} giờ." + (string.IsNullOrEmpty(SaveStore.LastError) ? "" : "\nLỗi lưu: " + SaveStore.LastError);
            });
            var reset = Row("ui.signX", "Chơi lại từ đầu", "Xóa bản lưu trên thiết bị này.", 76);
            RowButton(reset, () =>
            {
                if (!resetArmed) { resetArmed = true; Toast("Chạm lần nữa để xác nhận xóa tiến trình."); return; }
                GameBoot.I.ResetGame();
            }, 100);
            refreshers.Add(() => { reset.coin.enabled = false; reset.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); reset.btnText.text = resetArmed ? "Xác nhận" : "Xóa"; reset.btnImg.color = Pal.Red; });
            Row("illust.NewBread", "Tiệm Bánh Gấu · prototype", $"Asset: bộ Bear Bakery được cung cấp ({Art.Count} sprite dùng). Phím Esc đóng bảng.", 86);
        }

        // ------------------------------------------------------------------ banner & popups
        public void ShowBanner(string title, string icon, string sub)
        {
            var rt = K.Rect(PopupLayer, "Banner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(300, 170));
            var bg = K.Img(rt, "ui.btnBasic", Color.white, true);
            K.Pic(rt, "Ribbon", "ui.label", new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(260, 60));
            K.Txt(rt, "Title", title, 19, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(220, 30), true);
            K.Pic(rt, "Icon", icon, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(80, 64));
            K.Txt(rt, "Sub", sub, 13, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(280, 36), true);
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; cg.interactable = false;
            Tw.Pop(rt, 0.5f, 0.35f);
            Tw.Run(rt, 0.4f, t => { if (cg) cg.alpha = 1 - t; }, Ease.InQuad, () => { if (rt) Destroy(rt.gameObject); }, 2.2f);
            Fx.Stars(CameraRig.I.cam.ScreenToWorldPoint(RectTransformUtility.WorldToScreenPoint(null, rt.position)), 6, 1.2f);
        }

        public void ShowModal(string title, string body, string icon, string button, Action onButton)
        {
            if (modal != null) Destroy(modal.gameObject);
            modal = K.Stretch(PopupLayer, "Modal");
            var dim = K.Stretch(modal, "Dim"); K.Img(dim, "bg.white", new Color(0.12f, 0.06f, 0.02f, 0.55f), false, true, false);
            var card = K.Rect(modal, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(330, 340));
            K.Img(card, "bg.white", Pal.Cream, false, true, false);
            var ol = K.Stretch(card, "Outline", -4, -4, -4, -4); K.Img(ol, "ui.popupLineOut", Pal.BrownSoft, true);
            K.Pic(card, "Ribbon", "ui.label", new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(290, 70));
            K.Txt(card, "Title", title, 19, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(240, 30), true);
            if (icon != null) K.Pic(card, "Icon", icon, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(120, 96));
            if (title == "Tiệm Bánh Gấu") K.Pic(card, "Logo", "ui.logo", new Vector2(0.5f, 1), new Vector2(0, -174), new Vector2(150, 26)).color = Pal.BrownSoft;
            K.Txt(card, "Body", body, 15, Pal.Brown, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, icon != null ? (title == "Tiệm Bánh Gấu" ? -250 : -215) : -120), new Vector2(290, 110), true);
            var btn = K.At(card, "Btn", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(170, 52));
            K.Btn(btn, "ui.btnBasic", () => { var m = modal; modal = null; if (m) Destroy(m.gameObject); onButton?.Invoke(); }, Pal.Green, true);
            K.Txt(btn, "T", button, 17, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(160, 48), true, false, true);
            Tw.Pop(card, 0.6f, 0.3f);
        }

        // ------------------------------------------------------------------ tutorial hand
        RectTransform hand; Image handImg; int handShownFor = -1; float handT;

        // ------------------------------------------------------------------ delivery (dog next to the counter)
        void BuildDeliverySheet()
        {
            var d = M.S.delivery;
            if (!M.DeliveryUnlocked)
            {
                var q = M.C.quests.FirstOrDefault(x => x.id == M.C.delivery.unlockAfterQuest);
                Row("ui.qDog", "Chó giao hàng", $"Mở sau nhiệm vụ “{q?.textVi}”. Chó nhận đơn đặt bánh, bạn đóng gói bánh trong kho, chó đi giao và mang tiền về (x{M.C.delivery.rewardMult:0.#} giá bán).", 96);
                return;
            }
            if (d.state == (int)DeliveryState.Open)
            {
                var r = M.C.Recipe(d.recipe);
                var order = Row(Art.Bread(d.recipe), $"Đơn: {d.qty} × {r.nameVi}", "", 92);
                RowButton(order, () => M.PackDelivery(), 108);
                refreshers.Add(() =>
                {
                    int have = M.S.inventory[d.recipe - 1];
                    order.desc.text = $"Thưởng {d.reward} xu (giá bán {r.price}×{d.qty} ×{M.C.delivery.rewardMult:0.#} + {M.C.delivery.rewardBonus})\nTrong kho: {have}/{d.qty}";
                    order.coin.enabled = false; order.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                    bool ok = M.CanPackDelivery;
                    order.btnText.text = ok ? "Đóng gói" : "Thiếu bánh";
                    order.btn.interactable = ok; order.btnImg.color = ok ? Pal.Green : Pal.Grey;
                });
                var skip = Row("ui.signX", "Bỏ qua đơn này", $"Đơn mới sau {M.C.delivery.skipSeconds:0} giây.", 70);
                RowButton(skip, () => M.SkipDelivery(), 96);
                skip.coin.enabled = false; skip.btnText.rectTransform.anchoredPosition = new Vector2(0, 1); skip.btnText.text = "Bỏ qua"; skip.btnImg.color = Pal.Orange;
                Row("ui.inventory", "Mẹo", "Bánh lấy từ kho (không lấy trên kệ). Nướng thêm loại bánh trong đơn rồi quay lại đây.", 76);
            }
            else if (d.state == (int)DeliveryState.Out)
            {
                var r = Row("ui.box", "Đang đi giao", "", 80);
                refreshers.Add(() => r.desc.text = $"{d.qty} × {M.C.Recipe(Mathf.Max(1, d.recipe)).nameVi} · về sau {Mathf.CeilToInt(Mathf.Max(0, M.S.delivery.timer))} giây · thưởng {d.reward} xu");
            }
            else
            {
                var r = Row("ui.qDog", "Chưa có đơn", "", 80);
                refreshers.Add(() => r.desc.text = $"Đơn mới sau khoảng {Mathf.CeilToInt(Mathf.Max(0, M.S.delivery.timer))} giây.");
            }
            Row("ui.treasure", $"Đã giao {M.S.stats.deliveries} đơn", "Mỗi đơn trả nhiều hơn bán lẻ trên kệ.", 70);
        }

        // ------------------------------------------------------------------ cleaner side quest (raccoon)
        void BuildCleanerSheet()
        {
            if (M.S.cleanerHired)
            {
                Row("ui.qRaccoon", "Gấu mèo đang dọn phố", $"Rác do khách vứt sẽ được dọn tự động. Đã dọn {M.S.stats.trashCleanedByCleaner} rác.", 86);
                return;
            }
            Row("ui.qRaccoon", "Gấu mèo nhờ giúp", $"Khách đôi khi vứt rác trước tiệm. Mỗi món rác làm thanh quảng cáo chậm {M.C.marketing.trashPenalty * 100:0}% (tối đa {M.C.marketing.trashPenaltyCap * 100:0}%). Chạm vào rác để dọn (+{M.C.marketing.cleanReward} xu).", 104);
            var q = Row("trash.0", "", "", 86);
            RowButton(q, () => { if (M.ClaimCleanerQuest()) CloseSheet(); }, 100);
            refreshers.Add(() =>
            {
                int p = M.CleanerQuestProgress, t = M.C.marketing.cleanerQuestTarget;
                q.title.text = $"Dọn {t} rác ({p}/{t})";
                q.desc.text = $"Thưởng: gấu mèo thành nhân viên dọn rác + {M.C.marketing.cleanerQuestReward} xu";
                q.coin.enabled = false; q.btnText.rectTransform.anchoredPosition = new Vector2(0, 1);
                bool ok = M.CleanerQuestDone;
                q.btnText.text = ok ? "Nhận" : $"{p}/{t}";
                q.btn.interactable = ok; q.btnImg.color = ok ? Pal.Green : Pal.Grey;
            });
        }

        void BuildHand()
        {
            hand = K.Rect(HandLayer, "TutoHand", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.25f, 0.85f), Vector2.zero, new Vector2(64, 62));
            handImg = K.Img(hand, "ui.hand1", Color.white);
            hand.gameObject.SetActive(false);
        }

        Vector2 W2L(Vector3 world)
        {
            Vector2 sp = CameraRig.I.WorldToScreen(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(HandLayer, sp, null, out var lp); return lp;
        }
        Vector2 UI2L(RectTransform rt)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(HandLayer, sp, null, out var lp); return lp;
        }

        bool HandTarget(out Vector2 a, out Vector2 b, out bool drag)
        {
            a = b = Vector2.zero; drag = false;
            int step = M.ActiveQuestIndex;
            if (step < 0 || !M.C.quests[step].tutorial) return false;
            string type = M.C.quests[step].type;
            string zone = CameraRig.I.Zone();
            switch (type)
            {
                case "dough":
                {   // the speed-up button makes the bear knead twice as fast
                    var b = M.Boost("chef");
                    if (zone == "kitchen" && b.remaining <= 0 && b.cooldown <= 0) { a = UI2L(boostTile) + new Vector2(10, -10); return true; }
                    a = W2L(WL.ChefPos + new Vector2(0.15f, 0.6f)); return zone == "kitchen";
                }
                case "merge":
                    for (int i = 0; i < M.S.boardUnlocked; i++)
                        for (int j = i + 1; j < M.S.boardUnlocked; j++)
                            if (M.S.boardSlots[i].level > 0 && M.S.boardSlots[i].level == M.S.boardSlots[j].level && M.S.boardSlots[i].level < M.C.MaxRecipe)
                            { a = W2L(WL.DoughOnSlot(i)); b = W2L(WL.DoughOnSlot(j)); drag = true; return zone == "kitchen"; }
                    a = W2L(WL.ChefPos + new Vector2(0, 0.9f)); return zone == "kitchen";
                case "bake":
                    for (int o = 0; o < GameModel.OvenSlots; o++)
                        if (M.S.ovenJobs[o].owned && M.S.ovenJobs[o].state == 0)
                            for (int i = 0; i < M.S.boardUnlocked; i++)
                                if (M.S.boardSlots[i].level > 0) { a = W2L(WL.DoughOnSlot(i)); b = W2L(WL.Ovens[o]); drag = true; return zone == "kitchen"; }
                    return false;
                case "collect":
                    for (int o = 0; o < GameModel.OvenSlots; o++) if (M.S.ovenJobs[o].state == (int)OvenState.Ready) { a = W2L(WL.Ovens[o]); return zone == "kitchen"; }
                    return false;
                case "stock":
                    if (zone != "shop") { var n = NavTarget(1); if (n == null) return false; a = n.Value; return true; }
                    for (int s = 0; s < GameModel.ShelfSlots; s++) if (M.S.shelfStock[s].owned && M.S.shelfStock[s].stock == 0) { a = W2L(WL.Shelves[s]); return true; }
                    return false;
                case "buy":
                    a = UI2L(tUpgrade.rt) + new Vector2(8, -8); return true;
                case "visit_break":
                    a = UI2L(tFurniture.rt) + new Vector2(8, -8); return true;
            }
            return false;
        }

        /// <summary>Side arrow position if it leads one step toward 'dir' and is visible.</summary>
        Vector2? NavTarget(int dir)
        {
            var a = nav[dir < 0 ? 0 : 1];
            if (a == null || !a.rt.gameObject.activeInHierarchy) return null;
            return UI2L(a.rt) + new Vector2(dir < 0 ? 30 : -30, 10);
        }

        public RectTransform NavArrowRect(int dir) => nav[dir < 0 ? 0 : 1]?.rt;

        void UpdateHand()
        {
            if (hand == null) return;
            bool want = M.S.handHints && !SheetOpen && !ModalOpen && InputRouter.I != null && !InputRouter.I.IsDragging
                        && Time.time - InputRouter.I.LastInputTime > M.C.tutorial.handIdleSeconds;
            if (!want || !HandTarget(out var a, out var b, out bool drag)) { if (hand.gameObject.activeSelf) hand.gameObject.SetActive(false); handShownFor = -1; return; }
            if (!hand.gameObject.activeSelf) { hand.gameObject.SetActive(true); handT = 0; handShownFor = M.S.tutorialStep; }
            handT += Time.deltaTime;
            if (drag)
            {
                float k = (handT % 1.6f) / 1.6f;
                float m = Mathf.Clamp01((k - 0.15f) / 0.6f);
                hand.anchoredPosition = Vector2.Lerp(a, b, Ease.InOutQuad(m));
                handImg.sprite = Art.S(k < 0.15f || k > 0.8f ? "ui.hand1" : "ui.hand1");
                hand.localScale = Vector3.one * (k < 0.12f ? 1.1f - k : 1f);
                handImg.color = new Color(1, 1, 1, k > 0.85f ? (1 - k) / 0.15f : 1);
            }
            else
            {
                float k = Mathf.Abs(Mathf.Sin(handT * 4f));
                hand.anchoredPosition = a + new Vector2(0, -10 * k);
                hand.localScale = Vector3.one * (1f - 0.08f * k);
                handImg.color = Color.white;
            }
        }
    }
}
