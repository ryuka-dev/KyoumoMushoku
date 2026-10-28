using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 開いていないゴミ箱にたかる蠅。遠くからでも「あそこに何かある」と分かる、世界の中の目印。
    /// 一匹は画素絵のような三つの四角（<c>KyoumoMushoku/ProtoFlies</c>）：真ん中に黒い胴、左右に半透明の灰色の翅（胴より少し高い）。
    /// 大きさは世界の寸法なので近いほど大きい。飛んでいる間は翅が細かく上下し、止まると翅をたたんで薄くなる。
    /// 見た目だけで、遊びの状態には触れない。
    ///
    /// 動き：ふたの上の空間で、短く飛んでは急に向きを変える（狙う点を 0.1〜0.35 秒ごとに選び直し、強く加速する）。
    /// ときどきふたに止まって休み、しばらくすると飛び去る。来るときは遠くから淡く現れ、去るときは淡く消える。
    /// 箱が開くと一斉に散って消え、箱が戻る（補充される）とまた集まってくる。
    /// 時間はゲームの時間で進む（ヒットストップの間は止まる）。
    /// </summary>
    [RequireComponent(typeof(ProtoTrashCan))]
    public sealed class ProtoFlies : MonoBehaviour
    {
        static readonly int BodySizeId = Shader.PropertyToID("_BodySize");
        static readonly int MinBodyPixelsId = Shader.PropertyToID("_MinBodyPixels");

        // 一匹の形（胴の幅を 1 とした位置）。0：胴、1：左の翅、2：右の翅。翅は胴より少し高い。
        static readonly Rect Body = new Rect(-0.5f, -0.5f, 1f, 1f);
        static readonly Rect LeftWing = new Rect(-1.4f, -0.15f, 0.9f, 0.9f);
        static readonly Rect RightWing = new Rect(0.5f, -0.15f, 0.9f, 0.9f);
        const int QuadsPerFly = 3;
        const int VertsPerFly = QuadsPerFly * 4;

        enum Mode
        {
            Away,
            Arriving,
            Flying,
            Resting,
            Leaving,
        }

        struct Fly
        {
            public Mode Mode;
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 Target;
            public float Timer;
            public float Life;
            public float Alpha;
            public bool Landed;
            public bool Approaching;
            public int Tri;
            public Vector3 Local;
            public float Heading;
            public bool Crawling;
            public float CrawlTimer;
            public float ApproachTime;
        }

        /// <summary>蠅が止まって歩ける三角形。部品の局部座標で持つので、箱が揺れても一緒に動く。</summary>
        struct SurfaceTri
        {
            public Transform Space;
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;
            public Vector3 Normal;
        }

        [SerializeField] Material _material;
        [SerializeField, Range(1, 12)] int _count = 4;
        [SerializeField] Color _color = new Color(0.03f, 0.03f, 0.035f, 1f);
        [Tooltip("翅の色（半透明の灰色）。")]
        [SerializeField] Color _wingColor = new Color(0.55f, 0.55f, 0.58f, 0.5f);
        [Tooltip("胴の幅（m）。翅を広げた全幅はこの約 2.8 倍。")]
        [SerializeField, Min(0.001f)] float _bodySize = 0.01f;
        [Tooltip("遠くても、胴はこのピクセル数より小さく描かない（消えてしまわないように）。")]
        [SerializeField, Min(0f)] float _minBodyPixels = 1.5f;
        [Tooltip("羽ばたきの速さ（回/秒）と、翅が上下する幅（胴の幅を 1 として）。")]
        [SerializeField] Vector2 _flap = new Vector2(24f, 0.18f);
        [Tooltip("この距離より遠いと描かず、動かしもしない。")]
        [SerializeField, Min(1f)] float _visibleDistance = 25f;

        [Header("動き")]
        [Tooltip("ふたの上で飛び回る範囲の水平の半径（箱の天面の大きさに足す）。")]
        [SerializeField, Min(0.05f)] float _swarmRadius = 0.35f;
        [Tooltip("ふたの上で飛び回る高さの上限。")]
        [SerializeField, Min(0.05f)] float _swarmHeight = 0.6f;
        [SerializeField, Min(0.1f)] float _maxSpeed = 1.6f;
        [SerializeField, Min(0.1f)] float _acceleration = 14f;
        [Tooltip("一秒あたりに、ふたに止まる確率。")]
        [SerializeField, Range(0f, 1f)] float _restChance = 0.12f;
        [Tooltip("一匹がたかっている秒数の幅（過ぎると飛び去り、別の一匹が来る）。")]
        [SerializeField] Vector2 _stay = new Vector2(8f, 24f);
        [Tooltip("止まっている秒数の幅。止まっている間は、歩いては休むを繰り返す。")]
        [SerializeField] Vector2 _restSeconds = new Vector2(1.5f, 5f);
        [Tooltip("面の上を歩く速さ（m/秒）。")]
        [SerializeField, Min(0f)] float _crawlSpeed = 0.06f;

        ProtoTrashCan _can;
        Collider _collider;
        Fly[] _flies;
        Mesh _mesh;
        GameObject _view;
        MeshRenderer _renderer;
        Vector3[] _vertices;
        Color[] _colors;
        Vector2[] _offsets;
        MaterialPropertyBlock _block;
        bool _wasOpened;
        int _lastHits;
        SurfaceTri[] _tris = new SurfaceTri[0];
        SurfaceTri[] _solid = new SurfaceTri[0];
        Bounds _solidBounds;
        float[] _triWeights = new float[0];
        float _triTotal;
        ProtoOutline _outline;

        void Awake()
        {
            _can = GetComponent<ProtoTrashCan>();
            _collider = GetComponent<Collider>();
            _flies = new Fly[_count];
            _vertices = new Vector3[_count * VertsPerFly];
            _colors = new Color[_count * VertsPerFly];
            _offsets = new Vector2[_count * VertsPerFly];
            _block = new MaterialPropertyBlock();

            // 箱は叩かれて傾き・縮むので、蠅は箱の子にせず、世界に置いた一つの網で描く。
            // 一匹の全頂点は蠅の中心に置き、uv（蠅の中での位置）の分だけシェーダーが画面に向けて広げる。
            _view = new GameObject(name + "_Flies");
            _mesh = new Mesh { name = "Flies" };
            _mesh.MarkDynamic();
            var triangles = new int[_count * QuadsPerFly * 6];
            for (var q = 0; q < _count * QuadsPerFly; q++)
            {
                triangles[q * 6] = q * 4;
                triangles[q * 6 + 1] = q * 4 + 2;
                triangles[q * 6 + 2] = q * 4 + 1;
                triangles[q * 6 + 3] = q * 4;
                triangles[q * 6 + 4] = q * 4 + 3;
                triangles[q * 6 + 5] = q * 4 + 2;
            }

            _mesh.vertices = _vertices;
            _mesh.uv = _offsets;
            _mesh.colors = _colors;
            _mesh.triangles = triangles;
            _view.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _view.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            // 始まったときから半分ほどはもうたかっている。残りは少しずつ来る。
            for (var i = 0; i < _count; i++)
            {
                if (i % 2 == 0)
                {
                    _flies[i].Mode = Mode.Flying;
                    _flies[i].Position = SwarmPoint();
                    _flies[i].Target = SwarmPoint();
                    _flies[i].Life = Random.Range(_stay.x, _stay.y);
                    _flies[i].Alpha = 1f;
                }
                else
                {
                    _flies[i].Mode = Mode.Away;
                    _flies[i].Timer = Random.Range(0.5f, 6f);
                }
            }

            BuildSurface();
        }

        /// <summary>
        /// 止まって歩ける面を、箱の本当の形から集める。当たり判定の箱の上に止まらせると、ゴミ袋や形の不揃いな箱では宙に浮いて見える。
        /// 形は縁取りの殻の網から読む（元の網は読み取り不可でも、殻の網は同じ頂点で読める）。
        /// 下向きの面、ほかの部品に覆われた面（ふたの下の本体の天面など）、降りてくる道がふさがった面（本体の中の台座など）は除く。
        /// 上を向いた面ほど選ばれやすい。
        /// </summary>
        void BuildSurface()
        {
            _outline = GetComponent<ProtoOutline>();
            if (_outline == null)
            {
                return;
            }

            var solid = new System.Collections.Generic.List<SurfaceTri>();
            var candidates = new System.Collections.Generic.List<SurfaceTri>();
            var candidateWeights = new System.Collections.Generic.List<float>();
            var candidateProbes = new System.Collections.Generic.List<Vector3>();
            var candidateNormals = new System.Collections.Generic.List<Vector3>();
            foreach (var shell in _outline.Shells)
            {
                if (shell.Target == null || shell.Mesh == null || !shell.Mesh.isReadable)
                {
                    continue;
                }

                var space = shell.Target.transform;
                var vertices = shell.Mesh.vertices;
                var triangles = shell.Mesh.triangles;
                var scale = space.lossyScale;
                var mirror = scale.x * scale.y * scale.z < 0f ? -1f : 1f;
                for (var t = 0; t + 2 < triangles.Length; t += 3)
                {
                    var la = vertices[triangles[t]];
                    var lb = vertices[triangles[t + 1]];
                    var lc = vertices[triangles[t + 2]];
                    var a = space.TransformPoint(la);
                    var b = space.TransformPoint(lb);
                    var c = space.TransformPoint(lc);
                    var cross = Vector3.Cross(b - a, c - a);
                    var area = cross.magnitude * 0.5f;
                    if (area < 1e-6f)
                    {
                        continue;
                    }

                    // 面の外向きは巻き順で決める（描画の裏面の切り捨てと同じ決まり）。鏡に映した部品（拡大率の積が負）は向きが逆になる。
                    // 殻の頂点の法線は位置ごとに平均してあり、ふたの結び目のような薄い所では当てにならないので使わない。
                    var normal = cross / (area * 2f) * mirror;

                    var entry = new SurfaceTri
                    {
                        Space = space,
                        A = la,
                        B = lb,
                        C = lc,
                        Normal = space.InverseTransformDirection(normal).normalized,
                    };
                    solid.Add(entry);

                    if (normal.y < -0.2f || Covered((a + b + c) / 3f + normal * 0.02f, shell.Target, _outline))
                    {
                        continue;
                    }

                    candidates.Add(entry);
                    candidateWeights.Add(area * (normal.y > 0.6f ? 2f : 1f));
                    candidateProbes.Add((a + b + c) / 3f + normal * 0.02f);
                    candidateNormals.Add(normal);
                }
            }

            _solid = solid.ToArray();

            // 降りてくる道がふさがった面（網の内側に埋もれた台座、ふたの陰など）には止まらない。止まりに行くと箱を突き抜ける。
            var tris = new System.Collections.Generic.List<SurfaceTri>();
            var weights = new System.Collections.Generic.List<float>();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!LandingClear(candidateProbes[i] - candidateNormals[i] * 0.02f, candidateNormals[i]))
                {
                    continue;
                }

                tris.Add(candidates[i]);
                weights.Add(candidateWeights[i]);
            }

            _tris = tris.ToArray();
            _triWeights = weights.ToArray();
            _triTotal = 0f;
            foreach (var w in _triWeights)
            {
                _triTotal += w;
            }
        }

        /// <summary>半直線と三角形が交わるか（Möller–Trumbore）。交わるなら t に起点からの距離（direction が単位長のとき）。</summary>
        static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0f;
            var ab = b - a;
            var ac = c - a;
            var p = Vector3.Cross(direction, ac);
            var determinant = Vector3.Dot(ab, p);
            if (Mathf.Abs(determinant) < 1e-9f)
            {
                return false;
            }

            var inverse = 1f / determinant;
            var s = origin - a;
            var u = Vector3.Dot(s, p) * inverse;
            if (u < 0f || u > 1f)
            {
                return false;
            }

            var q = Vector3.Cross(s, ab);
            var v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            t = Vector3.Dot(ac, q) * inverse;
            return t > 1e-5f;
        }

        /// <summary>その点が、ほかの部品（self 以外）の中にあるか。覆われた面には止まらない。</summary>
        static bool Covered(Vector3 point, Renderer self, ProtoOutline outline)
        {
            foreach (var shell in outline.Shells)
            {
                if (shell.Target == null || shell.Target == self)
                {
                    continue;
                }

                if (shell.Target.bounds.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        void OnDestroy()
        {
            if (_view != null)
            {
                Destroy(_view);
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        void LateUpdate()
        {
            if (_material == null || _collider == null)
            {
                return;
            }

            var camera = Camera.main;
            var near = camera != null && Vector3.Distance(camera.transform.position, transform.position) <= _visibleDistance;
            _renderer.enabled = near;
            if (!near)
            {
                return;
            }

            var opened = _can.IsOpened;
            if (opened && !_wasOpened)
            {
                Scatter();
            }

            _wasOpened = opened;
            if (_can.Hits != _lastHits)
            {
                _lastHits = _can.Hits;
                Startle();
            }

            var dt = Time.deltaTime;
            var top = Top();
            UpdateSolidBounds();
            for (var i = 0; i < _flies.Length; i++)
            {
                Step(ref _flies[i], dt, opened, top);
                WriteFly(i);
            }

            _mesh.vertices = _vertices;
            _mesh.uv = _offsets;
            _mesh.colors = _colors;
            _mesh.bounds = new Bounds(top, Vector3.one * 8f);
            _block.SetFloat(BodySizeId, _bodySize);
            _block.SetFloat(MinBodyPixelsId, _minBodyPixels);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>一匹分の三つの四角を書く。飛んでいれば翅を羽ばたかせ、止まっていれば翅をたたんで薄くする。</summary>
        void WriteFly(int i)
        {
            var fly = _flies[i];
            var alpha = fly.Mode == Mode.Away ? 0f : fly.Alpha;
            var resting = fly.Mode == Mode.Resting && fly.Landed;

            // 羽ばたき：一匹ごとに位相をずらす（皆が同じ拍子で動かないように）。
            var flap = resting ? 0f : Mathf.Sin((Time.time * _flap.x + i * 0.37f) * Mathf.PI * 2f) * _flap.y;
            // 止まると翅をたたむ（幅を詰めて胴に寄せる）。
            var wingWidth = LeftWing.width * (resting ? 0.55f : 1f);
            var left = new Rect(Body.xMin - wingWidth, LeftWing.y + flap, wingWidth, LeftWing.height);
            var right = new Rect(Body.xMax, RightWing.y + flap, wingWidth, RightWing.height);

            var body = _color;
            body.a *= alpha;
            var wing = _wingColor;
            wing.a *= alpha * (resting ? 0.6f : 1f);

            var v = i * VertsPerFly;
            WriteQuad(v, fly.Position, Body, body);
            WriteQuad(v + 4, fly.Position, left, wing);
            WriteQuad(v + 8, fly.Position, right, wing);
        }

        void WriteQuad(int start, Vector3 position, Rect shape, Color color)
        {
            _offsets[start] = new Vector2(shape.xMin, shape.yMin);
            _offsets[start + 1] = new Vector2(shape.xMax, shape.yMin);
            _offsets[start + 2] = new Vector2(shape.xMax, shape.yMax);
            _offsets[start + 3] = new Vector2(shape.xMin, shape.yMax);
            for (var k = 0; k < 4; k++)
            {
                _vertices[start + k] = position;
                _colors[start + k] = color;
            }
        }

        void Step(ref Fly fly, float dt, bool opened, Vector3 top)
        {
            switch (fly.Mode)
            {
                case Mode.Away:
                    if (opened)
                    {
                        return;
                    }

                    fly.Timer -= dt;
                    if (fly.Timer <= 0f)
                    {
                        var dir = Random.insideUnitCircle.normalized;
                        fly.Mode = Mode.Arriving;
                        fly.Position = top + new Vector3(dir.x, 0f, dir.y) * Random.Range(1.5f, 3f) + Vector3.up * Random.Range(0.3f, 1.2f);
                        fly.Velocity = Vector3.zero;
                        fly.Target = SwarmPoint();
                        fly.Alpha = 0f;
                    }

                    return;

                case Mode.Arriving:
                    fly.Alpha = Mathf.Min(1f, fly.Alpha + dt / 0.5f);
                    Steer(ref fly, dt, _maxSpeed);
                    if ((fly.Target - fly.Position).sqrMagnitude < 0.15f * 0.15f)
                    {
                        fly.Mode = Mode.Flying;
                        fly.Life = Random.Range(_stay.x, _stay.y);
                        fly.Timer = 0f;
                    }

                    return;

                case Mode.Flying:
                    fly.Alpha = Mathf.Min(1f, fly.Alpha + dt / 0.5f);
                    fly.Timer -= dt;
                    if (fly.Timer <= 0f)
                    {
                        // 次の一飛び。前の勢いを少し残したまま、急に向きを変える。
                        fly.Target = SwarmPoint();
                        fly.Timer = Random.Range(0.08f, 0.35f);
                        fly.Velocity *= 0.4f;
                    }

                    Steer(ref fly, dt, _maxSpeed);
                    fly.Velocity += Random.insideUnitSphere * (6f * dt);
                    fly.Life -= dt;
                    if (fly.Life <= 0f)
                    {
                        Leave(ref fly);
                    }
                    else if (Random.value < _restChance * dt)
                    {
                        BeginRest(ref fly);
                    }

                    return;

                case Mode.Resting:
                    if (!fly.Landed)
                    {
                        // 面の正面に一度寄ってから、面へ降りる（箱の向こう側から箱を突き抜けて来ない）。
                        var surface = SurfacePosition(fly);
                        fly.Target = fly.Approaching ? surface + SurfaceNormal(fly) * 0.12f : surface;
                        Steer(ref fly, dt, _maxSpeed * (fly.Approaching ? 0.8f : 0.4f));
                        if (fly.Approaching)
                        {
                            // 回り込めずに箱の面に引っかかり続けたら、止まるのをやめてまた飛ぶ
                            // （そこから降りると、面へ向かう途中で箱を突き抜けることがある）。
                            fly.ApproachTime += dt;
                            if (fly.ApproachTime > 1.5f)
                            {
                                fly.Mode = Mode.Flying;
                                fly.Timer = 0f;
                                return;
                            }
                        }

                        var reach = fly.Approaching ? 0.04f : 0.012f;
                        if ((fly.Target - fly.Position).sqrMagnitude < reach * reach)
                        {
                            if (fly.Approaching)
                            {
                                fly.Approaching = false;
                            }
                            else
                            {
                                fly.Landed = true;
                                fly.Velocity = Vector3.zero;
                            }
                        }

                        return;
                    }

                    // 止まっている間は、面の上を歩いては休む。止まる点は部品に付いていくので、箱が揺れても浮かない。
                    // 箱が叩かれたら Startle で飛び立つ。
                    Crawl(ref fly, dt);
                    fly.Position = SurfacePosition(fly);

                    fly.Timer -= dt;
                    if (fly.Timer <= 0f)
                    {
                        TakeOff(ref fly);
                    }

                    return;

                case Mode.Leaving:
                    fly.Alpha -= dt / 0.5f;
                    Steer(ref fly, dt, _maxSpeed * 1.4f);
                    if (fly.Alpha <= 0f)
                    {
                        fly.Mode = Mode.Away;
                        fly.Alpha = 0f;
                        fly.Timer = Random.Range(1f, 4f);
                    }

                    return;
            }
        }

        void Steer(ref Fly fly, float dt, float maxSpeed)
        {
            // 目標の手前では減速する（止まるときに行き過ぎて震えない）。
            var desired = fly.Target - fly.Position;
            var distance = desired.magnitude;
            var wanted = distance > 1e-3f ? desired / distance * Mathf.Min(maxSpeed, distance * 6f) : Vector3.zero;
            fly.Velocity += (wanted - fly.Velocity) * Mathf.Min(1f, _acceleration * dt);
            if (fly.Velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                fly.Velocity = fly.Velocity.normalized * maxSpeed;
            }

            var previous = fly.Position;
            fly.Position += fly.Velocity * dt;

            // 箱を突き抜けない。面に降りる最後の一息は、降りる道が空いていることを止まり場を選ぶときに確かめてある。
            var landing = fly.Mode == Mode.Resting && !fly.Approaching;
            if (!landing)
            {
                KeepOutside(ref fly, previous);
            }
        }

        /// <summary>
        /// 箱の本当の形（全部の三角形）を突き抜けさせない。この一こまで動いた線分が面を横切ったら、横切る手前で止め、
        /// 面へ向かう速さを消す（面に沿って滑って回り込む）。外から来た蠅は面の外に残るので、
        /// 網が閉じているか、法線がどちらを向くかに左右されない。
        /// </summary>
        void KeepOutside(ref Fly fly, Vector3 previous)
        {
            var move = fly.Position - previous;
            var length = move.magnitude;
            if (_solid.Length == 0 || length < 1e-6f || (!_solidBounds.Contains(previous) && !_solidBounds.Contains(fly.Position)))
            {
                return;
            }

            if (!FirstHit(previous, fly.Position, out var distance, out var normal))
            {
                return;
            }

            var direction = move / length;
            fly.Position = previous + direction * Mathf.Max(0f, distance - 0.01f);

            // 面の、蠅が来た側を向く法線で、面へ向かう速さを消す。
            if (Vector3.Dot(normal, direction) > 0f)
            {
                normal = -normal;
            }

            var into = Vector3.Dot(fly.Velocity, normal);
            if (into < 0f)
            {
                fly.Velocity -= normal * into;
            }
        }

        /// <summary>線分 from→to が箱の網の面と最初に交わる所（from からの距離）と、その面の法線。</summary>
        bool FirstHit(Vector3 from, Vector3 to, out float distance, out Vector3 normal)
        {
            var move = to - from;
            var length = move.magnitude;
            distance = float.MaxValue;
            normal = Vector3.up;
            if (length < 1e-6f)
            {
                return false;
            }

            var direction = move / length;
            foreach (var tri in _solid)
            {
                if (tri.Space == null)
                {
                    continue;
                }

                if (RayTriangle(from, direction, tri.Space.TransformPoint(tri.A), tri.Space.TransformPoint(tri.B), tri.Space.TransformPoint(tri.C), out var t)
                    && t <= length && t < distance)
                {
                    distance = t;
                    normal = tri.Space.TransformDirection(tri.Normal).normalized;
                }
            }

            return distance <= length;
        }

        /// <summary>止まり場から外へ、降りてくる道（面の正面 12cm）が空いているか。埋もれた面や覆われた面には降りられない。</summary>
        bool LandingClear(Vector3 point, Vector3 normal) =>
            !FirstHit(point + normal * 0.006f, point + normal * 0.12f, out _, out _);

        void UpdateSolidBounds()
        {
            if (_outline == null)
            {
                return;
            }

            var first = true;
            foreach (var shell in _outline.Shells)
            {
                if (shell.Target == null)
                {
                    continue;
                }

                if (first)
                {
                    _solidBounds = shell.Target.bounds;
                    first = false;
                }
                else
                {
                    _solidBounds.Encapsulate(shell.Target.bounds);
                }
            }

            _solidBounds.Expand(0.1f);
        }

        /// <summary>止まっていた面から飛び立つ。面の法線の向き（少し上寄り）へ離れるので、ふたの下や横からでも箱を突き抜けない。</summary>
        void TakeOff(ref Fly fly)
        {
            var normal = fly.Landed ? SurfaceNormal(fly) : Vector3.up;
            fly.Mode = Mode.Flying;
            fly.Landed = false;
            fly.Velocity = (normal + Vector3.up * 0.4f).normalized * _maxSpeed * 0.8f;
            fly.Target = fly.Position + normal * 0.25f + Vector3.up * 0.15f;
            fly.Timer = Random.Range(0.15f, 0.3f);
        }

        void Leave(ref Fly fly)
        {
            var dir = Random.insideUnitCircle.normalized;
            fly.Mode = Mode.Leaving;
            fly.Target = fly.Position + new Vector3(dir.x, 0f, dir.y) * 3f + Vector3.up * Random.Range(0.5f, 1.5f);
        }

        void Scatter()
        {
            for (var i = 0; i < _flies.Length; i++)
            {
                if (_flies[i].Mode == Mode.Away)
                {
                    continue;
                }

                Leave(ref _flies[i]);
                _flies[i].Velocity = (_flies[i].Target - _flies[i].Position).normalized * _maxSpeed * 1.4f;
            }
        }

        /// <summary>箱が叩かれた。止まっていた蠅は飛び立ち、飛んでいた蠅は弾かれたように向きを変える。</summary>
        void Startle()
        {
            for (var i = 0; i < _flies.Length; i++)
            {
                if (_flies[i].Mode == Mode.Resting)
                {
                    TakeOff(ref _flies[i]);
                    continue;
                }

                if (_flies[i].Mode == Mode.Flying)
                {
                    _flies[i].Velocity = (Random.onUnitSphere + Vector3.up) * _maxSpeed;
                    _flies[i].Target = SwarmPoint() + Vector3.up * 0.3f;
                    _flies[i].Timer = Random.Range(0.2f, 0.4f);
                }
            }
        }

        Vector3 Top()
        {
            var b = _collider.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        Vector3 SwarmPoint()
        {
            var b = _collider.bounds;
            var r = Random.insideUnitCircle;
            return Top() + new Vector3(r.x * (b.extents.x + _swarmRadius), Random.Range(0.05f, _swarmHeight), r.y * (b.extents.z + _swarmRadius));
        }

        void BeginRest(ref Fly fly)
        {
            fly.Mode = Mode.Resting;
            fly.Timer = Random.Range(_restSeconds.x, _restSeconds.y);
            fly.Landed = false;
            fly.Approaching = true;
            fly.ApproachTime = 0f;
            fly.Crawling = false;
            fly.CrawlTimer = Random.Range(0.3f, 1f);
            fly.Heading = Random.Range(0f, 360f);
            fly.Tri = -1;
            for (var attempt = 0; attempt < 6 && _tris.Length > 0; attempt++)
            {
                var pick = Random.value * _triTotal;
                var i = 0;
                while (i < _triWeights.Length - 1 && pick > _triWeights[i])
                {
                    pick -= _triWeights[i];
                    i++;
                }

                var tri = _tris[i];
                if (tri.Space == null)
                {
                    continue;
                }

                var u = Random.value;
                var v = Random.value;
                if (u + v > 1f)
                {
                    u = 1f - u;
                    v = 1f - v;
                }

                var local = tri.A + (tri.B - tri.A) * u + (tri.C - tri.A) * v;
                var world = tri.Space.TransformPoint(local);
                var normal = tri.Space.TransformDirection(tri.Normal).normalized;
                if (Covered(world + normal * 0.02f, tri.Space.GetComponent<Renderer>(), _outline) || !LandingClear(world, normal))
                {
                    continue;
                }

                fly.Tri = i;
                fly.Local = local;
                return;
            }
        }

        /// <summary>
        /// 面の上を歩く。歩いては休み、歩き出すたびに向きを少し変える。一歩ごとに、同じ部品の止まれる面のうち一番近い点へ吸い付けるので、
        /// 角を越えても、でこぼこの面でも浮かず沈まない。近くに止まれる面が無ければ（ふたの下や底の縁）引き返す。
        /// </summary>
        void Crawl(ref Fly fly, float dt)
        {
            fly.CrawlTimer -= dt;
            if (fly.CrawlTimer <= 0f)
            {
                fly.Crawling = !fly.Crawling;
                fly.CrawlTimer = fly.Crawling ? Random.Range(0.2f, 0.6f) : Random.Range(0.3f, 1.2f);
                if (fly.Crawling)
                {
                    fly.Heading += Random.Range(-70f, 70f);
                }
            }

            if (!fly.Crawling || fly.Tri < 0 || fly.Tri >= _tris.Length || _tris[fly.Tri].Space == null)
            {
                return;
            }

            var current = _tris[fly.Tri];
            var space = current.Space;
            Frame(current.Normal, out var tangent, out var bitangent);
            var radians = fly.Heading * Mathf.Deg2Rad;
            var direction = space.TransformDirection(tangent * Mathf.Cos(radians) + bitangent * Mathf.Sin(radians)).normalized;
            var candidate = fly.Local + space.InverseTransformVector(direction * (_crawlSpeed * dt));

            var best = -1;
            var bestPoint = candidate;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < _tris.Length; i++)
            {
                if (_tris[i].Space != space)
                {
                    continue;
                }

                var point = ClosestPointOnTriangle(candidate, _tris[i].A, _tris[i].B, _tris[i].C);
                var distance = (point - candidate).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point;
                    best = i;
                }
            }

            if (best < 0 || space.TransformVector(bestPoint - candidate).magnitude > 0.01f)
            {
                fly.Heading += 180f;
                return;
            }

            fly.Tri = best;
            fly.Local = bestPoint;
        }

        static void Frame(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            bitangent = Vector3.Cross(normal, tangent);
        }

        /// <summary>三角形 abc の上で p に一番近い点（Ericson『Real-Time Collision Detection』5.1.5）。</summary>
        static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return a;
            }

            var bp = p - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return b;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                return a + ab * (d1 / (d1 - d3));
            }

            var cp = p - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return c;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                return a + ac * (d2 / (d2 - d6));
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
            }

            var denominator = 1f / (va + vb + vc);
            return a + ab * (vb * denominator) + ac * (vc * denominator);
        }

        bool OnSurface(Fly fly) => fly.Tri >= 0 && fly.Tri < _tris.Length && _tris[fly.Tri].Space != null;

        /// <summary>止まる点（面からわずかに浮かせる）。止まれる面を作れなかった箱は、当たり判定の天面に止まる。</summary>
        Vector3 SurfacePosition(Fly fly)
        {
            if (!OnSurface(fly))
            {
                return Top() + Vector3.up * 0.005f;
            }

            return _tris[fly.Tri].Space.TransformPoint(fly.Local) + SurfaceNormal(fly) * 0.004f;
        }

        Vector3 SurfaceNormal(Fly fly) =>
            OnSurface(fly) ? _tris[fly.Tri].Space.TransformDirection(_tris[fly.Tri].Normal).normalized : Vector3.up;
    }
}
