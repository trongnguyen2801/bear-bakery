
// Shop extras: delivery dog next to the counter, marketing street (sparrow on a stool + attraction bar),
// trash dropped by customers and the raccoon cleaner. Visuals only read model state.
using System.Collections.Generic;
using System.Linq;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BB.View
{
    public partial class World
    {
        NpcView dog, raccoon;
        readonly Dictionary<int, SpriteRenderer> trashViews = new Dictionary<int, SpriteRenderer>();   // by spot
        RectTransform mktBar; Bar mktGauge; Text mktText; RectTransform mktTrash; Text mktTrashText; Image mktIcon;
        float mktPopT;
        const float DogWalk = 1.6f;

        void BuildStreet()
        {
            // pavement in front of the shop, curb, road (marketing area below the storefront)
            float sideTop = WL.FacadeBottom + 0.05f, sideBot = -29.95f, roadBot = WL.WorldBottom - 3f;
            SR.Stretched(bgRoot, "Sidewalk", "bg.white", new Vector2(0, (sideTop + sideBot) / 2), new Vector2(WL.BgWidth, sideTop - sideBot), -1960, new Color32(240, 228, 208, 255));
            SR.Stretched(bgRoot, "Curb", "bg.white", new Vector2(0, sideBot), new Vector2(WL.BgWidth, 0.1f), -1955, new Color32(206, 182, 150, 255));
            SR.Stretched(bgRoot, "Road", "bg.white", new Vector2(0, (sideBot + roadBot) / 2), new Vector2(WL.BgWidth, sideBot - roadBot), -1962, new Color32(222, 208, 188, 255));
            for (int k = -4; k <= 4; k++)
                SR.Stretched(bgRoot, "Dash" + k, "bg.white", new Vector2(k * 1.25f, -33.15f), new Vector2(0.62f, 0.08f), -1958, new Color32(246, 238, 224, 255));

            // delivery dog (left of the counter)
            dog = MakeNpc("Dog", "npc.dog.idle", WL.DogHome, 1.0f);
            dog.carry = SR.Make(dog.root, "Box", "ui.box", WL.DogHome + new Vector2(0.18f, 0.55f), 3, 0.42f);
            dog.carry.enabled = false;
            dog.hit = AddHit(Box(WL.DogHome + new Vector2(0, 0.6f), new Vector2(1.1f, 1.3f)), 8, "dog");
            dog.bubble = UIRoot.I.MakeOrderBubble("DogOrder", out dog.bubbleIcon, out dog.bubbleText, out dog.bubbleCheck);
            UIRoot.I.Follow(dog.bubble, () => (Vector3)(dog.pos + new Vector2(0.05f, 1.42f)), Vector2.zero);
            dog.status = UIRoot.I.MakeLabel("DogStatus", out dog.statusText, "ui.box");
            dog.status.sizeDelta = new Vector2(112, 26);
            UIRoot.I.Follow(dog.status, () => (Vector3)(WL.DogHome + new Vector2(0.1f, 0.55f)), Vector2.zero);

            // raccoon (cleaner side quest) on the pavement
            raccoon = MakeNpc("Raccoon", "npc.raccoon.idle", WL.RaccoonHome, 0.95f);
            raccoon.hit = AddHit(Box(WL.RaccoonHome + new Vector2(0, 0.65f), new Vector2(1.1f, 1.4f)), 8, "raccoon");
            raccoon.bubble = UIRoot.I.MakeAlertBubble("RaccoonAlert", out raccoon.bubbleIcon, out raccoon.bubbleText);
            UIRoot.I.Follow(raccoon.bubble, () => (Vector3)(raccoon.pos + new Vector2(0.15f, 1.55f)), Vector2.zero);

            // trash spots (tap to clean)
            for (int k = 0; k < GameModel.TrashSpots && k < WL.TrashSpots.Length; k++)
            {
                int spot = k;
                AddHit(Box(WL.TrashSpots[k], new Vector2(0.95f, 0.85f)), 9, "trash", k, null, () => M.S.trash.Any(t => t.spot == spot));
            }

            // marketing bar next to the sparrow
            mktBar = UIRoot.I.MakeMarketingBar("MarketingBar", out mktGauge, out mktText, out mktIcon, out mktTrash, out mktTrashText);
            UIRoot.I.Follow(mktBar, () => (Vector3)WL.MarketingBarPos, Vector2.zero);
        }

        NpcView MakeNpc(string name, string anim, Vector2 at, float scale)
        {
            var v = new NpcView { pos = at };
            v.root = new GameObject("Npc_" + name).transform; v.root.SetParent(actorRoot, false);
            v.root.position = at;
            v.group = v.root.gameObject.AddComponent<SortingGroup>();
            v.group.sortingOrder = WL.SortOrder(at.y);
            v.shadow = SR.Make(v.root, "Shadow", "char.shadow", at + new Vector2(0, 0.02f), 0, 1.1f);
            v.shadow.color = new Color(1, 1, 1, 0.5f);
            var body = SR.Make(v.root, "Body", null, at, 1);
            v.anim = body.gameObject.AddComponent<FrameAnim>();
            v.anim.SetBaseScale(Vector3.one * scale);
            v.anim.Play(anim, true, Random.value);
            return v;
        }

        void PlaceNpc(NpcView v, Vector2 p, float alpha)
        {
            v.pos = p;
            v.root.position = p;
            v.anim.transform.position = p;
            v.shadow.transform.position = p + new Vector2(0, 0.02f);
            v.group.sortingOrder = WL.SortOrder(p.y);
            if (!Mathf.Approximately(alpha, v.alpha))
            {
                v.alpha = alpha;
                var c = v.anim.sr.color; c.a = alpha; v.anim.sr.color = c;
                v.shadow.color = new Color(1, 1, 1, 0.5f * alpha);
                if (v.carry != null) { var cc = v.carry.color; cc.a = alpha; v.carry.color = cc; }
            }
        }

        // ================================================================== per frame
        void UpdateStreet()
        {
            UpdateDog();
            UpdateRaccoon();
            UpdateMarketingBar();
        }

        void UpdateDog()
        {
            var d = M.S.delivery;
            bool unlocked = M.DeliveryUnlocked;
            Vector2 p = WL.DogHome; float a = 1; bool walking = false; bool carrying = false; float dx = 0;
            if (d.state == (int)DeliveryState.Out)
            {
                float elapsed = M.C.delivery.travelSeconds - d.timer;
                if (elapsed < DogWalk) { float k = elapsed / DogWalk; p = Vector2.Lerp(WL.DogHome, WL.DogAway, k); walking = true; carrying = true; dx = -1; a = 1f - Mathf.Clamp01((k - 0.7f) / 0.3f); }
                else if (d.timer < DogWalk) { float k = 1f - d.timer / DogWalk; p = Vector2.Lerp(WL.DogAway, WL.DogHome, k); walking = true; dx = 1; a = Mathf.Clamp01(k / 0.3f); }
                else a = 0;
            }
            PlaceNpc(dog, p, a);
            if (dx < 0) dog.anim.sr.flipX = false; else if (dx > 0) dog.anim.sr.flipX = true;
            if (!walking && dog.anim.sr.flipX) dog.anim.sr.flipX = false;
            dog.anim.Play(walking ? "npc.dog.walk" : "npc.dog.idle");
            dog.carry.enabled = carrying;
            dog.carry.transform.position = p + new Vector2(dog.anim.sr.flipX ? -0.2f : 0.2f, 0.55f);
            dog.hit.rect = Box(WL.DogHome + new Vector2(0, 0.6f), new Vector2(1.1f, 1.3f));
            // order bubble / status
            bool open = d.state == (int)DeliveryState.Open;
            dog.bubble.gameObject.SetActive(open && a > 0.9f);
            if (open)
            {
                dog.bubbleIcon.sprite = Art.S(Art.Bread(d.recipe));
                int have = M.S.inventory[d.recipe - 1];
                dog.bubbleText.text = "×" + d.qty;
                dog.bubbleCheck.enabled = have >= d.qty;
                float s = M.CanPackDelivery ? 1f + 0.06f * Mathf.Sin(Time.time * 5f) : 1f;
                dog.bubble.localScale = new Vector3(s, s, 1);
            }
            bool showStatus = d.state == (int)DeliveryState.Out || !unlocked;
            dog.status.gameObject.SetActive(showStatus);
            if (d.state == (int)DeliveryState.Out) dog.statusText.text = $"Đang giao {Mathf.CeilToInt(d.timer)}s";
            else if (!unlocked) dog.statusText.text = "Giao hàng";
        }

        void UpdateRaccoon()
        {
            Vector2 p = WL.RaccoonHome; bool walking = false, sweep = false; float dx = 0;
            if (M.S.cleanerHired)
            {
                int spot = M.CleanerTargetSpot;
                Vector2 target = spot >= 0 && spot < WL.TrashSpots.Length ? WL.TrashSpots[spot] + new Vector2(-0.45f, 0.05f) : WL.RaccoonHome;
                float k = M.CleanerPhaseLength > 0 ? Mathf.Clamp01(M.CleanerTimer / M.CleanerPhaseLength) : 1f;
                switch (M.Cleaner)
                {
                    case CleanerState.Walking: p = Vector2.Lerp(WL.RaccoonHome, target, Ease.InOutQuad(k)); walking = true; dx = target.x - WL.RaccoonHome.x; break;
                    case CleanerState.Sweeping: p = target; sweep = true; break;
                    case CleanerState.Returning: p = Vector2.Lerp(target, WL.RaccoonHome, Ease.InOutQuad(k)); walking = true; dx = WL.RaccoonHome.x - target.x; break;
                }
            }
            PlaceNpc(raccoon, p, 1);
            if (dx > 0.01f) raccoon.anim.sr.flipX = true; else if (dx < -0.01f) raccoon.anim.sr.flipX = false;
            raccoon.anim.Play(walking ? "npc.raccoon.walk" : sweep ? "npc.raccoon.sweep" : "npc.raccoon.idle");
            if (sweep)
            {
                raccoon.dustT -= Time.deltaTime;
                if (raccoon.dustT <= 0) { raccoon.dustT = 0.35f; Fx.Dust(p + new Vector2(0.45f, 0.05f), 3, 0.3f, 0.4f); }
            }
            raccoon.hit.rect = Box(p + new Vector2(0, 0.65f), new Vector2(1.1f, 1.4f));
            // side quest marker: "!" while the quest is open, CLEAR!! when it can be claimed
            bool show = !M.S.cleanerHired && (M.CleanerQuestAvailable || M.CleanerQuestDone);
            raccoon.bubble.gameObject.SetActive(show);
            if (show)
            {
                bool done = M.CleanerQuestDone;
                raccoon.bubbleIcon.enabled = done;
                raccoon.bubbleText.text = done ? "" : "!";
                float s = 1f + (done ? 0.08f : 0.04f) * Mathf.Sin(Time.time * (done ? 7f : 4f));
                raccoon.bubble.localScale = new Vector3(s, s, 1);
            }
        }

        void UpdateMarketingBar()
        {
            float prog = M.MarketingProgress;
            mktGauge.Set(prog);
            bool waiting = M.MarketingWaiting;
            if (waiting)
            {
                bool noStock = M.TotalAvailable() <= 0;
                mktText.text = noStock ? "Hết bánh trên kệ!" : "Tiệm đang đông";
                mktText.color = noStock ? Pal.Red : Pal.Brown;
                mktGauge.fill.color = Color.Lerp(Pal.Green, Color.white, 0.3f + 0.3f * Mathf.Sin(Time.time * 6f));
            }
            else
            {
                mktText.text = $"Khách tới sau {Mathf.CeilToInt(M.MarketingSecondsLeft)}s";
                mktText.color = Pal.Brown;
                mktGauge.fill.color = M.BoostActive("promoter") ? Pal.Orange : Pal.Green;
            }
            int n = M.S.trash.Count;
            mktTrash.gameObject.SetActive(n > 0);
            if (n > 0) mktTrashText.text = $"-{Mathf.RoundToInt(Mathf.Min(M.C.marketing.trashPenaltyCap, M.C.marketing.trashPenalty * n) * 100)}%";
            if (mktPopT > 0) mktPopT -= Time.deltaTime;
        }

        // ================================================================== trash
        void SyncTrash(bool instant)
        {
            foreach (var t in M.S.trash) if (!trashViews.ContainsKey(t.spot)) SpawnTrashView(t.spot, t.kind, instant);
            var gone = trashViews.Keys.Where(k => !M.S.trash.Any(t => t.spot == k)).ToList();
            foreach (var k in gone) { if (trashViews[k]) Destroy(trashViews[k].gameObject); trashViews.Remove(k); }
        }

        void SpawnTrashView(int spot, int kind, bool instant)
        {
            if (spot < 0 || spot >= WL.TrashSpots.Length) return;
            var p = WL.TrashSpots[spot];
            var sr = SR.Make(objRoot, "Trash" + spot, "trash." + Mathf.Clamp(kind, 0, 3), p, WL.SortOrder(p.y - 0.1f), 1.05f);
            trashViews[spot] = sr;
            if (!instant) { Tw.Pop(sr.transform, 0.3f, 0.3f, Vector3.one * 1.05f); Fx.Dust(p, 3, 0.3f, 0.4f); }
        }

        void RemoveTrashView(int spot, bool byPlayer, long reward)
        {
            if (!trashViews.TryGetValue(spot, out var sr)) return;
            trashViews.Remove(spot);
            var p = (Vector2)sr.transform.position;
            Fx.Dust(p, 5, 0.45f, 0.55f);
            Fx.Stars(p, 3, 0.5f, "fx.twinkle1", 0.35f);
            if (byPlayer && reward > 0) UIRoot.I.CoinFly(p, reward);
            AudioMan.Play(byPlayer ? "se_Etc_Bubble" : "se_Etc_UseItem", 0.6f);
            var tr = sr.transform;
            Tw.Run(sr, 0.22f, t => { if (tr) tr.localScale = Vector3.one * 1.05f * (1f - t); }, Ease.InQuad, () => { if (sr) Destroy(sr.gameObject); });
        }

        // ================================================================== events
        void OnStreetEvent(GameEvent e)
        {
            switch (e.type)
            {
                case Ev.MarketingAttract:
                {
                    var p = staff["promoter"]; p.fastUntil = Time.time + 0.8f;
                    AudioMan.Play("se_Etc_Speak_Sparrow", 0.7f);
                    UIRoot.I.FloatText(WL.PromoterPos + new Vector2(0.3f, 1.25f), "Mời ghé tiệm!", Pal.Orange);
                    Tw.Pop(mktBar, 0.85f, 0.25f);
                    break;
                }
                case Ev.TrashSpawned: SpawnTrashView(e.a, e.b, false); break;
                case Ev.TrashCleaned: RemoveTrashView(e.a, e.b == 0, e.amount); break;
                case Ev.CleanerHired:
                    Fx.Stars(raccoon.pos + new Vector2(0, 0.8f), 7, 1f);
                    Tw.Squash(raccoon.root, 0.15f, 0.35f, Vector3.one);
                    break;
                case Ev.DeliveryOrder:
                    AudioMan.Play("se_Etc_GetTicket", 0.6f);
                    Tw.Pop(dog.bubble, 0.4f, 0.3f);
                    break;
                case Ev.DeliveryPacked:
                    AudioMan.Play("se_Shop_OpenInventory", 0.7f);
                    Fx.Stars(WL.DogHome + new Vector2(0, 0.8f), 4, 0.6f);
                    break;
                case Ev.DeliveryDone:
                    if (e.c == 0)
                    {
                        UIRoot.I.CoinFly(WL.DogHome + new Vector2(0, 0.8f), e.amount);
                        UIRoot.I.FloatText(WL.DogHome + new Vector2(0.3f, 1.5f), "+" + e.amount, Pal.GreenDark);
                        AudioMan.Play("se_Etc_GetReward", 0.7f);
                    }
                    break;
                case Ev.BoostStarted:
                    if (staff.TryGetValue(e.s, out var sv))
                    {
                        Fx.Stars((Vector2)StaffHead(sv) - new Vector2(0, 0.4f), 7, 1.0f);
                        Tw.Squash(sv.root, 0.12f, 0.3f, Vector3.one);
                    }
                    else if (e.s == "rest") foreach (var s in staff.Values) if (s.inBreakRoom) Fx.Stars((Vector2)StaffHead(s), 4, 0.6f);
                    AudioMan.Play("se_Etc_Upgrade", 0.7f);
                    break;
            }
        }
    }
}
