"""拾い物（一人称の切片・2026-10-28）。ゴミ箱から飛び出し、宙で掴む品の模型（`Docs/切片の品目.md`）。

掴む道具なので、実寸で書いて最後に PROP_SCALE を掛ける。原点は底面の中心、正面は Blender の -Y。
宙を舞って回るので、正面の向きは意味を持たない。どの角度から見ても何か分かる形と色を優先する。
同じ向きの面を同じ平面に重ねない（色違いの部分は、貼り足さずに隣り合う別の箱として並べる）。

Blender で実行（背景でも可）：
    blender --background --python D:/Unity/KyoumoMushoku/Tools/Models/gen/items.py
書き出し先：Assets/Art/Models/Props/Items/Item_<id>.fbx と Assets/Art/Models/KM_Palette.png
"""

import importlib
import math
import sys

import bmesh
import bpy
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


def lathe(bm, sides, profile, rotate=0.0):
    """(半径, 高さ) の列を回した回転体。両端は閉じる。瓶・パン・帽子の山に使う。"""
    rings = []
    for r, z in profile:
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / sides + rotate),
                                    r * math.sin(2 * math.pi * i / sides + rotate), z)) for i in range(sides)])
    faces = []
    for a, b in zip(rings, rings[1:]):
        for i in range(sides):
            j = (i + 1) % sides
            faces.append(bm.faces.new((a[i], a[j], b[j], b[i])))
    faces.append(bm.faces.new(list(reversed(rings[0]))))
    faces.append(bm.faces.new(rings[-1]))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return [v for ring in rings for v in ring]


def prism(bm, points, y0, y1):
    """XZ 面の多角形を Y 方向へ押し出した柱（おにぎりの三角など）。"""
    front = [bm.verts.new((x, y0, z)) for x, z in points]
    back = [bm.verts.new((x, y1, z)) for x, z in points]
    faces = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
    n = len(points)
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((front[i], back[i], back[j], front[j])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return front + back


def rotate(bm, verts, axis, degrees, center=(0.0, 0.0, 0.0)):
    bmesh.ops.rotate(bm, verts=verts, cent=Vector(center), matrix=Matrix.Rotation(math.radians(degrees), 3, axis))


def move(bm, verts, offset):
    bmesh.ops.translate(bm, verts=verts, vec=Vector(offset))


def beam(bm, a, b, thickness):
    a, b = Vector(a), Vector(b)
    d = b - a
    verts = km.box(bm, 0.0, 0.0, 0.0, thickness, thickness, d.length)
    rot = d.normalized().to_track_quat("Z", "Y").to_matrix()
    bmesh.ops.rotate(bm, verts=verts, cent=Vector((0, 0, 0)), matrix=rot)
    move(bm, verts, (a + b) / 2)
    return verts


def pipe(bm, a, b, radius, sides=6):
    a, b = Vector(a), Vector(b)
    d = b - a
    verts = cylinder(bm, sides, radius, radius, -d.length / 2, d.length / 2)
    rot = d.normalized().to_track_quat("Z", "Y").to_matrix()
    bmesh.ops.rotate(bm, verts=verts, cent=Vector((0, 0, 0)), matrix=rot)
    move(bm, verts, (a + b) / 2)
    return verts


def torus(bm, sides, radius, tube, tube_sides=6):
    """寝かせた輪（Z 軸まわり）。巻いた銅線・腕時計の帯。"""
    rings = []
    for i in range(sides):
        a = 2 * math.pi * i / sides
        c = Vector((radius * math.cos(a), radius * math.sin(a), 0.0))
        out = Vector((math.cos(a), math.sin(a), 0.0))
        rings.append([bm.verts.new(c + out * (tube * math.cos(2 * math.pi * k / tube_sides))
                                   + Vector((0, 0, tube * math.sin(2 * math.pi * k / tube_sides))))
                      for k in range(tube_sides)])
    faces = []
    for i in range(sides):
        a, b = rings[i], rings[(i + 1) % sides]
        for k in range(tube_sides):
            n = (k + 1) % tube_sides
            faces.append(bm.faces.new((a[k], b[k], b[n], a[n])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return [v for ring in rings for v in ring]


def item(id_):
    return km.new_root("Item_" + id_)


# ── 飲み物 ─────────────────────────────────────────────

def pet_bottle(id_, label, squash=1.0):
    """500ml のペットボトル。squash < 1 なら上半分がつぶれている（飲みかけ）。"""
    root = item(id_)

    def squashed(verts):
        for v in verts:
            if v.co.z > 0.08:
                v.co.x *= squash

    part(root, "Bottle", lambda bm: squashed(lathe(bm, 8, [(0.031, 0.0), (0.033, 0.01), (0.033, 0.05), (0.033, 0.13),
                                                            (0.031, 0.15), (0.018, 0.178), (0.013, 0.19)])),
         shade("mirror", 4, 3, 1))
    part(root, "Label", lambda bm: squashed(cylinder(bm, 8, 0.0345, 0.0345, 0.055, 0.115, caps=False)), const(label, 2))
    part(root, "Cap", lambda bm: cylinder(bm, 8, 0.0145, 0.0145, 0.19, 0.207), shade("paper", 4, 3, 1))
    return root


def can_coffee():
    """缶コーヒー（190g の細い缶）。紺の胴に金の帯。"""
    root = item("can_coffee")
    part(root, "Body", lambda bm: cylinder(bm, 10, 0.026, 0.026, 0.006, 0.07, caps=False), const("indigo", 2))
    part(root, "Band", lambda bm: cylinder(bm, 10, 0.026, 0.026, 0.07, 0.085, caps=False), const("caution", 3))
    part(root, "Top", lambda bm: cylinder(bm, 10, 0.026, 0.024, 0.085, 0.104), shade("steel", 4, 3, 1))
    part(root, "Bottom", lambda bm: cylinder(bm, 10, 0.022, 0.026, 0.0, 0.006), shade("steel", 3, 2, 0))
    return root


def can_aluminum():
    """つぶれたアルミ缶（350ml）。胴がへこみ、上が少し傾いている。"""
    root = item("can_aluminum")

    def body(bm):
        verts = lathe(bm, 10, [(0.026, 0.0), (0.033, 0.01), (0.033, 0.04), (0.029, 0.06), (0.033, 0.08),
                               (0.033, 0.11), (0.027, 0.122)])
        for v in verts:
            if v.co.z > 0.06:
                v.co.x += (v.co.z - 0.06) * 0.12
            if 0.05 < v.co.z < 0.07 and v.co.y > 0:
                v.co.y *= 0.75
    part(root, "Body", body, shade("steel", 4, 3, 1))
    part(root, "Band", lambda bm: cylinder(bm, 10, 0.034, 0.034, 0.012, 0.038, caps=False), const("sign_red", 3))
    return root


def bottle_empty():
    """空き瓶（緑の 500ml）。"""
    root = item("bottle_empty")
    part(root, "Bottle", lambda bm: lathe(bm, 8, [(0.03, 0.0), (0.034, 0.01), (0.034, 0.16), (0.028, 0.19),
                                                   (0.013, 0.215), (0.012, 0.25), (0.015, 0.255), (0.012, 0.262)]),
         shade("bottle_green", 4, 3, 1))
    part(root, "Label", lambda bm: cylinder(bm, 8, 0.0352, 0.0352, 0.05, 0.11, caps=False), const("paper", 3))
    return root


# ── 食べ物 ─────────────────────────────────────────────

def onigiri():
    """おにぎり。角を落とした三角の白飯に、下半分を巻く海苔。"""
    root = item("onigiri")
    w, h, t = 0.095, 0.085, 0.034
    tri = [(-w / 2 + 0.012, 0.0), (w / 2 - 0.012, 0.0), (w / 2, 0.012), (0.009, h), (-0.009, h), (-w / 2, 0.012)]
    part(root, "Rice", lambda bm: prism(bm, tri, -t / 2, t / 2), shade("paper", 4, 3, 1))
    part(root, "Nori", lambda bm: km.box(bm, 0.0, 0.0, 0.022, 0.05, t + 0.012, 0.044), shade("black", 3, 2, 0))
    return root


def bento():
    """コンビニ弁当。黒い容器に、白飯と三つのおかず。"""
    root = item("bento")
    W, D, H = 0.2, 0.15, 0.035
    part(root, "Tray", lambda bm: km.box(bm, 0.0, 0.0, H / 2, W, D, H), shade("black", 3, 2, 0))

    def food(bm):
        km.box(bm, -0.045, 0.0, H + 0.008, 0.09, 0.13, 0.016)
    part(root, "Rice", food, shade("paper", 4, 3, 1))
    part(root, "Fry", lambda bm: km.rounded_box(bm, 0.05, -0.035, H + 0.012, 0.07, 0.05, 0.024, 0.006),
         shade("rust", 4, 3, 1))
    part(root, "Greens", lambda bm: km.box(bm, 0.035, 0.035, H + 0.008, 0.04, 0.045, 0.016), shade("park_green", 4, 3, 1))
    part(root, "Pickle", lambda bm: km.box(bm, 0.08, 0.035, H + 0.007, 0.025, 0.04, 0.014), shade("store_orange", 4, 3, 1))
    return root


def bread():
    """袋入りのパン（山型）。焼き色の山と、袋の口のねじり。"""
    root = item("bread")
    part(root, "Loaf", lambda bm: km.rounded_box(bm, 0.0, 0.0, 0.04, 0.17, 0.1, 0.08, 0.022), shade("rust", 4, 3, 1))
    part(root, "Twist", lambda bm: [km.box(bm, 0.1, 0.0, 0.04, 0.03, 0.03, 0.03), km.box(bm, 0.123, 0.0, 0.04, 0.016, 0.05, 0.05)],
         shade("paper", 4, 3, 1))
    return root


def sweet_bun():
    """菓子パン（丸いメロンパン風）。上が焼き色の黄、下が明るい生地。"""
    root = item("sweet_bun")
    part(root, "Bun", lambda bm: lathe(bm, 10, [(0.045, 0.0), (0.055, 0.012), (0.052, 0.03), (0.035, 0.046), (0.0, 0.05)]),
         km.by_normal("caution", 4, 3, 1))
    return root


def karaage():
    """ホットスナックの揚げ物。橙の紙の箱から唐揚げが三つのぞく。"""
    root = item("karaage")
    part(root, "Box", lambda bm: lathe(bm, 4, [(0.04, 0.0), (0.05, 0.065)], rotate=math.pi / 4), shade("store_orange", 4, 3, 1))

    def nuggets(bm):
        for x, y, z, sx, sy, sz, turn in ((-0.017, -0.01, 0.066, 0.042, 0.034, 0.03, 20),
                                         (0.018, 0.004, 0.07, 0.038, 0.036, 0.032, -35),
                                         (0.0, 0.014, 0.088, 0.036, 0.03, 0.03, 60)):
            before = set(bm.verts)
            km.rounded_box(bm, 0.0, 0.0, 0.0, sx, sy, sz, 0.009)
            group = [v for v in bm.verts if v not in before]
            rotate(bm, group, "Z", turn)
            rotate(bm, group, "X", turn * 0.4)
            move(bm, group, (x, y, z))
    part(root, "Nuggets", nuggets, shade("rust", 4, 3, 1))
    return root


# ── 気を晴らす物 ───────────────────────────────────────

def cigarette():
    """拾ったタバコ。くしゃっとした箱から二本のぞく。"""
    root = item("cigarette")
    part(root, "Pack", lambda bm: km.box(bm, 0.0, 0.0, 0.035, 0.055, 0.022, 0.07), shade("paper", 4, 3, 1))
    part(root, "Band", lambda bm: km.box(bm, 0.0, 0.0, 0.077, 0.055, 0.022, 0.014), shade("sign_red", 4, 3, 1))

    def sticks(bm):
        pipe(bm, (-0.012, 0.0, 0.08), (-0.016, 0.0, 0.11), 0.0045)
        pipe(bm, (0.006, 0.002, 0.08), (0.012, 0.002, 0.103), 0.0045)
    part(root, "Sticks", sticks, shade("store_orange", 4, 3, 2))
    return root


def cig_butt():
    """吸い殻。橙のフィルターに、焦げた紙。二本。"""
    root = item("cig_butt")

    def filters(bm):
        pipe(bm, (-0.02, -0.006, 0.005), (0.0, -0.006, 0.005), 0.005)
        pipe(bm, (-0.006, 0.01, 0.005), (0.012, 0.018, 0.005), 0.005)
    part(root, "Filter", filters, shade("store_orange", 4, 3, 1))

    def paper(bm):
        pipe(bm, (0.0, -0.006, 0.005), (0.014, -0.006, 0.005), 0.0048)
        pipe(bm, (0.012, 0.018, 0.005), (0.024, 0.0228, 0.005), 0.0048)
    part(root, "Paper", paper, shade("paper", 4, 3, 1))

    def ash(bm):
        pipe(bm, (0.014, -0.006, 0.005), (0.018, -0.006, 0.005), 0.0046)
        pipe(bm, (0.024, 0.0228, 0.005), (0.028, 0.0244, 0.005), 0.0046)
    part(root, "Ash", ash, shade("black", 3, 2, 1))
    return root


def weekly_mag():
    """読み古しの週刊誌。表紙は題字の赤い帯・写真・見出しの帯を隣り合わせて並べる。"""
    root = item("weekly_mag")
    W, D, T = 0.21, 0.28, 0.012
    part(root, "Pages", lambda bm: km.box(bm, 0.003, 0.0, T / 2, W - 0.006, D - 0.006, T - 0.002), shade("paper", 3, 2, 1))
    part(root, "Title", lambda bm: km.box(bm, 0.0, -D / 2 + 0.03, T, W, 0.06, 0.002), const("sign_red", 3))
    part(root, "Photo", lambda bm: km.box(bm, 0.0, 0.025, T, W, D - 0.06 - 0.05, 0.002), const("caution", 3))
    part(root, "Headline", lambda bm: km.box(bm, 0.0, D / 2 - 0.025, T, W, 0.05, 0.002), const("black", 3))
    part(root, "Back", lambda bm: km.box(bm, 0.0, 0.0, 0.001, W, D, 0.002), const("paper", 1))
    return root


def paperback():
    """文庫本。藍の表紙と、紙の小口。"""
    root = item("paperback")
    W, D, T = 0.105, 0.15, 0.018
    part(root, "Pages", lambda bm: km.box(bm, 0.003, 0.0, T / 2, W - 0.006, D - 0.006, T - 0.004), shade("paper", 3, 3, 1))

    def cover(bm):
        km.box(bm, 0.0, 0.0, T - 0.001, W, D, 0.002)
        km.box(bm, 0.0, 0.0, 0.001, W, D, 0.002)
        km.box(bm, -W / 2 + 0.001, 0.0, T / 2, 0.002, D, T - 0.004)
    part(root, "Cover", cover, shade("indigo", 4, 3, 2))
    return root


# ── 換金廃品・値打ち物 ─────────────────────────────────

def umbrella_broken():
    """壊れた傘（折れた骨が一本飛び出している）。長さ 0.75m。"""
    root = item("umbrella_broken")
    part(root, "Shaft", lambda bm: pipe(bm, (0.0, 0.0, 0.06), (0.0, 0.0, 0.75), 0.006), shade("steel", 4, 3, 1))
    part(root, "Canopy", lambda bm: lathe(bm, 8, [(0.012, 0.18), (0.05, 0.26), (0.045, 0.45), (0.025, 0.66), (0.008, 0.72)]),
         shade("black", 4, 3, 1))

    def rib(bm):
        beam(bm, (0.04, 0.0, 0.4), (0.13, -0.02, 0.5), 0.006)
        beam(bm, (0.13, -0.02, 0.5), (0.15, -0.03, 0.44), 0.006)
    part(root, "Rib", rib, shade("steel", 4, 3, 1))

    def handle(bm):
        beam(bm, (0.0, 0.0, 0.08), (0.0, 0.0, 0.0), 0.022)
        beam(bm, (0.0, 0.0, 0.0), (0.05, 0.0, -0.01), 0.022)
        beam(bm, (0.05, 0.0, -0.01), (0.07, 0.0, 0.03), 0.022)
    part(root, "Handle", handle, shade("wood_dark", 4, 3, 1))
    for obj in root.children:
        obj.location.z += 0.02
    return root


def copper_wire():
    """銅線くず。巻いた束と、ほどけた端。"""
    root = item("copper_wire")

    def coil(bm):
        for i, (r, z) in enumerate(((0.085, 0.012), (0.08, 0.03), (0.088, 0.046))):
            verts = torus(bm, 12, r, 0.01)
            move(bm, verts, (0.004 * i, 0.0, z))
        beam(bm, (0.085, 0.0, 0.046), (0.15, -0.04, 0.02), 0.012)
    part(root, "Coil", coil, shade("terracotta", 4, 3, 1))
    return root


def brand_box():
    """ブランド品の空き箱。黒い硬い箱に金のリボン。"""
    root = item("brand_box")
    W, D, H = 0.26, 0.19, 0.1
    part(root, "Base", lambda bm: km.box(bm, 0.0, 0.0, 0.035, W - 0.006, D - 0.006, 0.07), shade("black", 3, 2, 0))
    part(root, "Lid", lambda bm: km.box(bm, 0.0, 0.0, 0.07 + 0.015, W, D, 0.03), shade("black", 4, 3, 1))

    def ribbon(bm):
        km.box(bm, 0.0, 0.0, H - 0.0, 0.022, D + 0.004, 0.004)
        km.box(bm, 0.0, -D / 2 - 0.001, 0.085, 0.022, 0.004, 0.034)
        km.box(bm, 0.0, D / 2 + 0.001, 0.085, 0.022, 0.004, 0.034)
        km.rounded_box(bm, 0.0, 0.0, H + 0.01, 0.06, 0.03, 0.016, 0.005)
    part(root, "Ribbon", ribbon, shade("caution", 4, 3, 1))
    return root


def game_soft():
    """中古のゲームソフト。赤いケースに、絵の貼られた表。"""
    root = item("game_soft")
    W, D, T = 0.11, 0.17, 0.014
    part(root, "Case", lambda bm: km.box(bm, 0.0, 0.0, T / 2 - 0.001, W, D, T - 0.002), shade("sign_red", 3, 2, 1))
    part(root, "Art", lambda bm: km.box(bm, 0.004, 0.012, T, W - 0.014, D - 0.04, 0.002), const("indigo", 4))
    part(root, "Logo", lambda bm: km.box(bm, 0.004, -D / 2 + 0.015, T, W - 0.014, 0.022, 0.002), const("paper", 4))
    return root


def wristwatch():
    """止まった腕時計。金の文字盤と黒い帯。"""
    root = item("wristwatch")

    def strap(bm):
        verts = torus(bm, 12, 0.036, 0.007, 4)
        for v in verts:
            v.co.y *= 0.75
        move(bm, verts, (0.0, 0.0, 0.007))
    part(root, "Strap", strap, shade("black", 3, 2, 1))
    part(root, "Case", lambda bm: cylinder(bm, 12, 0.022, 0.022, 0.006, 0.02), shade("caution", 4, 3, 1))
    part(root, "Glass", lambda bm: cylinder(bm, 12, 0.017, 0.017, 0.02, 0.023), const("paper", 4))

    def hands(bm):
        km.box(bm, 0.0, 0.006, 0.0245, 0.003, 0.013, 0.001)
        km.box(bm, 0.004, 0.0, 0.0245, 0.009, 0.003, 0.001)
    part(root, "Hands", hands, const("black", 2))
    for obj in root.children:
        obj.location.x += 0.036
    return root


def handheld():
    """画面の割れた携帯ゲーム機。黒い本体・灰の画面・十字キーとボタン。"""
    root = item("handheld")
    W, D, T = 0.17, 0.085, 0.02
    part(root, "Body", lambda bm: km.rounded_box(bm, 0.0, 0.0, T / 2, W, D, T, 0.008), shade("black", 4, 3, 1))
    part(root, "Screen", lambda bm: km.box(bm, 0.0, 0.0, T + 0.0015, 0.08, 0.05, 0.003), const("mirror", 2))

    def crack(bm):
        beam(bm, (-0.03, -0.02, T + 0.0035), (0.01, 0.006, T + 0.0035), 0.002)
        beam(bm, (0.01, 0.006, T + 0.0035), (0.034, 0.02, T + 0.0035), 0.002)
        beam(bm, (0.01, 0.006, T + 0.0035), (0.02, -0.02, T + 0.0035), 0.002)
    part(root, "Crack", crack, const("paper", 4))

    def buttons(bm):
        km.box(bm, -0.063, 0.0, T + 0.002, 0.024, 0.008, 0.004)
        km.box(bm, -0.063, 0.0, T + 0.002, 0.008, 0.024, 0.004)
        for x, y in ((0.058, 0.007), (0.068, -0.004)):
            move(bm, cylinder(bm, 6, 0.0055, 0.0055, T, T + 0.004), (x, y, 0.0))
    part(root, "Buttons", buttons, shade("steel", 4, 3, 1))
    return root


# ── その場で金になる物 ─────────────────────────────────

def coin_purse():
    """落とした小銭入れ（がま口）。藍の袋に銀の口金と二つの玉。"""
    root = item("coin_purse")
    part(root, "Pouch", lambda bm: km.rounded_box(bm, 0.0, 0.0, 0.035, 0.1, 0.035, 0.07, 0.012), shade("indigo", 4, 3, 1))

    def clasp(bm):
        km.box(bm, 0.0, 0.0, 0.072, 0.096, 0.03, 0.008)
        for x in (-0.007, 0.007):
            verts = cylinder(bm, 6, 0.0055, 0.0055, 0.076, 0.088)
            move(bm, verts, (x, 0.0, 0.0))
    part(root, "Clasp", clasp, shade("steel", 4, 3, 1))
    return root


def scratch():
    """削っていないスクラッチくじ。黄の帯・銀の削る所・白い地。"""
    root = item("scratch")
    W, D, T = 0.09, 0.06, 0.004
    part(root, "Card", lambda bm: km.box(bm, 0.0, 0.0, T / 2 - 0.0005, W, D, T - 0.001), shade("paper", 4, 3, 1))
    part(root, "Band", lambda bm: km.box(bm, 0.0, -D / 2 + 0.008, T, W, 0.016, 0.001), const("caution", 4))
    part(root, "Silver", lambda bm: km.box(bm, 0.0, 0.01, T, W - 0.02, 0.026, 0.001), const("steel", 4))
    return root


# ── 道具 ───────────────────────────────────────────────

def work_gloves():
    """軍手（一組を重ねて置いたところ）。生成りの編み地に、灰の袖口。平たい形で読ませる。"""
    root = item("work_gloves")

    def glove(bm, ox, oy, angle, z):
        # rounded_box は面取りで元の頂点を消すので、作る前にあった頂点との差で一つの手袋の頂点を取る。
        before = set(bm.verts)
        km.rounded_box(bm, 0.0, 0.0, 0.0, 0.085, 0.09, 0.014, 0.004)          # 手のひら
        for i, x in enumerate((-0.03, -0.01, 0.01, 0.03)):
            length = (0.06, 0.07, 0.068, 0.055)[i]
            km.rounded_box(bm, x, 0.045 + length / 2, 0.0, 0.017, length, 0.012, 0.004)
        km.rounded_box(bm, -0.05, 0.015, 0.0, 0.018, 0.05, 0.012, 0.004)       # 親指
        group = [v for v in bm.verts if v not in before]
        rotate(bm, group, "Z", angle)
        move(bm, group, (ox, oy, z))

    part(root, "Gloves", lambda bm: [glove(bm, -0.012, 0.0, 8, 0.008), glove(bm, 0.015, -0.01, -6, 0.022)],
         shade("paper", 3, 2, 1))

    def cuffs(bm):
        for ox, oy, angle, z in ((-0.012, 0.0, 8, 0.008), (0.015, -0.01, -6, 0.022)):
            verts = km.box(bm, 0.0, -0.06, 0.0, 0.085, 0.03, 0.016)
            rotate(bm, verts, "Z", angle)
            move(bm, verts, (ox, oy, z))
    part(root, "Cuffs", cuffs, shade("grime", 4, 3, 1))
    return root


def cap():
    """キャップ。深緑の山・前へ出たつば・てっぺんのボタン。"""
    root = item("cap")
    part(root, "Crown", lambda bm: lathe(bm, 10, [(0.095, 0.0), (0.094, 0.04), (0.082, 0.075), (0.055, 0.1),
                                                   (0.02, 0.112), (0.0, 0.114)]), shade("park_green", 4, 3, 1))

    def brim(bm):
        verts = lathe(bm, 8, [(0.075, 0.0), (0.075, 0.01)], rotate=math.pi / 8)
        for v in verts:
            v.co.y = v.co.y * 1.4 - 0.09
        rotate(bm, verts, "X", -8, (0.0, -0.09, 0.0))
    part(root, "Brim", brim, shade("black", 4, 3, 1))
    part(root, "Button", lambda bm: cylinder(bm, 6, 0.008, 0.008, 0.112, 0.12), shade("black", 4, 3, 1))
    return root


def cardboard():
    """段ボール（たたんだ大きな一枚）。クラフト紙の板・折り目・ガムテープ。背負って運ぶ。"""
    root = item("cardboard")
    W, D = 0.6, 0.45
    part(root, "Sheet", lambda bm: km.box(bm, 0.0, 0.0, 0.015, W, D, 0.03), shade("kraft", 4, 3, 1))
    part(root, "Flap", lambda bm: km.box(bm, 0.02, 0.01, 0.04, W - 0.06, D - 0.04, 0.02), shade("kraft", 3, 2, 1))
    part(root, "Tape", lambda bm: km.box(bm, 0.0, 0.0, 0.051, 0.05, D - 0.04, 0.002), const("caution", 2))
    return root


# ── 組み立て・書き出し ─────────────────────────────────

km.clear_scene()

BUILDERS = [
    lambda: pet_bottle("water_bottle", "poly_blue"),
    lambda: pet_bottle("half_bottle", "sign_red", squash=0.8),
    onigiri, bento, bread, sweet_bun, karaage,
    can_coffee, cigarette, cig_butt, weekly_mag, paperback,
    can_aluminum, bottle_empty, umbrella_broken, copper_wire,
    brand_box, game_soft, wristwatch, handheld,
    coin_purse, scratch, work_gloves, cap, cardboard,
]

roots = []
for i, build in enumerate(BUILDERS):
    root = build()
    km.scale_hierarchy(root, km.PROP_SCALE)
    root.location.x = (i % 7) * 0.5
    root.location.y = (i // 7) * 0.6
    roots.append(root)

km.build_palette_image(OUT + "/KM_Palette.png")

if "--no-export" not in sys.argv:
    # 見比べるために並べてあるので、書き出す間だけ原点へ戻す（FBX の原点＝底面の中心）。
    for root in roots:
        x, y = root.location.x, root.location.y
        root.location.x = 0.0
        root.location.y = 0.0
        try:
            km.export_fbx([root], OUT + "/Props/Items/" + root.name + ".fbx")
        finally:
            root.location.x = x
            root.location.y = y

print("items:", len(roots), [r.name for r in roots])
