
using System;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class PersistentWorldPhaseContextTests
    {
        [Test]
        public void ContextStoresTurnAndPhaseIdentity()
        {
            var context = new PersistentWorldPhaseContext(
                turnNumber: 7,
                actionPhase: 2,
                actionPhaseCount: 3);

            Assert.That(context.TurnNumber, Is.EqualTo(7));
            Assert.That(context.ActionPhase, Is.EqualTo(2));
            Assert.That(context.ActionPhaseCount, Is.EqualTo(3));
        }

        [TestCase(1, false)]
        [TestCase(2, true)]
        public void ContextIdentifiesLastActionPhase(
            int phase,
            bool expected)
        {
            var context = new PersistentWorldPhaseContext(
                turnNumber: 1,
                actionPhase: phase,
                actionPhaseCount: 2);

            Assert.That(context.IsLastActionPhase,
                Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ContextRejectsInvalidTurnNumber(int turn)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PersistentWorldPhaseContext(
                    turn, 1, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ContextRejectsInvalidPhaseCount(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PersistentWorldPhaseContext(
                    1, 1, count));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void ContextRejectsPhaseOutsideTurn(int phase)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PersistentWorldPhaseContext(
                    1, phase, 2));
        }
    }
}
