using System;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class
        TurnPlanningConfirmationResultTests
    {
        [Test]
        public void ValidationFailurePublishesNoPlanOrTransition()
        {
            TurnPlanAssessment assessment =
                TurnPlanAssessment.Invalid(
                    TurnPlanFailureReason
                        .TurnNumberMismatch);

            TurnPlanningConfirmationResult result =
                TurnPlanningConfirmationResult
                    .ValidationFailure(
                        assessment);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.Assessment,
                Is.SameAs(
                    assessment));

            Assert.That(
                result.ConfirmedPlan,
                Is.Null);

            Assert.That(
                result.TransitionResult,
                Is.Null);
        }

        [Test]
        public void ValidationFailureRejectsValidAssessment()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    TurnPlanningConfirmationResult
                        .ValidationFailure(
                            TurnPlanAssessment
                                .Valid()));
        }

        [Test]
        public void TransitionFailurePublishesNoConfirmedPlan()
        {
            TurnPlanAssessment assessment =
                TurnPlanAssessment.Valid();

            TurnTransitionResult transition =
                TurnTransitionResult.Failure(
                    turnNumber: 1,
                    stage:
                        TurnStage.ActionResolution,
                    currentActionPhase: 1,
                    failureReason:
                        TurnTransitionFailureReason
                            .PlanningAlreadyConfirmed);

            TurnPlanningConfirmationResult result =
                TurnPlanningConfirmationResult
                    .TransitionFailure(
                        assessment,
                        transition);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.ConfirmedPlan,
                Is.Null);

            Assert.That(
                result.TransitionResult,
                Is.SameAs(
                    transition));
        }

        [Test]
        public void SuccessPublishesConfirmedPlanAndTransition()
        {
            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        Array.Empty<
                            TurnBrigadeParticipant>());

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            TurnPlanAssessment assessment =
                TurnPlanAssessment.Valid();

            TurnTransitionResult transition =
                TurnTransitionResult.Success(
                    turnNumber: 1,
                    previousStage:
                        TurnStage.Planning,
                    currentStage:
                        TurnStage.ActionResolution,
                    previousActionPhase: null,
                    currentActionPhase: 1);

            TurnPlanningConfirmationResult result =
                TurnPlanningConfirmationResult
                    .Success(
                        assessment,
                        plan,
                        transition);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.Assessment,
                Is.SameAs(
                    assessment));

            Assert.That(
                result.ConfirmedPlan,
                Is.SameAs(
                    plan));

            Assert.That(
                result.TransitionResult,
                Is.SameAs(
                    transition));
        }
    }
}