"""街の小物（一人称の切片・2026-10-27）。日本の街に見せるための背景の道具。

- 電柱（UtilityPole）：コンクリートの柱・腕金・碍子・柱上変圧器・足場ボルト・黄黒の巻き看板。
  電線は Unity 側で柱と柱の間に張る（腕金の両端＝ローカル (±0.85, 8.7, 0)）。
- ブロック塀（BlockWall_A / _B）：長さ 2m の一枚。基礎・ブロックの段・笠木。B は古く汚れた色。
- アーケードの屋根（ArcadeRoof）：長さ 4m・幅 12m の一区画。柱・梁・弧の骨・中央を明かり取りに空けた屋根板。
- のぼり（Banner_Red / _Indigo）：腕と旗。
- 室外機（AirConditioner）・植木鉢（PlantPot）・物干し（LaundryStand）・自転車（Bicycle）
- シャッター（Shutter）・袖看板（SignBlade）・立て看板（SignStand）・暖簾（Noren）・カーブミラー（CurveMirror）
- 児童公園：ブランコ（Swing）・すべり台（Slide）・砂場（Sandbox）・車止め（CarStop）・公園の時計（ParkClock）

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
    """縦に立てた輪（車輪）。Y 軸が回転軸。外周・内周・両側の面で閉じた輪にする
    （両側の面が無いと、横から見たとき面積がほぼ無く、輪が切れ切れの弧に見える）。"""
    rings = []
    for rr in (radius, radius - thickness):
        for y in (-width / 2, width / 2):
            rings.append([bm.verts.new((rr * math.cos(2 * math.pi * i / sides), y, rr * math.sin(2 * math.pi * i / sides)))
                          for i in range(sides)])
    o0, o1, i0, i1 = rings
    faces = []
    for a, b in ((o0, o1), (o1, i1), (i1, i0), (i0, o0)):
        for k in range(sides):
            n = (k + 1) % sides
            faces.append(bm.faces.new((a[k], a[n], b[n], b[k])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return o0 + o1 + i0 + i1


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
        # 柱はアーチの起点の真下へまっすぐ下ろす（2026-10-29 作者の指示で、横梁と方杖を足す前の形に戻した）。
        # 店の正面に掛かるので、切片では通りの両端の二組だけを残し、間の柱は置き場で消している。
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
            verts = ring(bm, 16, r, 0.06, 0.045)
            move(bm, verts, (px, 0.0, r))
            km.box(bm, px, 0.0, r, 0.07, 0.08, 0.07)  # ハブ

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
        for sy in (-0.06, 0.06):                                  # 後ろの両立スタンド（立ったまま停まる）
            beam(bm, (-0.55, sy, r), (-0.66, sy * 1.6, 0.012), 0.022)

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


# ── 路地の祠・歩道の敷石 ───────────────────────────────

def hokora(x):
    """路地の角の小さな祠。石の台・木の祠と瓦の小屋根・小さな赤い鳥居・賽銭箱。正面は -Y。"""
    root = km.new_root("Hokora", x)

    def base(bm):
        km.box(bm, 0.0, 0.0, 0.15, 1.1, 0.9, 0.3)
        km.box(bm, 0.0, 0.05, 0.45, 0.85, 0.7, 0.3)

    part(root, "Base", base, shade("concrete", 3, 2, 1))

    def shrine(bm):
        km.box(bm, 0.0, 0.1, 0.9, 0.55, 0.45, 0.6)
        km.box(bm, 0.0, -0.14, 0.85, 0.36, 0.02, 0.4)      # 扉

    part(root, "Shrine", shrine, shade("wood_dark", 3, 2, 1))

    def roof(bm):
        # 切妻：棟（y=0.1）から前後の軒へ下がる。X 軸回りの正の回転は +Y の側を持ち上げるので、
        # 奥（side=+1）の板は負の向きに回す（符号を逆にすると V 字に開いた屋根になる）。
        for side in (-1, 1):
            verts = km.box(bm, 0.0, 0.1 + side * 0.2, 1.29, 0.8, 0.46, 0.05)
            rotate(bm, verts, "X", -side * 28, center=(0.0, 0.1 + side * 0.2, 1.29))
        km.box(bm, 0.0, 0.1, 1.4, 0.82, 0.08, 0.06)

    part(root, "Roof", roof, shade("kawara", 3, 2, 0))

    def torii(bm):
        for sx in (-0.38, 0.38):
            move(bm, cylinder(bm, 8, 0.035, 0.035, 0.0, 1.15), (sx, -0.75, 0.0))
        km.box(bm, 0.0, -0.75, 1.2, 1.0, 0.07, 0.07)         # 笠木
        km.box(bm, 0.0, -0.75, 1.0, 0.85, 0.05, 0.05)        # 貫

    part(root, "Torii", torii, shade("sign_red", 3, 3, 1))

    def box(bm):
        # 賽銭箱は上の段の上、祠の正面に載せる（台に埋めない）。
        km.box(bm, 0.0, -0.21, 0.72, 0.32, 0.16, 0.24)

    part(root, "Offering", box, shade("wood_dark", 4, 3, 1))
    part(root, "OfferingSlats", lambda bm: [km.box(bm, -0.105 + i * 0.07, -0.21, 0.845, 0.025, 0.12, 0.01) for i in range(4)], const("black", 2))
    return root


def sidewalk_paver(x):
    """歩道の敷石 2m×2m の一枚。四×四の石を 2cm の目地で並べ、ところどころ色の違う石。"""
    root = km.new_root("SidewalkPaver", x)
    size, gap, n = 2.0, 0.02, 4
    stone = (size - gap * n) / n

    def stones(red):
        def build(bm):
            for i in range(n):
                for j in range(n):
                    is_red = (i * 3 + j * 5) % 7 == 0
                    if is_red != red:
                        continue
                    km.box(bm, -size / 2 + gap / 2 + stone / 2 + i * (stone + gap),
                           -size / 2 + gap / 2 + stone / 2 + j * (stone + gap), 0.0075, stone, stone, 0.015)
        return build

    part(root, "Stones", stones(False), shade("concrete", 3, 2, 1))
    part(root, "StonesRed", stones(True), shade("terracotta", 2, 1, 0))
    return root


# ── 側溝・ビールケース ─────────────────────────────────

def gutter_cover(x):
    """路地の端の側溝のコンクリート蓋 1m（幅 0.4m）。手を掛ける切り欠きが二つ。路面から 1cm 上。"""
    root = km.new_root("GutterCover", x)

    def slabs(bm):
        for i in range(2):
            km.box(bm, -0.25 + i * 0.5, 0.0, 0.01, 0.48, 0.4, 0.02)

    part(root, "Slabs", slabs, shade("concrete", 2, 1, 0))
    part(root, "Notches", lambda bm: [km.box(bm, -0.25 + i * 0.5, 0.17, 0.021, 0.08, 0.03, 0.002) for i in range(2)], const("black", 2))
    return root


def gutter_grate(x):
    """側溝の鉄の格子蓋 0.5m（幅 0.4m）。格子は太さ 3.5cm（細いとちらつく）。"""
    root = km.new_root("GutterGrate", x)

    def frame(bm):
        km.box(bm, 0.0, -0.185, 0.012, 0.5, 0.03, 0.024)
        km.box(bm, 0.0, 0.185, 0.012, 0.5, 0.03, 0.024)
        km.box(bm, -0.235, 0.0, 0.012, 0.03, 0.4, 0.024)
        km.box(bm, 0.235, 0.0, 0.012, 0.03, 0.4, 0.024)

    part(root, "Frame", frame, const("steel", 2))
    part(root, "Bars", lambda bm: [km.box(bm, -0.16 + i * 0.08, 0.0, 0.011, 0.035, 0.34, 0.022) for i in range(5)], const("steel", 1))
    part(root, "Hole", lambda bm: km.box(bm, 0.0, 0.0, 0.002, 0.42, 0.32, 0.004), const("black", 0))
    return root


def beer_crate(x):
    """赤いプラスチックのビールケース（大瓶 20 本）。積んで使う。高さ 0.3m。"""
    root = km.new_root("BeerCrate", x)
    w, d, h = 0.47, 0.37, 0.3

    def crate(bm):
        km.box(bm, 0.0, 0.0, 0.02, w, d, 0.04)                  # 底
        for sy in (-1, 1):
            km.box(bm, 0.0, sy * (d / 2 - 0.015), h * 0.55, w, 0.03, h * 0.9)
        for sx in (-1, 1):
            km.box(bm, sx * (w / 2 - 0.015), 0.0, h * 0.55, 0.03, d - 0.06, h * 0.9)

    part(root, "Crate", crate, shade("sign_red", 4, 3, 1))
    part(root, "Handles", lambda bm: [km.box(bm, sx * (w / 2 + 0.002), 0.0, h * 0.82, 0.006, 0.12, 0.04) for sx in (-1, 1)], const("black", 2))

    def bottles(bm):
        for i in range(5):
            for j in range(4):
                verts = cylinder(bm, 6, 0.03, 0.03, 0.04, 0.28)
                move(bm, verts, (-0.18 + i * 0.09, -0.135 + j * 0.09, 0.0))
                verts = cylinder(bm, 6, 0.012, 0.012, 0.28, 0.34)
                move(bm, verts, (-0.18 + i * 0.09, -0.135 + j * 0.09, 0.0))

    part(root, "Bottles", bottles, shade("terracotta", 2, 1, 0))
    return root


# ── 児童公園の遊具 ─────────────────────────────────────

def pipe(bm, a, b, radius, sides=6):
    """a から b へ向かう丸パイプ（両端は閉じる）。"""
    a, b = Vector(a), Vector(b)
    d = b - a
    verts = cylinder(bm, sides, radius, radius, -d.length / 2, d.length / 2)
    rot = d.normalized().to_track_quat("Z", "Y").to_matrix()
    bmesh.ops.rotate(bm, verts=verts, cent=Vector((0, 0, 0)), matrix=rot)
    move(bm, verts, (a + b) / 2)
    return verts


def slab(bm, a, b, width, thickness):
    """a から b へ（YZ 面内で）傾いた板。幅は X 方向。滑り台の滑走面・側板に使う。"""
    a, b = Vector(a), Vector(b)
    d = b - a
    verts = km.box(bm, 0.0, 0.0, 0.0, width, d.length, thickness)
    rotate(bm, verts, "X", math.degrees(math.atan2(d.z, d.y)))
    move(bm, verts, (a + b) / 2)
    return verts


def swing(x):
    """ブランコ（二人乗り）。鉄の門形の枠・二本の鎖・ゴムの座板と、前に黄色の安全柵。正面（-Y）へ漕ぐ。"""
    root = km.new_root("Swing", x)
    half, top = 1.6, 2.4

    def frame(bm):
        for sx in (-half, half):
            for sy in (-0.9, 0.9):
                pipe(bm, (sx, sy, 0.0), (sx, 0.0, top), 0.04)
            pipe(bm, (sx, -0.6, 0.8), (sx, 0.6, 0.8), 0.025)   # 脚をつなぐ横棒
        pipe(bm, (-half - 0.08, 0.0, top), (half + 0.08, 0.0, top), 0.05)

    part(root, "Frame", frame, shade("indigo", 3, 3, 2))

    def chains(bm):
        for cx in (-0.75, 0.75):
            for dx in (-0.2, 0.2):
                km.box(bm, cx + dx, 0.0, (top + 0.45) / 2, 0.02, 0.02, top - 0.45)

    part(root, "Chains", chains, const("steel", 3))
    part(root, "Seats", lambda bm: [km.box(bm, cx, 0.0, 0.43, 0.46, 0.17, 0.04) for cx in (-0.75, 0.75)], const("black", 3))

    def fence(bm):
        for sx in (-half - 0.2, half + 0.2):
            pipe(bm, (sx, -1.9, 0.0), (sx, -1.9, 0.62), 0.035)
        pipe(bm, (-half - 0.2, -1.9, 0.62), (half + 0.2, -1.9, 0.62), 0.035)
        pipe(bm, (-half - 0.2, -1.9, 0.32), (half + 0.2, -1.9, 0.32), 0.025)

    part(root, "Fence", fence, shade("caution", 4, 3, 2))
    return root


def slide(x):
    """すべり台。後ろ（+Y）の梯子で高さ 1.5m の踊り場へ上がり、前（-Y）へステンレスの滑走面で下りる。"""
    root = km.new_root("Slide", x)
    deck, half = 1.5, 0.35

    def frame(bm):
        for sx in (-half, half):
            for sy in (-half, half):
                pipe(bm, (sx, sy, 0.0), (sx, sy, deck + 0.75), 0.035)          # 柱（手すりの高さまで）
            pipe(bm, (sx, half + 0.95, 0.0), (sx, half, deck), 0.03)            # 梯子の桁
            pipe(bm, (sx, -half, deck + 0.75), (sx, half, deck + 0.75), 0.03)  # 踊り場の手すり
        for i in range(1, 6):                                                   # 踏み桟
            t = i / 6
            pipe(bm, (-half, half + 0.95 * (1 - t), deck * t), (half, half + 0.95 * (1 - t), deck * t), 0.022)

    part(root, "Frame", frame, shade("caution", 4, 3, 2))
    part(root, "Deck", lambda bm: km.box(bm, 0.0, 0.0, deck - 0.03, 2 * half, 2 * half, 0.06), shade("sign_red", 3, 2, 1))

    def chute(bm):
        top, low, end = (0.0, -half, deck), (0.0, -2.4, 0.32), (0.0, -2.9, 0.3)
        slab(bm, top, low, 0.5, 0.03)
        slab(bm, low, end, 0.5, 0.03)
        for sx in (-0.27, 0.27):
            for a, b in ((top, low), (low, end)):
                verts = slab(bm, (0.0, a[1], a[2] + 0.08), (0.0, b[1], b[2] + 0.08), 0.03, 0.16)
                move(bm, verts, (sx, 0.0, 0.0))

    part(root, "Chute", chute, shade("steel", 4, 3, 2))

    def chute_legs(bm):
        for sx in (-0.2, 0.2):
            pipe(bm, (sx, -2.6, 0.0), (sx, -2.6, 0.28), 0.03)
            pipe(bm, (sx, -1.4, 0.0), (sx, -1.4, 0.88), 0.03)

    part(root, "ChuteLegs", chute_legs, shade("caution", 4, 3, 2))
    return root


def sandbox(x):
    """砂場。コンクリートの縁石で囲った 2.4m × 2.0m の砂。"""
    root = km.new_root("Sandbox", x)
    w, d, t, h = 2.4, 2.0, 0.15, 0.25

    def curb(bm):
        km.box(bm, 0.0, -d / 2 + t / 2, h / 2, w, t, h)
        km.box(bm, 0.0, d / 2 - t / 2, h / 2, w, t, h)
        km.box(bm, -w / 2 + t / 2, 0.0, h / 2, t, d - 2 * t, h)
        km.box(bm, w / 2 - t / 2, 0.0, h / 2, t, d - 2 * t, h)

    part(root, "Curb", curb, shade("concrete", 3, 2, 1))
    part(root, "Sand", lambda bm: km.box(bm, 0.0, 0.0, 0.08, w - 2 * t, d - 2 * t, 0.12), shade("grime", 4, 3, 2))
    # 忘れ物のバケツ（子供の遊び場の生活感）
    part(root, "Bucket", lambda bm: move(bm, cylinder(bm, 8, 0.09, 0.11, 0.14, 0.32), (0.55, 0.3, 0.0)), shade("sign_red", 4, 3, 2))
    return root


def car_stop(x):
    """車止め（U 字のパイプ）。公園の入口に並べる。幅 0.9m・高さ 0.8m。"""
    root = km.new_root("CarStop", x)

    def u(bm):
        for sx in (-0.45, 0.45):
            pipe(bm, (sx, 0.0, 0.0), (sx, 0.0, 0.8), 0.04)
        pipe(bm, (-0.49, 0.0, 0.8), (0.49, 0.0, 0.8), 0.04)

    part(root, "Pipe", u, shade("caution", 4, 3, 2))
    part(root, "Bands", lambda bm: [move(bm, cylinder(bm, 6, 0.043, 0.043, z, z + 0.08, caps=False), (sx, 0.0, 0.0))
                                    for z in (0.45, 0.62) for sx in (-0.45, 0.45)], const("black", 2))
    return root


def park_clock(x):
    """公園の時計。高さ 3m の柱の上に両面の丸い文字盤。"""
    root = km.new_root("ParkClock", x)
    top = 3.0
    part(root, "Pole", lambda bm: pipe(bm, (0.0, 0.0, 0.0), (0.0, 0.0, top - 0.3), 0.06, sides=8), shade("concrete", 4, 3, 2))

    def case(bm):
        verts = cylinder(bm, 12, 0.32, 0.32, -0.07, 0.07)
        rotate(bm, verts, "X", 90)
        move(bm, verts, (0.0, 0.0, top))

    part(root, "Case", case, const("black", 3))

    def faces(bm):
        for sy in (-0.072, 0.072):
            verts = cylinder(bm, 12, 0.27, 0.27, -0.004, 0.004)
            rotate(bm, verts, "X", 90)
            move(bm, verts, (0.0, sy, top))

    part(root, "Face", faces, const("siding", 4))

    def hands(bm):
        for sy in (-0.08, 0.08):
            km.box(bm, 0.0, sy, top + 0.09, 0.02, 0.004, 0.18)                   # 長針（12 時）
            verts = km.box(bm, 0.0, 0.0, 0.0, 0.025, 0.004, 0.13)                # 短針（4 時ごろ）
            rotate(bm, verts, "Y", 120)
            move(bm, verts, (0.056, sy, top - 0.032))

    part(root, "Hands", hands, const("black", 1))
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
    hokora(47.0),
    sidewalk_paver(50.0),
    gutter_cover(53.0),
    gutter_grate(55.0),
    beer_crate(57.0),
    swing(60.0),
    slide(65.0),
    sandbox(69.0),
    car_stop(72.0),
    park_clock(74.0),
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
