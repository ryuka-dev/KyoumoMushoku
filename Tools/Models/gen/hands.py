"""一人称の手（右手。左手は Unity 側で鏡に映して使う）。

部品分割の剛体（作業計画 第一の二節「人物」）。蒙皮なし。指は関節ごとに別の物体で、
原点を関節に置く。握り・開きは Unity のコードが指を回して作る（X 軸まわりに +90° で内へ曲がる）。

- 袖（前腕）：くたびれた上着。肘を原点、手首へ向かって -Y（＝Unity の +Z、前）。
- 袖口：少し太く暗い。
- 掌：手の甲が上。
- 指 4 本 × 2 節：根元の節は指先の切れた毛糸の手袋、先の節は肌。
- 親指：掌の内側（右手なので -X 側）。

Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/hands.py", encoding="utf-8").read())
書き出し先：Assets/Art/Models/Characters/Hand_FirstPerson.fbx
"""

import importlib
import sys

import bmesh

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)

OUT = r"D:/Unity/KyoumoMushoku/Assets/Art/Models"

# 寸法（m）。一人称の手は画面で読めるよう実寸の 1.1 倍ほど。
WRIST_Y = -0.28
PALM_LEN = 0.095
PALM_W = 0.090
PALM_T = 0.036
KNUCKLE_Y = WRIST_Y - PALM_LEN
FINGER_W = 0.019
FINGER_T = 0.022
SEG1 = 0.040   # 根元の節
SEG2 = 0.034   # 先の節


def shade(material, top=3, side=2, bottom=1):
    def rule(poly):
        if poly.normal.z > 0.5:
            return material, top
        if poly.normal.z < -0.5:
            return material, bottom
        return material, side
    return rule


def block(name, cx, cy, cz, sx, sy, sz, rule, parent, pivot=None):
    bm = bmesh.new()
    km.box(bm, cx, cy, cz, sx, sy, sz)
    obj = km.new_object(name, bm)
    if pivot is not None:
        km.recenter(obj, pivot)
    km.parent_to(obj, parent, keep_world=True)
    km.paint(obj, rule)
    return obj


def build():
    root = km.new_root("Hand_FirstPerson")

    # 袖（八角の先細り）
    bm = bmesh.new()
    elbow, wrist = [], []
    import math
    for i in range(8):
        a = 2 * math.pi * i / 8 + math.pi / 8
        elbow.append(bm.verts.new((0.060 * math.cos(a), 0.0, 0.055 * math.sin(a))))
        wrist.append(bm.verts.new((0.050 * math.cos(a), WRIST_Y + 0.03, 0.045 * math.sin(a))))
    for i in range(8):
        j = (i + 1) % 8
        bm.faces.new((elbow[i], elbow[j], wrist[j], wrist[i]))
    km.cap(bm, elbow)
    sleeve = km.new_object("Sleeve", bm)
    km.parent_to(sleeve, root)
    km.paint(sleeve, shade("jacket", top=3, side=2, bottom=1))

    # 袖口（少し太い輪）
    bm = bmesh.new()
    km.frustum(bm, 8, 0.056, 0.056, 0.0, 0.035, rotate=math.pi / 8)
    cuff = km.new_object("Cuff", bm)
    cuff.rotation_euler.x = math.pi / 2          # 輪を Y 軸に沿わせる
    cuff.location.y = WRIST_Y + 0.035
    km.parent_to(cuff, root)
    km.paint(cuff, lambda p: ("jacket", 1))

    # 掌
    palm = block("Palm", 0.0, WRIST_Y - PALM_LEN / 2, 0.0, PALM_W, PALM_LEN, PALM_T,
                 shade("skin", top=3, side=2, bottom=1), root)

    # 指 4 本（人差し指が +X 側。右手の甲を上にしたとき親指は -X 側）
    xs = [0.034, 0.012, -0.010, -0.031]
    lengths = [1.0, 1.08, 1.0, 0.85]
    for n, (x, k) in enumerate(zip(xs, lengths), start=1):
        s1, s2 = SEG1 * k, SEG2 * k
        base = block(f"Finger{n}_Base", x, KNUCKLE_Y - s1 / 2, 0.003, FINGER_W, s1, FINGER_T,
                     shade("knit", top=3, side=2, bottom=1), palm, pivot=(x, KNUCKLE_Y, 0.003))
        block(f"Finger{n}_Tip", x, KNUCKLE_Y - s1 - s2 / 2, 0.003, FINGER_W * 0.95, s2, FINGER_T * 0.95,
              shade("skin", top=3, side=2, bottom=1), base, pivot=(x, KNUCKLE_Y - s1, 0.003))

    # 親指（掌の -X 側の根元から斜め前へ）
    thumb = block("Thumb", -0.055, WRIST_Y - 0.040, -0.004, 0.022, 0.050, 0.022,
                  shade("knit", top=3, side=2, bottom=1), palm, pivot=(-0.045, WRIST_Y - 0.020, -0.004))
    thumb.rotation_euler.z = math.radians(-20)

    return root


km.clear_scene()
km.build_palette_image(OUT + "/KM_Palette.png")
km.palette_material()
hand = build()
km.export_fbx([hand], OUT + "/Characters/Hand_FirstPerson.fbx")
print("exported hand:", [c.name for c in hand.children_recursive])
