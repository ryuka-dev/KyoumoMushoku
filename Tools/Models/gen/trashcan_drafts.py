"""ゴミ箱（路地裏のポリバケツ）で画風を定める白模 3 案。

甲：最少（6 角・面取りなし・2 色）
乙：中程度（12 角・縁・補強帯・取っ手・ラベル）
丙：誇張（8 角・ずんぐり・大きなふた・へこみ・汚れ帯）

右端の灰色の柱は 1.7m の人の目安。Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/trashcan_drafts.py", encoding="utf-8").read())
"""

import importlib
import sys

import bmesh

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)


def region_rule(material, bands):
    """bands: [(z_max, 段), ...] を下から順に当てる。上向きの面は一段明るく、下向きは最暗。"""
    def rule(poly):
        if poly.normal.z < -0.5:
            return material, 0
        z = poly.center.z
        step = bands[-1][1]
        for z_max, s in bands:
            if z <= z_max:
                step = s
                break
        if poly.normal.z > 0.5:
            step = min(4, step + 1)
        return material, step
    return rule


def draft_kou(x):
    bm = bmesh.new()
    b, t = km.frustum(bm, 6, 0.22, 0.26, 0.0, 0.62)
    km.cap(bm, b, flip=True)
    lb, lt = km.frustum(bm, 6, 0.28, 0.28, 0.62, 0.67)
    km.cap(bm, lb, flip=True)
    km.cap(bm, lt)
    km.box(bm, 0.0, 0.0, 0.70, 0.18, 0.04, 0.05)
    obj = km.new_object("Draft_Kou", bm)
    obj.location.x = x
    km.paint(obj, region_rule("poly_blue", [(0.62, 2), (9.0, 3)]))
    return obj


def draft_otsu(x):
    bm = bmesh.new()
    sides = 12
    # 補強帯（下）・胴・縁
    b0, t0 = km.frustum(bm, sides, 0.215, 0.225, 0.0, 0.10)
    km.cap(bm, b0, flip=True)
    _, t1 = km.frustum(bm, sides, 0.225, 0.255, 0.10, 0.56)
    _, t2 = km.frustum(bm, sides, 0.275, 0.275, 0.56, 0.61)
    # 縁の上面を閉じる代わりに、ふたを載せる
    lb, lt = km.frustum(bm, sides, 0.285, 0.25, 0.61, 0.67)
    km.cap(bm, lb, flip=True)
    km.cap(bm, lt)
    # ふたの取っ手（棒と脚）
    km.box(bm, 0.0, 0.0, 0.715, 0.20, 0.035, 0.03)
    km.box(bm, -0.085, 0.0, 0.69, 0.03, 0.035, 0.04)
    km.box(bm, 0.085, 0.0, 0.69, 0.03, 0.035, 0.04)
    # 胴の左右の握り
    km.box(bm, 0.27, 0.0, 0.50, 0.04, 0.12, 0.04)
    km.box(bm, -0.27, 0.0, 0.50, 0.04, 0.12, 0.04)
    obj_body = km.new_object("Draft_Otsu", bm)
    obj_body.location.x = x
    km.paint(obj_body, region_rule("poly_blue", [(0.10, 1), (0.56, 2), (0.61, 3), (9.0, 3)]))

    # 正面のラベル（収集日の貼り紙）。薄い板を胴に沿わせる。
    bm = bmesh.new()
    km.box(bm, 0.0, -0.243, 0.33, 0.16, 0.01, 0.14)
    label = km.new_object("Draft_Otsu_Label", bm)
    label.location.x = x
    km.paint(label, lambda p: ("paper", 3))
    return obj_body, label


def draft_hei(x):
    bm = bmesh.new()
    sides = 8
    b0, t0 = km.frustum(bm, sides, 0.27, 0.275, 0.0, 0.09, rotate=0.39)
    km.cap(bm, b0, flip=True)
    _, t1 = km.frustum(bm, sides, 0.275, 0.31, 0.09, 0.56, rotate=0.39)
    # へこみ：正面の上の輪の頂点を一つ内へ
    front = min(t1, key=lambda v: v.co.y)
    front.co.x *= 0.85
    front.co.y *= 0.85
    lb, lt = km.frustum(bm, sides, 0.345, 0.32, 0.56, 0.645, rotate=0.39)
    km.cap(bm, lb, flip=True)
    km.cap(bm, lt)
    km.box(bm, 0.0, 0.0, 0.70, 0.26, 0.06, 0.05)
    km.box(bm, -0.11, 0.0, 0.665, 0.05, 0.06, 0.04)
    km.box(bm, 0.11, 0.0, 0.665, 0.05, 0.06, 0.04)
    obj = km.new_object("Draft_Hei", bm)
    obj.location.x = x

    def rule(poly):
        if poly.normal.z < -0.5:
            return "black", 2
        z = poly.center.z
        if z <= 0.09:
            return "grime", 2
        if z <= 0.56:
            return "poly_blue", 3 if poly.normal.z > 0.5 else 2
        return "poly_blue", 4 if poly.normal.z > 0.5 else 3
    km.paint(obj, rule)
    return obj


def human_reference(x):
    bm = bmesh.new()
    km.box(bm, 0.0, 0.0, 0.85, 0.45, 0.25, 1.70)
    obj = km.new_object("Ref_Human_1.7m", bm)
    obj.location.x = x
    km.paint(obj, by_steel)
    return obj


def by_steel(poly):
    return "steel", 3


km.clear_scene()
km.palette_material()
draft_kou(-1.3)
draft_otsu(0.0)
draft_hei(1.3)
human_reference(2.6)
print("drafts built")
