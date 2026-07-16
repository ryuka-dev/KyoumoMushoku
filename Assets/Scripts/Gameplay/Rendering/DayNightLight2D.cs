using KyoumoMushoku.Gameplay.DayCycle;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace KyoumoMushoku.Gameplay.Rendering
{
    /// <summary>
    /// 時刻に応じてワールドの灯りを動かす。昼は重工業都市の曇天（沈んだ寒色・低い明度）、
    /// 薄暮でナトリウム灯の橙、夜は深い青。街灯は夜に向けて灯り、昼には消える。
    ///
    /// 権威は <see cref="GameClock"/> であり、この表現層はそれを毎フレーム読むだけで時刻を所有しない
    /// （<see cref="SanityColorGrade"/> が SAN を読むのと同じ姿勢）。
    /// 見た目の値はここで計算するだけで、ゲームプレイの状態は一切変えない（第七節）。
    /// </summary>
    public sealed class DayNightLight2D : MonoBehaviour
    {
        [SerializeField] GameClockDriver _clock;
        [SerializeField] Light2D _global;
        [SerializeField] Camera _camera;

        [Header("昼（曇天・重工業の空）")]
        [SerializeField] Color _dayColor = new Color(0.60f, 0.64f, 0.67f);
        [SerializeField] float _dayIntensity = 0.92f;
        [SerializeField] Color _daySky = new Color(0.60f, 0.63f, 0.66f);

        [Header("薄暮（ナトリウム灯の橙）")]
        [SerializeField] Color _duskColor = new Color(0.85f, 0.55f, 0.42f);
        [SerializeField] float _duskIntensity = 0.72f;
        [SerializeField] Color _duskSky = new Color(0.33f, 0.28f, 0.32f);

        [Header("夜（深い青）")]
        [SerializeField] Color _nightColor = new Color(0.34f, 0.40f, 0.62f);
        [SerializeField] float _nightIntensity = 0.50f;
        [SerializeField] Color _nightSky = new Color(0.07f, 0.08f, 0.12f);

        [Tooltip("移行の追従の速さ。日境界（夜→朝）の急変を少しなだらかにする。")]
        [SerializeField] float _smoothing = 4f;

        float _blend;
        bool _initialized;
        Color _sky;

        /// <summary>
        /// 現在の空色。カメラの背景色そのものであり、<see cref="SkyFogPlane"/> が奥の層を
        /// 溶かす先として読む。決めるのはここだけで、読み手は所有しない。
        ///
        /// まだ一度も時刻を読んでいない間（編集中、および再生の初回 Update 前）は昼の空を返す。
        /// 既定値の黒を返すと、編集中の霧板が真っ黒に沈んで絵を確認できないためである。
        /// </summary>
        public Color SkyColor => _initialized ? _sky : _daySky;

        /// <summary>
        /// ビルダー（シーン生成の単一の所有者）だけが呼ぶ。
        /// 街灯はここでは受け取らない。誰が街灯かは <see cref="Streetlamp"/> が自ら名乗る。
        /// </summary>
        public void Configure(GameClockDriver clock, Light2D global, Camera camera)
        {
            _clock = clock;
            _global = global;
            _camera = camera;
        }

        void OnEnable() => _initialized = false;

        void Update()
        {
            if (_clock == null || _clock.Clock == null || _global == null)
            {
                return;
            }

            float target = _clock.Clock.NightBlend01;
            if (!_initialized)
            {
                _blend = target;
                _initialized = true;
            }
            else
            {
                _blend = Mathf.Lerp(_blend, target, 1f - Mathf.Exp(-_smoothing * Time.deltaTime));
            }

            // 昼 → 薄暮 → 夜 の3点補間。中点(0.5)を薄暮に取る。
            Color color;
            float intensity;
            Color sky;
            if (_blend < 0.5f)
            {
                float t = _blend / 0.5f;
                color = Color.Lerp(_dayColor, _duskColor, t);
                intensity = Mathf.Lerp(_dayIntensity, _duskIntensity, t);
                sky = Color.Lerp(_daySky, _duskSky, t);
            }
            else
            {
                float t = (_blend - 0.5f) / 0.5f;
                color = Color.Lerp(_duskColor, _nightColor, t);
                intensity = Mathf.Lerp(_duskIntensity, _nightIntensity, t);
                sky = Color.Lerp(_duskSky, _nightSky, t);
            }

            _global.color = color;
            _global.intensity = intensity;
            _sky = sky;
            if (_camera != null)
            {
                _camera.backgroundColor = sky;
            }

            // 街灯は夜へ向けて灯る。基準強度に移行度を掛けるだけ（昼=0=消灯）。
            // 誰が街灯かは街灯自身が名乗るため、ここは名簿を読むだけで配線を持たない。
            // 置かれたばかりの prefab も、その場で名簿に載っているので何もせずに灯る。
            var lamps = Streetlamp.Active;
            for (int i = 0; i < lamps.Count; i++)
            {
                lamps[i].ApplyBlend(_blend);
            }
        }
    }
}
