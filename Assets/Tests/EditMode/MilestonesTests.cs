using KyoumoMushoku.Core.Persistence;
using KyoumoMushoku.Core.Progress;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class MilestoneBookTests
    {
        [Test]
        public void SurviveThreeDays_IsAchievedWhenTheFourthDayBegins_NotBefore()
        {
            var book = new MilestoneBook();

            for (var day = 2; day <= MilestoneTuning.SurvivalDays; day++)
            {
                Assert.IsFalse(book.RecordDayBegan(day), $"{day}日目の朝ではまだ達成しない。");
                Assert.IsFalse(book.Has(MilestoneId.SurviveThreeDays));
            }

            Assert.IsTrue(book.RecordDayBegan(MilestoneTuning.SurvivalDays + 1), "翌日の朝を迎えたら達成する。");
            Assert.IsTrue(book.Has(MilestoneId.SurviveThreeDays));
        }

        [Test]
        public void SurviveThreeDays_IsNotAchievedTwice()
        {
            var book = new MilestoneBook();
            book.RecordDayBegan(MilestoneTuning.SurvivalDays + 1);

            Assert.IsFalse(book.RecordDayBegan(MilestoneTuning.SurvivalDays + 2), "二度は達成しない。");
        }

        [Test]
        public void FirstInnStay_IsAchievedTheFirstTimeAndNeverAgain()
        {
            var book = new MilestoneBook();

            Assert.IsTrue(book.RecordInnStay());
            Assert.IsTrue(book.Has(MilestoneId.FirstInnStay));
            Assert.IsFalse(book.RecordInnStay());
        }

        [Test]
        public void BuyBackpack_IsAchievedTheFirstTimeAndNeverAgain()
        {
            var book = new MilestoneBook();

            Assert.IsTrue(book.RecordBackpackPurchase());
            Assert.IsTrue(book.Has(MilestoneId.BuyBackpack));
            Assert.IsFalse(book.RecordBackpackPurchase());
        }

        [Test]
        public void CaptureAndRestore_RoundTripsAchievedMilestones()
        {
            var book = new MilestoneBook();
            book.RecordInnStay();
            book.RecordBackpackPurchase();

            var restored = new MilestoneBook(book.CaptureState());

            Assert.IsTrue(restored.Has(MilestoneId.FirstInnStay));
            Assert.IsTrue(restored.Has(MilestoneId.BuyBackpack));
            Assert.IsFalse(restored.Has(MilestoneId.SurviveThreeDays));
            Assert.AreEqual(2, restored.AchievedCount);
        }
    }

    public sealed class MilestonePersistenceTests
    {
        [Test]
        public void Version6_IsUpgradedWithNoMilestones()
        {
            var save = new SaveGame { Version = 6, Milestones = null };

            Assert.IsTrue(SaveGameMigration.TryUpgrade(save, out var error), error);
            Assert.AreEqual(SaveGame.CurrentVersion, save.Version);
            Assert.IsNotNull(save.Milestones);
            Assert.AreEqual(0, save.Milestones.Achieved.Count, "版 6 の世界には段階目標がまだ存在しなかった。");
            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var validationError), validationError);
        }

        [Test]
        public void Validation_RejectsUnknownMilestone()
        {
            var save = new SaveGame();
            save.Milestones.Achieved.Add((MilestoneId)999);

            Assert.IsFalse(SaveGameValidation.TryValidate(save, out _));
        }

        [Test]
        public void Validation_RejectsDuplicateMilestones()
        {
            var save = new SaveGame();
            save.Milestones.Achieved.Add(MilestoneId.FirstInnStay);
            save.Milestones.Achieved.Add(MilestoneId.FirstInnStay);

            Assert.IsFalse(SaveGameValidation.TryValidate(save, out _));
        }

        [Test]
        public void Validation_AcceptsAchievedMilestones()
        {
            var save = new SaveGame();
            save.Milestones.Achieved.Add(MilestoneId.SurviveThreeDays);
            save.Milestones.Achieved.Add(MilestoneId.FirstInnStay);
            save.Milestones.Achieved.Add(MilestoneId.BuyBackpack);

            Assert.IsTrue(SaveGameValidation.TryValidate(save, out var error), error);
        }
    }
}
