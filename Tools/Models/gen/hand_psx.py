"""一人称の手：第三者素材 FPS_Psx_Hands（CC0・NiKo_Gillsky）から作る（2026-10-26）。

自作の手は二度作者に否まれた（skill「lowpoly-models」の踏んだ坑）。有機形は素材を使う。
素材は ArtSource/ThirdParty/FPS_Psx_Hands/（出典とライセンスは SOURCE.md）。

やること：
1. 素材の FBX を読む（骨入りの両腕）。
2. 骨で姿勢を作り、**右腕の肘から先だけ**を静的なメッシュとして焼く。握り（Hand_Fist）と開き（Hand_Open）の 2 つ。
   指を Unity で回すことはしない（前の版の「指の向きが逆」を繰り返さない）。2 つを差し替えて使う。
3. 肘を原点にし、前腕の向きを Blender の -Y（正面）へ回す。書き出しで Unity の +Z になる。
4. 貼り絵は素材のまま（FPSarm_256px.png）。

Blender で実行：
    exec(open(r"D:/Unity/KyoumoMushoku/Tools/Models/gen/hand_psx.py", encoding="utf-8").read())
書き出し先：Assets/Art/Models/Characters/Hand_Fist.fbx・Hand_Open.fbx
"""

import importlib
import math
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, r"D:/Unity/KyoumoMushoku/Tools/Models")
import kmlowpoly as km  # noqa: E402

importlib.reload(km)

SRC = r"D:/Unity/KyoumoMushoku/ArtSource/ThirdParty/FPS_Psx_Hands/FPS_Psx_Hands.fbx"
OUT = r"D:/Unity/KyoumoMushoku/Assets/Art/Models/Characters"

# 姿勢（度）。指は骨の局所 X で内へ曲がる（2026-10-26 に画像で確かめた）。
POSES = {
    "Hand_Fist": {
        "fingers": {"01": 80, "02": 95, "03": 70},
        "thumb": [(20, 0, -25), (35, 0, 0), (35, 0, 0)],
    },
    "Hand_Open": {
        "fingers": {"01": 8, "02": 10, "03": 6},
        "thumb": [(0, 0, 12), (6, 0, 0), (6, 0, 0)],
    },
}


def load():
    km.clear_scene()
    for armature in list(bpy.data.armatures):
        if armature.users == 0:
            bpy.data.armatures.remove(armature)
    bpy.ops.import_scene.fbx(filepath=SRC)
    rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    return rig, mesh


def pose(rig, spec):
    for pb in rig.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
    for finger in ("f_index", "f_ring"):
        for seg, deg in spec["fingers"].items():
            rig.pose.bones[f"{finger}.{seg}.R"].rotation_euler = (math.radians(deg), 0.0, 0.0)
    for i, rot in enumerate(spec["thumb"], start=1):
        rig.pose.bones[f"thumb.0{i}.R"].rotation_euler = tuple(math.radians(a) for a in rot)
    bpy.context.view_layer.update()


def bake(rig, mesh, name):
    """今の姿勢の右前腕と手を、肘を原点・前腕を -Y にした静的メッシュにする。"""
    elbow = rig.matrix_world @ rig.pose.bones["forearm.R"].head
    wrist = rig.matrix_world @ rig.pose.bones["forearm.R"].tail
    direction = (wrist - elbow).normalized()

    depsgraph = bpy.context.evaluated_depsgraph_get()
    data = bpy.data.meshes.new_from_object(mesh.evaluated_get(depsgraph))
    data.transform(mesh.matrix_world)

    bm = bmesh.new()
    bm.from_mesh(data)
    # 右腕は -X 側。肘より上（上腕）と左腕を落とす。
    drop = [v for v in bm.verts if v.co.x > -0.25 or v.co.z > elbow.z + 0.02]
    bmesh.ops.delete(bm, geom=drop, context="VERTS")
    # 肘を原点へ、前腕を -Y へ。
    turn = direction.to_track_quat("-Y", "Z").inverted().to_matrix().to_4x4()
    for v in bm.verts:
        v.co = turn @ (v.co - elbow)
    bm.to_mesh(data)
    bm.free()

    root = km.new_root(name)
    obj = bpy.data.objects.new(name + "_Mesh", data)
    bpy.context.scene.collection.objects.link(obj)
    km.parent_to(obj, root)
    for poly in data.polygons:
        poly.use_smooth = False
    return root


rig, mesh = load()
roots = []
for name, spec in POSES.items():
    pose(rig, spec)
    roots.append(bake(rig, mesh, name))
rig.hide_set(True)
mesh.hide_set(True)
for i, root in enumerate(roots):
    km.export_fbx([root], f"{OUT}/{root.name}.fbx")
    root.location.x = i * 0.25          # 見比べ用に並べる（書き出しの後）
print("exported:", [r.name for r in roots],
      [len(r.children[0].data.polygons) for r in roots])
