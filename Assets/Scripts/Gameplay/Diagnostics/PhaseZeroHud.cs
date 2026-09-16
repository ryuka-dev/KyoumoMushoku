using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Police;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Diagnostics
{
    /// <summary>
    /// Phase 0 の検証用オーバーレイ。読み取り専用であり、ゲームプレイには一切影響しない。
    /// 日本語フォントアセットがまだ無いため、表示は ASCII に限る。
    ///
    /// 警戒度と段階もここに出すが、これは開発用のプローブである。プレイヤーにとって
    /// 警戒度は見えない状態であり続けなければならない。それを世界の中で説明するのは
    /// 警官の台詞と先輩ホームレスの小言であって、この数字ではない（第十四節）。
    ///
    /// 既定では隠しておき、F1 で開閉する。開発用のプローブなので、通常のプレイ画面には出さない。
    ///
    /// 密度の計測（<see cref="DensityRecorder"/>）の所有者でもある。記録は表示と独立に走り、
    /// F2 でその場の報告を書き出す。終了時にも書き出す。
    /// </summary>
    public sealed class PhaseZeroHud : MonoBehaviour
    {
        [Tooltip("開発用オーバーレイの表示。既定は非表示で、F1 で切り替える。")]
        [SerializeField] bool _visible;
        [SerializeField] GameClockDriver _clock;
        [SerializeField] ZoneTracker _zones;
        [SerializeField] PlayerMotor _motor;
        [SerializeField] Transform _player;
        [SerializeField] ZoneAlertDirector _alerts;
        [SerializeField] PoliceOfficer _officer;

        GUIStyle _style;

        // 密度の計測（作業計画 第四節 M1）。積み上げと報告の整形は DensityRecorder / DensityLog が持ち、
        // ここはその所有者である。本番シーンは手の所有物になったのでビルダーが部品を足せない。既に
        // シーンに居るこのオーバーレイが生成し、駆動し、捨てるのが、配線を増やさない唯一の道である。
        //
        // 記録はオーバーレイの表示とは独立に走る。F1 で隠していても数え続ける——計測のために
        // 開発用の表示を出しっぱなしにしなければならない、というのでは遊びの手触りが変わってしまう。
        readonly DensityRecorder _density = new DensityRecorder();

        PlayerInteractor _interactor;

        public void Configure(GameClockDriver clock, ZoneTracker zones, PlayerMotor motor, Transform player)
        {
            _clock = clock;
            _zones = zones;
            _motor = motor;
            _player = player;
        }

        public void ConfigurePolice(ZoneAlertDirector alerts, PoliceOfficer officer)
        {
            _alerts = alerts;
            _officer = officer;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            // 行動の出所は一度だけ引く。毎フレーム GetComponent する理由はない（第7条）。
            if (_interactor == null && _player != null)
            {
                _interactor = _player.GetComponent<PlayerInteractor>();
                _density.Bind(_interactor);
                _density.BindConsumer(_player.GetComponent<PlayerConsumer>());
            }

            _density.Tick(_clock != null ? _clock.Clock : null);

            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
            {
                var path = _density.Dump();
                if (path != null)
                {
                    Debug.Log($"{nameof(DensityRecorder)}: {path}");
                }
            }
        }

        /// <summary>
        /// 一局を閉じる瞬間に報告を落とす。計測が手記だった頃に失われていたのは、たいてい
        /// 「遊び終えて、書き留めるのを忘れた」ぶんである。終了は canonical な区切りなので、ここに置く。
        /// </summary>
        void OnApplicationQuit() => _density.Dump();

        void OnDestroy() => _density.Dispose();

        void OnGUI()
        {
            if (!_visible || _clock == null || _clock.Clock == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, richText = false };

            var clock = _clock.Clock;
            var lines = new[]
            {
                $"Day {clock.Day}    Phase: {clock.Phase}    t {Mmss(clock.ElapsedInDay)}",
                $"Night in {Mmss(clock.SecondsUntilNight)}",
                MeasureLine(clock.ElapsedInDay),
                _density.Log.OverlayLine(),
                $"Zone: {(_zones != null ? _zones.CurrentZone.ToString() : "-")}",
                $"x {(_player != null ? _player.position.x : 0f):F1}    " +
                $"y {(_player != null ? _player.position.y : 0f):F1}    " +
                $"speed {(_motor != null ? _motor.CurrentSpeed : 0f):F1}",
                AlertLine(),
                OfficerLine(),
            };

            GUI.Box(new Rect(8f, 8f, 420f, 24f * lines.Length + 16f), GUIContent.none);
            for (var i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(18f, 16f + 24f * i, 400f, 24f), lines[i], _style);
            }
        }

        /// <summary>
        /// この日に費やした実時間と、時計/実時間の比。作業計画 第七節の計測に使う。
        /// 一時停止中は Time.time が止まるので、ポーズ時間は数えない。
        /// </summary>
        string MeasureLine(float clockElapsedInDay)
        {
            var today = _density.Log.Current;
            var real = today != null ? today.RealSeconds : 0f;
            if (real < 0.5f)
            {
                return $"Real {Mmss(real)}    ratio -";
            }

            return $"Real {Mmss(real)}    ratio x{clockElapsedInDay / real:F1}";
        }

        string AlertLine()
        {
            if (_alerts == null)
            {
                return "Alert: -";
            }

            return $"Alert  quiet {_alerts.Level(AlertZoneId.Quiet):F0}    " +
                   $"resid {_alerts.Level(AlertZoneId.Residential):F0}    " +
                   $"comm {_alerts.Level(AlertZoneId.Commercial):F0}";
        }

        string OfficerLine()
        {
            if (_officer == null)
            {
                return "Officer: -";
            }

            var multiplier = PoliceEscalation.SpeedMultiplier(_officer.Zone, _alerts != null ? _alerts.Level(_officer.Zone) : 0f);
            return $"Officer  {_officer.Stage}    suspicion {_officer.Suspicion:F0}    x{multiplier:F2}";
        }

        static string Mmss(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
