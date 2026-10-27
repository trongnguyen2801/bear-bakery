// Audio: source clips from the provided set. Nothing plays until the first user interaction; global mute.
using System.Collections.Generic;
using UnityEngine;

namespace BB.View
{
    public class AudioMan : MonoBehaviour
    {
        static AudioMan inst;
        AudioSource sfx, bgm;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        public static bool Unlocked { get; private set; }
        public static bool Muted { get; private set; }

        public static void Init(bool muted)
        {
            if (inst != null) return;
            var go = new GameObject("[Audio]");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioMan>();
            inst.sfx = go.AddComponent<AudioSource>();
            inst.bgm = go.AddComponent<AudioSource>();
            inst.bgm.loop = true; inst.bgm.volume = 0.35f;
            Muted = muted;
        }

        /// <summary>Called by input on the first tap/click (autoplay policy friendly).</summary>
        public static void UnlockByUser()
        {
            if (Unlocked || inst == null) return;
            Unlocked = true;
            inst.bgm.clip = inst.Clip("bgm_Main");
            if (!Muted && inst.bgm.clip != null) inst.bgm.Play();
        }

        public static void SetMuted(bool m)
        {
            Muted = m;
            if (inst == null) return;
            if (m) inst.bgm.Pause();
            else if (Unlocked) { if (inst.bgm.clip == null) inst.bgm.clip = inst.Clip("bgm_Main"); inst.bgm.Play(); }
        }

        AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            clips[name] = c;
            return c;
        }

        public static void Play(string name, float vol = 0.8f, float minGap = 0.06f)
        {
            if (inst == null || Muted || !Unlocked) return;
            float now = Time.unscaledTime;
            if (inst.lastPlay.TryGetValue(name, out var t) && now - t < minGap) return;
            inst.lastPlay[name] = now;
            var c = inst.Clip(name);
            if (c != null) inst.sfx.PlayOneShot(c, vol);
        }

        public static void Click() => Play("se_Etc_Click", 0.5f);
    }
}
