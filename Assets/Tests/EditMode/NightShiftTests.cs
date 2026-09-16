using System.Collections.Generic;
using KyoumoMushoku.Core.Shift;
using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    /// <summary>
    /// 第二章 試作の中心——用件と、SAN の階段の検証（`Docs/第二章試作.md`）。
    ///
    /// この試作が答える問いは「返事をする」が動詞として成立するか、ただ一つである。
    /// 成立しなければ第二章は作らない。だから土台の規則だけは、手で通す前に固めておく。
    /// </summary>
    public sealed class NightShiftTests
    {
        static ResponseOption Speak(float sanityDelta = 0f) => new ResponseOption("line", sanityDelta);

        static ResponseOption Silence(float sanityDelta = -2f) =>
            new ResponseOption("silence", sanityDelta, isSilence: true);

        static Matter MatterAt(ShiftFacing facing, string id = "m", float patience = 10f, float miss = -5f) =>
            new Matter(id, facing, "prompt", patience, new[] { Speak(), Silence() }, missSanityDelta: miss);

        static NightShift ShiftOf(params ScheduledMatter[] script) => new NightShift(script);

        // ---- SAN の階段（第三節）。新しい閾値を作らず、SanityScale の既存値に乗る ----

        [Test]
        public void Sight_Speakable_AboveConversationThreshold()
        {
            Assert.AreEqual(ResponseReach.Speakable, ResponseSight.Read(SanityScale.ConversationThreshold));
            Assert.AreEqual(ResponseReach.Speakable, ResponseSight.Read(SanityScale.Max));
        }

        [Test]
        public void Sight_Mute_BetweenBreakdownAndConversation()
        {
            // この帯がこの章の中心である。言うべきことが見えているのに、口が動かない。
            Assert.AreEqual(ResponseReach.Mute, ResponseSight.Read(SanityScale.BreakdownThreshold));
            Assert.AreEqual(ResponseReach.Mute, ResponseSight.Read(SanityScale.ConversationThreshold - 0.1f));
        }

        [Test]
        public void Sight_Unreadable_BelowBreakdown()
        {
            Assert.AreEqual(ResponseReach.Unreadable, ResponseSight.Read(SanityScale.BreakdownThreshold - 0.1f));
            Assert.AreEqual(ResponseReach.Unreadable, ResponseSight.Read(0f));
        }

        // ---- 用件の到着と我慢 ----

        [Test]
        public void Matter_ArrivesOnSchedule()
        {
            var shift = ShiftOf(new ScheduledMatter(5f, MatterAt(ShiftFacing.Door)));

            shift.Advance(4f);
            Assert.AreEqual(0, shift.Pending.Count, "台本の時刻より前には降ってこない。");

            shift.Advance(2f);
            Assert.AreEqual(1, shift.Pending.Count);
        }

        [Test]
        public void Matter_LeavesWhenPatienceRunsOut()
        {
            var missed = new List<Matter>();
            var shift = ShiftOf(new ScheduledMatter(0f, MatterAt(ShiftFacing.Counter, patience: 6f)));
            shift.Missed += missed.Add;

            shift.Advance(1f);
            shift.Advance(4f);
            Assert.AreEqual(1, shift.Pending.Count, "まだ待っている。");

            shift.Advance(2f);
            Assert.AreEqual(0, shift.Pending.Count);
            Assert.AreEqual(1, missed.Count, "去る相手は報される。世界の側が何か言うための合図である。");
            Assert.AreEqual(1, shift.MissedCount);
        }

        [Test]
        public void PatienceRunsEvenWhileFacingAway()
        {
            // 向いていない方向でも待ち時間は進む。それがこの章の圧力そのものである。
            var shift = ShiftOf(
                new ScheduledMatter(0f, MatterAt(ShiftFacing.Counter, "counter", patience: 5f)),
                new ScheduledMatter(0f, MatterAt(ShiftFacing.Door, "door", patience: 5f)));

            shift.Advance(6f);
            Assert.AreEqual(0, shift.Pending.Count, "レジを見ていても、扉の用件は待ってくれない。");
            Assert.AreEqual(2, shift.MissedCount);
        }

        // ---- 返事 ----

        [Test]
        public void Answer_RequiresFacingTheMatter()
        {
            var shift = ShiftOf(new ScheduledMatter(0f, MatterAt(ShiftFacing.Door)));
            shift.Advance(1f);

            var wrongWay = shift.Answer(ShiftFacing.Shelf, 0, SanityScale.Max);
            Assert.AreEqual(AnswerOutcome.NothingThere, wrongWay.Outcome);
            Assert.AreEqual(1, shift.Pending.Count, "間違った方向へ返事しても、用件は消えない。");

            var facing = shift.Answer(ShiftFacing.Door, 0, SanityScale.Max);
            Assert.IsTrue(facing.Succeeded);
            Assert.AreEqual(0, shift.Pending.Count);
        }

        [Test]
        public void Answer_CarriesTheSanityDeltaButDoesNotApplyIt()
        {
            // Core は増減を返すだけ。プレイヤーの状態の所有者は Gameplay 側の一箇所だけである。
            var matter = new Matter("m", ShiftFacing.Counter, "prompt", 10f,
                new[] { Speak(sanityDelta: -3f), Silence() }, missSanityDelta: -5f);
            var shift = ShiftOf(new ScheduledMatter(0f, matter));
            shift.Advance(1f);

            var result = shift.Answer(ShiftFacing.Counter, 0, SanityScale.Max);
            Assert.AreEqual(-3f, result.SanityDelta, 0.001f);
        }

        [Test]
        public void LowSanity_TakesTheWordsButNeverTheSilence()
        {
            var shift = ShiftOf(new ScheduledMatter(0f, MatterAt(ShiftFacing.Counter)));
            shift.Advance(1f);

            var mute = SanityScale.ConversationThreshold - 1f;

            var spoken = shift.Answer(ShiftFacing.Counter, 0, mute);
            Assert.AreEqual(AnswerOutcome.CannotSpeak, spoken.Outcome, "言えない。");
            Assert.AreEqual(1, shift.Pending.Count, "言えなかったのだから、用件はまだそこに居る。");

            var silent = shift.Answer(ShiftFacing.Counter, 1, mute);
            Assert.IsTrue(silent.Succeeded, "黙ることは最後まで残る。操作は決して奪わない。");
        }

        [Test]
        public void Silence_RemainsEvenAtZeroSanity()
        {
            var shift = ShiftOf(new ScheduledMatter(0f, MatterAt(ShiftFacing.Door)));
            shift.Advance(1f);

            Assert.IsTrue(shift.Answer(ShiftFacing.Door, 1, 0f).Succeeded,
                "手詰まりになる状態を作らない（第三節）。");
        }

        [Test]
        public void SameFacing_IsServedInArrivalOrder()
        {
            var shift = ShiftOf(
                new ScheduledMatter(0f, MatterAt(ShiftFacing.Counter, "first", patience: 60f)),
                new ScheduledMatter(1f, MatterAt(ShiftFacing.Counter, "second", patience: 60f)));
            shift.Advance(2f);

            Assert.AreEqual("first", shift.FrontAt(ShiftFacing.Counter).Matter.Id);
            shift.Answer(ShiftFacing.Counter, 0, SanityScale.Max);
            Assert.AreEqual("second", shift.FrontAt(ShiftFacing.Counter).Matter.Id);
        }

        [Test]
        public void Shift_IsOverOnlyWhenScriptAndQueueAreBothEmpty()
        {
            var shift = ShiftOf(new ScheduledMatter(3f, MatterAt(ShiftFacing.Shelf, patience: 60f)));
            Assert.IsFalse(shift.IsOver, "まだ降ってきていない用件がある。");

            shift.Advance(4f);
            Assert.IsFalse(shift.IsOver, "待たせている用件がある。");

            shift.Answer(ShiftFacing.Shelf, 0, SanityScale.Max);
            Assert.IsTrue(shift.IsOver);
        }

        [Test]
        public void Matter_RejectsAnEmptyOptionList()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new Matter("m", ShiftFacing.Door, "prompt", 10f, new ResponseOption[0], missSanityDelta: 0f));
        }
    }
}
