// Entry point: loads config + save, builds camera/UI/world/input, runs the model tick, saves, handles resume/offline.
using System;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BB.View
{
    public class GameBoot : MonoBehaviour
    {
        public static GameBoot I;
        public GameModel M;
        public GameConfig Cfg;
        double lastUnix;
        float autosaveT;
        bool resetting;
        public static string AutoScenario;
        public static string OutDir;
        public static float TimeScale = 1f;

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Input.multiTouchEnabled = true;
            SaveStore.Slot = Arg("-bbSlot") ?? "save";
            AutoScenario = Arg("-bbAuto");
            OutDir = Arg("-bbOut");
            if (float.TryParse(Arg("-bbTimeScale") ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ts)) TimeScale = ts;
            if (AutoScenario != null) Application.runInBackground = true;
            Art.Init();
            Frames.Init();
            Cfg = JsonUtility.FromJson<GameConfig>(Resources.Load<TextAsset>("config").text);
            if (Arg("-bbBurn") == "1") Cfg.burn.burnEnabled = true;
            var save = SaveStore.Load(out var status, out var message);
            int seed = AutoScenario != null ? 4242 : Environment.TickCount;
            M = new GameModel(Cfg, save, seed);
            AudioMan.Init(!M.S.soundOn || AutoScenario != null);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.Init();
            var back = new GameObject("BackdropCamera").AddComponent<Camera>();   // clears outside the portrait frame on desktop
            back.depth = -10; back.clearFlags = CameraClearFlags.SolidColor; back.backgroundColor = new Color32(214, 176, 140, 255); back.cullingMask = 0;
            rig.cam.depth = 0;

            var uiGo = new GameObject("UI", typeof(RectTransform));
            uiGo.AddComponent<UIRoot>().Build(M);
            var world = new GameObject("World").AddComponent<World>();
            world.Build(M);
            new GameObject("Input").AddComponent<InputRouter>();
            InputRouter.Blocked = () => UIRoot.I.SheetOpen || UIRoot.I.ModalOpen;

            if (save != null)
            {
                M.SettlePendingSales();
                M.ApplyElapsed(SaveStore.UnixNow() - M.S.lastSimAt);   // the same path is used when resuming
            }
            M.CheckQuests();
            world.SyncAll(true);
            lastUnix = SaveStore.UnixNow();
            M.S.lastSimAt = lastUnix;
            M.OnEvent += e =>
            {
                if (e.type == Ev.OfflineReport && started) ShowOffline();
                switch (e.type)
                {   // important transactions -> save soon (throttled)
                    case Ev.UpgradeBought: case Ev.ExpansionBought: case Ev.FurnitureBought: case Ev.FurnitureUpgraded:
                    case Ev.QuestClaimed: case Ev.OfflineClaimed: case Ev.TipsClaimed: case Ev.OvenCollected:
                    case Ev.ShelfStocked: case Ev.BakeStarted: case Ev.DoughMerged: case Ev.SaleCommitted: case Ev.AutoRestockUnlocked:
                    case Ev.BoostStarted: case Ev.DeliveryPacked: case Ev.DeliveryDone: case Ev.CleanerHired: case Ev.TrashCleaned:
                        importantPending = true; break;
                }
            };

            rig.GoToZone(1, true);                                   // open on the shop, then slide to the kitchen (leftmost area)
            Tw.Delay(this, 1.4f, () => rig.GoToZone(0));
            Tw.Delay(this, 0.3f, () => StartupPopups(save == null, message));
            if (AutoScenario != null) gameObject.AddComponent<AutoPilot>();
            started = true;
        }

        bool started;

        void StartupPopups(bool fresh, string message)
        {
            if (AutoScenario != null && AutoScenario != "welcome") { if (!string.IsNullOrEmpty(M.S.pendingOffline.id)) ShowOffline(); return; }
            Action next = () => { if (!string.IsNullOrEmpty(M.S.pendingOffline.id)) ShowOffline(); };
            if (!string.IsNullOrEmpty(message)) UIRoot.I.ShowModal("Thông báo", message, "illust.Bear", "Đã hiểu", next);
            else if (fresh) UIRoot.I.ShowModal("Tiệm Bánh Gấu", "Gấu nhồi bột, bạn ghép bột thành bánh mới, nướng, bày kệ và bán cho khách. Mở tiệm nào!", "illust.Bear", "Mở tiệm", next);
            else next();
        }

        public void ShowOffline()
        {
            var p = M.S.pendingOffline;
            if (string.IsNullOrEmpty(p.id) || UIRoot.I.ModalOpen) return;
            int min = Mathf.Max(1, Mathf.RoundToInt(p.seconds / 60f));
            string body = $"Trong {min} phút vắng mặt{(p.capped ? " (tính tối đa 2 giờ)" : "")}:\n";
            if (p.sold > 0) body += $"Đã bán {p.sold} bánh trên kệ: +{p.coins} xu\n";
            if (p.bakedDone > 0) body += $"{p.bakedDone} mẻ bánh đã chín, chờ bạn lấy.\n";
            if (p.coins > 0 && p.sold == 0) body += $"Chó giao hàng đã mang tiền về: +{p.coins} xu\n";
            if (p.sold == 0 && p.bakedDone == 0 && p.coins == 0) body += "Không có gì thay đổi.";
            UIRoot.I.ShowModal("Tiệm vẫn mở cửa", body.Trim(), "ui.piggyFull", p.coins > 0 ? $"Nhận {p.coins} xu" : "Tiếp tục", () =>
            {
                long c = M.ClaimOffline();
                if (c > 0) { UIRoot.I.CoinFly(CameraRig.I.cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 10)), c); AudioMan.Play("se_Etc_GetReward"); }
                Save();
            });
        }

        void Update()
        {
            if (M == null) return;
            double now = SaveStore.UnixNow();
            double gap = now - lastUnix;
            lastUnix = now;
            if (gap > Cfg.offline.resumeGapSeconds && AutoScenario == null)
            {
                // app was paused/backgrounded: same mechanism as loading a save (no double counting)
                M.ApplyElapsed(gap);
                M.S.lastSimAt = now;
                return;
            }
            float dt = Time.deltaTime * TimeScale;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.1f));
            for (int i = 0; i < steps; i++) M.Tick(dt / steps);
            M.S.lastSimAt = now;
            autosaveT += Time.unscaledDeltaTime;
            sinceSave += Time.unscaledDeltaTime;
            if (autosaveT >= Cfg.economy.autosaveSeconds || (importantPending && sinceSave > 1f)) { autosaveT = 0; Save(); }
            if (Input.GetKeyDown(KeyCode.Escape)) UIRoot.I.CloseSheet();
            if (!UIRoot.I.SheetOpen && !UIRoot.I.ModalOpen)
            {   // desktop shortcuts for the side arrows
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) CameraRig.I.Step(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) CameraRig.I.Step(1);
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) CameraRig.I.ScrollTo(1f);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) CameraRig.I.ScrollTo(0f);
                if (Input.GetKeyDown(KeyCode.Space)) UIRoot.I.PressBoost();
            }
        }

        bool importantPending; float sinceSave;
        public int SaveCount { get; private set; }

        public void Save()
        {
            if (M == null || resetting) return;
            importantPending = false; sinceSave = 0; SaveCount++;
            SaveStore.Save(M.Snapshot(SaveStore.UnixNow()));
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        void OnApplicationQuit() { Save(); }

        public void ResetGame()
        {
            resetting = true;
            SaveStore.DeleteAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

    }
}
