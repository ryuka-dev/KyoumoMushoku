"""一人称の手で画風を定める白模 3 案（2026-10-26）。

最初の版（指を関節ごとに分けた手）は「不気味・画風が合わない・指の向きが逆」と作者に言われた。
反省：関節の多い剛体の指は低ポリゴンでは不気味になりやすい。**指は動かさず、拳と開きを別の形として作り、切り替える。**

甲：軍手（白い綿の作業手袋・手首に色の縁）
乙：毛糸のミトン（濃紺・親指だけ分かれる・リブの袖口）
丙：漫画的な素手（肌色・大ぶり・拳の前に指の節が 4 つ）

前列が拳、後列が開き。袖は 3 案共通。Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/hand_drafts.py", encoding="utf-8").read())
"""

import importlib
import math
import sys

import bmesh

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)


def shade(material, top=3, side=2, bottom=1):
    def rule(poly):
        if poly.normal.z > 0.5:
            return material, top
        if poly.normal.z < -0.5:
            return material, bottom
        return material, side
    return rule


def sleeve(name, x, y):
    """袖：太めの八角、先に折り返し。手首は y=-0.24 付近。"""
    bm = bmesh.new()
    km.frustum(bm, 8, 0.054, 0.048, 0.0, 0.20, rotate=math.pi / 8)
    _, top = km.frustum(bm, 8, 0.058, 0.058, 0.20, 0.25, rotate=math.pi / 8)
    obj = km.new_object(name, bm)
    obj.rotation_euler.x = math.pi / 2       # Z 方向に作った筒を -Y（前）へ向ける
    obj.location = (x, y, 0.0)
    km.paint(obj, lambda p: ("jacket", 1) if p.center.z > 0.20 else ("jacket", 2))
    return obj


HAND_SCALE = 1.4   # 一人称では手を大きく、袖を細く（漫画の比率）。手首（y=-0.25）を中心に拡大する。


def piece(name, x, y, build, rule):
    bm = bmesh.new()
    build(bm)
    for v in bm.verts:
        v.co.x *= HAND_SCALE
        v.co.z *= HAND_SCALE
        v.co.y = -0.25 + (v.co.y + 0.25) * HAND_SCALE
    obj = km.new_object(name, bm)
    obj.location = (x, y, 0.0)
    km.paint(obj, rule)
    return obj


# ── 甲：軍手 ─────────────────────────────────────────

def gunte_fist(bm):
    km.rounded_box(bm, 0.0, -0.31, 0.0, 0.10, 0.10, 0.085, 0.018)       # 握った手
    km.rounded_box(bm, -0.045, -0.29, 0.022, 0.035, 0.06, 0.035, 0.010)  # 親指（上に乗る）
    km.rounded_box(bm, 0.0, -0.25, 0.0, 0.092, 0.03, 0.078, 0.008)      # 手首の色の縁


def gunte_open(bm):
    km.rounded_box(bm, 0.0, -0.33, 0.0, 0.10, 0.14, 0.035, 0.012)       # 掌と指（一枚）
    km.rounded_box(bm, -0.065, -0.29, 0.0, 0.035, 0.07, 0.028, 0.008)   # 開いた親指
    km.rounded_box(bm, 0.0, -0.25, 0.0, 0.092, 0.03, 0.05, 0.008)


def gunte_rule(poly):
    if poly.center.y > -0.265:
        return "store_orange", 3                 # 手首の縁（軍手の色糸）
    return shade("paper", top=4, side=3, bottom=2)(poly)


# ── 乙：ミトン ───────────────────────────────────────

def mitten_fist(bm):
    km.rounded_box(bm, 0.0, -0.315, 0.0, 0.11, 0.11, 0.095, 0.030)
    km.rounded_box(bm, -0.055, -0.29, 0.015, 0.04, 0.06, 0.04, 0.014)
    km.rounded_box(bm, 0.0, -0.245, 0.0, 0.10, 0.04, 0.09, 0.006)       # リブの袖口


def mitten_open(bm):
    km.rounded_box(bm, 0.0, -0.34, 0.0, 0.11, 0.16, 0.045, 0.020)
    km.rounded_box(bm, -0.075, -0.30, 0.0, 0.04, 0.07, 0.035, 0.012)
    km.rounded_box(bm, 0.0, -0.245, 0.0, 0.10, 0.04, 0.06, 0.006)


def mitten_rule(poly):
    if poly.center.y > -0.265:
        return "knit", 1
    return shade("knit", top=4, side=3, bottom=2)(poly)


# ── 丙：漫画的な素手 ──────────────────────────────────

def bare_fist(bm):
    km.rounded_box(bm, 0.0, -0.31, 0.0, 0.11, 0.10, 0.09, 0.022)
    for i, x in enumerate((0.036, 0.012, -0.012, -0.036)):                # 指の節
        km.rounded_box(bm, x, -0.365, 0.022, 0.022, 0.018, 0.024, 0.006)
    km.rounded_box(bm, -0.05, -0.30, 0.03, 0.035, 0.055, 0.035, 0.010)


def bare_open(bm):
    km.rounded_box(bm, 0.0, -0.30, 0.0, 0.11, 0.08, 0.035, 0.012)
    for x in (0.036, 0.012, -0.012, -0.036):
        km.rounded_box(bm, x, -0.375, 0.0, 0.020, 0.075, 0.026, 0.007)
    km.rounded_box(bm, -0.07, -0.29, 0.0, 0.032, 0.06, 0.028, 0.009)


DRAFTS = [
    ("Kou_Gunte", gunte_fist, gunte_open, gunte_rule),
    ("Otsu_Mitten", mitten_fist, mitten_open, mitten_rule),
    ("Hei_Bare", bare_fist, bare_open, shade("skin", top=4, side=3, bottom=1)),
]

km.clear_scene()
km.palette_material()
for i, (name, fist, open_hand, rule) in enumerate(DRAFTS):
    x = (i - 1) * 0.35
    sleeve(f"{name}_SleeveF", x, 0.0)
    piece(f"{name}_Fist", x, 0.0, fist, rule)
    sleeve(f"{name}_SleeveO", x, 0.5)
    piece(f"{name}_Open", x, 0.5, open_hand, rule)
print("hand drafts built")
