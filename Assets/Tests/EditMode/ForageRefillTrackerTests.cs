using KyoumoMushoku.Core.Foraging;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// ゴミ箱の補充契機（日付の変わり目と、夜への切り替わり）の検証。
    /// </summary>
    public sealed class ForageRefillTrackerTests
    {
        [Test]
        public void FirstObservation_Refills()
        {
            var tracker = new ForageRefillTracker();
            Assert.IsTrue(tracker.Observe(1, night: false), "起動・ロード直後は満タンから始める。");
        }

        [Test]
        public void SameDayDaytime_DoesNotRefill()
        {
            var tracker = new ForageRefillTracker();
            tracker.Observe(1, night: false);
            Assert.IsFalse(tracker.Observe(1, night: false));
        }

        [Test]
        public void EnteringNight_Refills()
        {
            var tracker = new ForageRefillTracker();
            tracker.Observe(1, night: false);
            Assert.IsTrue(tracker.Observe(1, night: true), "昼／夕→夜で新しい資源窓が開く。");
        }

        [Test]
        public void StayingInNight_DoesNotRefillAgain()
        {
            var tracker = new ForageRefillTracker();
            tracker.Observe(1, night: false);
            tracker.Observe(1, night: true);
            Assert.IsFalse(tracker.Observe(1, night: true), "同じ夜のあいだは二度補充しない。");
        }

        [Test]
        public void NewDay_Refills()
        {
            var tracker = new ForageRefillTracker();
            tracker.Observe(1, night: true);
            Assert.IsTrue(tracker.Observe(2, night: false), "就寝で日付が変わると再湧きする。");
        }

        [Test]
        public void LoadingIntoNight_RefillsOnceThenStable()
        {
            var tracker = new ForageRefillTracker();
            Assert.IsTrue(tracker.Observe(1, night: true), "夜にロードしても初回は満タン。");
            Assert.IsFalse(tracker.Observe(1, night: true), "その後は二度補充しない。");
        }
    }
}
