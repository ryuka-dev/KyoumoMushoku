using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// SAN に応じて世界の彩度を落とす（第三節：SAN は都市を読む力。低いほど都市は色を失う）。
    /// 70 で満色、0 で完全なモノクロ。曲線は Core の <c>SanityScale.Saturation</c>。
    ///
    /// 本編の <c>SanityColorGrade</c> と同じ作り（実行時に作る全域ボリュームの ColorAdjustments）で、
    /// 読む先だけが一人称の <see cref="ProtoVitals"/>。HUD（IMGUI）はポスト処理の後に描かれるので退色しない。
    /// カメラ側で renderPostProcessing を有効にしておくこと。
    /// </summary>
    public sealed class ProtoSanityGrade : MonoBehaviour
    {
        [SerializeField] ProtoVitals _vitals;
        [Tooltip("退色の追従の速さ。急変を少しなだらかにする。")]
        [SerializeField] float _smoothing = 6f;

        Volume _volume;
        ColorAdjustments _colorAdjustments;
        float _saturation01 = 1f;

        public void Configure(ProtoVitals vitals) => _vitals = vitals;

        void Awake()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ProtoSanityGrade (runtime)";
            _volume.sharedProfile = profile;
            _colorAdjustments = profile.Add<ColorAdjustments>(overrides: true);
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value = 0f;
        }

        void OnDestroy()
        {
            if (_volume != null && _volume.sharedProfile != null)
            {
                Destroy(_volume.sharedProfile);
            }
        }

        void Update()
        {
            if (_vitals == null || _vitals.Vitals == null)
            {
                return;
            }

            var target = _vitals.Vitals.Saturation;
            _saturation01 = Mathf.Lerp(_saturation01, target, 1f - Mathf.Exp(-_smoothing * Time.unscaledDeltaTime));
            // ColorAdjustments.saturation は -100（無彩色）〜0（等倍）。
            _colorAdjustments.saturation.value = (_saturation01 - 1f) * 100f;
        }
    }
}
