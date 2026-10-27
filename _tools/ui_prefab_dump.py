#!/usr/bin/env python3
"""Dump a uGUI prefab: hierarchy path, anchoredPosition, sizeDelta, scale, Image sprite name."""
import re, sys, os, json
SRC = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets"
idx = json.load(open("/Users/trongnp/BearBakery/_derived/sprites_index.json"))
g2n = {}
for e in idx:
    try:
        for line in open(os.path.join(SRC, e["asset"]) + ".meta"):
            if line.startswith("guid:"): g2n[line.split()[1]] = e["name"]; break
    except FileNotFoundError: pass
txt = open(sys.argv[1], encoding="utf-8", errors="ignore").read()
docs = re.split(r"\n--- !u!(\d+) &(-?\d+)[^\n]*\n", txt)
objs = {docs[i + 1]: (docs[i], docs[i + 2]) for i in range(1, len(docs) - 2, 3)}
name, rt, father, sprite_of_go, active = {}, {}, {}, {}, {}
for fid, (cls, b) in objs.items():
    if cls == "1":
        name[fid] = re.search(r"m_Name: (.*)", b).group(1).strip()
        active[fid] = re.search(r"m_IsActive: (\d)", b).group(1)
    if cls in ("224", "4"):
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)", b).group(1)
        fa = re.search(r"m_Father: \{fileID: (-?\d+)", b)
        ap = re.search(r"m_AnchoredPosition: \{x: ([-0-9.e]+), y: ([-0-9.e]+)", b)
        sd = re.search(r"m_SizeDelta: \{x: ([-0-9.e]+), y: ([-0-9.e]+)", b)
        lp = re.search(r"m_LocalPosition: \{x: ([-0-9.e]+), y: ([-0-9.e]+)", b)
        sc = re.search(r"m_LocalScale: \{x: ([-0-9.e]+), y: ([-0-9.e]+)", b)
        rt[fid] = dict(go=go, fa=fa.group(1) if fa else "0", ap=ap.groups() if ap else lp.groups() if lp else None, sd=sd.groups() if sd else None, sc=sc.groups() if sc else None)
    if cls in ("114", "212"):
        sp = re.search(r"m_Sprite: \{fileID: -?\d+, guid: ([0-9a-f]+)", b)
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)", b)
        if sp and go: sprite_of_go[go.group(1)] = g2n.get(sp.group(1), sp.group(1)[:8])
go2rt = {v["go"]: k for k, v in rt.items()}
def path(k):
    p = []
    while k in rt: p.append(name.get(rt[k]["go"], "?")); k = rt[k]["fa"]
    return "/".join(reversed(p))
for k, v in sorted(rt.items(), key=lambda kv: path(kv[0])):
    print(f"{path(k)[-70:]:70} act{active.get(v['go'])} pos={v['ap']} size={v['sd']} sc={v['sc']} spr={sprite_of_go.get(v['go'], '')}")
