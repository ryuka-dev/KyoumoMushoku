"""日本の街の建物（一人称の切片・2026-10-27）。日本の街並みの規則は .claude/skills/japanese-streetscape（ローカル）。

- 住宅（House_A / _B / _C / _D）：木造二階建て・平入りの切妻（瓦かトタン）。霧除け庇・一階の面格子・玄関の小庇・樋・壁の室外機・電気メーター。
  D は焦げ茶の下見板張り。
- アパート（Apartment）：二階建ての木造アパート。外廊下と外階段、各戸の扉と小窓とメーター。
- 店舗（Shop_A〜_D）：一階は上の階より奥へ引っ込んだ暗い店先、その上に横いっぱいの下屋庇（瓦かトタンの小屋根）。
  A＝ガラスの店先と赤提灯、B＝開いた店先と帳（テント）と袖看板、C＝閉じたシャッター、D＝町家風の格子戸と暖簾と提灯。
- 看板建築（Kanban_A）：屋根より高い平らな正面の壁。大きな看板の帯・飾りの軒・二階の窓一つ・黒い腰壁。
- 雑居ビル（Zakkyo_A / _B）：階ごとのテナントの看板（光る）、全高の袖看板（光る）、樋、室外機、屋上の水槽。
- 倉庫（Warehouse）・建物の裏（BackFacade_A / _B / _C）。

原点は **正面の壁の線の中心（底面）**。本体は +Y（奥）へ、軒・庇・ベランダ・看板は -Y（通り）へ出る。
部品の名前の約束：本体の箱は "Body…"（Unity 側の当たり判定）。夜に灯る窓は "LitGlass…"、灯らない窓は "Glass…"、
提灯や光る看板の面は "Glow…"（Unity 側で発光の材質に差し替え、夜の濃さで明るさを上げる）。
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
    if not bm.verts:
        bm.free()
        return None
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
    bm.faces.new([bm.verts.new(p) for p in (a, b, c)])


def wall_rule(material, step):
    def rule(poly):
        if poly.normal.z > 0.5:
            return material, min(4, step + 1)
        if poly.normal.z < -0.5:
            return material, 0
        # 正面と背面は少し明るく、妻（横）は一段暗く。平面陰影の明暗を面の向きで補う。
        return material, step if abs(poly.normal.y) > 0.5 else max(0, step - 1)
    return rule


def body(root, name, x0, x1, y0, y1, z0, z1, material, step):
    return part(root, name, lambda bm: km.box(bm, (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2, x1 - x0, y1 - y0, z1 - z0),
                wall_rule(material, step))


class Facade:
    """一棟の正面の細部をまとめて組む（部品の数を抑えるため、種類ごとに一つの物体にする）。"""

    def __init__(self, root, seed):
        self.root = root
        self.seed = seed
        self.count = 0
        self.frames, self.dark, self.lit, self.hoods, self.grilles = [], [], [], [], []
        self.pipes, self.acs, self.meters, self.glow = [], [], [], []

    def window(self, x, z, w, h, y=0.0, hood=True, grille=False, lit=None):
        """引き違いの窓。太く暗い枠・真ん中の召し合わせ・上に霧除け庇。lit=None なら三〜五割を灯す。"""
        self.count += 1
        if lit is None:
            lit = (self.seed * 7 + self.count * 3) % 5 in (0, 2)
        self.frames.append((x, y - 0.04, z, w + 0.16, 0.08, h + 0.16))
        self.frames.append((x, y - 0.06, z, 0.06, 0.04, h))
        self.frames.append((x, y - 0.09, z - h / 2 - 0.08, w + 0.24, 0.16, 0.05))
        (self.lit if lit else self.dark).append((x, y - 0.075, z, w - 0.02, 0.01, h - 0.02))
        if hood:
            self.hoods.append((x, y - 0.22, z + h / 2 + 0.14, w + 0.4, 0.44, 0.05))
        if grille:
            for i in range(7):
                self.grilles.append((x - w / 2 + w * (i + 0.5) / 7, y - 0.14, z, 0.025, 0.025, h + 0.05))
            self.grilles.append((x, y - 0.14, z + h / 2, w, 0.03, 0.03))
            self.grilles.append((x, y - 0.14, z - h / 2, w, 0.03, 0.03))

    def pipe(self, x, top, y=0.0):
        self.pipes.append((x, y - 0.1, top / 2, 0.11, 0.11, top))
        self.pipes.append((x, y - 0.25, top - 0.05, 0.11, 0.3, 0.1))

    def ac(self, x, z, y=0.0):
        self.acs.append((x, y - 0.2, z, 0.75, 0.28, 0.55))
        self.dark.append((x - 0.1, y - 0.345, z, 0.36, 0.01, 0.36))
        self.pipes.append((x + 0.3, y - 0.05, z + 0.5, 0.05, 0.05, 0.5))

    def meter(self, x, z, y=0.0):
        self.meters.append((x, y - 0.07, z, 0.28, 0.14, 0.38))

    def lantern(self, x, z, y=0.0):
        self.glow.append(("lantern", x, y, z))

    def build(self, frame=("wood_dark", 1)):
        def boxes(items):
            return lambda bm: [km.box(bm, *b) for b in items]

        part(self.root, "WinFrame", boxes(self.frames), const(*frame))
        part(self.root, "Glass", boxes(self.dark), const("mirror", 1))
        part(self.root, "LitGlass", boxes(self.lit), const("mirror", 1))
        part(self.root, "Hoods", boxes(self.hoods), shade("steel", 2, 1, 0))
        part(self.root, "Grilles", boxes(self.grilles), const("steel", 3))
        part(self.root, "Pipes", boxes(self.pipes), const("steel", 2))
        part(self.root, "AirCon", boxes(self.acs), shade("paper", 4, 2, 0))
        part(self.root, "Meters", boxes(self.meters), const("paper", 3))

        lanterns = [g for g in self.glow if g[0] == "lantern"]
        if lanterns:
            def bodies(bm):
                for _, x, y, z in lanterns:
                    bottom, top = km.frustum(bm, 8, 0.15, 0.15, z - 0.25, z + 0.25)
                    km.cap(bm, bottom, flip=True)
                    km.cap(bm, top)
                    bmesh.ops.translate(bm, verts=bottom + top, vec=Vector((x, y - 0.35, 0)))

            def caps(bm):
                for _, x, y, z in lanterns:
                    km.box(bm, x, y - 0.35, z + 0.28, 0.22, 0.22, 0.06)
                    km.box(bm, x, y - 0.35, z - 0.28, 0.22, 0.22, 0.06)
                    km.box(bm, x, y - 0.18, z + 0.33, 0.03, 0.34, 0.03)

            part(self.root, "GlowLantern", bodies, const("sign_red", 4))
            part(self.root, "LanternCaps", caps, const("black", 2))


def gable_roof(root, w, d, h, rise, eave, material, wall_material, wall_step):
    """平入りの切妻。棟は X 方向（通りに平行）。"""
    x0, x1 = -w / 2 - eave, w / 2 + eave
    y0, y1, ym = -eave, d + eave, d / 2
    z0, zr = h - eave * rise / (d / 2), h + rise

    def roof(bm):
        quad(bm, (x0, y0, z0), (x1, y0, z0), (x1, ym, zr), (x0, ym, zr), both=True)
        quad(bm, (x1, y1, z0), (x0, y1, z0), (x0, ym, zr), (x1, ym, zr), both=True)
        km.box(bm, 0.0, ym, zr + 0.04, w + eave * 2, 0.22, 0.12)       # 棟
        km.box(bm, 0.0, y0 + 0.03, z0 - 0.06, w + eave * 2, 0.06, 0.16)  # 鼻隠し

    def roof_rule(poly):
        if poly.normal.z < -0.2:
            return material, 0
        return material, 2 if poly.normal.y < 0 else 1

    part(root, "Roof", roof, roof_rule)

    def gables(bm):
        tri(bm, (-w / 2, 0, h), (-w / 2, ym, h + rise), (-w / 2, d, h))
        tri(bm, (w / 2, d, h), (w / 2, ym, h + rise), (w / 2, 0, h))

    part(root, "Gables", gables, const(wall_material, max(0, wall_step - 1)))


def geya(root, w, z_top, depth, material, x_center=0.0):
    """下屋庇：一階の上を横いっぱいに覆う、浅い勾配の小屋根。町家と商店の最も強い目印。"""
    def build(bm):
        y_out = -depth
        z_out = z_top - depth * 0.35
        x0, x1 = x_center - w / 2, x_center + w / 2
        quad(bm, (x0, 0.0, z_top), (x1, 0.0, z_top), (x1, y_out, z_out), (x0, y_out, z_out), both=True)
        km.box(bm, x_center, y_out + 0.03, z_out - 0.07, w, 0.06, 0.16)   # 鼻隠し
        for i in range(int(w / 0.9) + 1):                                 # 垂木の先
            km.box(bm, x0 + 0.1 + i * 0.9, y_out * 0.5, z_top - depth * 0.2, 0.06, abs(y_out), 0.06)

    def rule(poly):
        if poly.normal.z < -0.2:
            return material, 0
        return material, 1

    part(root, "Geya", build, rule)


def flat_roof(root, w, d, h, material, parapet=0.5, things=False, seed=0):
    def build(bm):
        km.box(bm, 0.0, d / 2, h + 0.05, w + 0.1, d + 0.1, 0.1)
        if parapet > 0:
            km.box(bm, 0.0, 0.05, h + parapet / 2, w + 0.1, 0.2, parapet)
            km.box(bm, 0.0, d, h + parapet / 2, w + 0.1, 0.2, parapet)
            km.box(bm, -w / 2, d / 2, h + parapet / 2, 0.2, d, parapet)
            km.box(bm, w / 2, d / 2, h + parapet / 2, 0.2, d, parapet)

    part(root, "RoofTop", build, shade(material, 3, 2, 0))
    if things:
        def stuff(bm):
            verts = km.frustum(bm, 8, 0.6, 0.6, h + 0.1, h + 1.4)
            km.cap(bm, verts[1])
            bmesh.ops.translate(bm, verts=verts[0] + verts[1], vec=Vector((w / 4 if seed % 2 else -w / 4, d * 0.65, 0)))
            km.box(bm, -w / 4 if seed % 2 else w / 4, d * 0.55, h + 0.4, 0.8, 0.3, 0.6)
            km.box(bm, 0.0, d * 0.8, h + 1.5, 0.04, 0.04, 2.8)             # アンテナ
            km.box(bm, 0.0, d * 0.8, h + 2.6, 1.0, 0.04, 0.04)

        part(root, "RoofThings", stuff, shade("steel", 4, 2, 1))


def sign_text(cx, cz, w, h, y):
    """看板の「字」：白地の上に濃い小さな四角を並べて暗示する（本物の文字は書かない）。"""
    blocks = []
    n = max(2, int(w / 0.55))
    for i in range(n):
        blocks.append((cx - w / 2 + w * (i + 0.5) / n, y, cz, w / n * 0.62, 0.01, h * 0.62))
    return blocks


# ── 住宅 ───────────────────────────────────────────────

def house(name, x, w, roof_material, wall_material, wall_step, balcony, boards=False, seed=0):
    root = km.new_root(name, x)
    d, h = 6.2, 5.8
    body(root, "Body", -w / 2, w / 2, 0.0, d, 0.0, h, wall_material, wall_step)
    gable_roof(root, w, d, h, 1.6, 0.55, roof_material, wall_material, wall_step)
    part(root, "Footing", lambda bm: km.box(bm, 0.0, d / 2, 0.15, w + 0.06, d + 0.06, 0.3), const("concrete", 1))
    # 一階と二階の境の帯（胴差し）。
    part(root, "Belt", lambda bm: km.box(bm, 0.0, d / 2, 2.95, w + 0.04, d + 0.04, 0.12), const("wood_dark", 2))

    if boards:
        def planks(bm):
            for i in range(22):
                z = 0.4 + i * 0.25
                if 2.85 < z < 3.1:
                    continue
                km.box(bm, 0.0, -0.012, z, w, 0.024, 0.12)
                km.box(bm, -w / 2 - 0.012, d / 2, z, 0.024, d, 0.12)
                km.box(bm, w / 2 + 0.012, d / 2, z, 0.024, d, 0.12)

        part(root, "Planks", planks, const(wall_material, max(0, wall_step - 1)))

    f = Facade(root, seed)
    door_x = -w / 2 + 1.1
    # 玄関：格子の入った引き戸と小庇、横に電気メーター。
    part(root, "Door", lambda bm: km.box(bm, door_x, -0.04, 1.0, 1.5, 0.08, 2.0), const("wood_dark", 2))

    def door_grid(bm):
        for i in range(6):
            km.box(bm, door_x - 0.62 + i * 0.25, -0.09, 1.05, 0.04, 0.02, 1.8)
        km.box(bm, door_x, -0.09, 1.3, 1.4, 0.02, 0.04)

    part(root, "DoorGrid", door_grid, const("wood_dark", 1))
    f.dark.append((door_x, -0.085, 1.15, 1.3, 0.01, 1.6))
    part(root, "Hood", lambda bm: km.box(bm, door_x, -0.45, 2.3, 1.9, 0.9, 0.08), shade("steel", 2, 1, 0))
    f.meter(door_x + 1.0, 1.6)

    # 一階の窓は面格子付き。二階は二つ、霧除け庇。
    f.window(1.25, 1.4, 1.6, 1.0, grille=True)
    f.window(-w / 4, 4.15, 1.3, 1.0)
    f.window(w / 4, 4.15, 1.3, 1.0)
    part(root, "Shutterbox", lambda bm: km.box(bm, w / 4 + 0.95, -0.08, 4.15, 0.32, 0.16, 1.15), const(wall_material, max(0, wall_step - 1)))
    f.pipe(w / 2 - 0.15, h - 0.2)
    f.ac(w / 2 - 0.9, 0.35, y=0.0)

    if balcony:
        def balc(bm):
            km.box(bm, 0.0, -0.45, 2.9, w - 1.0, 0.9, 0.12)
            km.box(bm, 0.0, -0.88, 3.85, w - 1.0, 0.05, 0.05)
            for i in range(int((w - 1.0) / 0.15) + 1):
                km.box(bm, -(w - 1.0) / 2 + i * 0.15, -0.88, 3.4, 0.025, 0.025, 0.9)

        part(root, "Balcony", balc, shade("steel", 3, 2, 1))
        part(root, "Laundry", lambda bm: (km.box(bm, 0.0, -0.6, 4.6, w - 1.2, 0.03, 0.03),
                                          km.box(bm, -0.8, -0.6, 4.25, 0.5, 0.02, 0.65),
                                          km.box(bm, 0.2, -0.6, 4.3, 0.4, 0.02, 0.55)), const("paper", 3))
    f.build()
    return root


def apartment(x, seed=3):
    root = km.new_root("Apartment", x)
    w, d, h = 9.0, 6.0, 5.6
    corridor = 1.1
    body(root, "Body", -w / 2, w / 2, corridor, d, 0.0, h, "siding", 3)
    flat_roof(root, w, d, h, "kawara", parapet=0.0)

    def corridor_slab(bm):
        km.box(bm, 0.0, corridor / 2, 2.8, w, corridor, 0.15)
        km.box(bm, 0.0, 0.02, 3.3, w, 0.05, 1.0)
        for px in (-w / 2 + 0.1, 0.0, w / 2 - 0.1):
            km.box(bm, px, 0.08, 1.4, 0.12, 0.12, 2.8)

    part(root, "Corridor", corridor_slab, shade("steel", 3, 2, 1))
    part(root, "Fascia", lambda bm: km.box(bm, 0.0, 0.0, h + 0.1, w + 0.2, 0.12, 0.25), const("kawara", 1))

    def stairs(bm):
        for i in range(10):
            km.box(bm, w / 2 + 0.55, corridor + 0.4 + (d - corridor - 0.8) * (1 - i / 10), 0.28 * i + 0.14, 1.0, 0.35, 0.06)
        km.box(bm, w / 2 + 1.05, corridor + (d - corridor) / 2, 1.5, 0.04, d - corridor, 0.04)

    part(root, "Stairs", stairs, shade("rust", 3, 2, 1))

    def doors(bm):
        for floor in (0, 1):
            for i in range(3):
                km.box(bm, -w / 2 + 1.0 + i * 3.0, corridor - 0.03, 1.0 + floor * 2.8, 0.8, 0.06, 1.9)

    part(root, "Doors", doors, const("indigo", 2))
    f = Facade(root, seed)
    for floor in (0, 1):
        for i in range(3):
            f.window(-w / 2 + 2.3 + i * 3.0, 1.5 + floor * 2.8, 0.9, 0.7, y=corridor, hood=False, grille=True)
            f.meter(-w / 2 + 1.7 + i * 3.0, 1.75 + floor * 2.8, y=corridor)
    f.pipe(-w / 2 + 0.1, h)
    f.build()
    return root


# ── 店舗 ───────────────────────────────────────────────

SHOP_D = 9.0
FLOOR1 = 3.0     # 一階の高さ（下屋庇の付け根）
RECESS = 0.45    # 一階の店先が上の階より奥へ引っ込む量


def shop(name, x, w, floors, front, roof_kind, sign, wall_material, wall_step, lanterns=False, tent=None, blade=None, seed=0):
    """front：'glass'・'open'・'closed'・'lattice'。roof_kind：下屋庇の材質（'kawara' か 'rust'）。"""
    root = km.new_root(name, x)
    d = SHOP_D
    h = FLOOR1 + 0.3 + 2.9 * (floors - 1)
    # 一階は奥へ下げ、上の階が通りへ張り出す。
    body(root, "BodyLow", -w / 2, w / 2, RECESS, d, 0.0, FLOOR1 + 0.3, wall_material, max(0, wall_step - 1))
    body(root, "BodyUp", -w / 2, w / 2, 0.0, d, FLOOR1 + 0.3, h, wall_material, wall_step)
    flat_roof(root, w, d, h, "concrete", parapet=0.6, things=floors >= 3, seed=seed)
    part(root, "Pilasters", lambda bm: (km.box(bm, -w / 2 + 0.12, RECESS / 2, FLOOR1 / 2, 0.24, RECESS, FLOOR1),
                                        km.box(bm, w / 2 - 0.12, RECESS / 2, FLOOR1 / 2, 0.24, RECESS, FLOOR1)),
         const(wall_material, max(0, wall_step - 1)))

    f = Facade(root, seed)
    y1 = RECESS
    inner = w - 0.6
    if front in ("glass", "lattice"):
        def frame(bm):
            km.box(bm, 0.0, y1 - 0.04, FLOOR1 - 0.15, inner, 0.08, 0.12)
            km.box(bm, 0.0, y1 - 0.04, 0.08, inner, 0.08, 0.16)
            n = max(3, int(inner / 0.9))
            for i in range(n + 1):
                km.box(bm, -inner / 2 + inner * i / n, y1 - 0.05, FLOOR1 / 2, 0.07, 0.1, FLOOR1 - 0.2)
            km.box(bm, 0.0, y1 - 0.05, 2.1, inner, 0.08, 0.06)

        part(root, "ShopFrame", frame, const("wood_dark", 2 if front == "lattice" else 1))
        (f.lit if (seed % 2 == 0) else f.dark).append((0.0, y1 - 0.09, 1.4, inner - 0.1, 0.01, 2.5))
        if front == "lattice":
            def slats(bm):
                for i in range(int(inner / 0.11)):
                    km.box(bm, -inner / 2 + 0.06 + i * 0.11, y1 - 0.12, 1.05, 0.035, 0.035, 1.9)

            part(root, "Lattice", slats, const("wood_dark", 3))
        else:
            part(root, "ShopStall", lambda bm: km.box(bm, 0.0, y1 - 0.45, 0.45, inner - 1.2, 0.8, 0.9), shade("grime", 4, 3, 1))
    else:
        def housing(bm):
            km.box(bm, 0.0, y1 - 0.18, FLOOR1 - 0.25, inner + 0.2, 0.36, 0.4)
            km.box(bm, -inner / 2, y1 - 0.05, FLOOR1 / 2 - 0.2, 0.1, 0.12, FLOOR1 - 0.4)
            km.box(bm, inner / 2, y1 - 0.05, FLOOR1 / 2 - 0.2, 0.1, 0.12, FLOOR1 - 0.4)

        part(root, "ShutterHousing", housing, shade("steel", 3, 1, 0))
        if front == "closed":
            def slats(bm):
                for i in range(13):
                    km.box(bm, 0.0, y1 - 0.05 - 0.01 * (i % 2), 0.1 + i * 0.2, inner, 0.04, 0.2)

            part(root, "ShutterSlats", slats, lambda p: ("steel", 3 if int(p.center.z / 0.2) % 2 == 0 else 2))
        else:
            f.lit.append((0.0, y1 - 0.01, 1.3, inner, 0.02, 2.5))
            part(root, "ShopStall", lambda bm: km.box(bm, 0.0, y1 - 0.6, 0.4, inner - 0.6, 1.0, 0.8), shade("terracotta", 4, 3, 1))

    # 下屋庇（または帳）と、その上の看板の帯。
    if tent:
        def tent_build(bm):
            quad(bm, (-w / 2 + 0.2, 0.0, FLOOR1 + 0.15), (w / 2 - 0.2, 0.0, FLOOR1 + 0.15),
                 (w / 2 - 0.2, -1.2, FLOOR1 - 0.25), (-w / 2 + 0.2, -1.2, FLOOR1 - 0.25), both=True)
            km.box(bm, 0.0, -1.2, FLOOR1 - 0.4, w - 0.4, 0.03, 0.3)

        part(root, "Awning", tent_build, lambda p: (tent, 1 if p.normal.z < 0 else 3))
    else:
        geya(root, w + 0.1, FLOOR1 + 0.2, 0.95, roof_kind)
    part(root, "SignBand", lambda bm: km.box(bm, 0.0, -0.06, FLOOR1 + 0.75, w - 0.5, 0.12, 0.6), shade(sign, 3, 3, 1))
    part(root, "SignFace", lambda bm: km.box(bm, 0.0, -0.125, FLOOR1 + 0.75, w - 0.9, 0.01, 0.42), const("paper", 4))
    part(root, "SignText", lambda bm: [km.box(bm, *b) for b in sign_text(0.0, FLOOR1 + 0.75, w - 1.2, 0.42, -0.135)], const(sign, 1))

    if lanterns:
        f.lantern(-inner / 2 + 0.5, 2.25, y=y1)
        f.lantern(inner / 2 - 0.5, 2.25, y=y1)

    if blade:
        top = h - 0.4
        def blade_box(bm):
            km.box(bm, w / 2 - 0.35, -0.6, (FLOOR1 + 1.2 + top) / 2, 0.22, 0.75, top - FLOOR1 - 1.2)
            km.box(bm, w / 2 - 0.35, -0.12, top - 0.3, 0.04, 0.3, 0.04)

        part(root, "Blade", blade_box, shade(blade, 3, 3, 1))
        part(root, "GlowBlade", lambda bm: [km.box(bm, w / 2 - 0.35 + s, -0.6, (FLOOR1 + 1.2 + top) / 2, 0.01, 0.6, top - FLOOR1 - 1.5) for s in (-0.115, 0.115)], const("paper", 4))

    for fl in range(floors - 1):
        z = FLOOR1 + 0.3 + 2.9 * fl + 1.55
        f.window(-w / 4, z, min(1.5, w / 2 - 0.9), 1.0)
        f.window(w / 4, z, min(1.5, w / 2 - 0.9), 1.0)
        if w > 6.4:
            f.ac(w / 2 - 0.55 if fl % 2 == 0 else -w / 2 + 0.75, z - 1.0)
    f.pipe(-w / 2 + 0.12, h)
    f.meter(-inner / 2 + 0.2, 1.7, y=y1)
    f.build()
    return root


def kanban(name, x, w, seed=0):
    """看板建築：奥の小屋根を隠す、屋根より高い平らな正面の壁。"""
    root = km.new_root(name, x)
    d, h = 8.0, 6.0
    front_top = 7.6
    body(root, "BodyLow", -w / 2, w / 2, RECESS, d, 0.0, FLOOR1 + 0.3, "siding", 1)
    body(root, "BodyUp", -w / 2, w / 2, 0.3, d, FLOOR1 + 0.3, h, "siding", 2)
    gable_roof(root, w - 0.4, d - 0.3, h, 1.0, 0.1, "rust", "siding", 2)

    def false_front(bm):
        km.box(bm, 0.0, 0.15, (FLOOR1 + 0.3 + front_top) / 2, w, 0.3, front_top - FLOOR1 - 0.3)

    part(root, "BodyFront", false_front, wall_rule("paper", 3))

    def cornice(bm):
        km.box(bm, 0.0, -0.05, front_top - 0.15, w + 0.2, 0.3, 0.3)
        km.box(bm, 0.0, -0.02, front_top - 0.5, w + 0.05, 0.24, 0.12)
        for i in range(int(w / 0.6)):
            km.box(bm, -w / 2 + 0.3 + i * 0.6, -0.06, front_top - 0.38, 0.22, 0.1, 0.12)
        for sx in (-w / 2 + 0.25, w / 2 - 0.25):
            km.box(bm, sx, -0.04, (FLOOR1 + 0.3 + front_top) / 2, 0.3, 0.12, front_top - FLOOR1 - 0.3)

    part(root, "Cornice", cornice, shade("concrete", 4, 3, 1))
    part(root, "SignBand", lambda bm: km.box(bm, 0.0, -0.08, 5.95, w - 1.0, 0.1, 0.8), shade("sign_red", 3, 3, 1))
    part(root, "SignText", lambda bm: [km.box(bm, *b) for b in sign_text(0.0, 5.95, w - 1.6, 0.6, -0.14)], const("paper", 4))
    f = Facade(root, seed)
    f.window(0.0, 4.5, 2.2, 1.1)
    # 一階：黒い腰壁と格子の店先、板張りの庇。
    part(root, "Base", lambda bm: km.box(bm, 0.0, RECESS - 0.05, 0.45, w - 0.5, 0.1, 0.9), const("black", 2))
    part(root, "ShopFrame", lambda bm: [km.box(bm, -w / 2 + 0.3 + i * (w - 0.6) / 4, RECESS - 0.06, 1.9, 0.08, 0.1, 2.0) for i in range(5)], const("wood_dark", 1))
    f.lit.append((0.0, RECESS - 0.08, 1.95, w - 0.7, 0.01, 1.9))

    def canopy(bm):
        for i in range(5):
            km.box(bm, 0.0, -0.45, FLOOR1 + 0.05 + i * 0.09, w, 0.9 - i * 0.12, 0.07)

    part(root, "Canopy", canopy, shade("wood_dark", 2, 1, 0))
    f.lantern(-w / 2 + 0.7, 2.3, y=RECESS)
    f.pipe(w / 2 - 0.1, front_top - 0.3)
    f.meter(w / 2 - 0.6, 1.6, y=RECESS)
    f.build()
    return root


def zakkyo(name, x, w, floors, sign_colors, seed=0):
    root = km.new_root(name, x)
    d = 9.0
    h = 3.4 + 3.0 * (floors - 1)
    body(root, "Body", -w / 2, w / 2, 0.0, d, 0.0, h, "concrete", 2)
    flat_roof(root, w, d, h, "concrete", parapet=0.8, things=True, seed=seed)
    part(root, "Floors", lambda bm: [km.box(bm, 0.0, -0.03, 3.4 + 3.0 * f_, w + 0.04, 0.06, 0.15) for f_ in range(floors - 1)], const("concrete", 1))

    f = Facade(root, seed)
    part(root, "Entrance", lambda bm: km.box(bm, -w / 2 + 1.0, -0.03, 1.15, 1.4, 0.06, 2.3), const("black", 2))
    part(root, "ShopFrame", lambda bm: km.box(bm, 0.9, -0.03, 1.4, w - 2.8, 0.06, 2.4), const("steel", 3))
    f.lit.append((0.9, -0.07, 1.4, w - 3.0, 0.01, 2.2))
    for fl in range(floors - 1):
        z = 3.4 + 3.0 * fl + 1.5
        f.window(0.5, z, w - 2.8, 1.2, hood=False)
        f.ac(w / 2 - 0.5, z - 1.05)
    f.pipe(-w / 2 + 0.1, h)
    f.build(frame=("steel", 2))

    for fl in range(floors - 1):
        color = sign_colors[fl % len(sign_colors)]
        zc = 3.4 + 3.0 * fl + 1.5
        part(root, "Plate%d" % fl, (lambda zz: lambda bm: km.box(bm, -w / 2 + 0.45, -0.06, zz, 0.6, 0.12, 1.4))(zc), shade(color, 4, 3, 1))
        part(root, "GlowPlate%d" % fl, (lambda zz: lambda bm: km.box(bm, -w / 2 + 0.45, -0.125, zz, 0.46, 0.01, 1.2))(zc), const("paper", 4))

    def blade(bm):
        km.box(bm, w / 2 - 0.3, -0.55, 3.4 + (h - 3.4) / 2, 0.22, 0.8, h - 4.2)

    part(root, "Blade", blade, shade(sign_colors[0], 4, 3, 1))
    part(root, "GlowBlade", lambda bm: [km.box(bm, w / 2 - 0.3 + s, -0.55, 3.4 + (h - 3.4) / 2, 0.01, 0.62, h - 4.6) for s in (-0.115, 0.115)], const("paper", 4))
    return root


def warehouse(x):
    root = km.new_root("Warehouse", x)
    w, d, h = 9.5, 8.0, 6.5
    body(root, "Body", -w / 2, w / 2, 0.0, d, 0.0, h, "rust", 2)
    gable_roof(root, w, d, h, 0.8, 0.3, "steel", "rust", 2)
    part(root, "Ribs", lambda bm: [km.box(bm, -w / 2 + i * 0.5, -0.03, h / 2, 0.06, 0.06, h) for i in range(int(w / 0.5) + 1)], const("rust", 1))

    def big_shutter(bm):
        for i in range(20):
            km.box(bm, -0.8, -0.08 - 0.01 * (i % 2), 0.1 + i * 0.2, 4.4, 0.04, 0.2)
        km.box(bm, -0.8, -0.2, 4.2, 4.8, 0.4, 0.4)

    part(root, "BigShutter", big_shutter, lambda p: ("steel", 3 if int(p.center.z / 0.2) % 2 == 0 else 2))
    part(root, "SideDoor", lambda bm: km.box(bm, w / 2 - 1.3, -0.07, 1.0, 0.9, 0.06, 2.0), const("steel", 1))
    part(root, "GlowLamp", lambda bm: km.box(bm, -0.8, -0.3, 4.75, 0.4, 0.3, 0.2), const("caution", 4))
    return root


# ── 建物の裏 ───────────────────────────────────────────

def back_facade(name, x, h, material, step, variant):
    """長さ 6m・奥行き 0.3m の一枚。正面（-Y）が通り。後ろは街区の塊に付く。"""
    root = km.new_root(name, x)
    w, d = 6.0, 0.3
    part(root, "Body", lambda bm: km.box(bm, 0.0, d / 2, h / 2, w, d, h), wall_rule(material, step))
    part(root, "Coping", lambda bm: km.box(bm, 0.0, d / 2 - 0.05, h + 0.06, w + 0.04, d + 0.12, 0.12), const("concrete", 3))
    part(root, "Band", lambda bm: [km.box(bm, 0.0, -0.02, z, w, 0.06, 0.12) for z in (3.1, 6.1) if z < h - 0.5], const(material, max(0, step - 1)))

    door_x = -1.6 if variant != 1 else 1.4
    part(root, "BackDoor", lambda bm: km.box(bm, door_x, -0.04, 1.0, 0.9, 0.08, 2.0), const("steel", 2 if variant == 1 else 1))
    part(root, "DoorHood", lambda bm: km.box(bm, door_x, -0.3, 2.2, 1.2, 0.6, 0.06), shade("steel", 2, 1, 0))
    part(root, "GlowDoorLamp", lambda bm: km.box(bm, door_x + 0.75, -0.08, 2.0, 0.15, 0.12, 0.2), const("caution", 4))

    f = Facade(root, variant + 1)
    f.window(1.2 if variant != 1 else -1.5, 1.6, 0.8, 0.6, grille=True)
    if h > 5.5:
        f.window(-1.4, 4.4, 0.8, 0.6)
        f.window(1.4, 4.4, 0.8, 0.6)
    if h > 8.5:
        f.window(-1.4, 7.4, 0.8, 0.6)
        f.window(1.4, 7.4, 0.8, 0.6)
    f.pipe(w / 2 - 0.35 if variant != 2 else -w / 2 + 0.35, h)
    f.meter(-0.2, 1.6)
    f.ac(2.0, 0.35)
    if h > 6:
        f.ac(-2.2, 3.6)
    f.build(frame=("steel", 2))
    part(root, "Vent", lambda bm: km.box(bm, 2.3, -0.06, 2.4, 0.35, 0.1, 0.35), const("black", 3))
    return root


# ── 組み立てと書き出し ─────────────────────────────────

km.clear_scene()
km.build_palette_image(OUT + "/KM_Palette.png")

roots = [
    house("House_A", 0.0, 7.0, "kawara", "siding", 3, True, seed=1),
    house("House_B", 10.0, 6.0, "rust", "concrete", 3, False, seed=2),
    house("House_C", 19.0, 7.6, "kawara", "paper", 2, True, seed=3),
    house("House_D", 29.0, 6.6, "kawara", "wood_dark", 3, False, boards=True, seed=4),
    apartment(40.0),
    shop("Shop_A", 54.0, 6.0, 2, "glass", "kawara", "caution", "siding", 3, lanterns=True, seed=1),
    shop("Shop_B", 63.0, 7.0, 3, "open", "rust", "sign_red", "concrete", 3, tent="indigo", blade="sign_red", seed=2),
    shop("Shop_C", 72.0, 6.5, 2, "closed", "rust", "paper", "siding", 2, seed=3),
    shop("Shop_D", 81.0, 5.5, 2, "lattice", "kawara", "indigo", "wood_dark", 3, lanterns=True, seed=4),
    kanban("Kanban_A", 90.0, 6.0, seed=5),
    zakkyo("Zakkyo_A", 100.0, 7.0, 4, ["sign_red", "caution", "indigo"], seed=6),
    zakkyo("Zakkyo_B", 110.0, 8.0, 3, ["indigo", "store_orange"], seed=7),
    warehouse(122.0),
    back_facade("BackFacade_A", 134.0, 7.5, "concrete", 2, 0),
    back_facade("BackFacade_B", 142.0, 6.2, "rust", 2, 1),
    back_facade("BackFacade_C", 150.0, 9.8, "siding", 2, 2),
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
