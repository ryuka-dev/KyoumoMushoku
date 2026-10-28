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
                var shells = new List<ProtoOutline.Shell>();
                var renderers = new List<MeshRenderer>();
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.gameObject.name != "Outline")
                    {
                        renderers.Add(renderer);
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

                var outline = root.GetComponent<ProtoOutline>() ?? Undo.AddComponent<ProtoOutline>(root);
                Undo.RecordObject(outline, "Bake outline");
                outline.Configure(hands, outlineMaterial, shells.ToArray());
                EditorUtility.SetDirty(outline);
                outlines++;

                if (root.GetComponent<ProtoTrashCan>() != null)
                {
                    var swarm = root.GetComponent<ProtoFlies>() ?? Undo.AddComponent<ProtoFlies>(root);
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

        /// <summary>縁取りを付ける物の根：ゴミ箱か使える物で、描画物を持つもの。触れる物の中にある触れる物（自販機の下など）は親に任せる。</summary>
        static List<GameObject> FindInteractableRoots()
        {
            var candidates = new List<GameObject>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((behaviour is ProtoTrashCan || behaviour is IProtoUsable) && !candidates.Contains(behaviour.gameObject))
                {
                    candidates.Add(behaviour.gameObject);
                }
            }

            var roots = new List<GameObject>();
            foreach (var go in candidates)
            {
                var nested = false;
                foreach (var other in candidates)
                {
                    if (other != go && go.transform.IsChildOf(other.transform))
                    {
                        nested = true;
                        break;
                    }
                }

                if (!nested && go.GetComponentInChildren<MeshRenderer>(true) != null)
                {
                    roots.Add(go);
                }
            }

            return roots;
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
