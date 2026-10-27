// A "reasonable player" policy used by the headless balance simulation and by the in-build AutoPilot.
// It only uses the same public commands a player has.
using System;
using System.Linq;

namespace BB.Model
{
    public enum BotKind { None, ClaimQuest, ClaimOffline, Collect, Stock, Merge, Bake, Buy, ClaimTips, Boost, VisitBreak, Knead, Checkout, SetAuto, CleanTrash, ClaimCleaner, Deliver }

    public struct BotAction
    {
        public BotKind kind; public int a, b; public string s;
        public override string ToString() => $"{kind}({a},{b},{s})";
    }

    public static class BotPolicy
    {
        /// <summary>Most valuable action right now (None = nothing useful to do).</summary>
        public static BotAction Decide(GameModel m, bool allowHelpTaps)
        {
            var S = m.S;
            // 1) rewards
            for (int i = 0; i < S.quests.Count; i++) if (S.quests[i].completed && !S.quests[i].claimed) return new BotAction { kind = BotKind.ClaimQuest, s = S.quests[i].id };
            if (!string.IsNullOrEmpty(S.pendingOffline.id)) return new BotAction { kind = BotKind.ClaimOffline };
            // 2) collect ready ovens
            for (int o = 0; o < GameModel.OvenSlots; o++)
            {
                var ov = S.ovenJobs[o];
                if (ov.owned && (ov.state == (int)OvenState.Burnt || (ov.state == (int)OvenState.Ready && m.StorageUsed + m.C.Recipe(ov.recipe).pieces <= m.StorageCap)))
                    return new BotAction { kind = BotKind.Collect, a = o };
            }
            // 2b) side systems: delivery order (paid 1.5x), cleaner side quest
            if (m.CanPackDelivery) return new BotAction { kind = BotKind.Deliver };
            if (m.CleanerQuestDone) return new BotAction { kind = BotKind.ClaimCleaner };
            // 3) stock shelves that are low
            for (int s = 0; s < GameModel.ShelfSlots; s++)
            {
                var sh = S.shelfStock[s];
                if (!sh.owned || sh.stock >= m.ShelfCap) continue;
                if (sh.stock > 0 && S.inventory[sh.recipe - 1] > 0 && !(sh.auto && S.autoRestockUnlocked)) return new BotAction { kind = BotKind.Stock, a = s, b = sh.recipe };
                if (sh.stock == 0)
                {
                    int best = -1;
                    for (int r = m.C.MaxRecipe; r >= 1; r--)
                        if (S.inventory[r - 1] > 0 && !Enumerable.Range(0, GameModel.ShelfSlots).Any(k => k != s && S.shelfStock[k].owned && S.shelfStock[k].stock > 0 && S.shelfStock[k].recipe == r)) { best = r; break; }
                    if (best < 0) for (int r = m.C.MaxRecipe; r >= 1; r--) if (S.inventory[r - 1] > 0) { best = r; break; }
                    if (best > 0) return new BotAction { kind = BotKind.Stock, a = s, b = best };
                }
            }
            if (S.autoRestockUnlocked)
                for (int s = 0; s < GameModel.ShelfSlots; s++) if (S.shelfStock[s].owned && !S.shelfStock[s].auto) return new BotAction { kind = BotKind.SetAuto, a = s };
            // 4) merge lowest pair
            int n = S.boardUnlocked;
            for (int lv = 1; lv < m.C.MaxRecipe; lv++)
            {
                int a = -1;
                for (int i = 0; i < n; i++)
                {
                    if (S.boardSlots[i].level != lv) continue;
                    if (a < 0) a = i; else return new BotAction { kind = BotKind.Merge, a = a, b = i };
                }
            }
            // 5) bake: the highest unpaired dough of level >= target, or anything when the board is full
            int freeOven = -1;
            for (int o = 0; o < GameModel.OvenSlots; o++) if (S.ovenJobs[o].owned && S.ovenJobs[o].state == 0) { freeOven = o; break; }
            int count = 0; for (int i = 0; i < n; i++) if (S.boardSlots[i].level > 0) count++;
            if (freeOven >= 0 && count > 0)
            {
                int target = TargetBakeLevel(m);
                int bestSlot = -1, bestLv = 0;
                for (int i = 0; i < n; i++) if (S.boardSlots[i].level > bestLv) { bestLv = S.boardSlots[i].level; bestSlot = i; }
                bool shelvesLow = m.TotalAvailable() + m.StorageUsed < 3;
                if (bestLv >= target || count >= n || (shelvesLow && bestLv >= Math.Max(1, target - 1)))
                    return new BotAction { kind = BotKind.Bake, a = bestSlot, b = freeOven };
            }
            // 6) buy
            var buy = PickPurchase(m);
            if (buy.kind != BotKind.None) return buy;
            // 7) tips, break room
            if (S.tipJar >= 25) return new BotAction { kind = BotKind.ClaimTips };
            if (!S.visitedBreakRoom && S.stats.upgradesBought > 0) return new BotAction { kind = BotKind.VisitBreak };
            // 8) staff away => manual work keeps the shop moving
            if (m.IsAway(m.Staff("chef")) && m.FirstEmptySlot() >= 0) return new BotAction { kind = BotKind.Knead };
            if (m.IsAway(m.Staff("cashier")) && m.FrontReady() != null) return new BotAction { kind = BotKind.Checkout };
            if (allowHelpTaps)
            {
                // trash on the street slows the marketing bar (the raccoon cleans once hired)
                if (S.trash.Count > 0 && !S.cleanerHired) return new BotAction { kind = BotKind.CleanTrash, s = S.trash[0].id.ToString() };
                // speed-up button: use it when the boosted character is actually the bottleneck
                var chef = m.Staff("chef");
                if (BoostReady(m, "chef") && !m.IsAway(chef) && m.FirstEmptySlot() >= 0) return new BotAction { kind = BotKind.Boost, s = "chef" };
                if (BoostReady(m, "cashier") && !m.IsAway(m.Staff("cashier")) && m.Queue.Count >= 2) return new BotAction { kind = BotKind.Boost, s = "cashier" };
                if (BoostReady(m, "promoter") && m.TotalAvailable() >= 3 && m.ActiveCustomers < 3) return new BotAction { kind = BotKind.Boost, s = "promoter" };
            }
            return new BotAction { kind = BotKind.None };
        }

        static bool BoostReady(GameModel m, string id) { var b = m.Boost(id); return b.remaining <= 0 && b.cooldown <= 0; }

        public static int TargetBakeLevel(GameModel m)
        {
            int ovens = m.OwnedOvens;
            int chefLv = m.UpLevel("chef");
            int t = 2 + (chefLv >= 3 ? 1 : 0) + (ovens >= 2 && chefLv >= 4 ? 1 : 0);
            // goal-directed: when the active goal asks for a new recipe, merge up to it
            int qi = m.ActiveQuestIndex;
            if (qi >= 0 && m.C.quests[qi].type == "recipe") t = Math.Max(t, m.C.quests[qi].target);
            return Math.Min(m.C.MaxRecipe, t);
        }

        static BotAction PickPurchase(GameModel m)
        {
            long c = m.S.currency;
            BotAction best = new BotAction { kind = BotKind.None }; float bestScore = 0;
            int qi = m.ActiveQuestIndex;
            if (qi >= 0 && m.C.quests[qi].type == "ovens")
            {
                int oc = m.ExpansionCost(1, out int oreq);
                if (oc > 0 && m.ShopLevel >= oreq)
                {
                    if (c >= oc) return new BotAction { kind = BotKind.Buy, s = "oven+" };
                    c = Math.Max(0, c - (oc - 80));   // keep saving; only small buys allowed
                }
            }
            void Consider(BotAction a, int cost, float value, int req = 1)
            {
                if (cost < 0 || cost > c || m.ShopLevel < req) return;
                float score = value / Math.Max(1, cost);
                if (score > bestScore) { bestScore = score; best = a; }
            }
            Consider(new BotAction { kind = BotKind.Buy, s = "chef" }, m.UpgradeCost("chef"), 160);
            Consider(new BotAction { kind = BotKind.Buy, s = "oven" }, m.UpgradeCost("oven"), 110);
            Consider(new BotAction { kind = BotKind.Buy, s = "capacity" }, m.UpgradeCost("capacity"), 90);
            int e0 = m.ExpansionCost(0, out int r0); Consider(new BotAction { kind = BotKind.Buy, s = "slot" }, e0, m.S.boardUnlocked < 8 ? 100 : 40, r0);
            int e1 = m.ExpansionCost(1, out int r1); Consider(new BotAction { kind = BotKind.Buy, s = "oven+" }, e1, 900, r1);
            int e2 = m.ExpansionCost(2, out int r2); Consider(new BotAction { kind = BotKind.Buy, s = "shelf+" }, e2, 260, r2);
            if (m.CanUnlockAutoRestock) Consider(new BotAction { kind = BotKind.Buy, s = "auto" }, m.C.economy.autoRestockCost, 140);
            foreach (var f in m.C.furniture)
            {
                var d = m.Furn(f.id);
                Consider(new BotAction { kind = BotKind.Buy, s = "furn:" + f.id }, m.FurnitureCost(f.id), d.owned ? 25 : 70, d.owned ? 1 : f.levelReq);
            }
            return best;
        }

        public static bool Execute(GameModel m, BotAction a)
        {
            switch (a.kind)
            {
                case BotKind.ClaimQuest: return m.ClaimQuest(a.s);
                case BotKind.ClaimOffline: return m.ClaimOffline() >= 0;
                case BotKind.Collect: return m.Collect(a.a) != CollectResult.NotReady;
                case BotKind.Stock: return m.StockShelf(a.a, a.b) > 0;
                case BotKind.Merge: return m.DropDough(a.a, a.b) == DropResult.Merged;
                case BotKind.Bake: return m.Bake(a.a, a.b);
                case BotKind.ClaimTips: return m.ClaimTips() > 0;
                case BotKind.Boost: return m.StartBoost(a.s);
                case BotKind.CleanTrash: return m.CleanTrash(long.Parse(a.s));
                case BotKind.ClaimCleaner: return m.ClaimCleanerQuest();
                case BotKind.Deliver: return m.PackDelivery();
                case BotKind.VisitBreak: m.MarkBreakRoomVisited(); return true;
                case BotKind.Knead: return m.ManualKnead();
                case BotKind.Checkout: return m.ManualCheckout();
                case BotKind.SetAuto: return m.SetAuto(a.a, true);
                case BotKind.Buy:
                    switch (a.s)
                    {
                        case "slot": return m.BuyExpansion(0);
                        case "oven+": return m.BuyExpansion(1);
                        case "shelf+": return m.BuyExpansion(2);
                        case "auto": return m.UnlockAutoRestock();
                        default:
                            if (a.s.StartsWith("furn:")) return m.BuyFurniture(a.s.Substring(5));
                            return m.BuyUpgrade(a.s);
                    }
            }
            return false;
        }
    }
}
