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
                        fly.Mode = Mode.Resting;
                        fly.Target = LidPoint();
                        fly.Timer = Random.Range(0.6f, 2.5f);
                        fly.Landed = false;
                    }

                    return;

                case Mode.Resting:
                    if (!fly.Landed)
                    {
                        Steer(ref fly, dt, _maxSpeed * 0.6f);
                        if ((fly.Target - fly.Position).sqrMagnitude < 0.02f * 0.02f)
                        {
                            fly.Landed = true;
                            fly.Velocity = Vector3.zero;
                        }

                        return;
                    }

                    // 止まっている間も、ときどき数ミリだけ歩く。箱が叩かれたら Startle で飛び立つ。
                    fly.Position = fly.Target;
                    if (Random.value < 2f * dt)
                    {
                        fly.Target += new Vector3(Random.Range(-0.01f, 0.01f), 0f, Random.Range(-0.01f, 0.01f));
                    }

                    fly.Timer -= dt;
                    if (fly.Timer <= 0f)
                    {
                        fly.Mode = Mode.Flying;
                        fly.Velocity = Vector3.up * _maxSpeed * 0.8f;
                        fly.Timer = Random.Range(0.08f, 0.2f);
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

            fly.Position += fly.Velocity * dt;

            // 箱の中へは潜らない（天面より下に落ちたら天面へ戻す。ただし飛び去る途中は好きにさせる）。
            if (fly.Mode != Mode.Leaving && fly.Mode != Mode.Arriving)
            {
                var floor = Top().y + 0.01f;
                if (fly.Position.y < floor && InsideFootprint(fly.Position))
                {
                    fly.Position.y = floor;
                    fly.Velocity.y = Mathf.Abs(fly.Velocity.y);
                }
            }
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
                    _flies[i].Mode = Mode.Flying;
                    _flies[i].Timer = 0f;
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

        bool InsideFootprint(Vector3 p)
        {
            var b = _collider.bounds;
            return p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z;
        }

        Vector3 SwarmPoint()
        {
            var b = _collider.bounds;
            var r = Random.insideUnitCircle;
            return Top() + new Vector3(r.x * (b.extents.x + _swarmRadius), Random.Range(0.05f, _swarmHeight), r.y * (b.extents.z + _swarmRadius));
        }

        Vector3 LidPoint()
        {
            var b = _collider.bounds;
            return Top() + new Vector3(Random.Range(-0.6f, 0.6f) * b.extents.x, 0.005f, Random.Range(-0.6f, 0.6f) * b.extents.z);
        }
    }
}
