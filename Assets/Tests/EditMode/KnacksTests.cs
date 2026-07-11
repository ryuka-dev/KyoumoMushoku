using KyoumoMushoku.Core.Knacks;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class KnackBookTests
    {
        [Test]
        public void SpotDuds_IsLearnedOnTheTenthRummage_NotBefore()
        {
            var book = new KnackBook();

            for (var i = 1; i < KnackTuning.RummagesForSpotDuds; i++)
            {
                Assert.IsFalse(book.RecordRummage(), $"{i} 個目ではまだ習得しない。");
                Assert.IsFalse(book.Has(KnackId.SpotDuds));
            }

            Assert.IsTrue(book.RecordRummage(), "閾値ちょうどで習得する。");
            Assert.IsTrue(book.Has(KnackId.SpotDuds));
        }

        [Test]
        public void OnceLearned_FurtherRummaging_NeitherRelearnsNorCounts()
        {
            var book = new KnackBook();
            for (var i = 0; i < KnackTuning.RummagesForSpotDuds; i++)
            {
                book.RecordRummage();
            }

            var countAtLearning = book.RummageCount;
            Assert.IsFalse(book.RecordRummage(), "二度は習得しない。");
            Assert.AreEqual(countAtLearning, book.RummageCount, "習得後はカウンタも進めない。");
        }

        [Test]
        public void SteadyHands_IsLearnedOnTheThirdForageWarning()
        {
            var book = new KnackBook();

            Assert.IsFalse(book.RecordForageWarned());
            Assert.IsFalse(book.RecordForageWarned());
            Assert.IsTrue(book.RecordForageWarned());
            Assert.IsTrue(book.Has(KnackId.SteadyHands));
        }

        [Test]
        public void StreetSleeper_IsLearnedOnTheSecondOutdoorSleep()
        {
            var book = new KnackBook();

            Assert.IsFalse(book.RecordOutdoorSleep());
            Assert.IsTrue(book.RecordOutdoorSleep());
            Assert.IsTrue(book.Has(KnackId.StreetSleeper));
        }

        [Test]
        public void IronStomach_IsLearnedTheFirstTimeAndNeverAgain()
        {
            var book = new KnackBook();

            Assert.IsTrue(book.RecordSurvivedRotten());
            Assert.IsTrue(book.Has(KnackId.IronStomach));
            Assert.IsFalse(book.RecordSurvivedRotten(), "閾値1のコツは二度習得しない。");
        }

        [Test]
        public void FamiliarFace_IsLearnedTheFirstTimeAndNeverAgain()
        {
            var book = new KnackBook();

            Assert.IsTrue(book.RecordFirstWarning());
            Assert.IsTrue(book.Has(KnackId.FamiliarFace));
            Assert.IsFalse(book.RecordFirstWarning());
        }

        [Test]
        public void CaptureAndRestore_RoundTripsAcquiredAndCounters()
        {
            var book = new KnackBook();
            book.RecordRummage();
            book.RecordRummage();
            book.RecordForageWarned();
            book.RecordSurvivedRotten();

            var state = book.CaptureState();
            var restored = new KnackBook();
            restored.Restore(state);

            Assert.IsTrue(restored.Has(KnackId.IronStomach));
            Assert.IsFalse(restored.Has(KnackId.SpotDuds));
            Assert.AreEqual(2, restored.RummageCount);
            Assert.AreEqual(1, restored.ForageWarnedCount);
        }

        [Test]
        public void CaptureState_IsADetachedCopy()
        {
            var book = new KnackBook();
            book.RecordRummage();

            var snapshot = book.CaptureState();
            book.RecordRummage();

            Assert.AreEqual(1, snapshot.RummageCount, "撮ったスナップショットは後の変更に引きずられない。");
        }

        [Test]
        public void Restore_Null_YieldsAnEmptyBook()
        {
            var book = new KnackBook();
            book.RecordSurvivedRotten();

            book.Restore(null);

            Assert.IsFalse(book.Has(KnackId.IronStomach));
            Assert.AreEqual(0, book.RummageCount);
        }
    }

    public sealed class ForageSightTests
    {
        [Test]
        public void WithoutTheKnack_NothingIsRevealed_RegardlessOfSanity()
        {
            Assert.AreEqual(ForagePeek.Hidden, ForageSight.Read(false, 100f));
            Assert.AreEqual(ForagePeek.Hidden, ForageSight.Read(false, 0f));
        }

        [TestCase(100f, ForagePeek.Detailed)]
        [TestCase(70f, ForagePeek.Detailed)]
        [TestCase(69.9f, ForagePeek.HitOrMiss)]
        [TestCase(20f, ForagePeek.HitOrMiss)]
        [TestCase(19.9f, ForagePeek.Unreadable)]
        [TestCase(0f, ForagePeek.Unreadable)]
        public void WithTheKnack_PrecisionFollowsSanity(float sanity, ForagePeek expected)
        {
            Assert.AreEqual(expected, ForageSight.Read(true, sanity));
        }
    }
}
