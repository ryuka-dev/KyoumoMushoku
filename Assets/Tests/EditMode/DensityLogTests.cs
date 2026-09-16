using System;
using KyoumoMushoku.Core.Diagnostics;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 密度の計測（作業計画 第四節 M1）の積み上げの検証。
    ///
    /// この報告は「5c をやるかどうか」という、数週間ぶんの作業を左右する判断の材料になる。
    /// 数字がずれていたら判断ごとずれるので、日の切り替わりと行動の帰属だけは固めておく。
    /// </summary>
    public sealed class DensityLogTests
    {
        [Test]
        public void BeforeFirstObservation_HasNoDay()
        {
            var log = new DensityLog();
            Assert.IsNull(log.Current);
            Assert.AreEqual(0, log.TotalActs);
        }

        [Test]
        public void ActsBeforeFirstObservation_AreDropped()
        {
            var log = new DensityLog();
            log.Record(ActKind.Forage, night: false, clockElapsedInDay: 0f);
            Assert.AreEqual(0, log.TotalActs, "帰属先の日が無い行動は数えない。");
        }

        [Test]
        public void RealSeconds_CountFromTheDayStart()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 100f);
            log.Observe(day: 1, clockElapsedInDay: 30f, realSeconds: 120f);

            Assert.AreEqual(20f, log.Current.RealSeconds, 0.001f);
            Assert.AreEqual(30f, log.Current.ClockSeconds, 0.001f);
        }

        [Test]
        public void ActClockSeconds_IsTheClockBeyondRealTime()
        {
            // ソフトクロックは実時間と等倍で進むので、差はすべて行動が焼いた分である。
            // 実時間20秒のあいだに時計が110秒進んだなら、90秒はバイト1シフトが焼いたことになる。
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 100f);
            log.Observe(day: 1, clockElapsedInDay: 110f, realSeconds: 120f);

            Assert.AreEqual(90f, log.Current.ActClockSeconds, 0.001f);
        }

        [Test]
        public void ActClockSeconds_NeverGoesNegative()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 0f);
            log.Observe(day: 1, clockElapsedInDay: 5f, realSeconds: 20f);

            Assert.AreEqual(0f, log.Current.ActClockSeconds, 0.001f);
        }

        [Test]
        public void DayChange_OpensANewRecordAndKeepsTheOld()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 0f);
            log.Record(ActKind.Forage, night: false, clockElapsedInDay: 10f);
            log.Observe(day: 1, clockElapsedInDay: 200f, realSeconds: 60f);

            log.Observe(day: 2, clockElapsedInDay: 0f, realSeconds: 60f);
            log.Record(ActKind.Shop, night: false, clockElapsedInDay: 5f);

            Assert.AreEqual(2, log.Days.Count);
            Assert.AreEqual(1, log.Days[0].Acts, "前日の行動は前日に残る。");
            Assert.AreEqual(200f, log.Days[0].ClockSeconds, 0.001f, "前日の時計は変わり目の値で止まる。");
            Assert.AreEqual(1, log.Days[1].Acts);
            Assert.AreEqual(2, log.TotalActs);
        }

        [Test]
        public void SleepTime_IsTheFirstSleepOfTheDay()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 0f);

            Assert.Less(log.Current.SleepAtClockSeconds, 0f, "寝るまでは未就寝。");

            log.Record(ActKind.Sleep, night: true, clockElapsedInDay: 208f);
            log.Record(ActKind.Sleep, night: true, clockElapsedInDay: 260f);

            Assert.AreEqual(208f, log.Current.SleepAtClockSeconds, 0.001f);
        }

        [Test]
        public void NightForages_CountOnlyTheNightOnes()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 0f);
            log.Record(ActKind.Forage, night: false, clockElapsedInDay: 10f);
            log.Record(ActKind.Forage, night: true, clockElapsedInDay: 190f);
            log.Record(ActKind.Water, night: true, clockElapsedInDay: 195f);

            Assert.AreEqual(3, log.Current.Acts);
            Assert.AreEqual(2, log.Current.NightActs);
            Assert.AreEqual(1, log.Current.NightForages, "夜の補充窓を使ったかは、夜の漁りの数で判る。");
            Assert.AreEqual(2, log.TotalCount(ActKind.Forage));
        }

        [Test]
        public void Format_ListsEveryDay()
        {
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 0f, realSeconds: 0f);
            log.Record(ActKind.Forage, night: false, clockElapsedInDay: 10f);
            log.Observe(day: 1, clockElapsedInDay: 210f, realSeconds: 90f);
            log.Observe(day: 2, clockElapsedInDay: 0f, realSeconds: 90f);
            log.Observe(day: 2, clockElapsedInDay: 240f, realSeconds: 190f);

            var report = log.Format(new DateTime(2026, 9, 16, 12, 0, 0));

            StringAssert.Contains("密度報告", report);
            StringAssert.Contains("2026-09-16 12:00:00", report);
            StringAssert.Contains("03:30", report); // Day1 の時計 210 秒
            StringAssert.Contains("04:00", report); // Day2 の時計 240 秒
            StringAssert.Contains("未就寝", report);
        }

        [Test]
        public void OverlayLine_IsAscii()
        {
            // 開発用オーバーレイの既定フォントは ASCII しか持たない。
            var log = new DensityLog();
            log.Observe(day: 1, clockElapsedInDay: 30f, realSeconds: 10f);
            log.Record(ActKind.Forage, night: false, clockElapsedInDay: 10f);

            foreach (var c in log.OverlayLine())
            {
                Assert.Less(c, (char)128, $"オーバーレイに非 ASCII が混じっている: {c}");
            }
        }
    }
}
