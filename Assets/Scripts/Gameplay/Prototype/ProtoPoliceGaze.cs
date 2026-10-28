using KyoumoMushoku.Core.Police;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 上機嫌（SAN 70 以上）のときだけ、警官の視線の届く範囲を地面に淡い扇で見せる（上機嫌の報酬・第三節：情報だけ）。
    ///
    /// 扇は警官の目と同じ規則で作る：目の高さから視界の角度の中へ線を何本も伸ばし、建物に当たればそこで止める
    /// （壁の向こうは見えないので描かない）。警告・追跡の間は向きを問わないので輪になる（<see cref="ProtoPolice.SightAngle"/>）。
    /// 描く扇と、警官が実際に見えている範囲を食い違わせない（第三節「嘘をつかない」）。地面の段差（歩道など）には沿わせる。
    /// 遠い警官の扇は描かない（近くの警官だけ）。色は使わず、淡い白の濃淡で語る（第十七節「色の言葉」）。
    /// </summary>
    [RequireComponent(typeof(ProtoPolice))]
    public sealed class ProtoPoliceGaze : MonoBehaviour
    {
        const int Rays = 32;
        const int RaysFullCircle = 56;
        const int Rings = 6;
        const float ShowWithin = 25f;
        const float FadeFrom = 18f;
        const float LiftMeters = 0.04f;
        const float NearAlpha = 0.16f;
        const float FarAlpha = 0.06f;
        const float RimAlpha = 0.28f;

        static readonly Color Tint = new Color(1f, 0.97f, 0.9f, 1f);

        ProtoPolice _police;
        Mesh _mesh;
        MeshRenderer _renderer;
        Vector3[] _vertices;
        Color[] _colors;
        int[] _triangles;
        int _builtRays;
        readonly RaycastHit[] _hits = new RaycastHit[8];

        void Awake()
        {
            _police = GetComponent<ProtoPolice>();
            var go = new GameObject("Gaze");
            go.layer = 2; // Ignore Raycast：扇そのものに目や手の線が当たらないように。
            go.transform.SetParent(null, false);
            _mesh = new Mesh { name = "PoliceGaze" };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = Resources.Load<Material>("ProtoSightFan");
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        void OnDestroy()
        {
            if (_renderer != null)
            {
                Destroy(_renderer.gameObject);
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        void OnDisable()
        {
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }

        void LateUpdate()
        {
            var player = _police.Player;
            var vitals = player != null && player.Vitals != null ? player.Vitals.Vitals : null;
            var distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : float.MaxValue;
            var show = vitals != null && PoliceAwarenessSight.ReadsGaze(vitals.Sanity) && distance <= ShowWithin
                       && _renderer.sharedMaterial != null;
            _renderer.enabled = show;
            if (!show)
            {
                return;
            }

            var fade = 1f - Mathf.InverseLerp(FadeFrom, ShowWithin, distance);
            Rebuild(fade);
        }

        void Rebuild(float fade)
        {
            var angle = _police.SightAngle;
            var full = angle >= 359f;
            var rays = full ? RaysFullCircle : Rays;
            EnsureBuffers(rays, full);

            var origin = transform.position;
            var eye = origin + Vector3.up * _police.EyeHeightMeters;
            var radius = _police.SightRadius;
            var forward = transform.forward;
            var half = angle * 0.5f;

            _vertices[0] = Ground(origin);
            _colors[0] = Tint * new Color(1f, 1f, 1f, NearAlpha * fade);
            for (var r = 0; r < rays; r++)
            {
                var t = full ? r / (float)rays : r / (float)(rays - 1);
                var direction = Quaternion.AngleAxis(Mathf.Lerp(-half, half, t), Vector3.up) * forward;
                var reach = Reach(eye, direction, radius);
                for (var k = 1; k <= Rings; k++)
                {
                    var along = k / (float)Rings;
                    var index = 1 + r * Rings + (k - 1);
                    _vertices[index] = Ground(origin + direction * (reach * along));
                    var alpha = k == Rings ? RimAlpha : Mathf.Lerp(NearAlpha, FarAlpha, along);
                    _colors[index] = Tint * new Color(1f, 1f, 1f, alpha * fade);
                }
            }

            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            if (_builtRays != (full ? -rays : rays))
            {
                _mesh.triangles = _triangles;
                _builtRays = full ? -rays : rays;
            }

            _mesh.RecalculateBounds();
        }

        /// <summary>目から伸ばした線が、建物に当たるまでの距離。人（自分・プレイヤー）は遮らない。</summary>
        float Reach(Vector3 eye, Vector3 direction, float radius)
        {
            var count = Physics.RaycastNonAlloc(eye, direction, _hits, radius, ~0, QueryTriggerInteraction.Ignore);
            var nearest = radius;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.distance < nearest && hit.collider.GetComponentInParent<ProtoHands>() == null
                                           && hit.collider.GetComponentInParent<ProtoPolice>() == null)
                {
                    nearest = hit.distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// その場所の地面のすぐ上（歩道の段差にも沿う）。腰より低い所から下へ測るので、台や棚の上には登らない
        /// （扇が物の上に立ち上がって板のように見えない）。
        /// </summary>
        Vector3 Ground(Vector3 at)
        {
            var from = new Vector3(at.x, transform.position.y + 0.5f, at.z);
            return Physics.Raycast(from, Vector3.down, out var hit, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore)
                   && hit.collider.GetComponentInParent<ProtoPolice>() == null && hit.collider.GetComponentInParent<ProtoHands>() == null
                ? hit.point + Vector3.up * LiftMeters
                : new Vector3(at.x, transform.position.y + LiftMeters, at.z);
        }

        void EnsureBuffers(int rays, bool full)
        {
            var vertexCount = 1 + rays * Rings;
            if (_vertices != null && _vertices.Length == vertexCount && _builtRays == (full ? -rays : rays))
            {
                return;
            }

            _vertices = new Vector3[vertexCount];
            _colors = new Color[vertexCount];
            var segments = full ? rays : rays - 1;
            _triangles = new int[segments * (3 + (Rings - 1) * 6)];
            var w = 0;
            for (var s = 0; s < segments; s++)
            {
                var a = s;
                var b = (s + 1) % rays;
                // 中心から最初の輪への三角形（上から見て表になるよう、時計回り）。
                _triangles[w++] = 0;
                _triangles[w++] = 1 + a * Rings;
                _triangles[w++] = 1 + b * Rings;
                for (var k = 0; k < Rings - 1; k++)
                {
                    var a0 = 1 + a * Rings + k;
                    var a1 = a0 + 1;
                    var b0 = 1 + b * Rings + k;
                    var b1 = b0 + 1;
                    _triangles[w++] = a0;
                    _triangles[w++] = a1;
                    _triangles[w++] = b0;
                    _triangles[w++] = b0;
                    _triangles[w++] = a1;
                    _triangles[w++] = b1;
                }
            }

            _mesh.Clear();
            _builtRays = 0;
        }
    }
}
