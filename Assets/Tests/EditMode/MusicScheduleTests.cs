using KyoumoMushoku.Core.DayCycle;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// BGM の順番（一曲流したら黙る・時間帯で曲を選ぶ・黙っている間に時間帯が変わったらすぐ始める）の検証。
    /// </summary>
    public sealed class MusicScheduleTests
    {
        [Test]
        public void WaitsForStartDelay_ThenPicksDayTrack()
        {
            var s = new MusicSchedule(silenceSeconds: 30f, startDelaySeconds: 5f);
            Assert.AreEqual(MusicTrack.None, s.Tick(0f, night: false));
            Assert.AreEqual(MusicTrack.None, s.Tick(4.9f, night: false));
            Assert.AreEqual(MusicTrack.Day, s.Tick(5f, night: false));
        }

        [Test]
        public void StartsNightTrackAtNight()
        {
            var s = new MusicSchedule(30f, 0f);
            Assert.AreEqual(MusicTrack.Night, s.Tick(0f, night: true));
        }

        [Test]
        public void WhilePlaying_DoesNotStartAnother()
        {
            var s = new MusicSchedule(30f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            Assert.IsTrue(s.IsPlaying(45f));
            Assert.AreEqual(MusicTrack.None, s.Tick(45f, false));
        }

        [Test]
        public void PhaseChangeWhilePlaying_LetsTheTrackFinish()
        {
            var s = new MusicSchedule(30f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            Assert.AreEqual(MusicTrack.None, s.Tick(60f, night: true), "鳴っている曲は時間帯が変わっても最後まで流す。");
        }

        [Test]
        public void AfterTrackEnds_StaysSilent_ThenPicksByCurrentPhase()
        {
            var s = new MusicSchedule(30f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            Assert.AreEqual(MusicTrack.None, s.Tick(100f, false), "曲が終わってから黙る間は始めない。");
            Assert.AreEqual(MusicTrack.Day, s.Tick(120f, false));
        }

        [Test]
        public void PhaseChangeDuringSilence_StartsNewTrackImmediately()
        {
            var s = new MusicSchedule(60f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            s.Tick(95f, false);
            Assert.AreEqual(MusicTrack.Night, s.Tick(100f, night: true), "黙っている間に夜になったら、待たずに夜の曲を始める。");
        }

        [Test]
        public void PhaseFlipDuringPlay_DoesNotSkipSilenceAfterward()
        {
            var s = new MusicSchedule(30f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            s.Tick(50f, night: true);
            Assert.AreEqual(MusicTrack.None, s.Tick(100f, night: true), "流れている間の切り替わりは、曲が終わった後の黙る間を飛ばさない。");
            Assert.AreEqual(MusicTrack.Night, s.Tick(120f, night: true));
        }

        [Test]
        public void Stopped_RestartsSilenceFromNow()
        {
            var s = new MusicSchedule(30f, 0f);
            s.Tick(0f, false);
            s.Started(0f, 90f);
            s.Stopped(10f);
            Assert.IsFalse(s.IsPlaying(11f));
            Assert.AreEqual(MusicTrack.None, s.Tick(39f, false));
            Assert.AreEqual(MusicTrack.Day, s.Tick(40f, false));
        }

        [Test]
        public void NegativeInputs_AreClampedToZero()
        {
            var s = new MusicSchedule(-5f, -5f);
            Assert.AreEqual(0f, s.SilenceSeconds);
            Assert.AreEqual(MusicTrack.Day, s.Tick(0f, false));
            s.Started(0f, -10f);
            Assert.IsFalse(s.IsPlaying(0f));
        }
    }
}
