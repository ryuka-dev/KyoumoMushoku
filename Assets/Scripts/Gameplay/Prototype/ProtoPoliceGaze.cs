using KyoumoMushoku.Core.Police;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    /// 地面へは上から投影する（URP のデカール）。網を地面に沿わせる作りは、縁石や物の上で三角形が立ち上がって割れ、
    /// 光を受けないので夜に白く浮いた（2026-10-28 の作者の截図）。投影なら段差に自然に沿い、壁には角度で映らず、
    /// 地面の色を変えるだけなので夜は夜の暗さになる。扇の形は小さな絵に描き、毎こまではなく一秒に数回描き直す。
    /// 境目は「外に暗い線・内に白い線」、その内側に淡い光の帯（範囲だと分かる）。線は数画素の幅でなめらかにぼかす
    /// （細すぎると、足もとで拡大されたときに階段状のぎざぎざが見える）。
    /// 遠い警官の扇は描かない（近くの警官だけ）。色は使わず、淡い白と黒の濃淡で語る（第十七節「色の言葉」）。
    /// </summary>
    [RequireComponent(typeof(ProtoPolice))]
    public sealed class ProtoPoliceGaze : MonoBehaviour
    {
        const int FanRays = 61;
        const int RingRays = 120;
        const int TextureSize = 256;
        const float RedrawSeconds = 1f / 10f;

        /// <summary>一こまに描く行数（絵の一辺の 1/4）。描き直しは 4 こまで終わる。</summary>
        const int RowsPerFrame = TextureSize / 4;
        const float ShowWithin = 25f;
        const float FadeFrom = 18f;

        // 投影する厚みは足もとの上下だけ（縁石の段差は入るが、人の脚や体には映らない）。
        const float ProjectorHeight = 0.25f;
        const float ProjectorDepth = 0.7f;

        // 地面の色を寄せる割合（投影は地面の色そのものを変える）。白だけでは明るい敷石の上で見えず、暗いだけでは暗い路面で見えない。
        const float InsideAlpha = 0.03f;
        const float LineDarkMeters = 0.12f;
        const float LineLightMeters = 0.18f;
        const float LineDarkAlpha = 0.45f;
        const float LineLightAlpha = 0.60f;
        const float GlowMeters = 1.5f;
        const float GlowAlpha = 0.16f;

        /// <summary>境目のぼかし（画素の何倍か）。</summary>
        const float SoftPixels = 1.6f;

        /// <summary>足もとでは線が一点に集まって煩いので、この距離より内側では線と帯を消す。</summary>
        const float ApexFadeMeters = 1.2f;

        /// <summary>隣り合う線の届く距離がこれ以上違えば壁の角とみなし、短い方を取る（外へはみ出さない）。</summary>
        const float CornerMeters = 0.8f;

        ProtoPolice _police;
        DecalProjector _projector;
        Material _material;
        Texture2D _texture;
        Color32[] _pixels;
        readonly float[] _reach = new float[RingRays];
        readonly float[] _raw = new float[RingRays];
        readonly RaycastHit[] _hits = new RaycastHit[8];
        float _redrawAt;

        // 画素ごとの表。足もとからの距離（m）、線の番号での位置（扇の外は -1）、扇の両脇の縁までの距離（m・外は負、輪は大）。
        // 絵の大きさ・届く距離・扇か輪か、が変わったときだけ作り直す。
        float[] _pixelDistance;
        float[] _pixelRay;
        float[] _pixelSide;
        float _tableRadius = -1f;
        float _tableHalf = -1f;

        void Awake()
        {
            _police = GetComponent<ProtoPolice>();
            var source = Resources.Load<Material>("ProtoSightDecal");
            if (source == null)
            {
                enabled = false;
                return;
            }

            _texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "PoliceGaze", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            _pixels = new Color32[TextureSize * TextureSize];
            _material = new Material(source) { name = "PoliceGaze" };
            _material.SetTexture("Base_Map", _texture);

            var go = new GameObject("Gaze");
            go.transform.SetParent(transform, false);
            _projector = go.AddComponent<DecalProjector>();
            _projector.material = _material;
            _projector.pivot = new Vector3(0f, 0f, ProjectorDepth * 0.5f);
            // 壁や人の脚のような立った面には映さない（地面と、物の上面だけ）。
            _projector.startAngleFade = 30f;
            _projector.endAngleFade = 55f;
            _projector.enabled = false;
        }

        void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }

            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        void OnDisable()
        {
            if (_projector != null)
            {
                _projector.enabled = false;
            }
        }

        void LateUpdate()
        {
            var player = _police.Player;
            var vitals = player != null && player.Vitals != null ? player.Vitals.Vitals : null;
            var distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : float.MaxValue;
            var show = vitals != null && PoliceAwarenessSight.ReadsGaze(vitals.Sanity) && distance <= ShowWithin;
            _projector.enabled = show;
            if (!show)
            {
                return;
            }

            // 上から真下へ、警官の前が絵の上になる向きで投影する（扇は警官についていく）。
            var radius = _police.SightRadius;
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            _projector.transform.SetPositionAndRotation(transform.position + Vector3.up * ProjectorHeight,
                Quaternion.LookRotation(Vector3.down, forward));
            _projector.size = new Vector3(radius * 2f, radius * 2f, ProjectorDepth);
            _projector.fadeFactor = 1f - Mathf.InverseLerp(FadeFrom, ShowWithin, distance);

            // 描き直しは数こまに分けて進め、描き終えたら絵を差し替える（一こまに重い処理を集めない）。
            if (_drawRow < 0 && Time.unscaledTime >= _redrawAt)
            {
                BeginRedraw(forward, radius);
            }

            if (_drawRow >= 0)
            {
                DrawRows();
            }
        }

        // 描き直しの途中の状態。
        int _drawRow = -1;
        int _drawRays;
        bool _drawFull;

        /// <summary>目から線を伸ばして届く距離を測り、扇の絵の描き直しを始める。</summary>
        void BeginRedraw(Vector3 forward, float radius)
        {
            _drawFull = _police.SightAngle >= 359f;
            var half = _drawFull ? 180f : _police.SightAngle * 0.5f;
            _drawRays = _drawFull ? RingRays : FanRays;
            MeasureReach(forward, radius, half, _drawRays, _drawFull);
            EnsureTables(radius, half, _drawRays, _drawFull, radius * 2f / TextureSize);
            _drawRow = 0;
        }

        /// <summary>扇の絵を、決まった行数だけ描き進める。最後の行まで描いたら絵を差し替える。</summary>
        void DrawRows()
        {
            var end = Mathf.Min(_drawRow + RowsPerFrame, TextureSize);
            DrawPixels(_drawRow * TextureSize, end * TextureSize, _drawRays, _drawFull, _tableRadius * 2f / TextureSize);
            _drawRow = end;
            if (_drawRow < TextureSize)
            {
                return;
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            _drawRow = -1;
            _redrawAt = Time.unscaledTime + RedrawSeconds;
        }

        void DrawPixels(int from, int to, int rays, bool full, float meterPerPixel)
        {
            var soft = meterPerPixel * SoftPixels;
            var lightStart = LineDarkMeters;
            var glowStart = LineDarkMeters + LineLightMeters;
            for (var i = from; i < to; i++)
            {
                var ray = _pixelRay[i];
                var side = _pixelSide[i];
                if (ray < 0f || side < -soft)
                {
                    _pixels[i] = default;
                    continue;
                }

                // 境目までの符号つき距離（m・内側が正）：届く端か、扇の両脇の縁の近い方。
                var dist = _pixelDistance[i];
                var edge = Mathf.Min(ReachAt(ray, rays, full) - dist, side);
                if (edge < -soft)
                {
                    _pixels[i] = default;
                    continue;
                }

                // 外 → 暗い線 → 白い線 → 光の帯 → 中、を数画素ずつなめらかにつなぐ。
                var inside = Ramp(edge, -soft, soft);
                var toLight = Ramp(edge, lightStart - soft, lightStart + soft);
                var toGlow = Ramp(edge, glowStart - soft, glowStart + soft);
                var glow = Mathf.Lerp(GlowAlpha, InsideAlpha, Ramp(edge, glowStart, glowStart + GlowMeters));
                var alpha = inside * Mathf.Lerp(Mathf.Lerp(LineDarkAlpha, LineLightAlpha, toLight), glow, toGlow);

                // 足もとでは線と帯を消し、中の淡さだけにする（線が一点に集まって煩くならない）。
                var apex = Ramp(dist, 0f, ApexFadeMeters);
                alpha = Mathf.Lerp(InsideAlpha * inside, alpha, apex);
                var white = (byte)(Mathf.Lerp(1f, toLight, apex) * 255f);
                _pixels[i] = new Color32(white, white, white, (byte)(alpha * 255f));
            }
        }

        static float Ramp(float value, float from, float to) => Mathf.Clamp01((value - from) / (to - from));

        /// <summary>
        /// 線ごとに届く距離を測る。扇は両端を含めて等間隔、輪は一周を等間隔（最後の次は最初）。
        /// 一本だけが短い隙間（電柱など細い物の陰）は両隣に合わせて埋める。
        /// </summary>
        void MeasureReach(Vector3 forward, float radius, float half, int rays, bool full)
        {
            var eye = transform.position + Vector3.up * _police.EyeHeightMeters;
            for (var r = 0; r < rays; r++)
            {
                var a = full ? -180f + r * 360f / rays : Mathf.Lerp(-half, half, r / (float)(rays - 1));
                _raw[r] = Reach(eye, Quaternion.AngleAxis(a, Vector3.up) * forward, radius);
            }

            for (var r = 0; r < rays; r++)
            {
                var hasPrev = full || r > 0;
                var hasNext = full || r < rays - 1;
                if (hasPrev && hasNext)
                {
                    var prev = _raw[(r - 1 + rays) % rays];
                    var next = _raw[(r + 1) % rays];
                    _reach[r] = Mathf.Max(_raw[r], Mathf.Min(prev, next));
                }
                else
                {
                    _reach[r] = _raw[r];
                }
            }
        }

        /// <summary>線の番号での位置から、届く距離。隣り合う線の間はなめらかに、急に変わる所（壁の角）は短い方。</summary>
        float ReachAt(float ray, int rays, bool full)
        {
            var i = (int)ray;
            var j = full ? (i + 1) % rays : Mathf.Min(i + 1, rays - 1);
            var a = _reach[i];
            var b = _reach[j];
            return Mathf.Abs(a - b) > CornerMeters ? Mathf.Min(a, b) : Mathf.Lerp(a, b, ray - i);
        }

        /// <summary>画素ごとの表を作る。絵の中心が警官の足もと、上（+y）が前、右（+x）が右。</summary>
        void EnsureTables(float radius, float half, int rays, bool full, float meterPerPixel)
        {
            if (_pixelDistance != null && Mathf.Approximately(_tableRadius, radius) && Mathf.Approximately(_tableHalf, half))
            {
                return;
            }

            _tableRadius = radius;
            _tableHalf = half;
            var count = TextureSize * TextureSize;
            _pixelDistance ??= new float[count];
            _pixelRay ??= new float[count];
            _pixelSide ??= new float[count];
            var limit = radius + meterPerPixel * SoftPixels;
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var i = y * TextureSize + x;
                    var dx = (x + 0.5f) * meterPerPixel - radius;
                    var dy = (y + 0.5f) * meterPerPixel - radius;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    _pixelDistance[i] = dist;
                    if (dist > limit)
                    {
                        _pixelRay[i] = -1f;
                        _pixelSide[i] = -1f;
                        continue;
                    }

                    if (full)
                    {
                        _pixelRay[i] = Mathf.Repeat((a + 180f) / 360f * rays, rays);
                        _pixelSide[i] = float.MaxValue;
                    }
                    else
                    {
                        _pixelRay[i] = Mathf.Clamp((Mathf.Clamp(a, -half, half) + half) / (2f * half) * (rays - 1), 0f, rays - 1.001f);
                        var side = Mathf.Clamp((half - Mathf.Abs(a)) * Mathf.Deg2Rad, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
                        _pixelSide[i] = dist * Mathf.Sin(side);
                    }
                }
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
