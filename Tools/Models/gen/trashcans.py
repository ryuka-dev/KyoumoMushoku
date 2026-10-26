"""ゴミ箱 3 種（画風：乙＝12 角前後・縁・補強帯・取っ手・ラベル。2026-10-26 作者が選定）。

- 路地裏：45L の青いポリバケツ（乙の案そのもの）
- 公園：緑の金属の角型くずかご。小さな屋根型のふた、正面の投入口、脚
- コンビニ：店の前の分別ボックス。白い箱に店の橙の帯、上の傾斜面に投入口 2 つ、分別のラベル

各ゴミ箱は「親（底面中心）＞ Body ＋ Lid」。Lid は叩き切ると飛ぶので、原点を自身の中心に置く。
正面は Blender の -Y。

Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/trashcans.py", encoding="utf-8").read())
書き出し先：Assets/Art/Models/Props/TrashCan_*.fbx と Assets/Art/Models/KM_Palette.png
"""

import importlib
import sys

import bmesh

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)

OUT = r"D:/Unity/KyoumoMushoku/Assets/Art/Models"


def bands_rule(material, bands, top_boost=1, bottom=("black", 2)):
    """bands: [(z_max, 段), ...] を下から当てる。上向きの面は明るく、下向きの面は bottom。"""
    def rule(poly):
        if poly.normal.z < -0.5:
            return bottom
        z = poly.center.z
        step = bands[-1][1]
        for z_max, s in bands:
            if z <= z_max:
                step = s
                break
        if poly.normal.z > 0.5:
            step = min(4, step + top_boost)
        return material, step
    return rule


# ── 路地裏：ポリバケツ ─────────────────────────────────

def back_alley(x):
    root = km.new_root("TrashCan_BackAlley", x)
    sides = 12

    bm = bmesh.new()
    b0, _ = km.frustum(bm, sides, 0.215, 0.225, 0.0, 0.10)
    km.cap(bm, b0, flip=True)
    km.frustum(bm, sides, 0.225, 0.255, 0.10, 0.56)
    _, rim_top = km.frustum(bm, sides, 0.275, 0.275, 0.56, 0.61)
    km.cap(bm, rim_top)
    km.box(bm, 0.27, 0.0, 0.50, 0.04, 0.12, 0.04)
    km.box(bm, -0.27, 0.0, 0.50, 0.04, 0.12, 0.04)
    body = km.new_object("Body", bm)
    km.parent_to(body, root)
    km.paint(body, bands_rule("poly_blue", [(0.10, 1), (0.56, 2), (9.0, 3)]))

    bm = bmesh.new()
    km.box(bm, 0.0, -0.243, 0.33, 0.16, 0.012, 0.14)
    label = km.new_object("Label", bm)
    km.parent_to(label, root)
    km.paint(label, lambda p: ("paper", 3))

    bm = bmesh.new()
    lb, lt = km.frustum(bm, sides, 0.29, 0.255, 0.61, 0.68)
    km.cap(bm, lb, flip=True)
    km.cap(bm, lt)
    km.box(bm, 0.0, 0.0, 0.725, 0.20, 0.035, 0.03)
    km.box(bm, -0.085, 0.0, 0.70, 0.03, 0.035, 0.04)
    km.box(bm, 0.085, 0.0, 0.70, 0.03, 0.035, 0.04)
    lid = km.new_object("Lid", bm)
    km.parent_to(lid, root)
    km.recenter(lid, (0.0, 0.0, 0.66))
    km.paint(lid, bands_rule("poly_blue", [(9.0, 3)], bottom=("poly_blue", 1)))
    return root


# ── 公園：緑の角型くずかご ───────────────────────────────

def park(x):
    root = km.new_root("TrashCan_Park", x)
    w, d = 0.50, 0.42

    bm = bmesh.new()
    # 脚 4 本
    for sx in (-1, 1):
        for sy in (-1, 1):
            km.box(bm, sx * (w / 2 - 0.04), sy * (d / 2 - 0.04), 0.05, 0.05, 0.05, 0.10)
    # 胴（下の帯・中・上の縁）
    km.box(bm, 0.0, 0.0, 0.16, w, d, 0.12)
    km.box(bm, 0.0, 0.0, 0.46, w - 0.02, d - 0.02, 0.48)
    km.box(bm, 0.0, 0.0, 0.725, w + 0.03, d + 0.03, 0.05)
    # 正面の投入口（暗い板を少し手前に）
    km.box(bm, 0.0, -(d / 2) + 0.004, 0.60, 0.30, 0.012, 0.09)
    body = km.new_object("Body", bm)
    km.parent_to(body, root)

    def body_rule(poly):
        c = poly.center
        if poly.normal.z < -0.5:
            return "black", 2
        if c.y < -(d / 2) + 0.01 and 0.55 < c.z < 0.65 and abs(c.x) < 0.16:
            return "black", 1     # 投入口
        if c.z <= 0.10:
            return "steel", 1     # 脚
        if c.z <= 0.22:
            return "park_green", 1
        if c.z >= 0.70:
            return "park_green", 3 if poly.normal.z > 0.5 else 2
        return "park_green", 2 if poly.normal.z <= 0.5 else 3
    km.paint(body, body_rule)

    bm = bmesh.new()
    km.box(bm, 0.0, -0.212, 0.42, 0.18, 0.012, 0.10)
    label = km.new_object("Label", bm)
    km.parent_to(label, root)
    km.paint(label, lambda p: ("paper", 3))

    # ふた：小さな切妻屋根
    bm = bmesh.new()
    hw, hd, z0, z1 = (w + 0.08) / 2, (d + 0.08) / 2, 0.75, 0.86
    v = [bm.verts.new(co) for co in (
        (-hw, -hd, z0), (hw, -hd, z0), (hw, hd, z0), (-hw, hd, z0),
        (-hw, 0.0, z1), (hw, 0.0, z1))]
    bm.faces.new((v[0], v[1], v[5], v[4]))
    bm.faces.new((v[2], v[3], v[4], v[5]))
    bm.faces.new((v[0], v[4], v[3]))
    bm.faces.new((v[1], v[2], v[5]))
    bm.faces.new((v[3], v[2], v[1], v[0]))
    lid = km.new_object("Lid", bm)
    km.parent_to(lid, root)
    km.recenter(lid, (0.0, 0.0, 0.79))
    km.paint(lid, lambda p: ("park_green", 4) if p.normal.z > 0.3 else
             (("black", 2) if p.normal.z < -0.5 else ("park_green", 3)))
    return root


# ── コンビニ：分別ボックス ───────────────────────────────

def convenience_store(x):
    root = km.new_root("TrashCan_ConvenienceStore", x)
    w, d, h = 0.86, 0.44, 0.90

    bm = bmesh.new()
    km.box(bm, 0.0, 0.0, 0.03, w + 0.02, d + 0.02, 0.06)       # 台座
    km.box(bm, 0.0, 0.0, 0.06 + (h - 0.06) / 2, w, d, h - 0.06)  # 胴
    km.box(bm, 0.0, -(d / 2) - 0.003, 0.70, w, 0.01, 0.07)       # 橙の帯
    # 投入口 2 つ（正面上部の暗い板）
    for sx in (-1, 1):
        km.box(bm, sx * 0.20, -(d / 2) - 0.006, 0.80, 0.22, 0.01, 0.07)
    # 仕切り（正面の縦の溝）
    km.box(bm, 0.0, -(d / 2) - 0.004, 0.40, 0.02, 0.01, 0.56)
    body = km.new_object("Body", bm)
    km.parent_to(body, root)

    def body_rule(poly):
        c = poly.center
        if poly.normal.z < -0.5:
            return "black", 2
        if c.z <= 0.06:
            return "steel", 1
        front = c.y < -(d / 2) - 0.001
        if front and 0.665 < c.z < 0.735:
            return "store_orange", 3
        if front and 0.765 < c.z < 0.835:
            return "black", 1
        if front and abs(c.x) < 0.02:
            return "steel", 2
        return "paper", 3 if poly.normal.z <= 0.5 else 4
    km.paint(body, body_rule)

    # 分別ラベル 2 枚（缶・びん／もえるゴミ）
    bm = bmesh.new()
    for sx in (-1, 1):
        km.box(bm, sx * 0.20, -(d / 2) - 0.008, 0.55, 0.20, 0.008, 0.12)
    label = km.new_object("Label", bm)
    km.parent_to(label, root)
    km.paint(label, lambda p: ("store_orange", 4))

    # ふた：上の天板（少し張り出す）
    bm = bmesh.new()
    km.box(bm, 0.0, 0.0, h + 0.025, w + 0.04, d + 0.04, 0.05)
    lid = km.new_object("Lid", bm)
    km.parent_to(lid, root)
    km.recenter(lid, (0.0, 0.0, h + 0.025))
    km.paint(lid, lambda p: ("steel", 3) if p.normal.z > 0.5 else
             (("black", 2) if p.normal.z < -0.5 else ("steel", 2)))
    return root


km.clear_scene()
km.build_palette_image(OUT + "/KM_Palette.png")
km.palette_material()
roots = [back_alley(-1.4), park(0.0), convenience_store(1.6)]
for root in roots:
    x = root.location.x
    root.location.x = 0.0
    km.export_fbx([root], OUT + "/Props/" + root.name + ".fbx")
    root.location.x = x
print("exported:", [r.name for r in roots])
