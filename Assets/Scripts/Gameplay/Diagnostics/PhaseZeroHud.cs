using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Diagnostics
{
    /// <summary>
    /// Phase 0 の検証用オーバーレイ。読み取り専用であり、ゲームプレイには一切影響しない。
    /// 日本語フォントアセットがまだ無いため、表示は ASCII に限る。
    /// </summary>
    public sealed class PhaseZeroHud : MonoBehaviour
    {
        [SerializeField] GameClockDriver _clock;
        [SerializeField] ZoneTracker _zones;
        [SerializeField] PlayerMotor _motor;
        [SerializeField] Transform _player;

        GUIStyle _style;

        public void Configure(GameClockDriver clock, ZoneTracker zones, PlayerMotor motor, Transform player)
        {
            _clock = clock;
            _zones = zones;
            _motor = motor;
            _player = player;
        }

        void OnGUI()
        {
            if (_clock == null || _clock.Clock == null)
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
            };

            GUI.Box(new Rect(8f, 8f, 420f, 24f * lines.Length + 16f), GUIContent.none);
            for (var i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(18f, 16f + 24f * i, 400f, 24f), lines[i], _style);
            }
        }

        static string Mmss(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
