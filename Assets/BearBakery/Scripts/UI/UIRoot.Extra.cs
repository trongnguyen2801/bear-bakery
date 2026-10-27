
// World-anchored widgets for the redesigned shop/kitchen (buy spots, boost tags, marketing bar, order/alert bubbles)
// and UI reactions to the speed-up / delivery / cleaner events.
using BB.Model;
using BB.View;
using UnityEngine;
using UnityEngine.UI;

namespace BB.UI
{
    public partial class UIRoot
    {
        /// <summary>Next purchasable slot/oven/stand: big "+" in a circle and a price pill (tap the spot to buy).</summary>
        public RectTransform MakeBuySpot(string name, out Text price, out Image icon)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
            K.Pic(rt, "Circle", "ui.whiteCircle", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(54, 54), new Color(1, 1, 1, 0.92f));
            K.Pic(rt, "Plus", "ui.plus", new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(34, 34));
            var pill = K.At(rt, "Price", new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(78, 24));
            K.Img(pill, "ui.btnBasic", Pal.Brown, true);
            icon = K.Pic(pill, "Coin", "ui.coin", new Vector2(0, 0.5f), new Vector2(13, 1), new Vector2(20, 20));
            price = K.Txt(pill, "T", "", 13, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(9, 1), new Vector2(58, 22), true);
            rt.gameObject.AddComponent<Bob>().amp = 2.5f;
            return rt;
        }

        public void SetBuySpot(RectTransform rt, Text price, Image icon, int cost, int levelReq)
        {
            var pill = (RectTransform)price.transform.parent;
            var bg = pill.GetComponent<Image>();
            if (cost < 0) { price.text = "—"; icon.enabled = false; return; }
            icon.enabled = true;
            if (M.ShopLevel < levelReq)
            {
                icon.sprite = Art.S("ui.lock"); price.text = "Cấp " + levelReq; bg.color = Pal.Grey;
                return;
            }
            icon.sprite = Art.S("ui.coin");
            price.text = Fmt(cost);
            bool can = M.S.currency >= cost;
            bg.color = can ? Pal.GreenDark : Pal.Brown;
        }

        /// <summary>"x2" tag above a boosted character.</summary>
        public RectTransform MakeBoostTag(string name)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 22));
            K.Img(rt, "ui.btnBasic", Pal.Red, true);
            K.Txt(rt, "T", "x2", 14, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(38, 22), true, false, true);
            rt.gameObject.AddComponent<Bob>().amp = 3;
            return rt;
        }

        /// <summary>Marketing (customer attraction) bar next to the sparrow: bread icon, gauge, "next customer in Ns", trash penalty.</summary>
        public RectTransform MakeMarketingBar(string name, out Bar gauge, out Text text, out Image icon, out RectTransform trash, out Text trashText)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 40));
            K.Img(rt, "ui.btnBasic", new Color(1, 0.98f, 0.93f, 0.96f), true);
            var circ = K.At(rt, "IconBg", new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(40, 40));
            K.Img(circ, "ui.whiteCircle", Pal.Orange, false, false, false);
            icon = K.Pic(circ, "Icon", "bread.1", new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(30, 26));
            text = K.Txt(rt, "T", "", 11, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(12, 8), new Vector2(118, 16), true);
            gauge = Bar.Make(rt, "Gauge", new Vector2(0.5f, 0.5f), new Vector2(12, -8), new Vector2(110, 10), Pal.Green);
            trash = K.At(rt, "Trash", new Vector2(1, 0.5f), new Vector2(18, -18), new Vector2(54, 22));
            K.Img(trash, "ui.btnBasic", Pal.Red, true);
            K.Pic(trash, "Icon", "trash.1", new Vector2(0, 0.5f), new Vector2(12, 1), new Vector2(20, 14));
            trashText = K.Txt(trash, "T", "", 11, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(8, 1), new Vector2(36, 20), true);
            return rt;
        }

        /// <summary>Delivery order bubble above the dog: bread x quantity, green tick when storage has enough.</summary>
        public RectTransform MakeOrderBubble(string name, out Image icon, out Text qty, out Image check)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), Vector2.zero, new Vector2(78, 50));
            K.Img(rt, "ui.btnBasic", Color.white, true);
            K.Pic(rt, "Box", "ui.box", new Vector2(0, 1), new Vector2(4, -2), new Vector2(26, 26));
            icon = K.Pic(rt, "Bread", "bread.1", new Vector2(0.5f, 0.5f), new Vector2(-8, 0), new Vector2(34, 30));
            qty = K.Txt(rt, "Q", "×3", 15, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(22, -6), new Vector2(34, 22), true, true);
            check = K.Pic(rt, "Check", "ui.check", new Vector2(1, 1), new Vector2(-4, -4), new Vector2(20, 20));
            return rt;
        }

        /// <summary>Side-quest marker: red "!" bubble, or the CLEAR!! sticker when it can be claimed.</summary>
        public RectTransform MakeAlertBubble(string name, out Image clear, out Text mark)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), Vector2.zero, new Vector2(40, 40));
            K.Img(rt, "ui.whiteCircle", Color.white, false, false, false);
            mark = K.Txt(rt, "Mark", "!", 26, Pal.Red, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(40, 40), true);
            clear = K.Pic(rt, "Clear", "ui.clearLabel", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(76, 32));
            return rt;
        }

        // ------------------------------------------------------------------ events of the new systems
        void OnExtraEvent(GameEvent e)
        {
            switch (e.type)
            {
                case Ev.BoostBusy:
                    Toast(e.a == 0 ? $"{BoostName(e.s)} đang được tăng tốc!" : $"Nút Tăng tốc đang hồi: chờ {e.amount} giây.");
                    break;
                case Ev.BoostEnded:
                    if (BoostIdForView() == e.s) Tw.Squash(boostTile, 0.1f, 0.3f, Vector3.one);
                    break;
                case Ev.CleanerHired:
                    ShowBanner("Gấu mèo nhận việc!", "ui.qRaccoon", $"Rác trên phố sẽ được dọn tự động. +{e.amount} xu");
                    AudioMan.Play("se_Etc_GetReward", 0.8f);
                    RebuildSheet();
                    break;
                case Ev.DeliveryOrder:
                    if (CameraRig.I.Zone() != "shop") Toast($"Có đơn giao hàng mới: {e.b} × {M.C.Recipe(e.a).nameVi}");
                    RebuildSheet();
                    break;
                case Ev.DeliveryPacked:
                    Toast($"Chó đã nhận {e.b} bánh, đi giao ngay! Thưởng {e.amount} xu khi về.");
                    if (sheetKind == SheetKind.Delivery) CloseSheet();
                    break;
                case Ev.DeliveryDone:
                    if (e.c == 1) break;   // offline: shown in the welcome-back popup
                    Toast($"Giao hàng xong: +{e.amount} xu");
                    RebuildSheet();
                    break;
                case Ev.DeliverySkipped: RebuildSheet(); break;
                case Ev.TrashCleaned:
                    if (e.b == 0 && M.CleanerQuestDone && M.S.stats.trashCleaned == M.C.marketing.cleanerQuestTarget) Toast("Gấu mèo muốn giúp bạn! Chạm vào gấu mèo để nhận.");
                    break;
            }
        }
    }
}
