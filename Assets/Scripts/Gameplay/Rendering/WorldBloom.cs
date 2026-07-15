using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KyoumoMushoku.Gameplay.Rendering
{
    /// <summary>
    /// 夜の街灯やコンビニの灯りをにじませる Bloom。閾値を超える明るい部分だけが光をこぼす。
    /// 静的な演出であり時刻には追従しない（灯りそのものの明るさは <see cref="DayNightLight2D"/> が動かす）。
    ///
    /// URP のポスト処理を実行時に生成した全域ボリュームで駆動する（<see cref="SanityColorGrade"/> と同方式）。
    /// カメラ側で renderPostProcessing を有効にしておくこと。
    /// </summary>
    public sealed class WorldBloom : MonoBehaviour
    {
        // 灰箱の面はもともと明るく、にじみを強くすると夜が白く霞む。閾値は高め・散りは控えめにして、
        // 街灯の芯だけが光をこぼすようにする。
        [SerializeField] float _threshold = 1.1f;
        [SerializeField] float _intensity = 0.35f;
        [SerializeField] float _scatter = 0.2f;

        Volume _volume;

        void Awake()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 5f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "WorldBloom (runtime)";
            _volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(overrides: true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = _threshold;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = _intensity;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = _scatter;
        }

        void OnDestroy()
        {
            if (_volume != null && _volume.sharedProfile != null)
            {
                Destroy(_volume.sharedProfile);
            }
        }
    }
}
