using System.Collections.Generic;
using KyoumoMushoku.Core.Police;
using UnityEngine;
using UnityEngine.Rendering;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 上機嫌（SAN 70 以上）のときだけ、警官の視線の届く範囲を地面に淡く映す（上機嫌の報酬・第三節：情報だけ）。
    ///
    /// 扇は警官の目と同じ規則で作る：目の高さから視界の角度の中へ線を何本も伸ばし、建物に当たればそこで止める
    /// （壁の向こうは見えないので描かない）。警告・追跡の間は向きを問わないので輪になる（<see cref="ProtoPolice.SightAngle"/>）。
    /// 描く範囲と、警官が実際に見えている範囲を食い違わせない（第三節「嘘をつかない」）。ただし電柱のような細い物が
    /// 一本の線だけを遮った隙間は描かない（扇が欠けて見えるだけで、読む助けにならない）。
    ///
    /// 描き方：扇の形を縦に伸ばした薄い立体（足もとの下から縁石の少し上まで）を作り、型抜き（ステンシル）で
    /// 「立体の中にある、画面に見えている表面」だけを塗る（<c>ProtoGazeVolume</c> シェーダー）。網そのものは地面に触れないので、
    /// 縁石や物の上で割れたり立ち上がったりせず、段差には画面の表面どおりに沿う。縁は形の縁なので、絵の解像度に関係なく鋭い。
    /// 塗りは今の色に比例して明るくするので、夜は夜の暗さになる。境目の帯（外の立体から内の立体を抜いた所）を明るく、
    /// 中は警官の足もとほど明るく届く端へ淡くする（縁だけでは範囲だと分かりにくかった・作者の指摘）。
    /// 地面に網を沿わせる作り（割れた・夜に白く浮いた）と、デカールの投影（縁がぼやけた・場面全体の下描きが毎こま要った）の後の形。
    /// 遠い警官の扇は描かない（近くの警官だけ）。色は使わず、明るさで語る（第十七節「色の言葉」）。
    /// </summary>
    [RequireComponent(typeof(ProtoPolice))]
    public sealed class ProtoPoliceGaze : MonoBehaviour
    {
        const int FanRays = 61;
        const int RingRays = 120;
        const float RedrawSeconds = 1f / 10f;
        const float ShowWithin = 25f;
        const float FadeFrom = 18f;

        // 立体の高さ（警官の足もとから）。縁石の段差（±0.2m）は入り、人の脚はほとんど入らない。
        const float VolumeBottom = -0.6f;
        const float VolumeTop = 0.25f;

        /// <summary>境目の明るい帯の幅（m）。</summary>
        const float RimMeters = 0.22f;

        // 明るくする割合（今の色 × (1 + 割合)・線形の色で）。中は警官の足もとほど明るく、届く端へ向かって淡くなる（視線が警官から出ていると分かる）。
        // 線形で ×1.3 でも画面（sRGB）では一割ほどしか明るく見えないので、大きめに取る（截図で測って決めた）。
        // 1 を超える値は HDR の色の上でだけ効く（切片のカメラと描画設定は HDR）。
        const float RimGain = 1.1f;
        const float InsideNearGain = 1.4f;
        const float InsideFarGain = 0.35f;

        // 型抜きの印（外の立体と内の立体）。
        const int OuterBit = 1;
        const int InnerBit = 2;

        /// <summary>警官ごとに描く順番の枠を分ける（二人の立体の印つけが混ざらないように）。</summary>
        static int _nextSlot;

        ProtoPolice _police;
        Mesh _outer;
        Mesh _inner;
        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Material> _materials = new List<Material>();
        Material _rimMaterial;
        Material _insideMaterial;
        readonly float[] _reach = new float[RingRays];
        readonly float[] _raw = new float[RingRays];
        readonly RaycastHit[] _hits = new RaycastHit[8];
        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _triangles = new List<int>();
        readonly List<Vector3> _loop = new List<Vector3>();
        float _redrawAt;
        bool _shown;

        void Awake()
        {
            _police = GetComponent<ProtoPolice>();
            var source = Resources.Load<Material>("ProtoGazeVolume");
            if (source == null)
            {
                enabled = false;
                return;
            }

            _outer = new Mesh { name = "PoliceGazeOuter" };
            _inner = new Mesh { name = "PoliceGazeInner" };
            _outer.MarkDynamic();
            _inner.MarkDynamic();

            var queue = (int)RenderQueue.Transparent + 1 + _nextSlot++ * 10;
            // 印をつける：裏面が表面より奥（＝見えている表面が立体の裏より手前）なら印。
            AddPass(source, _outer, queue + 0, CullMode.Front, CompareFunction.GreaterEqual, OuterBit, 0, OuterBit, CompareFunction.Always, StencilOp.Replace, 0f, 0f, false);
            // 印を消す：表面が見えている表面より奥（＝見えている表面が立体の手前）なら印を消す。
            AddPass(source, _outer, queue + 1, CullMode.Back, CompareFunction.Greater, 0, 0, OuterBit, CompareFunction.Always, StencilOp.Zero, 0f, 0f, false);
            AddPass(source, _inner, queue + 2, CullMode.Front, CompareFunction.GreaterEqual, InnerBit, 0, InnerBit, CompareFunction.Always, StencilOp.Replace, 0f, 0f, false);
            AddPass(source, _inner, queue + 3, CullMode.Back, CompareFunction.Greater, 0, 0, InnerBit, CompareFunction.Always, StencilOp.Zero, 0f, 0f, false);
            // 塗る：外にだけ印がある所＝境目の帯、両方に印がある所＝中。塗ったら印を消す（表が重なる所を二度塗らない）。
            _rimMaterial = AddPass(source, _outer, queue + 4, CullMode.Back, CompareFunction.Always, OuterBit, OuterBit | InnerBit,
                OuterBit | InnerBit, CompareFunction.Equal, StencilOp.Zero, RimGain, RimGain, true);
            _insideMaterial = AddPass(source, _inner, queue + 5, CullMode.Back, CompareFunction.Always, OuterBit | InnerBit, OuterBit | InnerBit,
                OuterBit | InnerBit, CompareFunction.Equal, StencilOp.Zero, InsideNearGain, InsideFarGain, true);
            SetShown(false);
        }

        Material AddPass(Material source, Mesh mesh, int queue, CullMode cull, CompareFunction zTest, int stencilRef, int readMask,
            int writeMask, CompareFunction stencilComp, StencilOp stencilPass, float gainNear, float gainFar, bool paints)
        {
            var material = new Material(source) { name = "PoliceGaze" + queue, renderQueue = queue };
            material.SetFloat("_Cull", (float)cull);
            material.SetFloat("_ZTest", (float)zTest);
            material.SetFloat("_ColorMask", paints ? 15f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.DstColor);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_StencilRef", stencilRef);
            material.SetFloat("_StencilReadMask", readMask == 0 ? 255 : readMask);
            material.SetFloat("_StencilWriteMask", writeMask);
            material.SetFloat("_StencilComp", (float)stencilComp);
            material.SetFloat("_StencilPass", (float)stencilPass);
            material.SetFloat("_GainNear", gainNear);
            material.SetFloat("_GainFar", gainFar);
            _materials.Add(material);

            var go = new GameObject("Gaze" + (queue % 10));
            go.layer = 2; // Ignore Raycast
            go.transform.SetParent(null, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderers.Add(renderer);
            return material;
        }

        void OnDestroy()
        {
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            foreach (var material in _materials)
            {
                Destroy(material);
            }

            if (_outer != null)
            {
                Destroy(_outer);
            }

            if (_inner != null)
            {
                Destroy(_inner);
            }
        }

        void OnDisable() => SetShown(false);

        void SetShown(bool shown)
        {
            if (_shown == shown && _renderers.Count > 0 && _renderers[0] != null && _renderers[0].enabled == shown)
            {
                return;
            }

            _shown = shown;
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = shown;
                }
            }
        }

        void LateUpdate()
        {
            var player = _police.Player;
            var vitals = player != null && player.Vitals != null ? player.Vitals.Vitals : null;
            var distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : float.MaxValue;
            var show = vitals != null && PoliceAwarenessSight.ReadsGaze(vitals.Sanity) && distance <= ShowWithin;
            var wasShown = _shown;
            SetShown(show);
            if (!show)
            {
                return;
            }

            var fade = 1f - Mathf.InverseLerp(FadeFrom, ShowWithin, distance);
            _rimMaterial.SetFloat("_GainNear", RimGain * fade);
            _rimMaterial.SetFloat("_GainFar", RimGain * fade);
            _insideMaterial.SetFloat("_GainNear", InsideNearGain * fade);
            _insideMaterial.SetFloat("_GainFar", InsideFarGain * fade);

            // 形は一秒に数回作り直す。見え始めた瞬間は待たずに作る（古い形を一こまも見せない）。
            if (!wasShown || Time.unscaledTime >= _redrawAt)
            {
                _redrawAt = Time.unscaledTime + RedrawSeconds;
                Rebuild();
            }
        }

        /// <summary>目から線を伸ばして届く距離を測り、外と内の立体を作り直す。</summary>
        void Rebuild()
        {
            var full = _police.SightAngle >= 359f;
            var half = full ? 180f : _police.SightAngle * 0.5f;
            var rays = full ? RingRays : FanRays;
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var radius = _police.SightRadius;
            MeasureReach(forward, radius, half, rays, full);

            var origin = transform.position;
            var bottom = origin.y + VolumeBottom;
            var top = origin.y + VolumeTop;

            // 外：線の届く所をそのままつなぐ。
            _loop.Clear();
            for (var r = 0; r < rays; r++)
            {
                _loop.Add(origin + Direction(forward, half, rays, full, r, 0f) * _reach[r]);
            }

            BuildPrism(_outer, _loop, full ? (Vector3?)null : origin, origin, bottom, top);

            // 内：帯の幅だけ内へ。扇の両脇は、要（かなめ）を前へずらし角度を詰めて、縁と平行に近くする。
            _loop.Clear();
            var inset = full ? 0f : Mathf.Asin(Mathf.Clamp01(RimMeters / Mathf.Max(radius, RimMeters))) * Mathf.Rad2Deg;
            var apex = full ? origin : origin + forward * (RimMeters / Mathf.Max(0.2f, Mathf.Sin(half * Mathf.Deg2Rad)));
            for (var r = 0; r < rays; r++)
            {
                var length = Mathf.Max(0.05f, _reach[r] - RimMeters);
                _loop.Add(origin + Direction(forward, half, rays, full, r, inset) * length);
            }

            BuildPrism(_inner, _loop, full ? (Vector3?)null : apex, full ? origin : apex, bottom, top);
        }

        static Vector3 Direction(Vector3 forward, float half, int rays, bool full, int r, float inset)
        {
            var a = full ? -180f + r * 360f / rays : Mathf.Lerp(-half + inset, half - inset, r / (float)(rays - 1));
            return Quaternion.AngleAxis(a, Vector3.up) * forward;
        }

        /// <summary>
        /// 平らな多角形を縦に伸ばした立体を作る。<paramref name="apex"/> があれば扇（要 → 線の先 → 要）、無ければ輪（線の先を一周）。
        /// 面はすべて外を向ける（型抜きの裏表の判定がこれに頼る）。
        /// </summary>
        void BuildPrism(Mesh mesh, List<Vector3> tips, Vector3? apex, Vector3 center, float bottom, float top)
        {
            _vertices.Clear();
            _triangles.Clear();

            // 輪郭：扇は要を先頭に、輪は線の先だけ。
            var outline = new List<Vector3>(tips.Count + 1);
            if (apex.HasValue)
            {
                outline.Add(apex.Value);
            }

            outline.AddRange(tips);
            var n = outline.Count;

            // 頂点：上の中心・上の輪郭・下の中心・下の輪郭。頂点色の赤は「警官の足もとからの近さ」（中心・要が 1、線の先が 0）。
            _colors.Clear();
            var topCenter = Flat(center, top);
            var bottomCenter = Flat(center, bottom);
            _vertices.Add(topCenter);
            _colors.Add(Color.white);
            for (var i = 0; i < n; i++)
            {
                _vertices.Add(Flat(outline[i], top));
                _colors.Add(apex.HasValue && i == 0 ? Color.white : Color.black);
            }

            _vertices.Add(bottomCenter);
            _colors.Add(Color.white);
            for (var i = 0; i < n; i++)
            {
                _vertices.Add(Flat(outline[i], bottom));
                _colors.Add(apex.HasValue && i == 0 ? Color.white : Color.black);
            }

            var topStart = 1;
            var bottomCenterIndex = n + 1;
            var bottomStart = n + 2;

            // 蓋（上は上向き・下は下向き）。扇は要から、輪は中心から三角形を広げる。
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                if (apex.HasValue && j == 0)
                {
                    continue; // 扇は最後の線の先から要へは蓋の三角形を作らない（要そのものが中心）。
                }

                AddTriangle(0, topStart + i, topStart + j, Vector3.up);
                AddTriangle(bottomCenterIndex, bottomStart + i, bottomStart + j, Vector3.down);
            }

            // 側面：輪郭の辺ごとに四角形を外向きに。輪郭の回る向き（符号つき面積）から、辺の外側を決める。
            var area = 0f;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                area += outline[i].x * outline[j].z - outline[j].x * outline[i].z;
            }

            var sign = area >= 0f ? 1f : -1f;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var edge = outline[j] - outline[i];
                var outward = new Vector3(edge.z, 0f, -edge.x) * sign;
                AddTriangle(topStart + i, topStart + j, bottomStart + i, outward);
                AddTriangle(topStart + j, bottomStart + j, bottomStart + i, outward);
            }

            mesh.Clear();
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>三角形を足す。面の向き（表）が <paramref name="outward"/> を向くよう、必要なら頂点の順を入れ替える。</summary>
        void AddTriangle(int a, int b, int c, Vector3 outward)
        {
            var normal = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
            }

            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }

        static Vector3 Flat(Vector3 at, float y) => new Vector3(at.x, y, at.z);

        /// <summary>
        /// 線ごとに届く距離を測る。扇は両端を含めて等間隔、輪は一周を等間隔（最後の次は最初）。
        /// 一本だけが短い隙間（電柱など細い物の陰）は両隣に合わせて埋める。
        /// </summary>
        void MeasureReach(Vector3 forward, float radius, float half, int rays, bool full)
        {
            var eye = transform.position + Vector3.up * _police.EyeHeightMeters;
            for (var r = 0; r < rays; r++)
            {
                _raw[r] = Reach(eye, Direction(forward, half, rays, full, r, 0f), radius);
            }

            for (var r = 0; r < rays; r++)
            {
                var hasPrev = full || r > 0;
                var hasNext = full || r < rays - 1;
                _reach[r] = hasPrev && hasNext
                    ? Mathf.Max(_raw[r], Mathf.Min(_raw[(r - 1 + rays) % rays], _raw[(r + 1) % rays]))
                    : _raw[r];
            }

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
    }
}
