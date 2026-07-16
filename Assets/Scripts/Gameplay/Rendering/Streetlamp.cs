using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace KyoumoMushoku.Gameplay.Rendering
{
    /// <summary>
    /// 夜に灯る街灯。灯り一つひとつが自分の基準強度を持ち、有効な間だけ自分を名簿に載せる。
    ///
    /// 誰が街灯であるかを知っているのは街灯自身であり、<see cref="DayNightLight2D"/> は名簿を読んで
    /// 移行度を掛けるだけである。これにより prefab を置いた時点で夕暮に灯る。配線の手作業は要らず、
    /// 街灯を消しても名簿に穴（NULL 参照）が残らない。
    ///
    /// 名簿は静的だが、載る／降りるは OnEnable／OnDisable と対であり、寿命はこの型の中で閉じている
    /// （第十三節）。見た目の値だけを動かし、ゲームプレイの状態には一切触れない（第七節）。
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class Streetlamp : MonoBehaviour
    {
        static readonly List<Streetlamp> Registry = new();

        /// <summary>いま灯りうる街灯。読むのは <see cref="DayNightLight2D"/> だけである。</summary>
        public static IReadOnlyList<Streetlamp> Active => Registry;

        [SerializeField] Light2D _light;

        [Tooltip("夜（移行度 1）で灯りきったときの強度。昼（移行度 0）では消灯する。")]
        [SerializeField] float _baseIntensity = 2.4f;

        /// <summary>
        /// Inspector で付けたとき、すでに作り込んである灯りの強度をそのまま基準として引き継ぐ。
        /// 既定値で上書きすると、手で決めた明るさが黙って変わってしまうためである。
        /// </summary>
        void Reset()
        {
            _light = GetComponent<Light2D>();
            if (_light != null)
            {
                _baseIntensity = _light.intensity;
            }
        }

        /// <summary>ビルダー（シーン生成の単一の所有者）だけが呼ぶ。</summary>
        public void Configure(Light2D light, float baseIntensity)
        {
            _light = light;
            _baseIntensity = baseIntensity;
        }

        void OnEnable()
        {
            if (_light == null)
            {
                _light = GetComponent<Light2D>();
            }

            Registry.Add(this);
        }

        void OnDisable() => Registry.Remove(this);

        /// <summary>
        /// 移行度（昼 0 → 夜 1）を灯りに反映する。呼ぶのは <see cref="DayNightLight2D"/> だけである。
        /// </summary>
        public void ApplyBlend(float blend)
        {
            if (_light != null)
            {
                _light.intensity = _baseIntensity * blend;
            }
        }

        // 「ドメインリロードなしで再生」を選んでいると、静的な名簿は前回の再生の亡霊を抱えたまま始まる。
        // 再生の最初に必ず空へ戻す。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => Registry.Clear();
    }
}
