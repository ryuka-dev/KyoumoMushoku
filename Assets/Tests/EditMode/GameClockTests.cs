using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.DayCycle;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class GameClockTests
    {
        /// <summary>第二節の時間帯の表。1日目 3分＋1分、2日目 4.5分＋1.5分、3日目 6分＋2分。</summary>
        static DaySchedule Schedule() => new DaySchedule(new[]
        {
            new DayPhaseDurations(180f, 60f),
            new DayPhaseDurations(270f, 90f),
            new DayPhaseDurations(360f, 120f),
        });

        [Test]
        public void DayOne_CrossesDayThenDuskThenNight()
        {
            var clock = new GameClock(Schedule());
            Assert.AreEqual(DayPhase.Day, clock.Phase);

            clock.Advance(179f);
            Assert.AreEqual(DayPhase.Day, clock.Phase);

            clock.Advance(2f);
            Assert.AreEqual(DayPhase.Dusk, clock.Phase);

            clock.Advance(58f);
            Assert.AreEqual(DayPhase.Dusk, clock.Phase);

            clock.Advance(2f);
            Assert.AreEqual(DayPhase.Night, clock.Phase, "1日目は約4分で夜に入る。");
        }

        [Test]
        public void Night_HasNoDeadline()
        {
            var clock = new GameClock(Schedule());
            clock.Advance(10_000f);

            Assert.AreEqual(DayPhase.Night, clock.Phase);
            Assert.AreEqual(1, clock.Day, "就寝しない限り日付は変わらない。");
            Assert.AreEqual(0f, clock.SecondsUntilNight);
        }

        [Test]
        public void Sleeping_IsTheOnlyWayToChangeDay()
        {
            var clock = new GameClock(Schedule());
            clock.Advance(500f);
            clock.BeginNextDay();

            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0f, clock.ElapsedInDay);
            Assert.AreEqual(DayPhase.Day, clock.Phase);

            clock.Advance(280f);
            Assert.AreEqual(DayPhase.Dusk, clock.Phase, "2日目の夕方は270秒から。");

            clock.Advance(90f);
            Assert.AreEqual(DayPhase.Night, clock.Phase, "2日目の夜は360秒から。");
        }

        [Test]
        public void DaysBeyondTheTable_ReuseTheLastRow()
        {
            var schedule = Schedule();
            Assert.AreEqual(schedule.ForDay(3).SecondsUntilNight, schedule.ForDay(4).SecondsUntilNight);
        }

        [Test]
        public void PhaseChanged_FiresOncePerTransition()
        {
            var clock = new GameClock(Schedule());
            var seen = new List<DayPhase>();
            clock.PhaseChanged += seen.Add;

            for (var i = 0; i < 300; i++)
            {
                clock.Advance(1f);
            }

            Assert.AreEqual(new[] { DayPhase.Dusk, DayPhase.Night }, seen);
        }

        [Test]
        public void State_RoundTripsThroughSaveAndLoad()
        {
            var clock = new GameClock(Schedule());
            clock.Advance(500f);
            clock.BeginNextDay();
            clock.Advance(300f);

            var restored = new GameClock(Schedule(), clock.CaptureState());

            Assert.AreEqual(clock.Day, restored.Day);
            Assert.AreEqual(clock.ElapsedInDay, restored.ElapsedInDay);
            Assert.AreEqual(clock.Phase, restored.Phase);
        }

        [Test]
        public void Advance_IgnoresNonPositiveDelta()
        {
            var clock = new GameClock(Schedule());
            clock.Advance(0f);
            clock.Advance(-5f);

            Assert.AreEqual(0f, clock.ElapsedInDay);
        }

        [Test]
        public void Schedule_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentException>(() => _ = new DaySchedule(Array.Empty<DayPhaseDurations>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new DayPhaseDurations(0f, 60f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new DayPhaseDurations(180f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = Schedule().ForDay(0));
        }
    }
}
