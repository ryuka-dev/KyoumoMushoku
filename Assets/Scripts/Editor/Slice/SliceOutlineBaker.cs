using System.Collections.Generic;
using System.IO;
using KyoumoMushoku.Gameplay.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KyoumoMushoku.Editor.Slice
{
    /// <summary>
    /// 一人称の切片で、触れる物（ゴミ箱・自販機・水道・ベンチ）に縁取りと蠅を付ける。
    ///
    /// 1. 縁取りの殻の網を焼く：元の網と同じ頂点で、法線だけを同じ位置ごとに平均する（角で殻が割れない）。
    ///    元の網は読み取り不可（isReadable=false）の物もあるが、編集時なら読める。焼いた網は Assets/Prototype/Outline に置く
    ///    （取り込み設定に頼らないので、第三者素材の .meta を書き換えなくてよい）。
    /// 2. 開いているシーンの触れる物に <see cref="ProtoOutline"/> を付けて殻を登録する。ゴミ箱には <see cref="ProtoFlies"/> も付ける。
    ///    縁の色は物の種類で決める（探る物は暖色・使う物は寒色、<see cref="ProtoInteractColors"/>）。
    ///    触れる物の中にある別の触れる物（自販機の下の小銭探し）は別々に縁取る。
    ///    描く物の無い触れる場所（自販機の下・コンビニの戸口）には、当たり判定の箱の手前の面（局部の −Z）の大きさの枠を、
    ///    その奥に見えている面のすぐ手前に置く（壁や本体に平らに付いた場所は、裏返しの殻では縁が出ないため）。
    ///
    /// 模型を差し替えたり触れる物を足したりしたら、もう一度実行する。何度実行しても同じ結果になる。
    /// </summary>
    public static class SliceOutlineBaker
    {
        const string OutlineFolder = "Assets/Prototype/Outline";
        const string OutlineMaterialPath = "Assets/Prototype/ProtoOutline.mat";
        const string FliesMaterialPath = "Assets/Prototype/ProtoFlies.mat";

        [MenuItem("KyoumoMushoku/Slice/Bake Interactable Outlines And Flies")]
        public static void Bake()
        {
            var outlineMaterial = EnsureMaterial(OutlineMaterialPath, "KyoumoMushoku/ProtoOutline");
            var fliesMaterial = EnsureMaterial(FliesMaterialPath, "KyoumoMushoku/ProtoFlies");
            if (outlineMaterial == null || fliesMaterial == null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutlineFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prototype", "Outline");
            }

            var hands = Object.FindFirstObjectByType<ProtoHands>();
            var baked = new Dictionary<Mesh, Mesh>();
            var roots = FindInteractableRoots();
            var outlines = 0;
            var flies = 0;
            foreach (var root in roots)
            {
                // 以前の版の見えない代わりの形が残っていれば、部品を集める前に消す。
                var oldProxy = root.transform.Find("OutlineProxy");
                if (oldProxy != null)
                {
                    Undo.DestroyObjectImmediate(oldProxy.gameObject);
                }

                var shells = new List<ProtoOutline.Shell>();
                var renderers = new List<MeshRenderer>();
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    // 中にある別の触れる物の部品は、その物が自分で縁取る。
                    if (renderer.gameObject.name != "Outline" && renderer.gameObject.name != ProtoOutline.FrameName && Owner(renderer.transform) == root)
                    {
                        renderers.Add(renderer);
                    }
                }

                if (renderers.Count == 0)
                {
                    var frame = EnsureFrame(root);
                    if (frame != null)
                    {
                        var frameMesh = frame.GetComponent<MeshFilter>().sharedMesh;
                        shells.Add(new ProtoOutline.Shell { Target = frame, Mesh = frameMesh });
                        baked[frameMesh] = frameMesh;
                    }
                }

                foreach (var renderer in renderers)
                {
                    // 他の大きな部品にすっぽり収まる部品（貼り紙・投入口・自販機の窓や瓶）は縁を付けない。
                    // 付けると物の内側に線が出て、輪郭がうるさくなる。
                    if (IsInsideAnother(renderer, renderers))
                    {
                        continue;
                    }

                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    if (!baked.TryGetValue(filter.sharedMesh, out var shell))
                    {
                        shell = BakeShell(filter.sharedMesh);
                        baked.Add(filter.sharedMesh, shell);
                    }

                    shells.Add(new ProtoOutline.Shell { Target = renderer, Mesh = shell });
                }

                if (shells.Count == 0)
                {
                    continue;
                }

                var outline = root.GetComponent<ProtoOutline>();
                if (outline == null)
                {
                    outline = Undo.AddComponent<ProtoOutline>(root);
                }

                Undo.RecordObject(outline, "Bake outline");
                outline.Configure(hands, outlineMaterial, shells.ToArray());
                var outlineSettings = new SerializedObject(outline);
                outlineSettings.FindProperty("_color").colorValue = ProtoInteractColors.For(Interactable(root));
                outlineSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(outline);
                outlines++;

                if (root.GetComponent<ProtoTrashCan>() != null)
                {
                    var swarm = root.GetComponent<ProtoFlies>();
                    if (swarm == null)
                    {
                        swarm = Undo.AddComponent<ProtoFlies>(root);
                    }

                    var so = new SerializedObject(swarm);
                    so.FindProperty("_material").objectReferenceValue = fliesMaterial;
                    so.ApplyModifiedProperties();
                    flies++;
                }
            }

            // 前に焼いて今は使わない殻（外した部品の殻など）を消す。
            var used = new HashSet<string>();
            foreach (var shell in baked.Values)
            {
                used.Add(AssetDatabase.GetAssetPath(shell));
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { OutlineFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!used.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[SliceOutlineBaker] outlined {outlines} interactables ({baked.Count} shell meshes), flies on {flies} trash cans.");
        }

        /// <summary>縁取りを付ける物：ゴミ箱か使える物の付いた物すべて（中に入れ子になった物も別々に）。</summary>
        static List<GameObject> FindInteractableRoots()
        {
            var roots = new List<GameObject>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((behaviour is ProtoTrashCan || behaviour is IProtoUsable) && !roots.Contains(behaviour.gameObject))
                {
                    roots.Add(behaviour.gameObject);
                }
            }

            return roots;
        }

        static Component Interactable(GameObject go)
        {
            var can = go.GetComponent<ProtoTrashCan>();
            if (can != null)
            {
                return can;
            }

            foreach (var behaviour in go.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IProtoUsable)
                {
                    return behaviour;
                }
            }

            return null;
        }

        /// <summary>その部品を持つ、一番近い触れる物。</summary>
        static GameObject Owner(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                if (Interactable(p.gameObject) != null)
                {
                    return p.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 描く物の無い触れる場所に、縁の枠を置く。枠の大きさは当たり判定の箱の手前の面（局部の −Z）。
        /// 置く深さは、その面から奥へ伸ばした線が最初に当たる見えている面のすぐ手前（自販機の本体・コンビニの壁）。
        /// 枠そのものは描かず（描画は無効）、縁取りの殻の網として使う。何度実行しても一つだけ。
        /// </summary>
        static MeshRenderer EnsureFrame(GameObject root)
        {
            var box = root.GetComponent<BoxCollider>();
            if (box == null)
            {
                return null;
            }

            var t = root.transform;
            var outward = t.TransformDirection(Vector3.back).normalized;
            var faceCenter = t.TransformPoint(box.center + Vector3.back * (box.size.z * 0.5f));
            var size = new Vector2(Mathf.Abs(box.size.x * t.lossyScale.x), Mathf.Abs(box.size.y * t.lossyScale.y));
            // 中央と、枠の四辺のすぐ内側・すぐ外側で測り、一番手前の面に合わせる（コンビニの戸は、真ん中のガラスが奥まり、
            // 戸の縁の枠が手前に出ている。一番手前に合わせないと、枠の線が戸の縁や壁に隠れる）。
            var right = t.TransformDirection(Vector3.right).normalized;
            var up = t.TransformDirection(Vector3.up).normalized;
            var depth = float.MinValue;
            var probes = new List<Vector3> { Vector3.zero };
            foreach (var reach in new[] { -0.03f, 0.03f })
            {
                probes.Add(right * (size.x * 0.5f + reach));
                probes.Add(-right * (size.x * 0.5f + reach));
                probes.Add(up * (size.y * 0.5f + reach));
                probes.Add(-up * (size.y * 0.5f + reach));
            }

            foreach (var offset in probes)
            {
                var measured = SurfaceDepth(faceCenter + offset, outward, root);
                if (!float.IsNaN(measured))
                {
                    depth = Mathf.Max(depth, measured);
                }
            }

            if (depth == float.MinValue)
            {
                depth = 0f;
            }

            var position = faceCenter + outward * (depth + 0.012f);

            var existing = t.Find(ProtoOutline.FrameName);
            var go = existing != null ? existing.gameObject : new GameObject(ProtoOutline.FrameName);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Outline frame");
                go.transform.SetParent(t, false);
            }

            go.layer = root.layer;
            go.transform.position = position;
            go.transform.rotation = t.rotation;
            go.transform.localScale = new Vector3(1f / t.lossyScale.x, 1f / t.lossyScale.y, 1f / t.lossyScale.z);
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = go.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = FrameMesh(size.x, size.y);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterial = null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.enabled = false;
            return renderer;
        }

        /// <summary>
        /// 面の中心から外向きの法線の逆へ、見えている物の面までの距離（外向きを正。何にも当たらなければ NaN）。
        /// 当たり判定の箱は見た目と離れていることがある（コンビニの戸口の箱は壁の 37cm 手前）ので、見た目の網で測る。
        /// </summary>
        static float SurfaceDepth(Vector3 faceCenter, Vector3 outward, GameObject root)
        {
            var temporary = new List<MeshCollider>();
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.gameObject.name == "Outline" || renderer.gameObject.name == ProtoOutline.FrameName)
                {
                    continue;
                }

                if (renderer.bounds.SqrDistance(faceCenter) > 4f)
                {
                    continue;
                }

                var collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                temporary.Add(collider);
            }

            Physics.SyncTransforms();
            var origin = faceCenter + outward * 0.3f;
            var best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(origin, -outward, 2f, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider is MeshCollider mesh && temporary.Contains(mesh) && hit.distance < best)
                {
                    best = hit.distance;
                }
            }

            foreach (var collider in temporary)
            {
                Object.DestroyImmediate(collider);
            }

            return best == float.MaxValue ? float.NaN : 0.3f - best;
        }

        /// <summary>幅 w・高さ h の枠の網（太さ 2cm・厚み 1cm の四本の棒）。同じ大きさは一つの資産を使い回す。</summary>
        static Mesh FrameMesh(float w, float h)
        {
            var path = $"{OutlineFolder}/Frame_{Mathf.RoundToInt(w * 100f)}x{Mathf.RoundToInt(h * 100f)}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            const float bar = 0.02f;
            const float thickness = 0.01f;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void Bar(Vector3 center, Vector3 size)
            {
                var start = vertices.Count;
                var e = size * 0.5f;
                for (var i = 0; i < 8; i++)
                {
                    vertices.Add(center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z));
                }

                int[] faces =
                {
                    0, 2, 3, 0, 3, 1, // -Z
                    4, 5, 7, 4, 7, 6, // +Z
                    0, 4, 6, 0, 6, 2, // -X
                    1, 3, 7, 1, 7, 5, // +X
                    0, 1, 5, 0, 5, 4, // -Y
                    2, 6, 7, 2, 7, 3, // +Y
                };
                foreach (var f in faces)
                {
                    triangles.Add(start + f);
                }
            }

            Bar(new Vector3(-w / 2f + bar / 2f, 0f, 0f), new Vector3(bar, h, thickness));
            Bar(new Vector3(w / 2f - bar / 2f, 0f, 0f), new Vector3(bar, h, thickness));
            Bar(new Vector3(0f, h / 2f - bar / 2f, 0f), new Vector3(w - bar * 2f, bar, thickness));
            Bar(new Vector3(0f, -h / 2f + bar / 2f, 0f), new Vector3(w - bar * 2f, bar, thickness));

            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static bool IsInsideAnother(Renderer part, List<MeshRenderer> all)
        {
            var b = part.bounds;
            var volume = b.size.x * b.size.y * b.size.z;
            foreach (var other in all)
            {
                if (other == part)
                {
                    continue;
                }

                var o = other.bounds;
                if (o.size.x * o.size.y * o.size.z <= volume)
                {
                    continue;
                }

                o.Expand(0.04f);
                if (o.Contains(b.min) && o.Contains(b.max))
                {
                    return true;
                }
            }

            return false;
        }

        static Mesh BakeShell(Mesh source)
        {
            var vertices = source.vertices;
            var normals = source.normals;
            var smooth = new Vector3[vertices.Length];

            // 同じ位置にある頂点の、向きの違う法線だけを足す（同じ面の頂点が多い所に引っ張られない）。
            var groups = new Dictionary<Vector3Int, List<Vector3>>();
            for (var i = 0; i < vertices.Length; i++)
            {
                var key = Key(vertices[i]);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<Vector3>();
                    groups.Add(key, list);
                }

                var n = normals.Length == vertices.Length ? normals[i] : Vector3.up;
                var known = false;
                foreach (var existing in list)
                {
                    if (Vector3.Dot(existing, n) > 0.999f)
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    list.Add(n);
                }
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var sum = Vector3.zero;
                foreach (var n in groups[Key(vertices[i])])
                {
                    sum += n;
                }

                smooth[i] = sum.sqrMagnitude > 1e-8f ? sum.normalized : Vector3.up;
            }

            var triangles = new List<int>();
            for (var s = 0; s < source.subMeshCount; s++)
            {
                triangles.AddRange(source.GetTriangles(s));
            }

            var mesh = new Mesh { name = source.name + "_Outline" };
            mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = smooth;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            var path = $"{OutlineFolder}/{Sanitize(source.name)}_{Sanitize(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source)))}_Outline.asset";
            mesh.name = Path.GetFileNameWithoutExtension(path);
            var existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existingAsset != null)
            {
                EditorUtility.CopySerialized(mesh, existingAsset);
                Object.DestroyImmediate(mesh);
                return existingAsset;
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Vector3Int Key(Vector3 v) =>
            new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));

        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "Builtin";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            return s.Replace(' ', '_');
        }

        static Material EnsureMaterial(string path, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[SliceOutlineBaker] shader not found: {shaderName}");
                return null;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
