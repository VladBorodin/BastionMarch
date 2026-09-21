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

        [Test]
        public void NewCycleUsesDefaultActionPhaseCount()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(
                    TurnCycle.DefaultActionPhaseCount));

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(7)]
        public void CycleAcceptsPositiveActionPhaseCount(
            int actionPhaseCount)
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount:
                        actionPhaseCount);

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(
                    actionPhaseCount));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void CycleRejectsInvalidActionPhaseCount(
            int invalidActionPhaseCount)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new TurnCycle(
                        turnNumber: 1,
                        actionPhaseCount:
                            invalidActionPhaseCount));
        }

        [Test]
        public void PlanningHasNoActiveActionPhase()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.HasActiveActionPhase,
                Is.False);
        }

        [Test]
        public void CycleCanCombineKnownTurnAndCustomPhaseCount()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 17,
                    actionPhaseCount: 4);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(17));

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(4));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }
    }
}