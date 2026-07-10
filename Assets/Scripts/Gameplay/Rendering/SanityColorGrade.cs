using KyoumoMushoku.Gameplay.Survival;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KyoumoMushoku.Gameplay.Rendering
{
    /// <summary>
    /// SAN に応じてワールドの彩度を落とす（第三節）。SAN は都市を読む力であり、
    /// 低いほど都市は色を失う。退色は 70 で満色、0 で完全なモノクロ。
    ///
    /// 退色はワールドの描画にのみかかる。UI は Screen Space - Overlay で描かれ、
    /// ポスト処理の後段にあるため一切退色しない。情報の欠落は `??` という明示的な形でのみ起こし、
    /// 退色の副作用としては決して起こさない。プレイヤーに画面を凝視させるのは事故である。
    ///
    /// URP のポスト処理（ColorAdjustments）を実行時に生成した全域ボリュームで駆動する。
    /// カメラ側で renderPostProcessing を有効にしておくこと。
    /// </summary>
    public sealed class SanityColorGrade : MonoBehaviour
    {
        [SerializeField] PlayerVitals _vitals;

        [Tooltip("退色の追従の速さ。急変を少しなだらかにする。")]
        [SerializeField] float _smoothing = 6f;

        Volume _volume;
        ColorAdjustments _colorAdjustments;
        float _saturation01 = 1f;

        public void Configure(PlayerVitals vitals) => _vitals = vitals;

        void Awake()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "SanityColorGrade (runtime)";
            _volume.sharedProfile = profile;

            _colorAdjustments = profile.Add<ColorAdjustments>(overrides: true);
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value = 0f;

            if (_vitals == null)
            {
                _vitals = FindFirstObjectByType<PlayerVitals>();
            }
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
            if (_vitals == null || _vitals.Vitals == null || _colorAdjustments == null)
            {
                return;
            }

            var target = _vitals.Vitals.Saturation;
            _saturation01 = Mathf.Lerp(_saturation01, target, 1f - Mathf.Exp(-_smoothing * Time.deltaTime));

            // ColorAdjustments.saturation は -100（無彩色）〜0（等倍）。満色 1 → 0、モノクロ 0 → -100。
            _colorAdjustments.saturation.value = (_saturation01 - 1f) * 100f;
        }
    }
}
