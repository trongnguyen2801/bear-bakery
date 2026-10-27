// Minimal tween runner (no external deps). Tweens are cosmetic only; gameplay never waits on them.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BB.View
{
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutQuad(float t) => 1 - (1 - t) * (1 - t);
        public static float InQuad(float t) => t * t;
        public static float InOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
        public static float OutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
        public static float OutElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;
            return t <= 0 ? 0 : t >= 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }
    }

    public class Tw : MonoBehaviour
    {
        class Job { public object owner; public float delay, dur, t; public Func<float, float> ease; public Action<float> step; public Action done; public bool dead; }
        static Tw inst;
        readonly List<Job> jobs = new List<Job>();
        readonly List<Job> adding = new List<Job>();

        static Tw I
        {
            get
            {
                if (inst == null) { var go = new GameObject("[Tw]"); inst = go.AddComponent<Tw>(); DontDestroyOnLoad(go); }
                return inst;
            }
        }

        public static void Run(object owner, float dur, Action<float> step, Func<float, float> ease = null, Action done = null, float delay = 0)
        {
            I.adding.Add(new Job { owner = owner, dur = Mathf.Max(0.0001f, dur), step = step, ease = ease ?? Ease.OutQuad, done = done, delay = delay });
        }

        public static void Kill(object owner)
        {
            if (inst == null || owner == null) return;
            foreach (var j in inst.jobs) if (ReferenceEquals(j.owner, owner)) j.dead = true;
            foreach (var j in inst.adding) if (ReferenceEquals(j.owner, owner)) j.dead = true;
        }

        public static void Delay(object owner, float delay, Action a) => Run(owner, 0.0001f, _ => { }, null, a, delay);

        /// <summary>True while a tween owned by 'owner' is running (or queued).</summary>
        public static bool Busy(object owner)
        {
            if (inst == null || owner == null) return false;
            foreach (var j in inst.jobs) if (!j.dead && ReferenceEquals(j.owner, owner)) return true;
            foreach (var j in inst.adding) if (!j.dead && ReferenceEquals(j.owner, owner)) return true;
            return false;
        }

        // convenience
        public static void Move(Transform tr, Vector3 to, float dur, Func<float, float> ease = null, Action done = null, float delay = 0)
        {
            Vector3 from = Vector3.zero; bool started = false;
            Run(tr, dur, t => { if (tr == null) return; if (!started) { from = tr.position; started = true; } tr.position = Vector3.LerpUnclamped(from, to, t); }, ease, done, delay);
        }

        public static void Pop(Transform tr, float from = 0.3f, float dur = 0.3f, Vector3? baseScale = null)
        {
            Vector3 b = baseScale ?? Vector3.one;
            Run(tr, dur, t => { if (tr != null) tr.localScale = b * Mathf.LerpUnclamped(from, 1f, t); }, Ease.OutBack);
        }

        public static void Squash(Transform tr, float amount = 0.18f, float dur = 0.25f, Vector3? baseScale = null)
        {
            Vector3 b = baseScale ?? tr.localScale;
            Run(tr, dur, t =>
            {
                if (tr == null) return;
                float s = Mathf.Sin(t * Mathf.PI) * amount * (1 - t * 0.5f);
                tr.localScale = new Vector3(b.x * (1 + s), b.y * (1 - s), b.z);
            }, Ease.Linear, () => { if (tr != null) tr.localScale = b; });
        }

        void Update()
        {
            if (adding.Count > 0) { jobs.AddRange(adding); adding.Clear(); }
            float dt = Time.deltaTime;
            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                if (j.dead) continue;
                if (j.owner is UnityEngine.Object uo && uo == null) { j.dead = true; continue; }
                if (j.delay > 0) { j.delay -= dt; continue; }
                j.t += dt;
                float k = Mathf.Clamp01(j.t / j.dur);
                try { j.step(j.ease(k)); } catch (Exception e) { Debug.LogException(e); j.dead = true; continue; }
                if (k >= 1f)
                {
                    j.dead = true;
                    try { j.done?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
                }
            }
            jobs.RemoveAll(x => x.dead);
        }
    }
}
