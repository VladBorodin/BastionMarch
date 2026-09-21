using System;
using BastionMarch.Simulation.Turns;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class TurnCoreTests
    {
        [Test]
        public void NewCycleStartsAtFirstTurn()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(
                    TurnCycle.FirstTurnNumber));
        }

        [Test]
        public void NewCycleStartsInPlanning()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));
        }

        [Test]
        public void CycleCanStartFromKnownPositiveTurnNumber()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 17);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(17));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void CycleRejectsInvalidTurnNumber(
            int invalidTurnNumber)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new TurnCycle(
                        invalidTurnNumber));
        }
    }
}