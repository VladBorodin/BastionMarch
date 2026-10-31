
using System;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class BrigadeActivityPhaseIntentTests
    {
        [Test]
        public void IntentStoresActivityAndActionPhase()
        {
            Guid brigadeId = Guid.NewGuid();
            var activity = new TestActivity(brigadeId);

            var intent = new BrigadeActivityPhaseIntent(
                activity,
                actionPhase: 2);

            Assert.That(intent.Activity, Is.SameAs(activity));
            Assert.That(intent.BrigadeId, Is.EqualTo(brigadeId));
            Assert.That(intent.ActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void IntentRejectsNullActivity()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BrigadeActivityPhaseIntent(
                    null,
                    actionPhase: 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void IntentRejectsInvalidActionPhase(int phase)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrigadeActivityPhaseIntent(
                    new TestActivity(Guid.NewGuid()),
                    phase));
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId)
                : base(brigadeId)
            {
            }
        }
    }
}
