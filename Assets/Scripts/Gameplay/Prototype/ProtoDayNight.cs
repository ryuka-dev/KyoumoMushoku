using System;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Gameplay.DayCycle;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片の時計。本編と同じ Core の <see cref="GameClock"/>（ソフトクロック）を持ち、実時間で進める。
    /// 昼の長さは <c>Assets/Config/DaySchedule.asset</c>。夜に上限は無く、寝るまで夜が続く（第二節）。
    /// 日付が変わるのは就寝の瞬間だけで、それは <see cref="ProtoSession"/> が <see cref="BeginNextDay"/> を呼んで起こす。
    ///
    /// 太陽の色と強さ・空と霧の色・環境光・街灯は <see cref="GameClock.NightBlend01"/> で補間する（表現であって権威ではない）。
    /// </summary>
    public sealed class ProtoDayNight : MonoBehaviour
    {
        [SerializeField] Light _sun;
        [SerializeField] DayScheduleAsset _schedule;
        [SerializeField] Color _dayColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] Color _nightColor = new Color(0.35f, 0.42f, 0.7f);
        [SerializeField, Min(0f)] float _dayIntensity = 1.0f;
        [SerializeField, Min(0f)] float _nightIntensity = 0.25f;
        [Header("空と環境光（上・横・下）")]
        [SerializeField] Camera _camera;
        [SerializeField] Color _daySky = new Color(0.62f, 0.7f, 0.78f);
        [SerializeField] Color _nightSky = new Color(0.04f, 0.05f, 0.1f);
        [SerializeField] Color _dayAmbientSky = new Color(0.3f, 0.33f, 0.4f);
        [SerializeField] Color _dayAmbientEquator = new Color(0.2f, 0.2f, 0.22f);
        [SerializeField] Color _dayAmbientGround = new Color(0.1f, 0.09f, 0.08f);
        [SerializeField] Color _nightAmbientSky = new Color(0.08f, 0.1f, 0.18f);
        [SerializeField] Color _nightAmbientEquator = new Color(0.05f, 0.06f, 0.1f);
        [SerializeField] Color _nightAmbientGround = new Color(0.03f, 0.03f, 0.04f);
        [Tooltip("夕方から灯る明かり（街灯・店の窓）。強さは置いたときの値まで、暮れるにつれて上がる。")]
        [SerializeField] Light[] _nightLights = new Light[0];

        float[] _nightLightIntensity;
        DaySchedule _days;

        bool _wasNight;

        public GameClock Clock { get; private set; }

        /// <summary>今夜、日が暮れきってから何秒たったか。夜でなければ 0。</summary>
        public float SecondsSinceNightfall =>
            Clock != null && IsNight ? Clock.ElapsedInDay - _days.ForDay(Clock.Day).SecondsUntilNight : 0f;

        /// <summary>産出テーブルの夜版を使う時間帯か。夕方はまだ昼の扱い。</summary>
        public bool IsNight => Clock != null && Clock.Phase == DayPhase.Night;

        /// <summary>昼と夜が入れ替わった、または新しい一日が始まった。</summary>
        public event Action Changed;

        public void Configure(Light sun, Camera camera, DayScheduleAsset schedule, Light[] nightLights)
        {
            _sun = sun;
            _camera = camera;
            _schedule = schedule;
            _nightLights = nightLights ?? new Light[0];
        }

        void Awake()
        {
            _days = _schedule != null
                ? _schedule.ToSchedule()
                : new DaySchedule(new[] { new DayPhaseDurations(150f, 60f) });
            Clock = new GameClock(_days);
            Clock.PhaseChanged += OnPhaseChanged;
            Clock.DayBegan += OnDayBegan;

            _nightLightIntensity = new float[_nightLights.Length];
            for (var i = 0; i < _nightLights.Length; i++)
            {
                _nightLightIntensity[i] = _nightLights[i] != null ? _nightLights[i].intensity : 0f;
            }
        }

        void OnDestroy()
        {
            if (Clock != null)
            {
                Clock.PhaseChanged -= OnPhaseChanged;
                Clock.DayBegan -= OnDayBegan;
            }
        }

        void Update()
        {
            Clock.Advance(Time.deltaTime);
            Apply();
        }

        /// <summary>就寝による日付の変わり目。<see cref="ProtoSession"/> だけが呼ぶ。</summary>
        public void BeginNextDay() => Clock.BeginNextDay();

        void OnPhaseChanged(DayPhase phase) => NotifyIfNightFlipped();

        void OnDayBegan(int day)
        {
            _wasNight = IsNight;
            Changed?.Invoke();
        }

        void NotifyIfNightFlipped()
        {
            if (IsNight == _wasNight)
            {
                return;
            }

            _wasNight = IsNight;
            Changed?.Invoke();
        }

        void Apply()
        {
            var t = Clock.NightBlend01;
            if (_sun != null)
            {
                _sun.color = Color.Lerp(_dayColor, _nightColor, t);
                _sun.intensity = Mathf.Lerp(_dayIntensity, _nightIntensity, t);
            }

            var skyColor = Color.Lerp(_daySky, _nightSky, t);
            if (_camera != null)
            {
                _camera.backgroundColor = skyColor;
            }

            // 霧は空と同じ色にする。遠くの建物が空へ溶け、夜は闇が濃くなる（霧の距離はシーンの照明設定）。
            RenderSettings.fogColor = skyColor;

            // 実行中は環境光の色を変えても球面調和（ambientProbe）が追従しないので、上・横・下の三色から直接組む。
            var sky = Color.Lerp(_dayAmbientSky, _nightAmbientSky, t);
            var equator = Color.Lerp(_dayAmbientEquator, _nightAmbientEquator, t);
            var ground = Color.Lerp(_dayAmbientGround, _nightAmbientGround, t);
            var probe = new UnityEngine.Rendering.SphericalHarmonicsL2();
            probe.AddAmbientLight(equator);
            probe.AddDirectionalLight(Vector3.down, (sky - equator) * 0.5f, 1f);
            probe.AddDirectionalLight(Vector3.up, (ground - equator) * 0.5f, 1f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Custom;
            RenderSettings.ambientProbe = probe;

            for (var i = 0; i < _nightLights.Length; i++)
            {
                if (_nightLights[i] == null)
                {
                    continue;
                }

                _nightLights[i].enabled = t > 0.05f;
                _nightLights[i].intensity = _nightLightIntensity[i] * t;
            }
        }
    }
}
