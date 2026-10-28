using System;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 触れる物（ゴミ箱・自販機・水道・ベンチ）の縁取り。初めての人が「これは触れる」と一目で分かるようにする。
    /// 照準の輪と同じ言葉で話す：手が届いて狙っていればはっきり、近くにあれば細く、遠ざかると消える。
    /// 色は探る物が暖色・使う物が寒色（<see cref="ProtoInteractColors"/>。焼くときに物ごとに決める）。
    /// 開けたゴミ箱はもう触れる物ではないので縁を消す。
    ///
    /// 描く物の無い場所（自販機の下・コンビニの戸口）は、そこに見えている面のすぐ手前に置いた枠（<see cref="FrameName"/>）を
    /// 殻の網として縁を出す（壁や本体に平らに付いた場所は、裏返しの殻では縁が出ないため）。
    ///
    /// 縁は裏返しの殻（<c>KyoumoMushoku/ProtoOutline</c>）。殻の網は編集時に焼いてある（<c>SliceOutlineBaker</c>）。
    /// 殻は各描画物の子に置くので、叩かれて揺れる箱にも付いていく。当たり判定は持たない。
    /// </summary>
    public sealed class ProtoOutline : MonoBehaviour
    {
        [Serializable]
        public struct Shell
        {
            public Renderer Target;
            public Mesh Mesh;
        }

        /// <summary>描く物の無い触れる場所に置く枠の名前（枠そのものは描かず、殻の網として使う）。</summary>
        public const string FrameName = "OutlineFrame";

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int WidthId = Shader.PropertyToID("_Width");

        [SerializeField] ProtoHands _hands;
        [SerializeField] Material _material;
        [SerializeField] Shell[] _shells = new Shell[0];

        [Header("見え方")]
        [SerializeField] Color _color = new Color(1f, 0.78f, 0.35f, 1f);
        [Tooltip("この距離までは縁がいちばん濃い（狙っていないとき）。")]
        [SerializeField, Min(0f)] float _fullDistance = 10f;
        [Tooltip("この距離で縁が消える。")]
        [SerializeField, Min(0f)] float _fadeDistance = 18f;
        [Tooltip("狙っていないときの濃さ。")]
        [SerializeField, Range(0f, 1f)] float _idleAlpha = 0.85f;
        [Tooltip("狙っていないときの幅（ピクセル）。")]
        [SerializeField, Min(0f)] float _idleWidth = 2.5f;
        [Tooltip("手が届いて狙っているときの幅（ピクセル）。")]
        [SerializeField, Min(0f)] float _aimedWidth = 4f;
        [Tooltip("濃さが切り替わるのにかかる秒数。")]
        [SerializeField, Min(0.01f)] float _fadeSeconds = 0.15f;

        ProtoTrashCan _can;
        Renderer[] _outlines = new Renderer[0];
        MaterialPropertyBlock _block;
        float _alpha;
        float _width;

        /// <summary>殻の一覧。殻の網は元の網と同じ頂点で読み取りができるので、蠅が止まる面を探すのにも使う。</summary>
        public System.Collections.Generic.IReadOnlyList<Shell> Shells => _shells;

        public void Configure(ProtoHands hands, Material material, Shell[] shells)
        {
            _hands = hands;
            _material = material;
            _shells = shells ?? new Shell[0];
        }

        void Awake()
        {
            _can = GetComponent<ProtoTrashCan>();
            _block = new MaterialPropertyBlock();
            _width = _idleWidth;
            if (_material == null)
            {
                return;
            }

            var built = new System.Collections.Generic.List<Renderer>(_shells.Length);
            foreach (var shell in _shells)
            {
                if (shell.Target == null || shell.Mesh == null)
                {
                    continue;
                }

                var go = new GameObject("Outline");
                go.layer = shell.Target.gameObject.layer;
                go.transform.SetParent(shell.Target.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = shell.Mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.enabled = false;
                built.Add(r);
            }

            _outlines = built.ToArray();
        }

        void LateUpdate()
        {
            if (_outlines.Length == 0)
            {
                return;
            }

            var camera = Camera.main;
            var target = 0f;
            var width = _idleWidth;
            var available = _can == null || !_can.IsOpened;
            if (available && camera != null)
            {
                // 狙われているのが自分そのものか（自販機と、その下の小銭探しは別々に光る）。
                var aimed = _hands != null && _hands.Aim == ProtoHands.AimState.InReach && _hands.AimedObject != null
                            && _hands.AimedObject.gameObject == gameObject;
                if (aimed)
                {
                    target = 1f;
                    width = _aimedWidth;
                }
                else
                {
                    var distance = Vector3.Distance(camera.transform.position, ClosestPoint(camera.transform.position));
                    target = _idleAlpha * (1f - Mathf.InverseLerp(_fullDistance, Mathf.Max(_fullDistance + 0.01f, _fadeDistance), distance));
                }
            }

            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime / _fadeSeconds);
            _width = Mathf.MoveTowards(_width, width, Time.unscaledDeltaTime / _fadeSeconds * Mathf.Max(1f, _aimedWidth));
            var visible = _alpha > 0.01f;
            var color = _color;
            color.a *= _alpha;
            _block.SetColor(ColorId, color);
            _block.SetFloat(WidthId, _width);
            foreach (var r in _outlines)
            {
                if (r == null)
                {
                    continue;
                }

                r.enabled = visible;
                if (visible)
                {
                    r.SetPropertyBlock(_block);
                }
            }
        }

        Vector3 ClosestPoint(Vector3 from)
        {
            var best = transform.position;
            var bestDistance = float.MaxValue;
            foreach (var shell in _shells)
            {
                if (shell.Target == null)
                {
                    continue;
                }

                // 枠は描かない（描画が無効）ので、描画の範囲ではなく置いた位置で測る。
                var t = shell.Target.transform;
                var bounds = shell.Target.enabled
                    ? shell.Target.bounds
                    : new Bounds(t.position, new Vector3(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z)));
                var p = bounds.ClosestPoint(from);
                var d = (p - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }

            return best;
        }
    }
}
