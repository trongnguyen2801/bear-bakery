using System.Linq;
using BB.Model;
using BB.UI;
using UnityEngine;

namespace BB.View
{
    public partial class World
    {
        int hlLevel = -1, hlFrom = -1;
        Hit preview;

        public Hit HitAt(Vector2 wp)
        {
            Hit best = null;
            foreach (var h in hits) if (h.Contains(wp) && (best == null || h.prio > best.prio)) best = h;
            return best;
        }

        public CustomerView CustomerAt(Vector2 wp)
        {
            foreach (var v in customers.Values)
            {
                var p = (Vector2)v.root.position;
                float hh = v.holdH / 0.36f;
                if (v.alpha > 0.5f && wp.x > p.x - 0.4f && wp.x < p.x + 0.4f && wp.y > p.y && wp.y < p.y + hh) return v;
            }
            return null;
        }

        public void SetDragHighlights(int level, int from) { hlLevel = level; hlFrom = from; }
        public void ClearDragHighlights()
        {
            hlLevel = -1; hlFrom = -1; preview = null;
            foreach (var s in slots) { s.table.color = s.index < M.S.boardUnlocked ? Color.white : s.table.color; s.dough.color = Color.white; }
            foreach (var o in ovens) if (M.S.ovenJobs[o.index].owned) o.body.color = Color.white;
        }
        public void SetDropPreview(Hit h, int from) { preview = h; }

        void LateUpdate()
        {
            if (hlLevel < 0) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            bool maxLv = hlLevel >= M.C.MaxRecipe;
            for (int i = 0; i < M.S.boardUnlocked; i++)
            {
                var s = slots[i]; if (i == hlFrom) continue;
                var d = M.S.boardSlots[i];
                bool match = d.level == hlLevel && !maxLv;
                bool target = preview != null && preview.kind == "slot" && preview.index == i;
                s.dough.color = match ? Color.Lerp(Color.white, new Color(1f, 0.93f, 0.6f), pulse) : Color.white;
                s.table.color = target && (match || d.level == 0) ? new Color(1f, 0.95f, 0.75f) : match ? Color.Lerp(Color.white, new Color(1f, 0.97f, 0.85f), pulse) : Color.white;
            }
            for (int i = 0; i < ovens.Length; i++)
            {
                var o = M.S.ovenJobs[i]; if (!o.owned) continue;
                bool free = o.state == (int)OvenState.Empty;
                bool target = preview != null && preview.kind == "oven" && preview.index == i;
                ovens[i].body.color = free ? (target ? new Color(1f, 0.9f, 0.7f) : Color.Lerp(Color.white, new Color(1f, 0.92f, 0.8f), pulse)) : Color.white;
            }
        }

        // ------------------------------------------------------------------ taps
        public void Tap(Hit h)
        {
            var ui = UIRoot.I;
            switch (h.kind)
            {
                case "slot":
                    if (h.index >= M.S.boardUnlocked) { if (M.BuyExpansion(0)) ui.Toast("Đã mở thêm ô bàn!"); }
                    else ui.Toast("Kéo bột vào ô trống, thả lên bột cùng loại để ghép.");
                    break;
                case "oven":
                {
                    var o = M.S.ovenJobs[h.index];
                    if (!o.owned) { M.BuyExpansion(1); break; }
                    var st = (OvenState)o.state;
                    if (st == OvenState.Ready || st == OvenState.Burnt) M.Collect(h.index);
                    else if (st == OvenState.Baking) ui.Toast($"Đang nướng {M.C.Recipe(o.recipe).nameVi}: còn {Mathf.CeilToInt(o.remaining)} giây.");
                    else ui.Toast("Kéo một cục bột thả vào lò để nướng.");
                    break;
                }
                case "chef":
                    ui.Toast(M.IsAway(M.Staff("chef")) ? "Gấu đang nghỉ — chạm bàn để tự nhồi bột." : "Muốn Gấu làm nhanh hơn? Bấm nút Tăng tốc ở giữa thanh dưới!");
                    ui.PulseBoost();
                    break;
                case "table":
                    if (M.IsAway(M.Staff("chef"))) M.ManualKnead();
                    else { ui.Toast("Bấm nút Tăng tốc để Gấu nhồi bột nhanh gấp đôi."); ui.PulseBoost(); }
                    break;
                case "storage": ui.OpenSheet(SheetKind.Inventory); break;
                case "shelf":
                    if (M.S.shelfStock[h.index].owned) ui.OpenSheet(SheetKind.ShelfPick, h.index);
                    else M.BuyExpansion(2);
                    break;
                case "counter":
                case "cashier":
                {
                    var cs = M.Staff("cashier");
                    if (M.IsAway(cs)) { if (!M.ManualCheckout()) ui.Toast("Chưa có khách chờ tính tiền."); }
                    else { ui.Toast($"Amber đang đứng quầy. Bấm Tăng tốc để bán nhanh gấp đôi! (năng lượng {Mathf.RoundToInt(cs.energy)}%)"); ui.PulseBoost(); }
                    break;
                }
                case "piggy":
                    if (M.ClaimTips() <= 0) ui.Toast("Heo đất trống. Khách mua đúng món thích sẽ để lại tip.");
                    break;
                case "promoter":
                {
                    var p = M.Staff("promoter");
                    ui.Toast(M.IsAway(p) ? "Tweetie đang nghỉ: khách tới chậm hơn." : "Tweetie mời khách. Bấm Tăng tốc để thanh quảng cáo đầy nhanh gấp đôi!");
                    ui.PulseBoost();
                    break;
                }
                case "dog": ui.OpenSheet(SheetKind.Delivery); break;
                case "raccoon":
                    if (M.S.cleanerHired) ui.Toast($"Gấu mèo đang giữ phố sạch sẽ (đã dọn {M.S.stats.trashCleanedByCleaner} rác).");
                    else ui.OpenSheet(SheetKind.Cleaner);
                    break;
                case "trash":
                {
                    var t = M.S.trash.FirstOrDefault(x => x.spot == h.index);
                    if (t != null && !M.CleanTrash(t.id)) ui.Toast("Gấu mèo đang dọn chỗ này rồi.");
                    break;
                }
                case "furniture": ui.OpenSheet(SheetKind.Furniture, -1, h.sid); break;
                case "staffdoor": ui.Toast("Cửa phòng nhân viên: nhân viên mệt sẽ vào đây nghỉ."); break;
            }
        }

        public void TapDough(int slot)
        {
            var d = M.S.boardSlots[slot];
            if (d.level == 0) return;
            int same = 0;
            for (int i = 0; i < M.S.boardUnlocked; i++) if (i != slot && M.S.boardSlots[i].level == d.level) same++;
            var r = M.C.Recipe(d.level);
            string msg = d.level >= M.C.MaxRecipe ? $"Bột {r.nameVi} (cấp tối đa) — kéo vào lò để nướng."
                : same > 0 ? $"Bột {r.nameVi} cấp {d.level}: kéo lên bột cùng loại để ghép, hoặc kéo vào lò."
                : $"Bột {r.nameVi} cấp {d.level}: chờ thêm một cục giống nó để ghép, hoặc kéo vào lò.";
            UIRoot.I.Toast(msg);
            Tw.Squash(slots[slot].dough.transform, 0.15f, 0.2f, Vector3.one * DoughScale);
        }

        public void TapCustomer(CustomerView v)
        {
            var cfg = M.C.Customer(v.type);
            int fav = M.FavoriteOf(v.type);
            ShowCustomerBubble(v, "bubble.heart", Art.Bread(fav), 1.5f);
            UIRoot.I.Toast($"{cfg.nameVi} thích {M.C.Recipe(fav).nameVi} (♥ {M.Hearts(v.type)}/3)");
        }
    }
}
