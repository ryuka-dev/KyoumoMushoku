"""街の小物（一人称の切片・2026-10-27）。日本の街に見せるための背景の道具。

- 電柱（UtilityPole）：コンクリートの柱・腕金・碍子・柱上変圧器・足場ボルト・黄黒の巻き看板。
  電線は Unity 側で柱と柱の間に張る（腕金の両端＝ローカル (±0.85, 8.7, 0)）。
- ブロック塀（BlockWall_A / _B）：長さ 2m の一枚。基礎・ブロックの段・笠木。B は古く汚れた色。
- アーケードの屋根（ArcadeRoof）：長さ 4m・幅 12m の一区画。柱・梁・弧の骨・中央を明かり取りに空けた屋根板。
- のぼり（Banner_Red / _Indigo）：腕と旗。
- 室外機（AirConditioner）・植木鉢（PlantPot）・物干し（LaundryStand）・自転車（Bicycle）
- シャッター（Shutter）・袖看板（SignBlade）・立て看板（SignStand）・暖簾（Noren）・カーブミラー（CurveMirror）

背景の道具なので実寸（PROP_SCALE は掛けない。叩く・掴む物ではない）。正面は Blender の -Y。

Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/street_dressing.py", encoding="utf-8").read())
書き出し先：Assets/Art/Models/Props/Street/*.fbx と Assets/Art/Models/KM_Palette.png
"""

import importlib
import math
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)

OUT = r"D:/Unity/KyoumoMushoku/Assets/Art/Models"


# ── 共通 ───────────────────────────────────────────────

def part(root, name, build, rule):
    bm = bmesh.new()
    build(bm)
    obj = km.new_object(name, bm)
    km.paint(obj, rule)
    km.parent_to(obj, root)
    return obj


def const(material, step):
    return lambda poly: (material, step)


def shade(material, top=4, side=2, bottom=0):
    return km.by_normal(material, top, side, bottom)


def cylinder(bm, sides, r0, r1, z0, z1, caps=True, rotate=0.0):
    bottom, top = km.frustum(bm, sides, r0, r1, z0, z1, rotate)
    if caps:
        km.cap(bm, bottom, flip=True)
        km.cap(bm, top)
    return bottom + top


def rotate(bm, verts, axis, degrees, center=(0.0, 0.0, 0.0)):
    bmesh.ops.rotate(bm, verts=verts, cent=Vector(center), matrix=Matrix.Rotation(math.radians(degrees), 3, axis))


def move(bm, verts, offset):
    bmesh.ops.translate(bm, verts=verts, vec=Vector(offset))


def beam(bm, a, b, thickness):
    """a から b へ向かう角材（断面 thickness 角）。"""
    a, b = Vector(a), Vector(b)
    d = b - a
    verts = km.box(bm, 0.0, 0.0, 0.0, thickness, thickness, d.length)
    rot = d.normalized().to_track_quat("Z", "Y").to_matrix()
    bmesh.ops.rotate(bm, verts=verts, cent=Vector((0, 0, 0)), matrix=rot)
    move(bm, verts, (a + b) / 2)
    return verts


def ring(bm, sides, radius, thickness, width):
    """縦に立てた輪（車輪）。Y 軸が回転軸。"""
    outer = cylinder(bm, sides, radius, radius, -width / 2, width / 2, caps=False)
    inner = cylinder(bm, sides, radius - thickness, radius - thickness, -width / 2, width / 2, caps=False)
    rotate(bm, outer + inner, "X", 90)
    return outer + inner


# ── 電柱 ───────────────────────────────────────────────

def utility_pole(x):
    root = km.new_root("UtilityPole", x)

    def pole(bm):
        cylinder(bm, 8, 0.17, 0.12, 0.0, 10.0, rotate=math.pi / 8)

    def pole_rule(poly):
        if poly.normal.z > 0.5:
            return "concrete", 3
        return "concrete", 1 if poly.center.z < 0.6 else 2

    part(root, "Pole", pole, pole_rule)

    def band(bm):
        cylinder(bm, 8, 0.172, 0.171, 1.3, 2.3, caps=False, rotate=math.pi / 8)

    part(root, "Band", band, const("caution", 3))

    def stripes(bm):
        for z in (1.45, 1.75, 2.05):
            cylinder(bm, 8, 0.176, 0.176, z, z + 0.1, caps=False, rotate=math.pi / 8)

    part(root, "Stripes", stripes, const("black", 2))

    def arm(bm):
        km.box(bm, 0.0, 0.0, 8.6, 2.0, 0.1, 0.1)
        km.box(bm, 0.0, 0.0, 8.25, 0.9, 0.06, 0.06)

    part(root, "Arm", arm, shade("steel", 3, 2, 1))

    def insulators(bm):
        for ix in (-0.85, 0.0, 0.85):
            move(bm, cylinder(bm, 6, 0.05, 0.035, 8.65, 8.8), (ix, 0.0, 0.0))
        cylinder(bm, 6, 0.05, 0.035, 10.0, 10.18)

    part(root, "Insulators", insulators, shade("paper", 4, 3, 1))

    def transformer(bm):
        verts = cylinder(bm, 8, 0.24, 0.24, 5.9, 6.75)
        move(bm, verts, (0.0, 0.42, 0.0))
        verts = cylinder(bm, 8, 0.25, 0.22, 6.75, 6.85)
        move(bm, verts, (0.0, 0.42, 0.0))
        km.box(bm, 0.0, 0.2, 6.3, 0.12, 0.22, 0.5)

    part(root, "Transformer", transformer, shade("steel", 4, 3, 1))

    def steps(bm):
        for i in range(12):
            z = 2.6 + i * 0.45
            side = 1 if i % 2 == 0 else -1
            km.box(bm, side * 0.2, 0.0, z, 0.16, 0.03, 0.03)

    part(root, "Steps", steps, const("steel", 1))
    return root


# ── ブロック塀 ─────────────────────────────────────────

def block_wall(name, x, base_step, worn):
    root = km.new_root(name, x)

    def base(bm):
        km.box(bm, 0.0, 0.0, 0.15, 2.0, 0.17, 0.3)

    part(root, "Base", base, shade("grime" if worn else "concrete", 2, 1, 0))

    courses = 9

    # ブロックの段は同じ色にし、段の間の目地を一段暗く・少し奥に引っ込める（縞に見せない）。
    def blocks(bm):
        for i in range(courses):
            z = 0.3 + 0.2 * i
            km.box(bm, 0.0, 0.0, z + 0.1, 2.0, 0.14, 0.18)

    def block_rule(poly):
        if poly.normal.z > 0.5:
            return "concrete", min(4, base_step + 1)
        if worn and poly.center.z < 0.75:
            return "concrete", max(0, base_step - 1)
        return "concrete", base_step

    part(root, "Blocks", blocks, block_rule)

    def joints(bm):
        for i in range(courses + 1):
            z = 0.3 + 0.2 * i
            km.box(bm, 0.0, 0.0, z, 1.98, 0.12, 0.03)
        for i in range(courses):
            z = 0.3 + 0.2 * i + 0.1
            offset = 0.2 if i % 2 else 0.0
            x = -1.0 + offset + 0.4
            while x < 1.0:
                km.box(bm, x, 0.0, z, 0.02, 0.16, 0.17)  # 1cm 出す（2.5mm だと遠くで明滅する）
                x += 0.4

    part(root, "Joints", joints, const("concrete", max(0, base_step - 1)))

    def cap(bm):
        km.box(bm, 0.0, 0.0, 2.15, 2.02, 0.19, 0.1)

    part(root, "Cap", cap, shade("concrete", 4, 3, 1))
    return root


# ── アーケード ─────────────────────────────────────────

ARC_SEGMENTS = 8
ARC_HALF = 6.0
ARC_SPRING = 6.8
ARC_RISE = 1.6


def arc_point(i):
    t = i / ARC_SEGMENTS
    a = math.pi * t
    y = -ARC_HALF * math.cos(a)
    z = ARC_SPRING + ARC_RISE * math.sin(a)
    return y, z


def arcade_roof(x):
    root = km.new_root("ArcadeRoof", x)
    length = 4.0

    def posts(bm):
        for y in (-ARC_HALF, ARC_HALF):
            km.box(bm, 0.0, y, ARC_SPRING / 2, 0.22, 0.22, ARC_SPRING)

    part(root, "Posts", posts, shade("steel", 3, 2, 1))

    def beams(bm):
        for y in (-ARC_HALF, ARC_HALF):
            km.box(bm, length / 2, y, ARC_SPRING, length, 0.24, 0.34)

    part(root, "Beams", beams, shade("steel", 3, 1, 0))

    def ribs(bm):
        for i in range(ARC_SEGMENTS):
            y0, z0 = arc_point(i)
            y1, z1 = arc_point(i + 1)
            beam(bm, (0.0, y0, z0), (0.0, y1, z1), 0.16)

    part(root, "Ribs", ribs, const("steel", 2))

    def panels(bm):
        # 中央の二区画は明かり取りに空ける（日が縞になって通りに落ちる）。
        for i in range(ARC_SEGMENTS):
            if i in (3, 4):
                continue
            y0, z0 = arc_point(i)
            y1, z1 = arc_point(i + 1)
            a, b = Vector((0.12, y0, z0 + 0.1)), Vector((0.12, y1, z1 + 0.1))
            c, d = a + Vector((length - 0.24, 0, 0)), b + Vector((length - 0.24, 0, 0))
            # 表と裏を別の頂点で張る（下から見上げても板が見える）。
            up = Vector((0, 0, 0.02))
            va, vb, vc, vd = (bm.verts.new(p) for p in (a, b, c, d))
            bm.faces.new((va, vc, vd, vb))
            va, vb, vc, vd = (bm.verts.new(p - up) for p in (a, b, c, d))
            bm.faces.new((vb, vd, vc, va))

    part(root, "Panels", panels, const("paper", 3))
    return root


def banner(name, x, material):
    root = km.new_root(name, x)

    def arm(bm):
        km.box(bm, 0.0, -0.3, 1.25, 0.04, 0.6, 0.04)
        km.box(bm, 0.0, -0.3, 0.05, 0.03, 0.56, 0.03)

    part(root, "Arm", arm, const("steel", 2))

    def flag(bm):
        km.box(bm, 0.0, -0.31, 0.65, 0.02, 0.5, 1.15)

    part(root, "Flag", flag, shade(material, 3, 3, 2))

    def mark(bm):
        km.box(bm, -0.012, -0.31, 0.95, 0.01, 0.3, 0.3)

    part(root, "Mark", mark, const("paper", 4))
    return root


# ── 住宅地の小物 ───────────────────────────────────────

def air_conditioner(x):
    root = km.new_root("AirConditioner", x)

    def body(bm):
        km.box(bm, 0.0, 0.0, 0.42, 0.8, 0.29, 0.62)

    part(root, "Body", body, shade("paper", 4, 2, 0))

    def fan(bm):
        verts = cylinder(bm, 12, 0.22, 0.22, 0.0, 0.025)
        rotate(bm, verts, "X", 90)
        move(bm, verts, (-0.1, -0.15, 0.42))

    part(root, "Fan", fan, const("steel", 1))

    def grille(bm):
        for i in range(4):
            km.box(bm, -0.1, -0.165, 0.27 + i * 0.1, 0.46, 0.01, 0.015)

    part(root, "Grille", grille, const("black", 3))

    def feet(bm):
        km.box(bm, -0.3, 0.0, 0.05, 0.08, 0.35, 0.1)
        km.box(bm, 0.3, 0.0, 0.05, 0.08, 0.35, 0.1)
        km.box(bm, 0.36, 0.13, 0.7, 0.04, 0.04, 0.6)

    part(root, "Feet", feet, shade("grime", 2, 1, 0))
    return root


def plant_pot(x):
    root = km.new_root("PlantPot", x)

    def pot(bm):
        bottom, top = km.frustum(bm, 8, 0.16, 0.21, 0.0, 0.3)
        km.cap(bm, bottom, flip=True)
        km.frustum(bm, 8, 0.23, 0.23, 0.26, 0.32)

    part(root, "Pot", pot, shade("terracotta", 3, 2, 0))

    def leaves(bm):
        _, t0 = km.frustum(bm, 7, 0.14, 0.3, 0.3, 0.5, rotate=0.3)
        _, t1 = km.frustum(bm, 7, 0.3, 0.26, 0.5, 0.7, rotate=0.3)
        _, t2 = km.frustum(bm, 7, 0.26, 0.08, 0.7, 0.88, rotate=0.3)
        km.cap(bm, t2)

    def leaf_rule(poly):
        if poly.normal.z > 0.3:
            return "park_green", 4
        return "park_green", 3 if poly.center.z > 0.6 else 2

    part(root, "Leaves", leaves, leaf_rule)
    return root


def laundry_stand(x):
    root = km.new_root("LaundryStand", x)

    def frame(bm):
        for px in (-1.1, 1.1):
            km.box(bm, px, 0.0, 0.85, 0.04, 0.04, 1.7)
            km.box(bm, px, 0.0, 0.03, 0.06, 0.7, 0.06)
        km.box(bm, 0.0, 0.0, 1.66, 2.5, 0.035, 0.035)

    part(root, "Frame", frame, const("steel", 3))

    def towels(bm):
        km.box(bm, -0.55, 0.0, 1.32, 0.42, 0.02, 0.64)
        km.box(bm, 0.25, 0.0, 1.38, 0.36, 0.02, 0.52)

    part(root, "TowelWhite", towels, const("paper", 3))

    def towel2(bm):
        km.box(bm, 0.75, 0.0, 1.3, 0.38, 0.02, 0.68)

    part(root, "TowelBlue", towel2, const("indigo", 3))
    return root


def bicycle(x):
    root = km.new_root("Bicycle", x)
    r = 0.33

    def wheels(bm):
        for px in (-0.55, 0.55):
            verts = ring(bm, 12, r, 0.05, 0.04)
            move(bm, verts, (px, 0.0, r))

    part(root, "Wheels", wheels, const("black", 2))

    def frame(bm):
        rear, front = Vector((-0.55, 0, r)), Vector((0.55, 0, r))
        crank = Vector((-0.05, 0, 0.3))
        seat = Vector((-0.25, 0, 0.82))
        head = Vector((0.38, 0, 0.82))
        beam(bm, rear, crank, 0.035)
        beam(bm, rear, seat, 0.03)
        beam(bm, crank, seat, 0.04)
        beam(bm, crank, head + Vector((-0.08, 0, -0.25)), 0.045)
        beam(bm, head + Vector((-0.08, 0, -0.25)), head, 0.045)
        beam(bm, head, front, 0.035)

    part(root, "Frame", frame, const("sign_red", 2))

    def parts(bm):
        km.box(bm, -0.27, 0.0, 0.87, 0.24, 0.12, 0.05)          # サドル
        beam(bm, (0.38, -0.25, 0.98), (0.38, 0.25, 0.98), 0.03)  # ハンドル
        beam(bm, (0.38, 0, 0.82), (0.38, 0, 0.98), 0.028)
        km.box(bm, 0.62, 0.0, 0.88, 0.3, 0.34, 0.03)            # かごの底
        km.box(bm, 0.62, 0.17, 0.98, 0.3, 0.01, 0.2)
        km.box(bm, 0.62, -0.17, 0.98, 0.3, 0.01, 0.2)
        km.box(bm, 0.77, 0.0, 0.98, 0.01, 0.34, 0.2)
        km.box(bm, 0.47, 0.0, 0.98, 0.01, 0.34, 0.2)
        beam(bm, (-0.05, 0, 0.3), (-0.2, 0.18, 0.02), 0.025)     # スタンド

    part(root, "Parts", parts, shade("steel", 3, 2, 1))
    return root


# ── 商店街の小物 ───────────────────────────────────────

def shutter(x):
    root = km.new_root("Shutter", x)

    def slats(bm):
        for i in range(13):
            z = 0.1 + i * 0.2
            km.box(bm, 0.0, -0.01 * (i % 2), z + 0.1, 3.0, 0.06, 0.2)

    def slat_rule(poly):
        return "steel", 3 if int((poly.center.z - 0.1) / 0.2) % 2 == 0 else 2

    part(root, "Slats", slats, slat_rule)

    def housing(bm):
        km.box(bm, 0.0, -0.08, 2.9, 3.2, 0.36, 0.4)
        km.box(bm, -1.55, -0.02, 1.35, 0.1, 0.12, 2.7)
        km.box(bm, 1.55, -0.02, 1.35, 0.1, 0.12, 2.7)

    part(root, "Housing", housing, shade("steel", 3, 1, 0))

    def bottom(bm):
        km.box(bm, 0.0, -0.04, 0.05, 3.0, 0.09, 0.1)
        km.box(bm, 0.0, -0.09, 0.14, 0.14, 0.02, 0.1)

    part(root, "Bottom", bottom, const("black", 3))
    return root


def sign_blade(x):
    """壁から通りへ突き出す袖看板。原点は壁の面。看板は -Y（通り側）へ出る。"""
    root = km.new_root("SignBlade", x)

    def bracket(bm):
        km.box(bm, 0.0, -0.2, 1.55, 0.04, 0.4, 0.04)
        km.box(bm, 0.0, -0.2, 0.05, 0.04, 0.4, 0.04)

    part(root, "Bracket", bracket, const("steel", 2))

    def frame(bm):
        km.box(bm, 0.0, -0.65, 0.8, 0.22, 0.6, 1.6)

    part(root, "Frame", frame, shade("sign_red", 3, 2, 1))

    def faces(bm):
        for sx in (-0.115, 0.115):
            km.box(bm, sx, -0.65, 0.8, 0.01, 0.48, 1.42)

    part(root, "Face", faces, const("paper", 4))

    def letters(bm):
        for sx in (-0.122, 0.122):
            for i in range(4):
                km.box(bm, sx, -0.65, 1.33 - i * 0.34, 0.01, 0.3, 0.22)

    part(root, "Letters", letters, const("sign_red", 1))
    return root


def sign_stand(x):
    root = km.new_root("SignStand", x)

    def boards(bm):
        for side in (-1, 1):
            verts = km.box(bm, 0.0, 0.0, 0.45, 0.56, 0.03, 0.9)
            rotate(bm, verts, "X", side * 12, center=(0.0, 0.0, 0.9))
            move(bm, verts, (0.0, side * 0.06, 0.0))

    part(root, "Boards", boards, shade("black", 3, 3, 1))

    def faces(bm):
        verts = km.box(bm, 0.0, -0.02, 0.4, 0.46, 0.01, 0.62)
        rotate(bm, verts, "X", -12, center=(0.0, 0.0, 0.9))
        move(bm, verts, (0.0, -0.06, 0.0))
        verts = km.box(bm, 0.0, -0.02, 0.78, 0.46, 0.012, 0.12)
        rotate(bm, verts, "X", -12, center=(0.0, 0.0, 0.9))
        move(bm, verts, (0.0, -0.065, 0.0))

    def face_rule(poly):
        return ("sign_red", 3) if poly.center.z > 0.72 else ("paper", 4)

    part(root, "Face", faces, face_rule)
    return root


def noren(x):
    root = km.new_root("Noren", x)

    def rod(bm):
        km.box(bm, 0.0, 0.0, 2.12, 1.9, 0.04, 0.04)

    part(root, "Rod", rod, const("grime", 3))

    def cloth(bm):
        for i in range(3):
            km.box(bm, -0.6 + i * 0.6, 0.0, 1.72, 0.56, 0.015, 0.76)

    def cloth_rule(poly):
        return "indigo", 2 if abs(poly.center.x) > 0.3 else 3

    part(root, "Cloth", cloth, cloth_rule)

    def crest(bm):
        km.box(bm, 0.0, -0.01, 1.78, 0.26, 0.01, 0.26)

    part(root, "Crest", crest, const("paper", 4))
    return root


def curve_mirror(x):
    root = km.new_root("CurveMirror", x)

    def pole(bm):
        cylinder(bm, 8, 0.04, 0.04, 0.0, 2.75)

    part(root, "Pole", pole, shade("store_orange", 4, 3, 1))

    def frame(bm):
        verts = cylinder(bm, 12, 0.42, 0.42, 0.0, 0.06)
        rotate(bm, verts, "X", 90 - 12)
        move(bm, verts, (0.0, -0.08, 2.55))
        km.box(bm, 0.0, -0.02, 2.55, 0.08, 0.1, 0.12)

    part(root, "Frame", frame, shade("store_orange", 3, 3, 1))

    def glass(bm):
        verts = cylinder(bm, 12, 0.37, 0.37, 0.0, 0.01)
        rotate(bm, verts, "X", 90 - 12)
        move(bm, verts, (0.0, -0.142, 2.55))

    part(root, "Glass", glass, const("mirror", 3))
    return root


# ── 組み立てと書き出し ─────────────────────────────────

km.clear_scene()
km.build_palette_image(OUT + "/KM_Palette.png")

roots = [
    utility_pole(0.0),
    block_wall("BlockWall_A", 3.0, 2, False),
    block_wall("BlockWall_B", 5.5, 1, True),
    arcade_roof(8.0),
    banner("Banner_Red", 21.0, "sign_red"),
    banner("Banner_Indigo", 22.5, "indigo"),
    air_conditioner(24.5),
    plant_pot(26.0),
    laundry_stand(28.5),
    bicycle(32.0),
    shutter(35.5),
    sign_blade(38.5),
    sign_stand(40.0),
    noren(42.0),
    curve_mirror(44.5),
]

# Blender の中では見比べるために横へ並べてあるので、書き出す間だけ原点へ戻す（FBX の原点＝底面の中心）。
for root in roots:
    x = root.location.x
    root.location.x = 0.0
    try:
        km.export_fbx([root], OUT + "/Props/Street/" + root.name + ".fbx")
    finally:
        root.location.x = x

print("exported:", [r.name for r in roots])
