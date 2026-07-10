using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Zones;

namespace KyoumoMushoku.Core.Police
{
    /// <summary>
    /// 3つの警戒ゾーンの警戒度の、唯一の権威（第五節）。
    ///
    /// これは注目度（<see cref="PoliceEscalation"/>）とは別の概念である。警戒度はゾーンごとに永続し、
    /// 日をまたいで残る。注目度は警官ごとの一過性の値であり、視界を外れれば消える。
    /// 一方が他方を駆動することはあっても、同じ数値にまとめてはならない。
    ///
    /// <see cref="AlertZoneId.None"/> は警戒度を持たない。ゾーンの外は誰も見ていない。
    /// </summary>
    public sealed class ZoneAlertLevels
    {
        /// <summary>警戒度を追跡するゾーン。「追跡しないゾーン」は設けない（第五節）。</summary>
        static readonly AlertZoneId[] TrackedZones =
        {
            AlertZoneId.Quiet,
            AlertZoneId.Residential,
            AlertZoneId.Commercial,
        };

        readonly Dictionary<AlertZoneId, float> _levels = new Dictionary<AlertZoneId, float>();
        readonly ZoneAlertTuning _tuning;

        public ZoneAlertLevels(ZoneAlertTuning tuning = null)
        {
            _tuning = tuning ?? ZoneAlertTuning.Default;

            foreach (var zone in TrackedZones)
            {
                _levels[zone] = 0f;
            }
        }

        public static IReadOnlyList<AlertZoneId> Zones => TrackedZones;

        public ZoneAlertTuning Tuning => _tuning;

        /// <summary>警戒度が変わったゾーンとその新しい値。</summary>
        public event Action<AlertZoneId, float> Changed;

        public float Level(AlertZoneId zone) => _levels.TryGetValue(zone, out var level) ? level : 0f;

        /// <summary>目立った。追跡しないゾーンでは何も起きない。</summary>
        public void Raise(AlertZoneId zone, float amount)
        {
            if (amount <= 0f || !_levels.ContainsKey(zone))
            {
                return;
            }

            Set(zone, _levels[zone] + amount);
        }

        /// <summary>時間経過による減衰。ゾーンごとに速さが違う。</summary>
        public void Decay(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            foreach (var zone in TrackedZones)
            {
                var decayed = _levels[zone] - _tuning.DecayPerSecond(zone) * deltaSeconds;
                Set(zone, decayed);
            }
        }

        /// <summary>就寝して日付が変わったときの減衰（第五節）。</summary>
        public void BeginNextDay()
        {
            foreach (var zone in TrackedZones)
            {
                Set(zone, _levels[zone] - _tuning.SleepDecay(zone));
            }
        }

        public ZoneAlertState CaptureState()
        {
            var state = new ZoneAlertState();
            foreach (var zone in TrackedZones)
            {
                state.Zones.Add(new ZoneAlertEntry(zone, _levels[zone]));
            }

            return state;
        }

        /// <summary>
        /// セーブデータから復元する。外部入力として扱い、未知のゾーン・範囲外の値は黙って受け入れない。
        /// 記載のないゾーンは 0（誰にも顔を覚えられていない）とする。
        /// </summary>
        public void Restore(ZoneAlertState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            foreach (var zone in TrackedZones)
            {
                Set(zone, 0f);
            }

            if (state.Zones == null)
            {
                return;
            }

            foreach (var entry in state.Zones)
            {
                if (_levels.ContainsKey(entry.Zone))
                {
                    Set(entry.Zone, entry.Level);
                }
            }
        }

        void Set(AlertZoneId zone, float level)
        {
            var clamped = ZoneAlertTuning.Clamp(level);
            if (_levels[zone].Equals(clamped))
            {
                return;
            }

            _levels[zone] = clamped;
            Changed?.Invoke(zone, clamped);
        }
    }
}
