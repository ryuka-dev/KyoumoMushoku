"""低ポリゴン模型の共通部品（Blender 内で実行する）。

方針（Docs/作業計画.md 第一の二節「美術」）：
- 平面陰影＋一枚の小さな調色板テクスチャ。全模型がこの一枚を共有し、UV は色の升目に落とす。
  色はここ（PALETTE）だけで管理する。色を変えたければ升目の色を変える。
- 1 単位 = 1 メートル。原点は底面の中心。面数は道具 1 個あたり数百まで。
- 比例（2026-10-26 決定）：建物・扉・人は実寸。**叩く・掴む道具は実寸の PROP_SCALE 倍**
  （一人称では実寸の小物が小さく見え、下を向かないと叩けないため）。視点の高さは 1.55m、FOV 65。
  形は実寸で書き、最後に scale_hierarchy(root, PROP_SCALE) を掛ける。
- 生成スクリプトが源。.blend は生成物であって手で直さない（直すならスクリプトを直す）。
"""

import math
import os

import bmesh
import bpy

# ── 調色板 ─────────────────────────────────────────────
# 一つの素材に一本の色階（暗→明）。暗部は青寄り、明部は黄寄り。格差は均等に。
PALETTE = {
    # ポリバケツの青
    "poly_blue": ["#1d3557", "#28507f", "#3570a8", "#4a8cc4", "#74aedb"],
    # 公園の金網・金属の緑
    "park_green": ["#1f3a2c", "#2c5240", "#3d6b53", "#55876a", "#7aa887"],
    # 鉄・灰
    "steel": ["#2b2f36", "#40464f", "#5a616b", "#7a828c", "#a2a9b1"],
    # 白いラベル・紙
    "paper": ["#9a9488", "#bdb6a8", "#d8d2c3", "#ece7da", "#fbf8ef"],
    # 汚れ・土
    "grime": ["#2a241e", "#3d342a", "#54483a", "#6e5f4c", "#8c7a62"],
    # コンビニの差し色（橙）
    "store_orange": ["#6b2a12", "#9a3f1a", "#c95a22", "#e67b33", "#f5a35a"],
    # 黒
    "black": ["#0e0f12", "#17191d", "#212429", "#2c3036", "#383d44"],
    # ── 以下は追記のみ（既存の升目の位置を動かさないため、途中に挿入しない） ──
    # 肌
    "skin": ["#5a3a2a", "#7d5440", "#a0705a", "#c08e74", "#ddb196"],
    # 主人公の上着（くたびれたオリーブ茶）
    "jacket": ["#24261f", "#363a2c", "#4b503b", "#646a4d", "#80866a"],
    # 指先の切れた毛糸の手袋
    "knit": ["#1c1e24", "#2a2d35", "#3a3e48", "#4d525e", "#646a77"],
    # ── 街の小物（2026-10-27） ──
    # コンクリート（電柱・ブロック塀）
    "concrete": ["#3e3f3d", "#5a5b57", "#77786f", "#96968a", "#b8b6a6"],
    # 看板・のぼりの赤
    "sign_red": ["#3d1214", "#5e1a1d", "#8a2528", "#b33a36", "#d65d4e"],
    # 暖簾・手ぬぐいの藍
    "indigo": ["#141a33", "#1f2a4d", "#2d3d6b", "#43578c", "#6276ab"],
    # 素焼きの植木鉢
    "terracotta": ["#4a2417", "#6b3420", "#8f472b", "#b35f3b", "#cf8257"],
    # カーブミラーの鏡面（空を映した青灰）
    "mirror": ["#3b4a5c", "#566a80", "#7890a6", "#9fb6c9", "#cfe0ec"],
    # 電柱の巻き看板・注意の黄
    "caution": ["#4d3b0e", "#7a5e15", "#a8821f", "#d1a531", "#ecc85a"],
    # ── 建物（2026-10-27） ──
    # 瓦（青みの灰）
    "kawara": ["#1e2329", "#2b323b", "#3b4450", "#4f5a68", "#6a7684"],
    # 外壁のサイディング（くすんだ生成り）
    "siding": ["#5e554a", "#7f7466", "#a09484", "#c0b4a3", "#ddd3c3"],
    # トタンの錆
    "rust": ["#3d2015", "#5c2e1c", "#7d4126", "#9e5934", "#b97a52"],
}

PROP_SCALE = 1.35  # 叩く・掴む道具の誇張倍率（上の「比例」）

CELL = 4          # 一色あたりの升目（ピクセル）
COLUMNS = 5       # 色階の段数
ROWS = 16         # 調色板の行数（素材の数の上限）
TEX_W = CELL * COLUMNS * 2   # 2 列組
TEX_H = CELL * ROWS


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def _cell_of(material, step):
    names = list(PALETTE.keys())
    index = names.index(material)
    column_block = index % 2
    row = index // 2
    x = column_block * COLUMNS * CELL + step * CELL
    y = row * CELL
    return x, y


def uv_of(material, step):
    """その色の升目の中心の UV。"""
    x, y = _cell_of(material, step)
    u = (x + CELL / 2) / TEX_W
    v = 1.0 - (y + CELL / 2) / TEX_H
    return u, v


def build_palette_image(path=None):
    """調色板の画像を作り（あれば作り直し）、path が与えられれば PNG に保存する。"""
    name = "KM_Palette"
    image = bpy.data.images.get(name)
    if image is None:
        image = bpy.data.images.new(name, TEX_W, TEX_H, alpha=False)
    pixels = [0.0] * (TEX_W * TEX_H * 4)
    for material, ramp in PALETTE.items():
        for step, hexcolor in enumerate(ramp):
            r, g, b = _hex(hexcolor)
            x0, y0 = _cell_of(material, step)
            for dy in range(CELL):
                for dx in range(CELL):
                    px = x0 + dx
                    py = TEX_H - 1 - (y0 + dy)   # Blender の画像は下から上
                    i = (py * TEX_W + px) * 4
                    pixels[i:i + 4] = [r, g, b, 1.0]
    image.pixels = pixels
    if path:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
    return image


def palette_material():
    """全模型共有の材質。テクスチャは最近傍（升目がにじまない）。"""
    name = "KM_PaletteMat"
    mat = bpy.data.materials.get(name)
    if mat is not None:
        return mat
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = build_palette_image()
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs[0], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.8
    return mat


# ── 形を作る ──────────────────────────────────────────

def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def new_object(name, bm, collection=None):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    (collection or bpy.context.scene.collection).objects.link(obj)
    obj.data.materials.append(palette_material())
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def frustum(bm, sides, r_bottom, r_top, z0, z1, rotate=0.0):
    """側面だけの角錐台（上下は開いたまま）。戻り値は (下の輪, 上の輪) の頂点列。"""
    bottom, top = [], []
    for i in range(sides):
        a = 2 * math.pi * i / sides + rotate
        bottom.append(bm.verts.new((r_bottom * math.cos(a), r_bottom * math.sin(a), z0)))
        top.append(bm.verts.new((r_top * math.cos(a), r_top * math.sin(a), z1)))
    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    return bottom, top


def cap(bm, ring, flip=False):
    face = bm.faces.new(list(reversed(ring)) if flip else ring)
    return face


def box(bm, cx, cy, cz, sx, sy, sz):
    """中心 (cx,cy,cz)・寸法 (sx,sy,sz) の箱。"""
    result = bmesh.ops.create_cube(bm, size=1.0)
    for v in result["verts"]:
        v.co.x = cx + v.co.x * sx
        v.co.y = cy + v.co.y * sy
        v.co.z = cz + v.co.z * sz
    return result["verts"]


def rounded_box(bm, cx, cy, cz, sx, sy, sz, chamfer):
    """角を一段だけ面取りした箱（丸みのある塊。手・ミトン・袋など）。chamfer は面取り幅（m）。"""
    verts = box(bm, cx, cy, cz, sx, sy, sz)
    edges = list({e for v in verts for e in v.link_edges})
    bmesh.ops.bevel(bm, geom=edges, offset=chamfer, offset_type="OFFSET",
                    segments=1, profile=0.5, affect="EDGES", clamp_overlap=True)
    return verts


# ── 色を塗る（UV を升目へ） ─────────────────────────────

def paint(obj, rule):
    """rule(face) -> (素材名, 段) を各面に適用する。"""
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        material, step = rule(poly)
        u, v = uv_of(material, step)
        for li in poly.loop_indices:
            uv[li].uv = (u, v)


def by_normal(material, top=4, side=2, bottom=0):
    """法線で段を選ぶ：上面は明るく、側面は中間、下面は暗く。"""
    def rule(poly):
        nz = poly.normal.z
        if nz > 0.5:
            return material, top
        if nz < -0.5:
            return material, bottom
        return material, side
    return rule


def new_root(name, x=0.0):
    """部品をまとめる空の親。Unity では同名の GameObject になる。原点は底面の中心。"""
    root = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(root)
    root.location.x = x
    return root


def parent_to(child, root, keep_world=False):
    """keep_world=True なら子の世界位置を保つ（関節のように親が原点以外にいるとき）。"""
    if keep_world:
        # 位置を変えた直後は matrix_world が古いままなので、先に更新する。
        # matrix_parent_inverse は使わない：FBX の bake_space_transform が入れ子の逆行列を正しく変換せず、
        # 深い部品（指）だけ Blender の軸のまま Unity に来る（2026-10-26）。局所変換を直接書く。
        bpy.context.view_layer.update()
        local = root.matrix_world.inverted() @ child.matrix_world
        child.parent = root
        child.matrix_parent_inverse.identity()
        child.matrix_basis = local
    else:
        child.parent = root


def recenter(obj, pivot):
    """メッシュを動かさずに原点を pivot（ローカル座標）へ移す。ふたのように飛ぶ部品は自身の中心を原点にする。"""
    from mathutils import Matrix, Vector
    pivot = Vector(pivot)
    obj.data.transform(Matrix.Translation(-pivot))
    obj.location = obj.location + pivot


def scale_hierarchy(root, factor):
    """親の下の部品を、メッシュごと factor 倍する（変換に倍率を残さない。Unity 側の scale は 1 のまま）。"""
    from mathutils import Matrix
    for child in root.children_recursive:
        if child.type == "MESH":
            child.data.transform(Matrix.Scale(factor, 4))
        child.location = child.location * factor


def export_fbx(objects, path):
    """Unity 向けに FBX を書き出す（Y 上・-Z 前・1 単位 1m）。子も一緒に選ぶ。"""
    bpy.ops.object.select_all(action="DESELECT")
    # Blender の正面（-Y）はこのままだと Unity の -Z に来る。書き出しの間だけ親を 180° 回し、
    # Unity では正面＝+Z（transform.forward）になるようにする（2026-10-26 実測）。
    turned = []
    for obj in objects:
        obj.select_set(True)
        for child in obj.children_recursive:
            child.select_set(True)
        turned.append((obj, obj.rotation_euler.z))
        obj.rotation_euler.z += math.pi
    bpy.context.view_layer.update()
    try:
        _export_selected(path)
    finally:
        for obj, z in turned:
            obj.rotation_euler.z = z
        bpy.context.view_layer.update()


def _export_selected(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        # 軸の変換は Unity 側（ModelImporter.bakeAxisConversion）で行う。Blender の bake_space_transform は
        # 入れ子の 2 段目以降を変換しない（指が Blender の軸のまま来た。2026-10-26）。
        bake_space_transform=False,
        path_mode="STRIP",
        embed_textures=False,
    )
