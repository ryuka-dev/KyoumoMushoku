using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 街の歩ける所（ナビメッシュ）。遊び始めに、シーンの静的な当たり判定から作る（編集器で焼かないので、街を手で直しても焼き直しが要らない）。
    /// 警官（<see cref="ProtoPolice"/>）は、ここで道を求めて建物やアーケードを回り込む。シーンに一つ置く。
    ///
    /// 動く物（プレイヤー・警官・宙の物・ゴミ箱のふた・物理で動く物）と触れない当たり判定（trigger）は歩ける所を削らない。
    /// 道が求まらない（目標が壁の中・街の外）ときは、呼び手がまっすぐ歩く（<see cref="ProtoNavPath"/>）。
    /// </summary>
    public sealed class ProtoStreetNav : MonoBehaviour
    {
        [Tooltip("歩ける所を作る範囲の余白（m）。当たり判定すべてを囲む箱をこれだけ広げる。")]
        [SerializeField, Min(0f)] float _margin = 2f;

        [Tooltip("警官の体の半径（m）。壁からこれだけ離れて歩く。CharacterController の半径と同じに。")]
        [SerializeField, Min(0.05f)] float _agentRadius = 0.3f;

        [SerializeField, Min(0.5f)] float _agentHeight = 1.8f;
        [SerializeField, Min(0f)] float _agentClimb = 0.3f;
        [SerializeField, Range(0f, 60f)] float _agentSlope = 45f;

        NavMeshDataInstance _instance;
        int _agentTypeId;

        /// <summary>歩ける所が出来ているか。</summary>
        public bool Ready { get; private set; }

        /// <summary>作るのに掛かった秒数（診断）。</summary>
        public float BuildSeconds { get; private set; }

        /// <summary>道を求める物差し（この街の歩ける所だけを使う）。</summary>
        public NavMeshQueryFilter Filter => new NavMeshQueryFilter { agentTypeID = _agentTypeId, areaMask = NavMesh.AllAreas };

        void Start() => Build();

        void OnDestroy()
        {
            if (_instance.valid)
            {
                _instance.Remove();
            }

            Ready = false;
        }

        void Build()
        {
            var started = Time.realtimeSinceStartup;
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds();
            var any = false;
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!IsObstacle(collider))
                {
                    continue;
                }

                if (TryMakeSource(collider, out var source))
                {
                    sources.Add(source);
                    if (any)
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                    else
                    {
                        bounds = collider.bounds;
                        any = true;
                    }
                }
            }

            if (!any)
            {
                return;
            }

            bounds.Expand(_margin * 2f);
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = _agentRadius;
            settings.agentHeight = _agentHeight;
            settings.agentClimb = _agentClimb;
            settings.agentSlope = _agentSlope;
            _agentTypeId = settings.agentTypeID;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                return;
            }

            _instance = NavMesh.AddNavMeshData(data);
            Ready = _instance.valid;
            BuildSeconds = Time.realtimeSinceStartup - started;
        }

        /// <summary>
        /// 道を求める。<paramref name="corners"/> に曲がり角を入れ、求まれば true。始点と終点が歩ける所から外れていれば、近くの歩ける所へ寄せる。
        /// </summary>
        public bool TryPath(Vector3 from, Vector3 to, NavMeshPath path)
        {
            if (!Ready)
            {
                return false;
            }

            var filter = Filter;
            if (!NavMesh.SamplePosition(from, out var start, 1.5f, filter) || !NavMesh.SamplePosition(to, out var end, 2.5f, filter))
            {
                return false;
            }

            return NavMesh.CalculatePath(start.position, end.position, filter, path) && path.status != NavMeshPathStatus.PathInvalid;
        }

        /// <summary>歩いた道の長さ（m）。道が求まらなければ、まっすぐの距離。</summary>
        public float RouteLength(Vector3 from, Vector3 to)
        {
            var straight = new Vector3(to.x - from.x, 0f, to.z - from.z).magnitude;
            var path = new NavMeshPath();
            if (!TryPath(from, to, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                return straight;
            }

            var length = 0f;
            var corners = path.corners;
            for (var i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }

            return Mathf.Max(straight, length);
        }

        static bool IsObstacle(Collider collider)
        {
            if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
            {
                return false;
            }

            return collider.GetComponentInParent<CharacterController>(true) == null
                   && collider.GetComponentInParent<Rigidbody>(true) == null
                   && collider.GetComponentInParent<ProtoLoot>(true) == null
                   && collider.GetComponentInParent<ProtoPolice>(true) == null
                   && collider.GetComponentInParent<ProtoHands>(true) == null;
        }

        static bool TryMakeSource(Collider collider, out NavMeshBuildSource source)
        {
            source = new NavMeshBuildSource { area = 0, component = collider };
            var t = collider.transform;
            switch (collider)
            {
                case BoxCollider box:
                    source.shape = NavMeshBuildSourceShape.Box;
                    source.transform = Matrix4x4.TRS(t.TransformPoint(box.center), t.rotation, Vector3.one);
                    source.size = Abs(Vector3.Scale(box.size, t.lossyScale));
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    source.shape = NavMeshBuildSourceShape.Mesh;
                    source.sourceObject = mesh.sharedMesh;
                    source.transform = t.localToWorldMatrix;
                    return true;
                case SphereCollider sphere:
                    source.shape = NavMeshBuildSourceShape.Sphere;
                    source.transform = Matrix4x4.TRS(t.TransformPoint(sphere.center), t.rotation, Vector3.one);
                    var s = Abs(t.lossyScale);
                    source.size = Vector3.one * (sphere.radius * 2f * Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
                    return true;
                case CapsuleCollider capsule:
                    source.shape = NavMeshBuildSourceShape.Capsule;
                    source.transform = Matrix4x4.TRS(t.TransformPoint(capsule.center), t.rotation, Vector3.one);
                    var c = Abs(t.lossyScale);
                    var d = capsule.radius * 2f;
                    source.size = capsule.direction == 0 ? new Vector3(capsule.height * c.x, d * c.y, d * c.z)
                        : capsule.direction == 1 ? new Vector3(d * c.x, capsule.height * c.y, d * c.z)
                        : new Vector3(d * c.x, d * c.y, capsule.height * c.z);
                    return true;
                case TerrainCollider:
                    return false;
                default:
                    return false;
            }
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
