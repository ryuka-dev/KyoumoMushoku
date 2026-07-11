using KyoumoMushoku.Core.Survival;
using NUnit.Framework;

namespace KyoumoMushoku.Core.Tests
{
    public sealed class VitalsTests
    {
        static VitalsTuning Tuning() => new VitalsTuning();

        [Test]
        public void Thirst_FallsFasterThanHunger_WhichFallsFasterThanSanity()
        {
            var tuning = Tuning();

            Assert.Greater(tuning.ThirstDrainPerSecond, tuning.HungerDrainPerSecond,
                "渇きは最も早く低下し、短期的な圧力を形成する。");
            Assert.Greater(tuning.HungerDrainPerSecond, tuning.SanityDrainPerSecond,
                "SAN は最も緩やかに低下するが、最も回復が難しい。");
        }

        [Test]
        public void Advance_DrainsAllThreeGauges()
        {
            var vitals = new Vitals(Tuning());
            vitals.Advance(10f, running: false);

            Assert.AreEqual(98.5f, vitals.Thirst, 1e-3f);
            Assert.AreEqual(98.7f, vitals.Hunger, 1e-3f);
            Assert.AreEqual(99.75f, vitals.Sanity, 1e-3f);
            Assert.AreEqual(100f, vitals.Hp, 1e-3f, "満たされているうちは HP は減らない。");
        }

        [Test]
        public void Running_DrainsThirstAndHungerFaster_ButNotSanity()
        {
            var walking = new Vitals(Tuning());
            var running = new Vitals(Tuning());

            walking.Advance(10f, running: false);
            running.Advance(10f, running: true);

            Assert.Less(running.Thirst, walking.Thirst);
            Assert.Less(running.Hunger, walking.Hunger);
            Assert.AreEqual(walking.Sanity, running.Sanity, 1e-5f, "走っても気分は余計に沈まない。");
        }

        [Test]
        public void RunningWhileHungry_CostsExtraHunger()
        {
            var tuning = Tuning();
            var fed = new Vitals(tuning, new VitalsState { Hunger = tuning.LowHungerThreshold + 1f });
            var starving = new Vitals(tuning, new VitalsState { Hunger = tuning.LowHungerThreshold - 1f });

            var fedBefore = fed.Hunger;
            var starvingBefore = starving.Hunger;

            fed.Advance(5f, running: true);
            starving.Advance(5f, running: true);

            Assert.Greater(starvingBefore - starving.Hunger, fedBefore - fed.Hunger,
                "空腹度が低いとき、走行時の消耗が増加する。");
        }

        [Test]
        public void ZeroThirst_DealsDamageAndSlowsMovement()
        {
            var tuning = Tuning();
            var vitals = new Vitals(tuning, new VitalsState { Thirst = 0f });

            Assert.AreEqual(tuning.DehydratedSpeedMultiplier, vitals.SpeedMultiplier, 1e-5f);

            vitals.Advance(10f, running: false);
            Assert.AreEqual(100f - tuning.DehydrationDamagePerSecond * 10f, vitals.Hp, 1e-3f);
        }

        [Test]
        public void ZeroHungerAndThirst_StackTheirDamage()
        {
            var tuning = Tuning();
            var vitals = new Vitals(tuning, new VitalsState { Thirst = 0f, Hunger = 0f });
            vitals.Advance(10f, running: false);

            var expected = 100f - (tuning.DehydrationDamagePerSecond + tuning.StarvationDamagePerSecond) * 10f;
            Assert.AreEqual(expected, vitals.Hp, 1e-3f);
        }

        [Test]
        public void ZeroSanity_DoesNotKill()
        {
            var vitals = new Vitals(Tuning(), new VitalsState { Sanity = 0f });
            vitals.Advance(60f, running: false);

            Assert.IsTrue(vitals.IsAlive, "SAN はゼロになっても即死させない。");
            Assert.AreEqual(0f, vitals.Sanity);
            Assert.AreEqual(SanityTier.Broken, vitals.SanityTier);
        }

        [Test]
        public void Died_FiresExactlyOncePerDeath()
        {
            var vitals = new Vitals(Tuning());
            var deaths = 0;
            vitals.Died += () => deaths++;

            vitals.Apply(new VitalsDelta { Hp = -1000f });
            Assert.AreEqual(1, deaths);

            vitals.Apply(new VitalsDelta { Hp = -1000f });
            vitals.Advance(10f, running: false);
            Assert.AreEqual(1, deaths, "死んだあとは再発火しない。");

            vitals.Revive();
            vitals.Apply(new VitalsDelta { Hp = -1000f });
            Assert.AreEqual(2, deaths, "搬送して生き返ったあとは、また死にうる。");
        }

        [Test]
        public void Revive_RestoresHpOnly()
        {
            var vitals = new Vitals(Tuning(), new VitalsState { Thirst = 12f, Hunger = 8f, Sanity = 44f });
            vitals.Apply(new VitalsDelta { Hp = -1000f });
            vitals.Revive();

            Assert.AreEqual(100f, vitals.Hp, 1e-5f);
            Assert.AreEqual(12f, vitals.Thirst, 1e-5f, "渇きは死亡直前の値を維持する。");
            Assert.AreEqual(8f, vitals.Hunger, 1e-5f, "空腹は死亡直前の値を維持する。");
            Assert.AreEqual(44f, vitals.Sanity, 1e-5f, "SAN のペナルティは搬送側が別に適用する。");
        }

        [Test]
        public void Advance_DoesNothingWhenDeadOrGivenNonPositiveTime()
        {
            var vitals = new Vitals(Tuning());
            vitals.Advance(0f, false);
            vitals.Advance(-5f, false);
            Assert.AreEqual(100f, vitals.Thirst, 1e-5f);

            vitals.Apply(new VitalsDelta { Hp = -1000f });
            var thirstAtDeath = vitals.Thirst;
            vitals.Advance(30f, false);
            Assert.AreEqual(thirstAtDeath, vitals.Thirst, 1e-5f);
        }

        [Test]
        public void Apply_ClampsToTheConfiguredMaximums()
        {
            var vitals = new Vitals(Tuning(), new VitalsState { Thirst = 90f, Sanity = 95f });
            vitals.Apply(new VitalsDelta { Thirst = 999f, Sanity = 999f, Hunger = -999f });

            Assert.AreEqual(100f, vitals.Thirst, 1e-5f);
            Assert.AreEqual(100f, vitals.Sanity, 1e-5f);
            Assert.AreEqual(0f, vitals.Hunger, 1e-5f);
        }

        [Test]
        public void State_RoundTripsThroughSaveAndLoad()
        {
            var vitals = new Vitals(Tuning());
            vitals.Advance(123f, running: true);

            var restored = new Vitals(Tuning(), vitals.CaptureState());

            Assert.AreEqual(vitals.Hp, restored.Hp, 1e-5f);
            Assert.AreEqual(vitals.Thirst, restored.Thirst, 1e-5f);
            Assert.AreEqual(vitals.Hunger, restored.Hunger, 1e-5f);
            Assert.AreEqual(vitals.Sanity, restored.Sanity, 1e-5f);
        }
    }

    public sealed class DeathPenaltyTests
    {
        [TestCase(100f, 20f)]
        [TestCase(80f, 20f)]
        [TestCase(79.9f, 10f)]
        [TestCase(50f, 10f)]
        [TestCase(49.9f, 5f)]
        [TestCase(20f, 5f)]
        [TestCase(19.9f, 2f)]
        [TestCase(0f, 2f)]
        public void SanityLoss_FollowsTheBandedCurve(float sanityAtDeath, float expected)
        {
            Assert.AreEqual(expected, DeathPenalty.SanityLoss(sanityAtDeath), 1e-5f);
        }

        [Test]
        public void SanityLoss_NeverGrowsAsSanityFalls()
        {
            var previous = DeathPenalty.SanityLoss(100f);
            for (var sanity = 99f; sanity >= 0f; sanity -= 1f)
            {
                var current = DeathPenalty.SanityLoss(sanity);
                Assert.LessOrEqual(current, previous,
                    "SAN が既に低いプレイヤーをさらに叩き潰してはならない。");
                previous = current;
            }
        }
    }
}
