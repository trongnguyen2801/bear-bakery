// Headless 15-minute pacing simulation with the "reasonable player" bot (reaction delay ~0.8s, occasional pauses).
// Writes Docs/balance_sim.md: milestones, coins per minute, idle gaps (no useful action for > 8 s).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BB.Model;
using UnityEditor;
using UnityEngine;

namespace BB.EditorTools
{
    public static class BalanceSim
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Mô phỏng nhịp chơi 15 phút (headless, bot \"người chơi hợp lý\")");
            sb.AppendLine();
            sb.AppendLine("Bot dùng đúng các lệnh người chơi có (ghép, nướng, lấy bánh, bày kệ, mua, nút Tăng tốc, dọn rác, đóng gói đơn giao). Phản xạ 1,2 s/thao tác, +1 s khi chuyển giữa Bếp và Tiệm, bấm Tăng tốc/dọn rác tối đa 1 lần/3 s; đường đi khách dùng thời lượng mặc định. Số liệu là **mô phỏng**, không phải đo trên người thật.");
            sb.AppendLine();
            foreach (int seed in new[] { 1, 2, 3 })
            {
                var cfg = JsonUtility.FromJson<GameConfig>(File.ReadAllText("Assets/BearBakery/Resources/config.json"));
                var m = new GameModel(cfg, null, seed);
                var log = new List<string>();
                var perMin = new List<long>();
                float t = 0, actT = 0, idleStart = -1;
                var gaps = new List<(float at, float len)>();
                float dt = 0.1f, horizon = 15 * 60;
                int actions = 0; float lastHelp = -10; string zone = "kitchen";
                m.OnEvent += e =>
                {
                    string s = null;
                    switch (e.type)
                    {
                        case Ev.RecipeUnlocked: s = $"mở công thức cấp {e.a}"; break;
                        case Ev.UpgradeBought: s = $"mua nâng cấp {e.s} → cấp {e.b} ({e.amount} xu)"; break;
                        case Ev.ExpansionBought: s = $"mở rộng {(e.a == 0 ? "ô bàn" : e.a == 1 ? "LÒ" : "KỆ")} #{e.b + 1} ({e.amount} xu)"; break;
                        case Ev.FurnitureBought: s = $"mua nội thất {e.s} ({e.amount} xu)"; break;
                        case Ev.LevelUp: s = $"tiệm lên cấp {e.a}"; break;
                        case Ev.QuestCompleted: s = $"xong nhiệm vụ {e.s}"; break;
                        case Ev.StaffState: if (e.b == (int)StaffState.ToRest || e.b == (int)StaffState.RestInPlace) s = $"{e.s} đi nghỉ"; break;
                        case Ev.SaleCommitted: if (m.S.stats.sold == 1) s = "BÁN LẦN ĐẦU"; break;
                        case Ev.DeliveryOrder: if (m.S.stats.deliveries == 0) s = $"chó giao hàng nhận đơn đầu: {e.b} × bánh cấp {e.a} ({e.amount} xu)"; break;
                        case Ev.DeliveryDone: s = $"giao xong đơn #{m.S.stats.deliveries} (+{e.amount} xu)"; break;
                        case Ev.CleanerHired: s = "gấu mèo nhận việc dọn rác"; break;
                    }
                    if (s != null) log.Add($"{TimeFmt(m.S.playSeconds)}  {s}");
                };
                long lastCoins = 0;
                while (t < horizon)
                {
                    m.Tick(dt); t += dt; actT -= dt;
                    if (actT <= 0)
                    {
                        var a = BotPolicy.Decide(m, t - lastHelp > 3f);
                        bool useful = a.kind != BotKind.None && a.kind != BotKind.Boost;
                        string z = a.kind == BotKind.Stock || a.kind == BotKind.ClaimTips || a.kind == BotKind.Checkout || a.kind == BotKind.SetAuto
                                   || a.kind == BotKind.CleanTrash || a.kind == BotKind.ClaimCleaner || a.kind == BotKind.Deliver || (a.kind == BotKind.Boost && a.s != "chef") ? "shop"
                                 : a.kind == BotKind.Merge || a.kind == BotKind.Bake || a.kind == BotKind.Collect || a.kind == BotKind.Knead || (a.kind == BotKind.Boost && a.s == "chef") ? "kitchen" : zone;
                        float travel = z != zone ? 1.0f : 0f; zone = z;
                        if (a.kind == BotKind.Boost || a.kind == BotKind.CleanTrash) lastHelp = t;
                        if (a.kind != BotKind.None) { BotPolicy.Execute(m, a); actions++; }
                        actT = a.kind == BotKind.None ? 0.6f : 1.2f + travel;
                        if (useful) { if (idleStart >= 0 && t - idleStart > 8) gaps.Add((idleStart, t - idleStart)); idleStart = -1; }
                        else if (idleStart < 0) idleStart = t;
                    }
                    if ((int)(t / 60) > perMin.Count - 1 && Math.Abs(t % 60) < dt) { perMin.Add(m.S.totalEarned - lastCoins); lastCoins = m.S.totalEarned; }
                }
                var st = m.S.stats;
                sb.AppendLine($"## Lượt seed {seed}");
                sb.AppendLine();
                sb.AppendLine($"- Bán lần đầu: **{TimeFmt(st.firstSaleAt)}** (mục tiêu < 60 s)");
                sb.AppendLine($"- Mua nâng cấp đầu: **{TimeFmt(st.firstUpgradeAt)}** (mục tiêu 45–90 s)");
                sb.AppendLine($"- Mở lò thứ hai: **{TimeFmt(st.oven2At)}** (mục tiêu 3–5 phút)");
                sb.AppendLine($"- Mở công thức: " + string.Join(", ", Enumerable.Range(0, 6).Where(i => st.recipeUnlockAt[i] >= 0).Select(i => $"cấp {i + 1} @ {TimeFmt(st.recipeUnlockAt[i])}")));
                sb.AppendLine($"- Sau 15 phút: bán {st.sold} bánh, tổng thu {m.S.totalEarned} xu, cấp tiệm {m.ShopLevel}, {actions} thao tác (~{actions / 15f:0} /phút), ô bàn {m.S.boardUnlocked}, lò {m.OwnedOvens}, kệ {m.OwnedShelves}, nâng cấp [{string.Join(",", m.S.upgrades)}], nội thất mua {st.furnitureBought}");
                sb.AppendLine($"- Hệ thống mới: tăng tốc dùng {st.boostsUsed} lần · đơn giao {st.deliveries} · rác dọn tay {st.trashCleaned} / gấu mèo {st.trashCleanedByCleaner} · rác còn {m.S.trash.Count}");
                sb.AppendLine($"- Thu nhập theo phút: {string.Join(" · ", perMin.Select((v, i) => $"p{i + 1}:{v}"))}");
                sb.AppendLine($"- Khoảng chờ không có việc hữu ích > 8 s: {(gaps.Count == 0 ? "không có" : string.Join(", ", gaps.Select(g => $"{TimeFmt(g.at)} ({g.len:0}s)")))}");
                sb.AppendLine();
                sb.AppendLine("<details><summary>Dòng thời gian</summary>");
                sb.AppendLine();
                foreach (var l in log) sb.AppendLine("    " + l);
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/balance_sim.md", sb.ToString());
            Debug.Log("[BalanceSim]\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static string TimeFmt(double s) => s < 0 ? "chưa đạt" : $"{(int)(s / 60)}:{(int)(s % 60):00}";
    }
}
