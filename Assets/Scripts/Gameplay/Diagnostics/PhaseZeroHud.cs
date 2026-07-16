using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
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
        }

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
