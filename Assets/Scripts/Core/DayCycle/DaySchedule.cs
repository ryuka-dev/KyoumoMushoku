using System;
using System.Collections.Generic;
using System.Linq;

namespace KyoumoMushoku.Core.DayCycle
{
    /// <summary>
    /// 1日の昼と夕方の長さ。夜は上限を持たないため、ここには含まれない。
    /// </summary>
    public readonly struct DayPhaseDurations
    {
        public float DaySeconds { get; }
        public float DuskSeconds { get; }

        public DayPhaseDurations(float daySeconds, float duskSeconds)
        {
            if (daySeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(daySeconds), daySeconds, "昼の長さは正の値でなければならない。");
            }

            if (duskSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duskSeconds), duskSeconds, "夕方の長さは正の値でなければならない。");
            }

            DaySeconds = daySeconds;
            DuskSeconds = duskSeconds;
        }

        /// <summary>その日に夜が始まるまでの秒数。</summary>
        public float SecondsUntilNight => DaySeconds + DuskSeconds;
    }

    /// <summary>
    /// ゲームデイごとの時間帯の長さの表。
    /// 序盤を短くしてチュートリアルとして機能させ、後半にかけて伸ばす。
    /// </summary>
    public sealed class DaySchedule
    {
        readonly DayPhaseDurations[] _days;

        public DaySchedule(IEnumerable<DayPhaseDurations> days)
        {
            if (days is null)
            {
                throw new ArgumentNullException(nameof(days));
            }

            _days = days.ToArray();

            if (_days.Length == 0)
            {
                throw new ArgumentException("最低でも1日分の長さが必要。", nameof(days));
            }
        }

        public int DefinedDayCount => _days.Length;

        /// <summary>
        /// 指定した日（1始まり）の長さを返す。
        /// 表に定義されていない日は、最終日の値をそのまま使う。
        /// </summary>
        public DayPhaseDurations ForDay(int day)
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), day, "日は1始まり。");
            }

            return _days[Math.Min(day - 1, _days.Length - 1)];
        }
    }
}
