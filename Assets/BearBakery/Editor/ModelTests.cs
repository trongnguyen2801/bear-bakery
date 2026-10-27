// Headless rule tests (run: Unity -batchmode -executeMethod BB.EditorTools.ModelTests.RunAll). Writes Docs/test_results.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BB.Model;
using BB.View;
using UnityEditor;
using UnityEngine;

namespace BB.EditorTools
{
    public static class ModelTests
    {
        static readonly List<(string name, bool ok, string note)> results = new List<(string, bool, string)>();
        static string cur;
        static bool curOk; static string curNote;

        static GameConfig Cfg() => JsonUtility.FromJson<GameConfig>(File.ReadAllText("Assets/BearBakery/Resources/config.json"));
        static GameModel NewModel(Action<GameConfig> tweak = null)
        {
            var c = Cfg(); tweak?.Invoke(c);
            return new GameModel(c, null, 7);
        }
        static void Check(bool cond, string msg) { if (!cond) { curOk = false; curNote += msg + "; "; } }
        static void T(string name, Action body)
        {
            cur = name; curOk = true; curNote = "";
            try { body(); } catch (Exception e) { curOk = false; curNote += "EXCEPTION " + e.GetType().Name + ": " + e.Message; }
            results.Add((name, curOk, curNote));
        }
        static void Put(GameModel m, int slot, int level) { m.S.boardSlots[slot].level = level; m.S.boardSlots[slot].id = 1000 + slot; }
        static void Run(GameModel m, float seconds, float dt = 0.1f) { for (float t = 0; t < seconds; t += dt) m.Tick(dt); }
        static bool NoNegatives(GameModel m) =>
            m.S.currency >= 0 && m.S.tipJar >= 0 && m.S.inventory.All(x => x >= 0) && m.S.shelfStock.All(s => s.stock >= 0) && m.Reserved.All(r => r >= 0)
            && m.S.shelfStock.Select((s, i) => s.stock - m.Reserved[i]).All(v => v >= 0);

        public static void RunAll()
        {
            results.Clear();
            // ---------------- merge
            T("Ghép đúng: 2 bột cùng cấp -> 1 bột cấp kế, tiêu thụ đúng 2 đầu vào", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 1);
                var r = m.DropDough(0, 1);
                Check(r == DropResult.Merged, "result " + r);
                Check(m.S.boardSlots[0].level == 0 && m.S.boardSlots[1].level == 2, "levels");
                Check(m.S.boardSlots.Count(s => s.level > 0) == 1, "count");
                Check(m.IsUnlocked(2), "recipe 2 unlocked");
                var r2 = m.DropDough(0, 1); Check(r2 == DropResult.Invalid, "source consumed once");
            });
            T("Ghép sai cấp: không đổi trạng thái (trở về ô cũ)", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 2);
                var r = m.DropDough(0, 1);
                Check(r == DropResult.Invalid && m.S.boardSlots[0].level == 1 && m.S.boardSlots[1].level == 2, "changed");
            });
            T("Thả vào ô trống: di chuyển giữ nguyên ID", () =>
            {
                var m = NewModel(); Put(m, 0, 3); long id = m.S.boardSlots[0].id;
                Check(m.DropDough(0, 2) == DropResult.Moved && m.S.boardSlots[2].id == id && m.S.boardSlots[0].level == 0, "move");
            });
            T("Thả ra ngoài / ô khóa / chính nó: không đổi trạng thái", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 1);
                Check(m.DropDough(0, 11) == DropResult.Invalid, "locked slot");
                Check(m.DropDough(0, -1) == DropResult.Invalid, "outside");
                Check(m.DropDough(0, 0) == DropResult.Same, "self");
                Check(m.S.boardSlots[0].level == 1 && m.S.boardSlots[1].level == 1, "unchanged");
            });
            T("Cấp tối đa không ghép tiếp", () =>
            {
                var m = NewModel(); Put(m, 0, 6); Put(m, 1, 6);
                Check(m.DropDough(0, 1) == DropResult.MaxLevel && m.S.boardSlots[0].level == 6 && m.S.boardSlots[1].level == 6, "max merged");
            });
            T("Bàn đầy: đầu bếp dừng, không mất bột; mở chỗ thì làm tiếp", () =>
            {
                var m = NewModel(); for (int i = 0; i < m.S.boardUnlocked; i++) Put(m, i, 1);
                int before = m.S.stats.doughMade;
                Run(m, 20);
                Check(m.S.stats.doughMade == before, "made dough while full");
                Check(m.Staff("chef").state == (int)StaffState.IdleBlocked, "chef not blocked");
                Check(!m.HelpChef(), "help while full should fail");
                m.Bake(0, 0);
                Run(m, 6);
                Check(m.S.stats.doughMade == before + 1, "resume after freeing");
            });
            T("Hỗ trợ đầu bếp có giới hạn mỗi chu kỳ", () =>
            {
                var m = NewModel();
                int ok = 0; for (int i = 0; i < 20; i++) if (m.HelpChef()) ok++;
                Check(ok <= m.C.chef.helpMaxPerCycle + 1 && m.S.stats.doughMade <= 1, $"help taps counted {ok}, dough {m.S.stats.doughMade}");
            });
            // ---------------- oven
            T("Lò bận: không nhận bột thứ hai, bột vẫn ở bàn", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 2);
                Check(m.Bake(0, 0), "first bake");
                Check(!m.Bake(1, 0) && m.S.boardSlots[1].level == 2, "second bake accepted");
                Check(!m.Bake(1, 1), "unowned oven accepted");
            });
            T("Nướng dùng thời gian mô phỏng; giữ recipeId", () =>
            {
                var m = NewModel(); Put(m, 0, 2); m.Bake(0, 0);
                var o = m.S.ovenJobs[0];
                Check(o.recipe == 2 && o.state == 1, "job");
                Run(m, m.C.Recipe(2).bakeSeconds - 0.5f);
                Check(o.state == 1, "ready too early");
                Run(m, 1f);
                Check(o.state == 2, "not ready");
            });
            T("Kho đầy: bánh ở lại lò, không mất, không nhân đôi", () =>
            {
                var m = NewModel(); m.S.inventory[0] = m.StorageCap - 1;
                Put(m, 0, 1); m.Bake(0, 0); Run(m, 10);
                var r = m.Collect(0);
                Check(r == CollectResult.StorageFull && m.S.ovenJobs[0].state == 2 && m.StorageUsed == m.StorageCap - 1, "lost/dup");
            });
            T("Spam nhận bánh: chỉ cộng một lần", () =>
            {
                var m = NewModel(); Put(m, 0, 1); m.Bake(0, 0); Run(m, 10);
                int got = 0; for (int i = 0; i < 10; i++) if (m.Collect(0) == CollectResult.Collected) got++;
                Check(got == 1 && m.S.inventory[0] == m.C.Recipe(1).pieces, $"got {got} inv {m.S.inventory[0]}");
            });
            T("Chế độ cháy (bật qua config): chỉ tính khi chơi, có dọn lò", () =>
            {
                var m = NewModel(c => c.burn.burnEnabled = true);
                Put(m, 0, 1); m.Bake(0, 0); Run(m, 9);
                m.ApplyElapsed(3600);
                Check(m.S.ovenJobs[0].state == 2, "burnt offline");
                Run(m, m.C.burn.burnGraceSeconds + 1);
                Check(m.S.ovenJobs[0].state == 3, "not burnt");
                Check(m.Collect(0) == CollectResult.Cleared && m.S.ovenJobs[0].state == 0, "clear");
            });
            T("Mặc định tắt cháy: bánh chờ lâu không mất", () =>
            {
                var m = NewModel(); Put(m, 0, 1); m.Bake(0, 0); Run(m, 300, 0.5f);
                Check(m.S.ovenJobs[0].state == 2, "state " + m.S.ovenJobs[0].state);
            });
            // ---------------- shelves & customers
            T("Bày kệ: kho giảm đúng số chuyển, không quá sức chứa", () =>
            {
                var m = NewModel(); m.S.inventory[1] = 10;
                int n = m.StockShelf(0, 2);
                Check(n == m.ShelfCap && m.S.inventory[1] == 10 - n && m.S.shelfStock[0].stock == n, $"n={n}");
                Check(m.StockShelf(0, 2) == 0, "overfill");
                m.S.inventory[0] = 3; Check(m.StockShelf(0, 1) == 0, "mixed types on one shelf");
                Check(m.StockShelf(5, 1) == 0, "locked shelf");
            });
            T("Hai khách cùng chọn đơn vị cuối: chỉ một người lấy được", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 1;
                var a = m.SpawnCustomer("Cat"); var b = m.SpawnCustomer("Pig");
                Check(a != null && b == null, "second customer reserved the same last unit");
                Run(m, 30);
                Check(m.S.shelfStock[0].stock == 0 && m.S.stats.sold == 1 && NoNegatives(m), "sold " + m.S.stats.sold);
            });
            T("Kệ hết hàng: không sinh khách mua; khách buồn ghé không lấy gì", () =>
            {
                var m = NewModel(); m.S.stats.stocked = 1;
                Run(m, 60);
                Check(m.S.stats.sold == 0 && m.S.currency == 0, "sold from empty shelves");
            });
            T("Thanh toán ghi tiền đúng một lần; tip vào heo đất riêng", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 2; m.S.shelfStock[0].stock = 1; m.S.unlockedRecipes.Add(2);
                var c = m.SpawnCustomer("Cat");   // Cat likes recipe 2
                long coins0 = m.S.currency;
                Run(m, 30);
                Check(m.S.currency - coins0 == m.C.Recipe(2).price, "coins " + (m.S.currency - coins0));
                Check(m.S.tipJar > 0, "tip");
                long tip = m.S.tipJar;
                long a = m.ClaimTips(), b = m.ClaimTips();
                Check(a == tip && b == 0 && m.S.tipJar == 0, "tip claim twice");
            });
            T("Món thích luôn nằm trong công thức đã mở", () =>
            {
                var m = NewModel();
                foreach (var c in m.C.customers) Check(m.IsUnlocked(m.FavoriteOf(c.id)), c.id);
                m.S.unlockedRecipes.Add(2); m.S.unlockedRecipes.Add(3);
                foreach (var c in m.C.customers) Check(m.IsUnlocked(m.FavoriteOf(c.id)), c.id + " after unlock");
            });
            // ---------------- purchases & rewards
            T("Spam mua nâng cấp: không trừ/cộng hai lần", () =>
            {
                var m = NewModel(); m.S.currency = m.UpgradeCost("chef") + 10;
                int ok = 0; for (int i = 0; i < 5; i++) if (m.BuyUpgrade("chef")) ok++;
                Check(ok == 1 && m.UpLevel("chef") == 2 && m.S.currency == 10, $"ok={ok} lv={m.UpLevel("chef")} cur={m.S.currency}");
                Check(!m.BuyExpansion(1), "oven bought without level/coins");
            });
            T("Nhận thưởng nhiệm vụ đúng một lần", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 1); m.S.stats.doughMade = 2; m.CheckQuests();
                var q = m.S.quests[0];
                Check(q.completed, "quest not completed");
                bool a = m.ClaimQuest(q.id), b = m.ClaimQuest(q.id);
                Check(a && !b && m.S.currency == m.C.quests[0].reward, "double claim");
            });
            T("Nhiệm vụ hướng dẫn hoàn tất bằng sự kiện thật (không có Next)", () =>
            {
                var m = NewModel();
                Check(!m.S.quests[1].completed, "merge quest done without merging");
                Put(m, 0, 1); Put(m, 1, 1); m.S.stats.doughMade = 2; m.CheckQuests();
                m.DropDough(0, 1); m.CheckQuests();
                Check(m.S.quests[1].completed, "merge quest not completed after real merge");
            });
            // ---------------- save / reload / offline
            T("Reload giữa lúc nướng: không mất/nhân đôi job", () =>
            {
                var m = NewModel(); Put(m, 0, 3); m.Bake(0, 0); Run(m, 5);
                float rem = m.S.ovenJobs[0].remaining;
                var json = JsonUtility.ToJson(m.Snapshot(1000));
                var s2 = JsonUtility.FromJson<SaveData>(json);
                var m2 = new GameModel(m.C, s2, 9);
                Check(m2.S.ovenJobs[0].state == 1 && Mathf.Abs(m2.S.ovenJobs[0].remaining - rem) < 0.01f && m2.S.ovenJobs[0].jobId == m.S.ovenJobs[0].jobId && m2.S.stats.bakes == 1, "job changed");
                m2.ApplyElapsed(2000 - 1000);
                Check(m2.S.ovenJobs[0].state == 2, "offline didn't finish job");
                Check(m2.Collect(0) == CollectResult.Collected && m2.Collect(0) == CollectResult.NotReady, "double collect");
            });
            T("Reload khi khách đang cầm bánh chờ thanh toán: tính tiền đúng một lần", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 2;
                m.SpawnCustomer("Giraffe");
                for (int i = 0; i < 400 && !m.Customers.Any(c => c.carrying); i++) m.Tick(0.05f);
                Check(m.Customers.Any(c => c.carrying), "no carrying customer");
                var json = JsonUtility.ToJson(m.Snapshot(500));
                var m2 = new GameModel(m.C, JsonUtility.FromJson<SaveData>(json), 3);
                long c0 = m2.S.currency;
                int n = m2.SettlePendingSales();
                int n2 = m2.SettlePendingSales();
                Check(n == 1 && n2 == 0 && m2.S.currency - c0 == m.C.Recipe(1).price, $"settled {n}/{n2} coins {m2.S.currency - c0}");
                Check(m2.S.shelfStock[0].stock == 1, "stock " + m2.S.shelfStock[0].stock);
                // the original session continues: the same tx paid there must not be paid again in a later snapshot
                Run(m, 30);
                var m3 = new GameModel(m.C, JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(m.Snapshot(600))), 3);
                Check(m3.SettlePendingSales() == 0, "paid twice after continuing");
            });
            T("Reservation chưa lấy hàng được trả lại khi lưu/tải", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 1;
                m.SpawnCustomer("Cat");
                Check(m.Avail(0) == 0, "not reserved");
                var m2 = new GameModel(m.C, JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(m.Snapshot(10))), 3);
                Check(m2.Avail(0) == 1 && m2.S.shelfStock[0].stock == 1, "reservation not released");
            });
            T("Offline: không bán vượt tồn kho, tối đa 2 giờ, nhận một lần", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 2; m.S.shelfStock[0].stock = 4; m.S.inventory[1] = 5;
                m.ApplyElapsed(99999);
                var p = m.S.pendingOffline;
                Check(p.sold == 4 && p.coins == 4 * m.C.Recipe(2).price && p.capped, $"sold {p.sold} coins {p.coins}");
                Check(m.S.inventory[1] == 5, "auto restock used while locked");
                long a = m.ClaimOffline(), b = m.ClaimOffline();
                Check(a == p.coins && b == 0, "double claim");
                Check(NoNegatives(m), "negatives");
            });
            T("Offline có tự bổ sung (đã mở+bật): chỉ lấy từ kho, không tạo hàng", () =>
            {
                var m = NewModel(); m.S.autoRestockUnlocked = true; m.S.shelfStock[0].auto = true;
                m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 2; m.S.inventory[0] = 3;
                m.ApplyElapsed(7200);
                Check(m.S.pendingOffline.sold == 5 && m.S.inventory[0] == 0 && m.S.shelfStock[0].stock == 0, $"sold {m.S.pendingOffline.sold}");
            });
            T("Offline không tự tạo hoặc ghép bột", () =>
            {
                var m = NewModel(); Put(m, 0, 1); Put(m, 1, 1);
                int d = m.S.stats.doughMade, mg = m.S.stats.merges;
                m.ApplyElapsed(7200);
                Check(m.S.stats.doughMade == d && m.S.stats.merges == mg && m.S.boardSlots[0].level == 1 && m.S.boardSlots[1].level == 1, "board changed");
            });
            T("Delta âm (đồng hồ lùi) = 0", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 3;
                m.ApplyElapsed(-5000);
                Check(string.IsNullOrEmpty(m.S.pendingOffline.id) && m.S.shelfStock[0].stock == 3, "negative delta sold");
            });
            T("Resume và load dùng chung cơ chế: không tính thời gian hai lần", () =>
            {
                var m = NewModel(); m.S.shelfStock[0].recipe = 1; m.S.shelfStock[0].stock = 6;
                m.S.lastSimAt = 1000;
                // resume in-session after 120s
                m.ApplyElapsed(1120 - m.S.lastSimAt); m.S.lastSimAt = 1120;
                int sold1 = m.S.pendingOffline.sold;
                var m2 = new GameModel(m.C, JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(m.Snapshot(1120))), 3);
                m2.ApplyElapsed(1120 - m2.S.lastSimAt);
                Check(m2.S.pendingOffline.sold == sold1 && sold1 == Math.Min(6, (int)(120 / m.C.offline.saleInterval)) && m2.S.shelfStock[0].stock == 6 - sold1, $"sold1={sold1} after={m2.S.pendingOffline.sold}");
            });
            // ---------------- energy / break room
            T("Năng lượng thấp: chậm hơn nhưng vẫn chạy; nghỉ trên nội thất hồi phục", () =>
            {
                var m = NewModel(); var chef = m.Staff("chef"); chef.energy = 25;
                int d0 = m.S.stats.doughMade; Run(m, 30); int slow = m.S.stats.doughMade - d0;
                Check(slow > 0, "stopped when tired");
                chef.energy = 10; Run(m, 0.2f);
                Check(chef.state == (int)StaffState.ToRest && chef.restSpot == "beanbag", "not going to rest: " + chef.state);
                Run(m, 30);
                Check(chef.energy > 50 || chef.state == (int)StaffState.Working, "not recovering");
            });
            T("Mua nội thất thay đổi tốc độ hồi (có trần)", () =>
            {
                var m = NewModel(); float r0 = m.RestRate("beanbag");
                m.S.currency = 100000; m.S.xp = 100000;
                m.BuyFurniture("lamp"); m.BuyFurniture("lamp"); m.BuyFurniture("lamp");
                float r1 = m.RestRate("beanbag");
                m.BuyFurniture("bed"); m.BuyFurniture("bed"); m.BuyFurniture("bed");
                Check(r1 > r0 && m.RestRate("bed") <= m.C.energy.maxRecovery, $"r0={r0} r1={r1} bed={m.RestRate("bed")}");
                m.BuyFurniture("coffee"); m.BuyFurniture("coffee"); m.BuyFurniture("coffee");
                Check(m.DrainMult >= 1 - m.C.energy.maxDrainReduction - 0.001f, "drain cap");
            });
            T("Không số âm sau 30 phút mô phỏng có bot", () =>
            {
                var m = NewModel(); bool neg = false;
                for (int i = 0; i < 18000; i++) { m.Tick(0.1f); if (i % 5 == 0) BotPolicy.Execute(m, BotPolicy.Decide(m, true)); if (!NoNegatives(m)) { neg = true; break; } }
                Check(!neg, "negative values");
            });
            // ---------------- save store
            T("Save lỗi: dùng bản sao lưu; cả hai lỗi: giữ tệp lỗi, không xóa âm thầm", () =>
            {
                SaveStore.Slot = "unittest";
                SaveStore.DeleteAll();
                var m = NewModel(); m.S.currency = 1234;
                SaveStore.Save(m.Snapshot(1)); m.S.currency = 999; SaveStore.Save(m.Snapshot(2));
                File.WriteAllText(SaveStore.MainPath, "{ broken");
                var d = SaveStore.Load(out var st, out _);
                Check(st == SaveStore.Status.LoadedFromBackup && d != null && d.currency == 1234, "backup " + st);
                File.WriteAllText(SaveStore.BackupPath, "garbage");
                var d2 = SaveStore.Load(out var st2, out _);
                Check(st2 == SaveStore.Status.CorruptStartedNew && d2 == null, "corrupt " + st2);
                Check(Directory.GetFiles(SaveStore.Dir, "unittest_corrupt_*").Length > 0, "corrupt file not kept");
                File.WriteAllText(SaveStore.MainPath, "{\"saveVersion\":99}");
                var d3 = SaveStore.Load(out var st3, out _);
                Check(st3 == SaveStore.Status.NewerVersionReadOnly && SaveStore.ReadOnly && !SaveStore.Save(m.S), "newer version");
                SaveStore.ReadOnly = false;
                File.WriteAllText(SaveStore.MainPath, "{\"saveVersion\":1,\"lastSavedAt\":55,\"currency\":7,\"boardSlots\":[]}");
                var d4 = SaveStore.Load(out var st4, out _);
                Check(st4 == SaveStore.Status.Migrated && d4.currency == 7 && Math.Abs(d4.lastSimAt - 55) < 0.01, "migrate " + st4);
                foreach (var f in Directory.GetFiles(SaveStore.Dir, "unittest*")) File.Delete(f);
                SaveStore.Slot = "save";
            });

            var sb = new StringBuilder();
            int pass = results.Count(r => r.ok);
            sb.AppendLine("# Kết quả kiểm thử model (tự động)");
            sb.AppendLine();
            sb.AppendLine($"Chạy: {DateTime.Now:yyyy-MM-dd HH:mm} · Unity {Application.unityVersion} · batchmode · **{pass}/{results.Count} đạt**");
            sb.AppendLine();
            sb.AppendLine("| # | Kiểm tra | Kết quả | Ghi chú |");
            sb.AppendLine("|---|---|---|---|");
            for (int i = 0; i < results.Count; i++) sb.AppendLine($"| {i + 1} | {results[i].name} | {(results[i].ok ? "ĐẠT" : "**LỖI**")} | {results[i].note.Replace("|", "/")} |");
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/test_results.md", sb.ToString());
            Debug.Log($"[ModelTests] {pass}/{results.Count} passed\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(pass == results.Count ? 0 : 2);
        }
    }
}
