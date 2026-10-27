"""日本の街の建物（一人称の切片・2026-10-27）。米国風の住宅と Urban の建物の置き換え。

- 住宅（House_A / _B / _C）：木造二階建て、平入りの切妻屋根（瓦かトタン）、窓・雨戸・庇・ベランダ。
- アパート（Apartment）：二階建ての木造アパート。外廊下と外階段、各戸の扉と窓。
- 店舗（Shop_A / _B / _C / _D）：一階が店（ガラス・開いたシャッター・閉じたシャッター）、テントの庇、看板の帯、上階は住まい。
- 雑居ビル（Zakkyo_A / _B）：細長いコンクリートのビル。階ごとのテナントの看板、縦の袖看板、屋上の水槽。
- 倉庫（Warehouse）：搬入口の奥。トタンの壁、大きなシャッター。
- 建物の裏（BackFacade_A / _B / _C）：裏道や路地の何もない壁を隠す一枚。裏口・小窓・樋・メーター・換気扇。

原点は **正面の壁の線の中心（底面）**。本体は +Y（奥）へ、軒・ベランダ・庇は -Y（通り）へ出る。
本体の箱は必ず "Body" という名前にする（Unity 側はこれの大きさで当たり判定を作る）。
建物は実寸。正面は Blender の -Y。

Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/buildings.py", encoding="utf-8").read())
書き出し先：Assets/Art/Models/Buildings/*.fbx と Assets/Art/Models/KM_Palette.png
"""

import importlib
import sys

import bmesh
from mathutils import Vector

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


def quad(bm, a, b, c, d, both=False):
    vs = [bm.verts.new(p) for p in (a, b, c, d)]
    bm.faces.new(vs)
    if both:
        vs = [bm.verts.new(p) for p in (d, c, b, a)]
        bm.faces.new(vs)


def tri(bm, a, b, c):
    vs = [bm.verts.new(p) for p in (a, b, c)]
    bm.faces.new(vs)


def body(root, w, d, h, material, step=2):
    def build(bm):
        km.box(bm, 0.0, d / 2, h / 2, w, d, h)

    def rule(poly):
        if poly.normal.z > 0.5:
            return material, min(4, step + 1)
        if poly.normal.z < -0.5:
            return material, 0
        # 正面と背面は少し明るく、妻（横）は一段暗く。平面陰影の明暗を面の向きで補う。
        return material, step if abs(poly.normal.y) > 0.5 else max(0, step - 1)

    return part(root, "Body", build, rule)


def windows(root, name, centers, size, frame=("steel", 3), glass=("mirror", 1), y=0.0, sill=True):
    """正面（y=0 の面）に窓を並べる。centers は (x, z) の列、size は (幅, 高さ)。"""
    w, h = size

    def frames(bm):
        for x, z in centers:
            km.box(bm, x, y - 0.03, z, w + 0.1, 0.06, h + 0.1)
            if sill:
                km.box(bm, x, y - 0.07, z - h / 2 - 0.06, w + 0.2, 0.14, 0.05)

    def panes(bm):
        for x, z in centers:
            km.box(bm, x, y - 0.065, z, w - 0.04, 0.01, h - 0.04)

    part(root, name + "Frame", frames, const(*frame))
    part(root, name + "Glass", panes, const(*glass))


def gable_roof(root, w, d, h, rise, eave, material, wall_material, wall_step):
    """平入りの切妻。棟は X 方向（通りに平行）。"""
    x0, x1 = -w / 2 - eave, w / 2 + eave
    y0, y1, ym = -eave, d + eave, d / 2
    z0, zr = h - eave * rise / (d / 2), h + rise

    def roof(bm):
        quad(bm, (x0, y0, z0), (x1, y0, z0), (x1, ym, zr), (x0, ym, zr), both=True)
        quad(bm, (x1, y1, z0), (x0, y1, z0), (x0, ym, zr), (x1, ym, zr), both=True)
        km.box(bm, 0.0, ym, zr + 0.04, w + eave * 2, 0.22, 0.12)   # 棟

    def roof_rule(poly):
        if poly.normal.z < 0:
            return material, 0
        return material, 2 if poly.normal.y < 0 else 1

    part(root, "Roof", roof, roof_rule)

    def gables(bm):
        tri(bm, (-w / 2, 0, h), (-w / 2, ym, h + rise), (-w / 2, d, h))
        tri(bm, (w / 2, d, h), (w / 2, ym, h + rise), (w / 2, 0, h))

    part(root, "Gables", gables, const(wall_material, max(0, wall_step - 1)))


def flat_roof(root, w, d, h, material, parapet=0.5):
    def build(bm):
        km.box(bm, 0.0, d / 2, h + 0.05, w + 0.1, d + 0.1, 0.1)
        if parapet > 0:
            km.box(bm, 0.0, -0.0, h + parapet / 2, w + 0.1, 0.2, parapet)
            km.box(bm, 0.0, d, h + parapet / 2, w + 0.1, 0.2, parapet)
            km.box(bm, -w / 2, d / 2, h + parapet / 2, 0.2, d, parapet)
            km.box(bm, w / 2, d / 2, h + parapet / 2, 0.2, d, parapet)

    part(root, "RoofTop", build, shade(material, 3, 2, 0))


# ── 住宅 ───────────────────────────────────────────────

def house(name, x, w, roof_material, wall_material, wall_step, balcony):
    root = km.new_root(name, x)
    d, h = 6.2, 5.8
    body(root, w, d, h, wall_material, wall_step)
    gable_roof(root, w, d, h, 1.6, 0.55, roof_material, wall_material, wall_step)

    # 腰の基礎（地面との境）。
    part(root, "Footing", lambda bm: km.box(bm, 0.0, d / 2, 0.15, w + 0.06, d + 0.06, 0.3), const("concrete", 1))

    # 一階：玄関（引き戸）と窓。二階：窓二つ。
    door_x = -w / 2 + 1.1
    part(root, "Door", lambda bm: km.box(bm, door_x, -0.04, 1.0, 1.4, 0.08, 2.0), const("grime", 3))
    part(root, "DoorGlass", lambda bm: (km.box(bm, door_x - 0.35, -0.085, 1.15, 0.5, 0.01, 1.4),
                                         km.box(bm, door_x + 0.35, -0.085, 1.15, 0.5, 0.01, 1.4)), const("mirror", 2))
    part(root, "Hood", lambda bm: km.box(bm, door_x, -0.45, 2.3, 1.9, 0.9, 0.08), shade("steel", 3, 2, 1))
    windows(root, "Win1", [(1.2, 1.35)], (1.7, 1.1))
    windows(root, "Win2", [(-w / 4, 4.1), (w / 4, 4.1)], (1.3, 1.0))

    # 雨戸の戸袋（窓の横の箱）。
    part(root, "Shutterbox", lambda bm: km.box(bm, w / 4 + 1.0, -0.08, 4.1, 0.35, 0.16, 1.2), const(wall_material, max(0, wall_step - 1)))

    if balcony:
        def balc(bm):
            km.box(bm, 0.0, -0.45, 2.9, w - 1.0, 0.9, 0.12)
            km.box(bm, 0.0, -0.88, 3.4, w - 1.0, 0.04, 0.9)
            km.box(bm, -(w - 1.0) / 2, -0.45, 3.4, 0.04, 0.9, 0.9)
            km.box(bm, (w - 1.0) / 2, -0.45, 3.4, 0.04, 0.9, 0.9)

        part(root, "Balcony", balc, shade("steel", 3, 2, 1))
    return root


def apartment(x):
    root = km.new_root("Apartment", x)
    w, d, h = 9.0, 6.0, 5.6
    corridor = 1.1
    # 本体は外廊下のぶん奥へ下げる。正面の線＝廊下の手すり。
    def build(bm):
        km.box(bm, 0.0, corridor + (d - corridor) / 2, h / 2, w, d - corridor, h)

    def rule(poly):
        if poly.normal.z > 0.5:
            return "siding", 4
        return "siding", 3 if abs(poly.normal.y) > 0.5 else 2

    part(root, "Body", build, rule)
    flat_roof(root, w, d, h, "kawara", parapet=0.0)

    def corridor_slab(bm):
        km.box(bm, 0.0, corridor / 2, 2.8, w, corridor, 0.15)
        km.box(bm, 0.0, 0.02, 3.3, w, 0.05, 1.0)       # 手すりの腰板
        for px in (-w / 2 + 0.1, 0.0, w / 2 - 0.1):
            km.box(bm, px, 0.08, 1.4, 0.12, 0.12, 2.8)  # 柱

    part(root, "Corridor", corridor_slab, shade("steel", 3, 2, 1))

    # 外階段（右端）。
    def stairs(bm):
        for i in range(10):
            z = 0.28 * i + 0.14
            km.box(bm, w / 2 + 0.55, corridor + 0.4 + (d - corridor - 0.8) * (1 - i / 10), z, 1.0, 0.35, 0.06)
        km.box(bm, w / 2 + 1.05, corridor + (d - corridor) / 2, 1.5, 0.04, d - corridor, 0.04)

    part(root, "Stairs", stairs, shade("rust", 3, 2, 1))

    def doors(bm):
        for floor in (0, 1):
            for i in range(3):
                dx = -w / 2 + 1.0 + i * 3.0
                z = 1.0 + floor * 2.8
                km.box(bm, dx, corridor - 0.03, z, 0.8, 0.06, 1.9)

    part(root, "Doors", doors, const("indigo", 2))
    centers = [(-w / 2 + 2.3 + i * 3.0, 1.5 + f * 2.8) for f in (0, 1) for i in range(3)]
    windows(root, "Win", centers, (0.9, 0.7), y=corridor)

    def meters(bm):
        for floor in (0, 1):
            for i in range(3):
                km.box(bm, -w / 2 + 1.7 + i * 3.0, corridor - 0.06, 1.6 + floor * 2.8, 0.25, 0.1, 0.35)

    part(root, "Meters", meters, const("paper", 3))
    return root


# ── 店舗 ───────────────────────────────────────────────

def shop(name, x, w, floors, front, awning, sign, wall_material, wall_step):
    """front：'glass'（ガラスの店先）・'open'（開いたシャッター）・'closed'（閉じたシャッター）。"""
    root = km.new_root(name, x)
    d = 9.0
    h = 3.3 + 2.9 * (floors - 1)
    body(root, w, d, h, wall_material, wall_step)
    flat_roof(root, w, d, h, "concrete", parapet=0.6)

    # 一階の店先。
    if front == "glass":
        part(root, "ShopFrame", lambda bm: km.box(bm, 0.0, -0.04, 1.35, w - 0.6, 0.08, 2.7), const("steel", 3))
        part(root, "ShopGlass", lambda bm: km.box(bm, 0.0, -0.085, 1.35, w - 0.9, 0.01, 2.5), const("mirror", 2))
        part(root, "ShopStall", lambda bm: km.box(bm, 0.0, -0.5, 0.45, w - 1.6, 0.8, 0.9), shade("grime", 4, 3, 1))
    else:
        def housing(bm):
            km.box(bm, 0.0, -0.18, 2.85, w - 0.4, 0.36, 0.4)
            km.box(bm, -w / 2 + 0.25, -0.05, 1.35, 0.1, 0.12, 2.7)
            km.box(bm, w / 2 - 0.25, -0.05, 1.35, 0.1, 0.12, 2.7)

        part(root, "ShutterHousing", housing, shade("steel", 3, 1, 0))
        if front == "closed":
            def slats(bm):
                for i in range(13):
                    km.box(bm, 0.0, -0.05 - 0.01 * (i % 2), 0.1 + i * 0.2, w - 0.6, 0.04, 0.2)

            part(root, "ShutterSlats", slats, lambda p: ("steel", 3 if int((p.center.z - 0.0) / 0.2) % 2 == 0 else 2))
        else:
            # 開いた店の奥（暗い）と、店先に並べた台。
            part(root, "ShopDark", lambda bm: km.box(bm, 0.0, -0.01, 1.3, w - 0.6, 0.02, 2.6), const("black", 1))
            part(root, "ShopStall", lambda bm: km.box(bm, 0.0, -0.6, 0.4, w - 1.2, 1.0, 0.8), shade("terracotta", 4, 3, 1))

    # テントの庇（傾いた板）と看板の帯。
    def tent(bm):
        quad(bm, (-w / 2 + 0.2, 0.0, 3.15), (w / 2 - 0.2, 0.0, 3.15), (w / 2 - 0.2, -1.2, 2.75), (-w / 2 + 0.2, -1.2, 2.75), both=True)
        km.box(bm, 0.0, -1.2, 2.6, w - 0.4, 0.03, 0.3)

    part(root, "Awning", tent, lambda p: (awning, 1 if p.normal.z < 0 else 3))
    part(root, "SignBand", lambda bm: km.box(bm, 0.0, -0.06, 3.6, w - 0.3, 0.12, 0.55), shade(sign, 3, 3, 1))
    part(root, "SignText", lambda bm: [km.box(bm, -w / 2 + 0.9 + i * 0.8, -0.125, 3.6, 0.5, 0.01, 0.34) for i in range(int((w - 1.2) / 0.8))], const("paper", 4))

    # 上の階：窓。
    if floors >= 2:
        cs = [(-w / 4, 3.3 + 2.9 * f + 1.4) for f in range(floors - 1)] + [(w / 4, 3.3 + 2.9 * f + 1.4) for f in range(floors - 1)]
        windows(root, "Win", cs, (min(1.6, w / 2 - 0.8), 1.1))
    return root


def zakkyo(name, x, w, floors, sign_colors):
    root = km.new_root(name, x)
    d = 9.0
    h = 3.4 + 3.0 * (floors - 1)
    body(root, w, d, h, "concrete", 2)
    flat_roof(root, w, d, h, "concrete", parapet=0.8)

    # 一階：入口と小さな店。
    part(root, "Entrance", lambda bm: km.box(bm, -w / 2 + 1.0, -0.03, 1.15, 1.4, 0.06, 2.3), const("black", 2))
    part(root, "ShopGlass", lambda bm: km.box(bm, 0.9, -0.06, 1.4, w - 3.0, 0.02, 2.2), const("mirror", 2))
    part(root, "ShopFrame", lambda bm: km.box(bm, 0.9, -0.03, 1.4, w - 2.8, 0.04, 2.4), const("steel", 3))

    # 上の階：横長の窓と、階ごとのテナントの看板。
    cs = [(0.4, 3.4 + 3.0 * f + 1.5) for f in range(floors - 1)]
    windows(root, "Win", cs, (w - 2.6, 1.2), sill=False)

    for f in range(floors - 1):
        color = sign_colors[f % len(sign_colors)]
        part(root, "Plate%d" % f, (lambda ff: lambda bm: km.box(bm, -w / 2 + 0.45, -0.06, 3.4 + 3.0 * ff + 1.5, 0.6, 0.12, 1.4))(f), shade(color, 4, 3, 1))

    # 縦の袖看板。
    def blade(bm):
        km.box(bm, w / 2 - 0.3, -0.55, 3.4 + (h - 3.4) / 2, 0.22, 0.8, h - 4.2)

    part(root, "Blade", blade, shade(sign_colors[0], 4, 3, 1))
    part(root, "BladeFace", lambda bm: [km.box(bm, w / 2 - 0.3 + s, -0.55, 3.4 + (h - 3.4) / 2, 0.01, 0.62, h - 4.6) for s in (-0.115, 0.115)], const("paper", 4))

    # 屋上の水槽と室外機。
    def roof_things(bm):
        verts = km.frustum(bm, 8, 0.7, 0.7, h + 0.1, h + 1.5)
        top = verts[1]
        km.cap(bm, top)
        bmesh.ops.translate(bm, verts=verts[0] + verts[1], vec=Vector((w / 4, d * 0.65, 0)))
        km.box(bm, -w / 4, d * 0.6, h + 0.45, 0.8, 0.3, 0.6)

    part(root, "RoofThings", roof_things, shade("steel", 4, 2, 1))
    return root


def warehouse(x):
    root = km.new_root("Warehouse", x)
    w, d, h = 9.5, 8.0, 6.5
    body(root, w, d, h, "rust", 2)
    gable_roof(root, w, d, h, 0.8, 0.3, "steel", "rust", 2)

    def ribs(bm):
        for i in range(int(w / 0.5) + 1):
            km.box(bm, -w / 2 + i * 0.5, -0.03, h / 2, 0.06, 0.06, h)

    part(root, "Ribs", ribs, const("rust", 1))

    def big_shutter(bm):
        for i in range(20):
            km.box(bm, -0.8, -0.08 - 0.01 * (i % 2), 0.1 + i * 0.2, 4.4, 0.04, 0.2)
        km.box(bm, -0.8, -0.2, 4.2, 4.8, 0.4, 0.4)

    part(root, "BigShutter", big_shutter, lambda p: ("steel", 3 if int(p.center.z / 0.2) % 2 == 0 else 2))
    part(root, "SideDoor", lambda bm: km.box(bm, w / 2 - 1.3, -0.07, 1.0, 0.9, 0.06, 2.0), const("steel", 1))
    part(root, "Lamp", lambda bm: km.box(bm, -0.8, -0.3, 4.75, 0.4, 0.3, 0.2), const("caution", 4))
    return root


# ── 建物の裏 ───────────────────────────────────────────

def back_facade(name, x, h, material, step, variant):
    """長さ 6m・奥行き 0.3m の一枚。正面（-Y）が通り。後ろは街区の塊に付く。"""
    root = km.new_root(name, x)
    w, d = 6.0, 0.3

    def build(bm):
        km.box(bm, 0.0, d / 2, h / 2, w, d, h)

    def rule(poly):
        if poly.normal.z > 0.5:
            return material, min(4, step + 1)
        return material, step

    part(root, "Body", build, rule)
    part(root, "Coping", lambda bm: km.box(bm, 0.0, d / 2 - 0.05, h + 0.06, w + 0.04, d + 0.12, 0.12), const("concrete", 3))
    # 階の境の帯（高さの目安）。
    part(root, "Band", lambda bm: [km.box(bm, 0.0, -0.02, z, w, 0.06, 0.12) for z in (3.1, 6.1) if z < h - 0.5], const(material, max(0, step - 1)))

    door_x = -1.6 if variant != 1 else 1.4
    part(root, "BackDoor", lambda bm: km.box(bm, door_x, -0.04, 1.0, 0.9, 0.08, 2.0), const("steel", 2 if variant == 1 else 1))
    part(root, "DoorHood", lambda bm: km.box(bm, door_x, -0.3, 2.2, 1.2, 0.6, 0.06), shade("steel", 3, 2, 1))
    part(root, "DoorLamp", lambda bm: km.box(bm, door_x + 0.75, -0.08, 2.0, 0.15, 0.12, 0.2), const("caution", 4))

    win = [(1.2, 1.6)] if variant != 1 else [(-1.5, 1.6)]
    win += [(-1.4, 4.4), (1.4, 4.4)] if h > 5.5 else []
    win += [(-1.4, 7.4), (1.4, 7.4)] if h > 8.5 else []
    windows(root, "Win", win, (0.8, 0.6), glass=("mirror", 0))

    def pipes(bm):
        px = w / 2 - 0.35 if variant != 2 else -w / 2 + 0.35
        km.box(bm, px, -0.1, h / 2, 0.1, 0.1, h)              # 樋
        km.box(bm, 0.2, -0.06, 2.6, 0.06, 0.06, 0.0 + 1.2)   # 配管
        km.box(bm, 0.2 + 1.1, -0.06, 3.2, 2.2, 0.06, 0.06)

    part(root, "Pipes", pipes, const("steel", 2))
    part(root, "Meter", lambda bm: km.box(bm, -0.2, -0.08, 1.6, 0.35, 0.14, 0.45), const("paper", 3))
    part(root, "Vent", lambda bm: km.box(bm, 2.3, -0.06, 2.4, 0.35, 0.1, 0.35), const("black", 3))
    return root


# ── 組み立てと書き出し ─────────────────────────────────

km.clear_scene()
km.build_palette_image(OUT + "/KM_Palette.png")

roots = [
    house("House_A", 0.0, 7.0, "kawara", "siding", 3, True),
    house("House_B", 10.0, 6.0, "rust", "concrete", 3, False),
    house("House_C", 19.0, 7.6, "kawara", "paper", 2, True),
    apartment(30.0),
    shop("Shop_A", 44.0, 6.0, 2, "glass", "sign_red", "caution", "siding", 3),
    shop("Shop_B", 53.0, 7.0, 3, "open", "indigo", "sign_red", "concrete", 3),
    shop("Shop_C", 62.0, 6.5, 2, "closed", "park_green", "paper", "siding", 2),
    shop("Shop_D", 71.0, 5.5, 3, "glass", "store_orange", "indigo", "paper", 2),
    zakkyo("Zakkyo_A", 82.0, 7.0, 4, ["sign_red", "caution", "indigo"]),
    zakkyo("Zakkyo_B", 92.0, 8.0, 3, ["indigo", "store_orange"]),
    warehouse(104.0),
    back_facade("BackFacade_A", 116.0, 7.5, "concrete", 2, 0),
    back_facade("BackFacade_B", 124.0, 6.2, "rust", 2, 1),
    back_facade("BackFacade_C", 132.0, 9.8, "siding", 2, 2),
]

# 見比べるために横へ並べてあるので、書き出す間だけ原点へ戻す。
for root in roots:
    x = root.location.x
    root.location.x = 0.0
    try:
        km.export_fbx([root], OUT + "/Buildings/" + root.name + ".fbx")
    finally:
        root.location.x = x

print("exported:", [r.name for r in roots])
