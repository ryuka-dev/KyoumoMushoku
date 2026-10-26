using System.Collections.Generic;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の手の指を曲げる（試作）。模型は部品分割の剛体で、指は関節ごとの別物体
    /// （<c>Tools/Models/gen/hands.py</c>）。握り具合 0（開き）〜1（拳）で、指を内へ回す。
    /// </summary>
    public sealed class ProtoViewHand : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float _curl = 1f;
        [SerializeField, Min(1f)] float _curlSpeed = 14f;
        [SerializeField] float _fingerCurlDegrees = 90f;
        [SerializeField] float _thumbCurlDegrees = -50f;

        readonly List<(Transform joint, Quaternion rest)> _fingers = new List<(Transform, Quaternion)>();
        Transform _thumb;
        Quaternion _thumbRest;
        float _target = 1f;

        /// <summary>掌の中心（掴んだ物を寄せる先）。</summary>
        public Transform Palm { get; private set; }

        /// <summary>目標の握り具合。実際の値はこれへ素早く追いつく。</summary>
        public void SetCurl(float curl) => _target = Mathf.Clamp01(curl);

        public void SnapCurl(float curl)
        {
            _target = Mathf.Clamp01(curl);
            _curl = _target;
            Apply();
        }

        void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Finger", System.StringComparison.Ordinal))
                {
                    _fingers.Add((t, t.localRotation));
                }
                else if (t.name.StartsWith("Thumb", System.StringComparison.Ordinal))
                {
                    _thumb = t;
                    _thumbRest = t.localRotation;
                }
                else if (t.name.StartsWith("Palm", System.StringComparison.Ordinal))
                {
                    var anchor = new GameObject("PalmAnchor").transform;
                    anchor.SetParent(transform, false);
                    // 掌の中心は肘（原点）から前へ約 0.33m（hands.py の寸法）。
                    anchor.localPosition = new Vector3(0f, 0f, 0.33f);
                    Palm = anchor;
                }
            }

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _target = _curl;
            Apply();
        }

        void Update()
        {
            _curl = Mathf.MoveTowards(_curl, _target, Time.unscaledDeltaTime * _curlSpeed);
            Apply();
        }

        void Apply()
        {
            // Blender の X 軸まわり +90°（内へ曲げる）。子の部品は Blender の軸のまま来る（親だけが軸変換の回転を持つ）ので、
            // 符号もそのまま（2026-10-26 実測：逆にすると指が反り返る）。
            var finger = Quaternion.Euler(_fingerCurlDegrees * _curl, 0f, 0f);
            foreach (var (joint, rest) in _fingers)
            {
                joint.localRotation = rest * finger;
            }

            if (_thumb != null)
            {
                _thumb.localRotation = _thumbRest * Quaternion.Euler(0f, 0f, _thumbCurlDegrees * _curl);
            }
        }
    }
}
