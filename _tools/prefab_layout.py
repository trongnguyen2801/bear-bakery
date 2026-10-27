#!/usr/bin/env python3
"""Dump SpriteRenderer layout (world pos/scale/sorting) of a Unity YAML prefab/scene.
usage: prefab_layout.py <file.prefab> """
import re, sys, os, json
SRC = "/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject/Assets"
idx = json.load(open("/Users/trongnp/BearBakery/_derived/sprites_index.json"))
guid2name = {}
for e in idx:
    meta = os.path.join(SRC, e["asset"]) + ".meta"
    try:
        for line in open(meta):
            if line.startswith("guid:"):
                guid2name[line.split()[1]] = (e["name"], e.get("size"), e["pivot"]); break
    except FileNotFoundError:
        pass
txt = open(sys.argv[1], encoding="utf-8", errors="ignore").read()
docs = re.split(r"\n--- !u!(\d+) &(-?\d+)[^\n]*\n", txt)
objs = {}
for i in range(1, len(docs) - 2, 3):
    objs[docs[i + 1]] = (docs[i], docs[i + 2])
def f3(s, key):
    m = re.search(key + r": \{x: ([-0-9.eE]+), y: ([-0-9.eE]+), z: ([-0-9.eE]+)", s)
    return tuple(float(v) for v in m.groups()) if m else (0, 0, 0)
go_name, tr_of_go, tr = {}, {}, {}
for fid, (cls, body) in objs.items():
    if cls == "1":
        m = re.search(r"m_Name: (.*)", body); go_name[fid] = m.group(1).strip() if m else "?"
    if cls in ("4", "224"):
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)", body).group(1)
        fa = re.search(r"m_Father: \{fileID: (-?\d+)", body)
        tr[fid] = dict(go=go, pos=f3(body, "m_LocalPosition"), scl=f3(body, "m_LocalScale"), father=fa.group(1) if fa else "0")
        tr_of_go[go] = fid
def world(tid):
    x = y = 0.0; sx = sy = 1.0
    chain = []
    while tid in tr:
        chain.append(tr[tid]); tid = tr[tid]["father"]
    for t in reversed(chain):
        x += t["pos"][0] * sx; y += t["pos"][1] * sy
        sx *= t["scl"][0]; sy *= t["scl"][1]
    return x, y, sx, sy
def path(tid):
    p = []
    while tid in tr:
        p.append(go_name.get(tr[tid]["go"], "?")); tid = tr[tid]["father"]
    return "/".join(reversed(p))
rows = []
for fid, (cls, body) in objs.items():
    if cls != "212": continue
    go = re.search(r"m_GameObject: \{fileID: (-?\d+)", body).group(1)
    sp = re.search(r"m_Sprite: \{fileID: -?\d+(?:, guid: ([0-9a-f]+))?", body)
    nm = guid2name.get(sp.group(1), ("<none>",))[0] if sp and sp.group(1) else "<none>"
    so = re.search(r"m_SortingOrder: (-?\d+)", body); so = int(so.group(1)) if so else 0
    sl = re.search(r"m_SortingLayer: (-?\d+)", body); sl = int(sl.group(1)) if sl else 0
    en = re.search(r"m_Enabled: (\d)", body).group(1)
    t = tr_of_go.get(go)
    if not t: continue
    x, y, sx, sy = world(t)
    rows.append((round(y, 2), round(x, 2), nm, round(sx, 2), round(sy, 2), sl, so, en, path(t)))
for r in sorted(rows, reverse=True):
    print(f"y={r[0]:7} x={r[1]:7} {r[2]:34} s=({r[3]},{r[4]}) L{r[5]} o{r[6]} en{r[7]}  {r[8][-80:]}")
