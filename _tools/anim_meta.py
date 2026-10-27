#!/usr/bin/env python3
"""Extract keyframe data from AnimationClips (frame-toggle timing via m_IsActive, transform curves).
Writes _derived/anim_metadata.json and prints a compact summary for selected clips."""
import re, os, json, sys
D = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets/AnimationClip"
OUT = {}
def parse_curve_block(block):
    keys = []
    for m in re.finditer(r"time: ([-0-9.eE]+)\n\s+value: (\{[^}]*\}|[-0-9.eE]+)", block):
        v = m.group(2)
        if v.startswith("{"):
            v = [float(x) for x in re.findall(r"[xyzw]: ([-0-9.eE]+)", v)]
        else:
            v = float(v)
        keys.append((round(float(m.group(1)), 4), v))
    return keys
for f in sorted(os.listdir(D)):
    if not f.endswith(".anim"): continue
    t = open(os.path.join(D, f), encoding="utf-8", errors="ignore").read()
    name = re.search(r"m_Name: (.*)", t).group(1).strip()
    sr = float(re.search(r"m_SampleRate: ([0-9.]+)", t).group(1))
    stop = re.search(r"m_StopTime: ([0-9.eE-]+)", t); stop = float(stop.group(1)) if stop else 0
    loop = re.search(r"m_LoopTime: (\d)", t); loop = loop and loop.group(1) == "1"
    curves = []
    main = t.split("m_EditorCurves:")[0]  # runtime curves section
    for sect in ["m_RotationCurves", "m_PositionCurves", "m_ScaleCurves", "m_FloatCurves", "m_EulerCurves", "m_PPtrCurves"]:
        m = re.search(sect + r":(.*?)(?=\n  m_[A-Z][A-Za-z]+:)", main, re.S)
        if not m or m.group(1).strip() in ("[]", ""): continue
        for c in re.split(r"\n  - (?=curve:|serializedVersion:)", m.group(1)):
            p = re.search(r"\n\s+path: (.*)", c)
            a = re.search(r"\n\s+attribute: (.*)", c)
            keys = parse_curve_block(c)
            if p is None: continue
            curves.append({"type": sect[2:], "path": p.group(1).strip(), "attr": a.group(1).strip() if a else "", "keys": keys})
    ev = [(float(m.group(1)), m.group(2)) for m in re.finditer(r"time: ([0-9.eE-]+)\n\s+functionName: (\S+)", t)]
    OUT[name] = {"file": "AnimationClip/" + f, "sampleRate": sr, "length": stop, "loop": bool(loop), "curves": curves, "events": ev}
json.dump(OUT, open("/Users/trongnp/BearBakery/_derived/anim_metadata.json", "w"), indent=1)
sel = sys.argv[1:] or []
for n in sel:
    c = OUT.get(n)
    if not c: print("missing", n); continue
    print(f"## {n} len={c['length']:.3f} loop={c['loop']} events={c['events']}")
    for cv in c["curves"]:
        ks = cv["keys"][:8]
        print(f"   {cv['type']:13} {cv['path'][:40]:40} {cv['attr'][:22]:22} {ks}")
