// Automated in-build verification: plays through real input (synthetic pointer), takes screenshots,
// self-tests drag mapping at the current resolution and records FPS. Enabled with -bbAuto <scenario>.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BB.View
{
    public class AutoPilot : MonoBehaviour
    {
        GameModel M => GameBoot.I.M;
        string outDir;
        readonly StringBuilder log = new StringBuilder();
        readonly List<float> frameTimes = new List<float>();
        string res;
        int shot;
        float started;

        void Start()
        {
            outDir = GameBoot.OutDir ?? Path.Combine(Application.persistentDataPath, "auto");
            Directory.CreateDirectory(outDir);
            res = $"{Screen.width}x{Screen.height}";
            started = Time.realtimeSinceStartup;
            Log($"AutoPilot scenario={GameBoot.AutoScenario} screen={res} dpi={Screen.dpi} safe={Screen.safeArea} device={SystemInfo.deviceModel} gpu={SystemInfo.graphicsDeviceName} os={SystemInfo.operatingSystem}");
            M.OnEvent += e =>
            {
                switch (e.type)
                {
                    case Ev.SaleCommitted: if (M.S.stats.sold <= 3) Log($"sale #{M.S.stats.sold} +{e.amount}"); break;
                    case Ev.UpgradeBought: case Ev.ExpansionBought: case Ev.FurnitureBought: Log($"{e.type} {e.s} kind={e.a} idx={e.b} cost={e.amount}"); break;
                    case Ev.RecipeUnlocked: Log($"recipe unlocked {e.a}"); break;
                    case Ev.QuestCompleted: Log($"quest done {e.s}"); break;
                    case Ev.LevelUp: Log($"shop level {e.a}"); break;
                }
            };
            StartCoroutine(GameBoot.AutoScenario == "play" ? Play() : GameBoot.AutoScenario == "resume" ? Resume() : GameBoot.AutoScenario == "welcome" ? Welcome() : Shots());
        }

        void Log(string s)
        {
            string line = $"[{Time.realtimeSinceStartup - started,7:0.0}s | play {M.S.playSeconds,6:0.0}s] {s}";
            log.AppendLine(line);
            Debug.Log("[Auto] " + line);
        }

        void Update() { frameTimes.Add(Time.unscaledDeltaTime); }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            var path = Path.Combine(outDir, $"{res}_{++shot:00}_{name}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            Log("screenshot " + Path.GetFileName(path));
        }

        Vector2 W2S(Vector2 w) => CameraRig.I.WorldToScreen(w);

        IEnumerator Drag(Vector2 fromWorld, Vector2 toWorld, float dur = 0.45f, string midShot = null)
        {
            var ir = InputRouter.I;
            Vector2 a = W2S(fromWorld), b = W2S(toWorld);
            ir.SynthDown(a);
            yield return null; yield return null;
            int steps = Mathf.Max(4, (int)(dur / 0.03f));
            for (int i = 1; i <= steps; i++)
            {
                ir.SynthMove(Vector2.Lerp(a, b, i / (float)steps));
                if (midShot != null && i == steps * 2 / 3) yield return Shot(midShot);
                yield return new WaitForSeconds(dur / steps);
            }
            ir.SynthUp();
            yield return null; yield return null;
        }

        IEnumerator Tap(Vector2 world)
        {
            var ir = InputRouter.I;
            ir.SynthDown(W2S(world)); yield return null; yield return null;
            ir.SynthUp(); yield return null; yield return null;
        }

        IEnumerator FocusZone(float y) { CameraRig.I.Focus(y); yield return new WaitForSeconds(0.7f); }

        /// <summary>Zone + (in the shop) the selling floor / marketing street, waiting only when the camera has to move.</summary>
        IEnumerator FocusArea(float y)
        {
            var cam = CameraRig.I;
            int z = CameraRig.ZoneOfY(y);
            bool wantMkt = z == 1 && y < -27.2f;
            if (cam.ZoneIndex == z && (z != 1 || (cam.ShopSub() == "marketing") == wantMkt) && !cam.Transitioning) yield break;
            cam.Focus(y);
            yield return new WaitForSeconds(0.75f);
        }

        Vector2 RectCenter(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return new Vector2(-100, -100);
            return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        }

        /// <summary>Executes one bot decision, through synthetic input when it is a world interaction.</summary>
        IEnumerator Act(BotAction a)
        {
            switch (a.kind)
            {
                case BotKind.Merge:
                    if (CameraRig.I.Zone() != "kitchen") yield return FocusZone(WL.FocusKitchen);
                    yield return Drag(WL.DoughOnSlot(a.a), WL.DoughOnSlot(a.b));
                    break;
                case BotKind.Bake:
                    if (CameraRig.I.Zone() != "kitchen") yield return FocusZone(WL.FocusKitchen);
                    yield return Drag(WL.DoughOnSlot(a.a), WL.Ovens[a.b]);
                    break;
                case BotKind.Collect:
                    if (CameraRig.I.Zone() != "kitchen") yield return FocusZone(WL.FocusKitchen);
                    yield return Tap(WL.Ovens[a.a]);
                    break;
                case BotKind.Boost:
                {
                    // through the real bottom-bar button: go where that character works, then click the centre tile
                    float y = a.s == "chef" ? WL.FocusKitchen : a.s == "promoter" ? WL.FocusMarketing : a.s == "cashier" ? WL.FocusShop : WL.FocusBreak;
                    yield return FocusArea(y);
                    if (UIRoot.I.BoostIdForView() != a.s || !UIClickAt(RectCenter(UIRoot.I.BoostTileRect))) BotPolicy.Execute(M, a);
                    yield return new WaitForSeconds(0.2f);
                    break;
                }
                case BotKind.Stock:
                    if (CameraRig.I.Zone() != "shop") yield return FocusZone(WL.FocusShop);
                    BotPolicy.Execute(M, a);   // picker sheet interaction is covered by the screenshot pass
                    break;
                case BotKind.CleanTrash:
                {
                    var t = M.S.trash.FirstOrDefault(x => x.id.ToString() == a.s);
                    if (t == null) break;
                    yield return FocusArea(WL.FocusMarketing);
                    yield return Tap(WL.TrashSpots[t.spot]);
                    break;
                }
                case BotKind.ClaimCleaner:
                    yield return FocusArea(WL.FocusMarketing);
                    yield return Tap(WL.RaccoonHome + new Vector2(0, 0.6f));     // opens the side-quest sheet
                    yield return new WaitForSeconds(0.3f);
                    BotPolicy.Execute(M, a);
                    UIRoot.I.CloseSheet();
                    break;
                case BotKind.Deliver:
                    yield return FocusArea(WL.FocusShop);
                    yield return Tap(WL.DogHome + new Vector2(0, 0.6f));         // opens the delivery sheet
                    yield return new WaitForSeconds(0.3f);
                    BotPolicy.Execute(M, a);
                    UIRoot.I.CloseSheet();
                    break;
                case BotKind.ClaimTips:
                    if (CameraRig.I.Zone() != "shop") yield return FocusZone(WL.FocusShop);
                    yield return Tap(WL.PiggyPos);
                    break;
                case BotKind.VisitBreak:
                    yield return FocusZone(WL.FocusBreak);
                    yield return new WaitForSeconds(0.6f);
                    break;
                default:
                    BotPolicy.Execute(M, a);
                    break;
            }
        }

        // ------------------------------------------------------------------ screenshot scenario
        IEnumerator Shots()
        {
            UIRoot.I.CloseSheet();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("start_shop");
            yield return new WaitForSeconds(1.6f);
            yield return Shot("kitchen_start");
            // idle (no input) => tutorial hand appears after handIdleSeconds
            yield return new WaitForSeconds(M.C.tutorial.handIdleSeconds + 1.2f);
            yield return Shot("tutorial_hand_idle");
            // side-arrow navigation through the real UI raycast (the arrow must be the top target at its screen position)
            {
                int z0 = CameraRig.I.ZoneIndex;
                bool leftHidden = UIRoot.I.NavArrowRect(-1) == null || !UIRoot.I.NavArrowRect(-1).gameObject.activeInHierarchy;
                bool okR = UIClickAt(NavCenter(1)); yield return new WaitForSeconds(0.18f);
                yield return Shot("arrow_sliding");
                yield return new WaitForSeconds(0.4f);
                int z1 = CameraRig.I.ZoneIndex;
                yield return Shot("arrow_to_shop");
                bool okR2 = UIClickAt(NavCenter(1)); yield return new WaitForSeconds(0.6f);
                int z2 = CameraRig.I.ZoneIndex;
                yield return Shot("arrow_to_breakroom");
                bool rightHidden = UIRoot.I.NavArrowRect(1) == null || !UIRoot.I.NavArrowRect(1).gameObject.activeInHierarchy;
                bool okL = UIClickAt(NavCenter(-1)); yield return new WaitForSeconds(0.6f);
                int z3 = CameraRig.I.ZoneIndex;
                Log($"SELFTEST side arrows: {(okR && okR2 && okL && z0 == 0 && z1 == 1 && z2 == 2 && z3 == 1 && leftHidden && rightHidden ? "PASS" : "FAIL")} (zones {z0}->{z1}->{z2}->{z3}, left hidden at kitchen={leftHidden}, right hidden at break room={rightHidden})");
                // shop: a vertical drag scrolls down to the marketing street (real pointer path), the hint pill scrolls back
                var cam = CameraRig.I;
                float f0 = cam.ScrollFrac;
                var fr = cam.frame;
                var ir = InputRouter.I;
                Vector2 a0 = new Vector2(fr.center.x + fr.width * 0.12f, fr.y + fr.height * 0.34f), a1 = a0 + new Vector2(0, fr.height * 0.3f);
                ir.SynthDown(a0); yield return null; yield return null;
                for (int i = 1; i <= 12; i++) { ir.SynthMove(Vector2.Lerp(a0, a1, i / 12f)); yield return new WaitForSeconds(0.025f); }
                ir.SynthUp(); yield return new WaitForSeconds(0.8f);
                float f1 = cam.ScrollFrac;
                yield return Shot("shop_marketing_street");
                Log($"SELFTEST shop drag scrolls to marketing: {(f1 > f0 + 0.3f && cam.ShopSub() == "marketing" ? "PASS" : "FAIL")} (scroll {f0:0.00}->{f1:0.00}, range {cam.ScrollRange(1):0.00}u, boost button -> {UIRoot.I.BoostIdForView()})");
                cam.ScrollTo(0f); yield return new WaitForSeconds(0.7f);
                // kitchen: no scrolling there
                CameraRig.I.GoToZone(0); yield return new WaitForSeconds(0.7f);
                float camY = CameraRig.I.cam.transform.position.y;
                yield return Drag(new Vector2(3.3f, -16.0f), new Vector2(3.3f, -12.0f), 0.4f);
                yield return new WaitForSeconds(0.3f);
                Log($"SELFTEST kitchen does not scroll: {(Mathf.Abs(CameraRig.I.cam.transform.position.y - camY) < 0.01f ? "PASS" : "FAIL")}");
            }
            // speed-up button (bottom bar, real UI raycast) boosts the chef in the kitchen
            {
                int d0 = M.S.stats.doughMade;
                bool clicked = UIClickAt(RectCenter(UIRoot.I.BoostTileRect));
                yield return new WaitForSeconds(0.3f);
                var b = M.Boost("chef");
                Log($"SELFTEST speed-up button: {(clicked && b.remaining > 0 && M.BoostActive("chef") ? "PASS" : "FAIL")} (chef boost {b.remaining:0.0}s, x{M.BoostMult("chef")})");
                yield return Shot("boost_active");
            }
            // play until 2 same-level doughs exist, then self-test a drag merge through the real input path
            float t0 = Time.time;
            while (Time.time - t0 < 30f && !HasPair(out _, out _)) yield return new WaitForSeconds(0.3f);
            if (HasPair(out int pa, out int pb))
            {
                int merges = M.S.stats.merges;
                int lv = M.S.boardSlots[pa].level;
                yield return Drag(WL.DoughOnSlot(pa), WL.DoughOnSlot(pb), 0.6f, "drag_merge");
                yield return new WaitForSeconds(0.1f);
                bool ok = M.S.stats.merges == merges + 1 && M.S.boardSlots[pb].level == lv + 1 && M.S.boardSlots[pa].level == 0;
                Log($"SELFTEST drag-merge at {res}: {(ok ? "PASS" : "FAIL")} (slot {pa}->{pb}, level {lv}->{M.S.boardSlots[pb].level})");
                yield return Shot("after_merge");
            }
            // drop outside the board returns the dough (no state change)
            int any = Enumerable.Range(0, M.S.boardUnlocked).FirstOrDefault(i => M.S.boardSlots[i].level > 0);
            if (M.S.boardSlots[any].level > 0)
            {
                long id = M.S.boardSlots[any].id;
                yield return Drag(WL.DoughOnSlot(any), new Vector2(3.3f, -11.0f), 0.4f);
                yield return new WaitForSeconds(0.3f);
                Log($"SELFTEST drop-outside returns item: {(M.S.boardSlots[any].id == id ? "PASS" : "FAIL")}");
            }
            // bake
            int slot = Enumerable.Range(0, M.S.boardUnlocked).OrderByDescending(i => M.S.boardSlots[i].level).First();
            if (M.S.boardSlots[slot].level > 0)
            {
                int bakes = M.S.stats.bakes;
                yield return Drag(WL.DoughOnSlot(slot), WL.Ovens[0], 0.5f, "drag_to_oven");
                Log($"SELFTEST drag-to-oven: {(M.S.stats.bakes == bakes + 1 ? "PASS" : "FAIL")}");
            }
            yield return new WaitForSeconds(0.6f);
            // simulation keeps running while a bottom sheet is open / camera moved
            {
                var o = M.S.ovenJobs[0];
                float r0 = o.remaining; int d0 = M.S.stats.doughMade;
                UIRoot.I.OpenSheet(SheetKind.Notebook, 0);
                CameraRig.I.Focus(WL.FocusShop);
                yield return new WaitForSeconds(2.5f);
                Log($"SELFTEST sim continues with sheet open + other zone: {(o.state != 1 || o.remaining < r0 - 1.5f ? "PASS" : "FAIL")} (oven {r0:0.0}s -> {o.remaining:0.0}s, dough {d0}->{M.S.stats.doughMade})");
                UIRoot.I.CloseSheet();
                CameraRig.I.Focus(WL.FocusKitchen);
                yield return new WaitForSeconds(0.8f);
            }
            yield return Shot("oven_baking");
            // let the bot run the loop until the first sale
            yield return RunBot(240f, () => M.S.stats.sold >= 2, true);
            yield return Shot("kitchen_progress");
            yield return FocusZone(WL.FocusShop);
            yield return new WaitForSeconds(1.0f);
            yield return Shot("shop_customers");
            yield return RunBot(120f, () => M.S.stats.upgradesBought >= 2 && M.Customers.Count >= 2, true);
            yield return FocusZone(WL.FocusShop);
            yield return Shot("shop_busy");
            // marketing street: trash, raccoon side quest, sparrow on the stool with the attraction bar
            yield return FocusArea(WL.FocusMarketing);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("shop_marketing");
            if (!M.S.cleanerHired) { UIRoot.I.OpenSheet(SheetKind.Cleaner); yield return new WaitForSeconds(0.5f); yield return Shot("sheet_cleaner"); UIRoot.I.CloseSheet(); }
            // delivery dog: the order system opens after the 'sell 15' goal (screenshot aid: unlock it now if the run is short)
            var dq = M.S.quests.FirstOrDefault(q => q.id == M.C.delivery.unlockAfterQuest);
            if (dq != null && !dq.completed) { dq.completed = true; dq.claimed = true; Log("screenshot aid: delivery unlocked early"); }
            if (M.S.delivery.state == (int)DeliveryState.None) M.S.delivery.timer = 0.1f;
            yield return FocusArea(WL.FocusShop);
            yield return new WaitForSeconds(1.0f);
            yield return Shot("shop_delivery_order");
            UIRoot.I.OpenSheet(SheetKind.Delivery); yield return new WaitForSeconds(0.5f);
            yield return Shot("sheet_delivery");
            if (M.S.delivery.state == (int)DeliveryState.Open && !M.CanPackDelivery) M.S.inventory[M.S.delivery.recipe - 1] += M.S.delivery.qty;   // screenshot aid
            M.PackDelivery(); UIRoot.I.CloseSheet();
            yield return new WaitForSeconds(0.9f);
            yield return Shot("shop_delivery_out");
            UIRoot.I.OpenSheet(SheetKind.Kitchen); yield return new WaitForSeconds(0.5f);
            yield return Shot("sheet_kitchen");
            UIRoot.I.OpenSheet(SheetKind.ShelfPick, 0); yield return new WaitForSeconds(0.5f);
            yield return Shot("sheet_shelfpick");
            UIRoot.I.OpenSheet(SheetKind.Notebook, 1); yield return new WaitForSeconds(0.5f);
            yield return Shot("notebook_recipes");
            UIRoot.I.OpenSheet(SheetKind.Notebook, 2); yield return new WaitForSeconds(0.5f);
            yield return Shot("notebook_guests");
            UIRoot.I.CloseSheet(); yield return new WaitForSeconds(0.4f);
            yield return FocusZone(WL.FocusBreak);
            yield return Shot("breakroom");
            UIRoot.I.OpenSheet(SheetKind.Furniture); yield return new WaitForSeconds(0.5f);
            yield return Shot("sheet_furniture");
            UIRoot.I.CloseSheet();
            // force low energy (screenshot aid only) to show staff walking to the break room and resting on furniture
            M.Staff("chef").energy = 12; M.Staff("cashier").energy = 12;
            yield return new WaitForSeconds(0.4f);
            yield return Shot("staff_to_rest");
            yield return new WaitForSeconds(1.6f);
            yield return Shot("staff_resting");
            yield return FocusZone(WL.FocusKitchen);
            yield return Shot("kitchen_chef_away");
            yield return new WaitForSeconds(1f);
            yield return Shot("kitchen_end");
            Finish();
        }

        IEnumerator Welcome()
        {
            yield return new WaitForSeconds(1.2f);
            yield return Shot("welcome");
            Finish();
        }

        IEnumerator Resume()
        {
            Log($"LOADED coins={M.S.currency} sold={M.S.stats.sold} lastSimAgo={SaveStore.UnixNow() - M.S.lastSavedAt:0}s pendingOffline={M.S.pendingOffline.id} coins={M.S.pendingOffline.coins} soldOff={M.S.pendingOffline.sold} baked={M.S.pendingOffline.bakedDone}");
            Log("OVENS " + string.Join(" | ", M.S.ovenJobs.Select((o, i) => $"#{i} owned={o.owned} state={o.state} recipe={o.recipe} rem={o.remaining:0.0} job={o.jobId}")));
            Log("SHELVES " + string.Join(" | ", M.S.shelfStock.Select((o, i) => $"#{i} {o.recipe}x{o.stock}")) + " inv=" + string.Join(",", M.S.inventory));
            yield return new WaitForSeconds(1.0f);
            yield return Shot("resume_offline_popup");
            long before = M.S.currency; long got = M.ClaimOffline(); long again = M.ClaimOffline();
            Log($"SELFTEST offline claim once: {(again == 0 && M.S.currency == before + got ? "PASS" : "FAIL")} (+{got}, second +{again})");
            yield return new WaitForSeconds(0.5f);
            Finish();
        }

        Vector2 NavCenter(int dir)
        {
            var rt = UIRoot.I.NavArrowRect(dir);
            if (rt == null || !rt.gameObject.activeInHierarchy) return new Vector2(-100, -100);
            var c = rt.TransformPoint(rt.rect.center);
            return RectTransformUtility.WorldToScreenPoint(null, c);
        }

        /// <summary>Click whatever uGUI element is on top at a screen point (same raycast a finger would hit).</summary>
        bool UIClickAt(Vector2 sp)
        {
            var es = EventSystem.current; if (es == null) return false;
            var data = new PointerEventData(es) { position = sp, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); es.RaycastAll(data, hits);
            if (hits.Count == 0) return false;
            var go = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            if (go == null) return false;
            data.pointerPress = go; data.pointerCurrentRaycast = hits[0];
            ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
            return true;
        }

        bool HasPair(out int a, out int b)
        {
            a = b = -1;
            for (int i = 0; i < M.S.boardUnlocked; i++)
                for (int j = i + 1; j < M.S.boardUnlocked; j++)
                    if (M.S.boardSlots[i].level > 0 && M.S.boardSlots[i].level == M.S.boardSlots[j].level && M.S.boardSlots[i].level < M.C.MaxRecipe) { a = i; b = j; return true; }
            return false;
        }

        IEnumerator RunBot(float maxSeconds, System.Func<bool> until, bool help)
        {
            float t0 = Time.time;
            while (Time.time - t0 < maxSeconds && (until == null || !until()))
            {
                var a = BotPolicy.Decide(M, help);
                if (a.kind != BotKind.None) yield return Act(a);
                yield return new WaitForSeconds(a.kind == BotKind.None ? 0.5f : 0.35f);
            }
        }

        // ------------------------------------------------------------------ long play scenario (FPS + timeline + clip frames)
        IEnumerator Play()
        {
            float seconds = 600;
            var arg = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arg.Length - 1; i++) if (arg[i] == "-bbSeconds") float.TryParse(arg[i + 1], out seconds);
            bool frames = arg.Contains("-bbFrames");
            UIRoot.I.CloseSheet();
            yield return new WaitForSeconds(1.5f);
            float t0 = Time.time, nextShot = 0, nextFrame = 0;
            int frameNo = 0;
            string frameDir = Path.Combine(outDir, "frames"); if (frames) { Directory.CreateDirectory(frameDir); StartCoroutine(Grab(frameDir, seconds)); }
            while (Time.time - t0 < seconds)
            {
                var a = BotPolicy.Decide(M, true);
                if (a.kind != BotKind.None) yield return Act(a);
                if (Time.time - t0 >= nextShot) { nextShot += 60f; yield return Shot($"t{Mathf.RoundToInt(Time.time - t0):000}s"); }
                yield return new WaitForSeconds(a.kind == BotKind.None ? 0.3f : 0.25f);
            }
            Finish();
        }

        IEnumerator Grab(string dir, float seconds)
        {
            int n = 0; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < seconds && n < 2400)
            {
                yield return new WaitForEndOfFrame();
                float due = t0 + n / 15f;
                if (Time.realtimeSinceStartup < due) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"f{n++:00000}.jpg"), tex.EncodeToJPG(82));
                Destroy(tex);
            }
        }

        void Finish()
        {
            var ft = frameTimes.Skip(30).OrderBy(x => x).ToList();
            if (ft.Count > 0)
            {
                float avg = ft.Average();
                float p95 = ft[(int)(ft.Count * 0.95f)], p99 = ft[(int)(ft.Count * 0.99f)];
                Log($"PERF frames={ft.Count} avgFPS={1f / avg:0.0} p95 frame={p95 * 1000:0.0}ms p99={p99 * 1000:0.0}ms worst={ft.Last() * 1000:0.0}ms (targetFrameRate=60, {SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName})");
            }
            var st = M.S.stats;
            Log($"STATS play={M.S.playSeconds:0}s sold={st.sold} coins={M.S.currency} earned={M.S.totalEarned} level={M.ShopLevel} firstSale={st.firstSaleAt:0.0}s firstUpgrade={st.firstUpgradeAt:0.0}s oven2={st.oven2At:0.0}s recipes={string.Join(",", M.S.unlockedRecipes)} missingSprites={Art.Missing.Count} saves={GameBoot.I.SaveCount}");
            File.WriteAllText(Path.Combine(outDir, $"autolog_{res}_{GameBoot.AutoScenario}.txt"), log.ToString());
            GameBoot.I.Save();
            Application.Quit();
        }
    }
}
