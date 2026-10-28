using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using KyoumoMushoku.Gameplay.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KyoumoMushoku.Editor.Slice
{
    /// <summary>
    /// 見えている面より下にしか当たり判定の無い静的な物を探し、形どおりの当たり判定（MeshCollider）を足す。
    /// 宙の物（<see cref="ProtoLoot"/>）は当たり判定の上に落ちるので、当たり判定が見た目より低いと品が地面に埋まる
    /// （2026-10-28：歩道の敷石 260 枚・道路・柏油の上掛け・芝・砂場などで最大 23cm 埋まっていた）。
    ///
    /// 動く物（警官・ゴミ箱のふた・宙の物・プレイヤー）には足さない。街の小物を足したら、もう一度走らせる。
    /// </summary>
    public static class SliceGroundColliders
    {
        /// <summary>見えている面がこれより高ければ「埋まる」とみなす（m）。</summary>
        const float GapThreshold = 0.04f;

        [MenuItem("KyoumoMushoku/Slice/Report Ground Colliders (items sink)")]
        public static void Report() => Debug.Log(Run(apply: false));

        [MenuItem("KyoumoMushoku/Slice/Fix Ground Colliders (items sink)")]
        public static void Fix()
        {
            Debug.Log(Run(apply: true));
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>調べて（<paramref name="apply"/> なら足して）、種類ごとの件数と最大の埋まり幅を返す。</summary>
        public static string Run(bool apply)
        {
            Physics.SyncTransforms();
            var groups = new SortedDictionary<string, (int count, float gap)>();
            var added = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!IsCandidate(renderer))
                {
                    continue;
                }

                var gap = WorstGap(renderer.bounds);
                if (gap < GapThreshold)
                {
                    continue;
                }

                var key = Regex.Replace(PathOf(renderer.transform), @"(\.\d+|\(\d+\)| \d+)", string.Empty);
                groups.TryGetValue(key, out var entry);
                groups[key] = (entry.count + 1, Mathf.Max(entry.gap, gap));

                if (apply && renderer.GetComponent<Collider>() == null)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        var collider = Undo.AddComponent<MeshCollider>(renderer.gameObject);
                        collider.sharedMesh = filter.sharedMesh;
                        added++;
                    }
                }
            }

            if (apply)
            {
                Physics.SyncTransforms();
            }

            var report = new StringBuilder();
            report.AppendLine(apply ? $"地面の当たり判定：{added} 個を足した" : "地面の当たり判定：埋まる所");
            foreach (var pair in groups)
            {
                report.AppendLine($"  {pair.Key} ×{pair.Value.count}（最大 {pair.Value.gap:F2}m）");
            }

            return report.ToString();
        }

        /// <summary>地面の近くにある、薄く広い静的な物。動く物と文字は除く。</summary>
        static bool IsCandidate(MeshRenderer renderer)
        {
            var b = renderer.bounds;
            if (b.size.y > 0.35f || b.max.y > 1.2f || b.max.y < 0.02f || b.size.x < 0.25f || b.size.z < 0.25f)
            {
                return false;
            }

            if (renderer.GetComponent<TMPro.TextMeshPro>() != null || renderer.GetComponent<MeshFilter>() == null)
            {
                return false;
            }

            return renderer.GetComponentInParent<ProtoPolice>(true) == null
                   && renderer.GetComponentInParent<ProtoTrashCan>(true) == null
                   && renderer.GetComponentInParent<ProtoLoot>(true) == null
                   && renderer.GetComponentInParent<CharacterController>(true) == null
                   && renderer.GetComponentInParent<Rigidbody>(true) == null;
        }

        /// <summary>中心と四隅の内側で、見えている上面と、その下で一番高い当たり判定との差の最大。</summary>
        static float WorstGap(Bounds b)
        {
            var worst = 0f;
            var points = new List<Vector3> { b.center };
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    points.Add(b.center + new Vector3(b.extents.x * 0.7f * sx, 0f, b.extents.z * 0.7f * sz));
                }
            }

            foreach (var p in points)
            {
                var top = -99f;
                foreach (var hit in Physics.RaycastAll(new Vector3(p.x, b.max.y + 0.05f, p.z), Vector3.down, 3f, ~0,
                             QueryTriggerInteraction.Ignore))
                {
                    top = Mathf.Max(top, hit.point.y);
                }

                worst = Mathf.Max(worst, b.max.y - top);
            }

            return worst;
        }

        static string PathOf(Transform t)
        {
            var path = t.name;
            for (var depth = 0; depth < 3 && t.parent != null; depth++)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
