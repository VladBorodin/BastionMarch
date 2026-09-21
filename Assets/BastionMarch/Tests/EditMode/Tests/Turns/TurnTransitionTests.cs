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

        [Test]
        public void AdvanceActionPhaseMovesToNextPhase()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            cycle.TryConfirmPlanning();

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(2));

            Assert.That(
                cycle.HasActiveActionPhase,
                Is.True);
        }

        [Test]
        public void AdvanceActionPhaseResultDescribesPhaseChange()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 9,
                    actionPhaseCount: 3);

            cycle.TryConfirmPlanning();

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.TurnNumber,
                Is.EqualTo(9));

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                result.CurrentStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                result.PreviousActionPhase,
                Is.EqualTo(1));

            Assert.That(
                result.CurrentActionPhase,
                Is.EqualTo(2));
        }

        [Test]
        public void FinalActionPhaseTransitionsToTurnEnd()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            cycle.TryConfirmPlanning();

            cycle.TryAdvanceActionPhase();

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.HasActiveActionPhase,
                Is.False);
        }

        [Test]
        public void FinalActionPhaseResultDescribesTurnEndTransition()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 5,
                    actionPhaseCount: 2);

            cycle.TryConfirmPlanning();

            cycle.TryAdvanceActionPhase();

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                result.CurrentStage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                result.PreviousActionPhase,
                Is.EqualTo(2));

            Assert.That(
                result.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void SinglePhaseTurnTransitionsDirectlyToTurnEnd()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 1);

            cycle.TryConfirmPlanning();

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void CustomPhaseCountAdvancesThroughEveryPhase()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 4);

            cycle.TryConfirmPlanning();

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(2));

            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(3));

            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(4));

            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void ActionPhaseCannotAdvanceDuringPlanning()
        {
            var cycle =
                new TurnCycle();

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason
                        .ActionResolutionNotActive));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(1));
        }

        [Test]
        public void ActionPhaseCannotAdvanceAfterTurnEnd()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 1);

            cycle.TryConfirmPlanning();
            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason
                        .ActionResolutionNotActive));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void FailedActionAdvanceDoesNotMutateCycle()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 12,
                    actionPhaseCount: 3);

            TurnTransitionResult result =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.TurnNumber,
                Is.EqualTo(12));

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                result.CurrentStage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                result.PreviousActionPhase,
                Is.Null);

            Assert.That(
                result.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(12));

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(3));
        }

        [Test]
        public void CompletingActionResolutionDoesNotStartNextTurn()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 17,
                    actionPhaseCount: 2);

            cycle.TryConfirmPlanning();

            cycle.TryAdvanceActionPhase();
            cycle.TryAdvanceActionPhase();

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(17));
        }
    }
}