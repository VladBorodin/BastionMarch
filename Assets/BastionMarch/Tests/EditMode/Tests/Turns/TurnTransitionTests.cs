using BastionMarch.Simulation.Turns;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class TurnTransitionTests
    {
        [Test]
        public void PlanningStartsUnconfirmed()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.IsPlanningConfirmed,
                Is.False);

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void ConfirmPlanningStartsFirstActionPhase()
        {
            var cycle =
                new TurnCycle();

            TurnTransitionResult result =
                cycle.TryConfirmPlanning();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason.None));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            Assert.That(
                cycle.HasActiveActionPhase,
                Is.True);

            Assert.That(
                cycle.IsPlanningConfirmed,
                Is.True);
        }

        [Test]
        public void ConfirmPlanningResultDescribesTransition()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 17,
                    actionPhaseCount: 4);

            TurnTransitionResult result =
                cycle.TryConfirmPlanning();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.TurnNumber,
                Is.EqualTo(17));

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                result.CurrentStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                result.PreviousActionPhase,
                Is.Null);

            Assert.That(
                result.CurrentActionPhase,
                Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public void ConfirmPlanningAlwaysStartsAtPhaseOne(
            int actionPhaseCount)
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount:
                        actionPhaseCount);

            TurnTransitionResult result =
                cycle.TryConfirmPlanning();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(
                    actionPhaseCount));
        }

        [Test]
        public void ConfirmPlanningDoesNotChangeTurnNumber()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 42);

            cycle.TryConfirmPlanning();

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(42));
        }

        [Test]
        public void PlanningCannotBeConfirmedTwice()
        {
            var cycle =
                new TurnCycle();

            TurnTransitionResult first =
                cycle.TryConfirmPlanning();

            TurnTransitionResult second =
                cycle.TryConfirmPlanning();

            Assert.That(
                first.IsSuccess,
                Is.True);

            Assert.That(
                second.IsSuccess,
                Is.False);

            Assert.That(
                second.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason
                        .PlanningAlreadyConfirmed));
        }

        [Test]
        public void FailedRepeatedConfirmationDoesNotMutateCycle()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 7,
                    actionPhaseCount: 3);

            cycle.TryConfirmPlanning();

            TurnTransitionResult failed =
                cycle.TryConfirmPlanning();

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(7));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            Assert.That(
                failed.PreviousStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                failed.CurrentStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                failed.PreviousActionPhase,
                Is.EqualTo(1));

            Assert.That(
                failed.CurrentActionPhase,
                Is.EqualTo(1));
        }

        [Test]
        public void FailureResultRejectsNoneFailureReason()
        {
            Assert.Throws<
                System.ArgumentException>(
                () =>
                    TurnTransitionResult.Failure(
                        turnNumber: 1,
                        stage:
                            TurnStage.Planning,
                        currentActionPhase:
                            null,
                        failureReason:
                            TurnTransitionFailureReason
                                .None));
        }

        [Test]
        public void FailedTransitionKeepsBeforeAndAfterStateEqual()
        {
            TurnTransitionResult result =
                TurnTransitionResult.Failure(
                    turnNumber: 3,
                    stage:
                        TurnStage.ActionResolution,
                    currentActionPhase:
                        1,
                    failureReason:
                        TurnTransitionFailureReason
                            .PlanningAlreadyConfirmed);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    result.CurrentStage));

            Assert.That(
                result.PreviousActionPhase,
                Is.EqualTo(
                    result.CurrentActionPhase));
        }
    }
}