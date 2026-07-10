using System;

namespace KyoumoMushoku.Core.DayCycle
{
    /// <summary>
    /// ソフトクロック。時刻は実時間で進むが、強制的な締め切りは存在しない。
    /// 夜に入ったあとは、プレイヤーが自ら就寝するまで夜が続く。
    /// </summary>
    public sealed class GameClock
    {
        readonly DaySchedule _schedule;

        int _day;
        float _elapsedInDay;
        DayPhase _phase;

        public GameClock(DaySchedule schedule, GameClockState state = null)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

            _day = Math.Max(1, state?.Day ?? 1);
            _elapsedInDay = Math.Max(0f, state?.ElapsedInDay ?? 0f);
            _phase = PhaseAt(_day, _elapsedInDay);
        }

        /// <summary>ゲームデイ（1始まり）。</summary>
        public int Day => _day;

        /// <summary>その日、休憩地点を出てから経過した秒数。</summary>
        public float ElapsedInDay => _elapsedInDay;

        public DayPhase Phase => _phase;

        /// <summary>夜までの残り秒数。すでに夜であれば 0。</summary>
        public float SecondsUntilNight =>
            Math.Max(0f, _schedule.ForDay(_day).SecondsUntilNight - _elapsedInDay);

        public event Action<DayPhase> PhaseChanged;
        public event Action<int> DayBegan;

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            _elapsedInDay += deltaSeconds;
            UpdatePhase();
        }

        /// <summary>
        /// 就寝して日付を進める。夜に上限がないため、これが唯一の日付の変わり目である。
        /// </summary>
        public void BeginNextDay()
        {
            _day++;
            _elapsedInDay = 0f;
            DayBegan?.Invoke(_day);
            UpdatePhase();
        }

        public GameClockState CaptureState() => new GameClockState
        {
            Day = _day,
            ElapsedInDay = _elapsedInDay,
        };

        void UpdatePhase()
        {
            var next = PhaseAt(_day, _elapsedInDay);
            if (next == _phase)
            {
                return;
            }

            _phase = next;
            PhaseChanged?.Invoke(next);
        }

        DayPhase PhaseAt(int day, float elapsed)
        {
            var durations = _schedule.ForDay(day);

            if (elapsed < durations.DaySeconds)
            {
                return DayPhase.Day;
            }

            return elapsed < durations.SecondsUntilNight ? DayPhase.Dusk : DayPhase.Night;
        }
    }
}
