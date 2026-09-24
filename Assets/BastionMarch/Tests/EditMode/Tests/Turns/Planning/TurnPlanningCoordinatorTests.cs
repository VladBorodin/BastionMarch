using System;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanningCoordinatorTests
    {
        [Test]
        public void ValidEmptyPlanConfirmsTurnCycle()
        {
            var cycle =
                new TurnCycle();

            var draft =
                CreateDraftFromCycle(
                    cycle);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult result =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.Assessment.IsValid,
                Is.True);

            Assert.That(
                result.ConfirmedPlan,
                Is.Not.Null);

            Assert.That(
                result.TransitionResult,
                Is.Not.Null);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulConfirmationReturnsFrozenPlan()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        new[]
                        {
                            participant
                        });

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult result =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.ConfirmedPlan.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                result.ConfirmedPlan.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                result.ConfirmedPlan.Orders[0],
                Is.SameAs(order));
        }

        [Test]
        public void InvalidDraftDoesNotAdvanceTurnCycle()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 2);

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount:
                        cycle.ActionPhaseCount,
                    participants:
                        cycle.ActiveBrigades);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult result =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                result.ConfirmedPlan,
                Is.Null);

            Assert.That(
                result.TransitionResult,
                Is.Null);
        }

        [Test]
        public void InvalidDraftReturnsValidationDiagnostics()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 2);

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount:
                        cycle.ActionPhaseCount,
                    participants:
                        cycle.ActiveBrigades);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult result =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                result.Assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .TurnNumberMismatch));
        }

        [Test]
        public void RepeatedConfirmationIsRejected()
        {
            var cycle =
                new TurnCycle();

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult first =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                first.IsSuccess,
                Is.True);

            TurnPlanningConfirmationResult second =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                second.IsSuccess,
                Is.False);

            Assert.That(
                second.Assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .PlanningNotActive));

            Assert.That(
                second.ConfirmedPlan,
                Is.Null);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));
        }

        [Test]
        public void ConfirmedPlanDoesNotFollowLaterDraftChanges()
        {
            var cycle =
                new TurnCycle();

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanningConfirmationResult result =
                coordinator.TryConfirmPlan(
                    cycle,
                    draft);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddOrder(
                        new TestBastionOrder(
                            Guid.NewGuid(),
                            requiredPhases: 1,
                            bastionId:
                                Guid.NewGuid()))
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                result.ConfirmedPlan.OrderCount,
                Is.EqualTo(0));
        }

        [Test]
        public void CoordinatorRejectsNullCycle()
        {
            var coordinator =
                new TurnPlanningCoordinator();

            TurnPlanDraft draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        Array.Empty<
                            TurnBrigadeParticipant>());

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    coordinator.TryConfirmPlan(
                        null,
                        draft));
        }

        [Test]
        public void CoordinatorRejectsNullDraft()
        {
            var coordinator =
                new TurnPlanningCoordinator();

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    coordinator.TryConfirmPlan(
                        new TurnCycle(),
                        null));
        }

        private static TurnPlanDraft
            CreateDraftFromCycle(
                TurnCycle cycle)
        {
            return new TurnPlanDraft(
                cycle.TurnNumber,
                cycle.ActionPhaseCount,
                cycle.ActiveBrigades);
        }

        private static TurnBrigadeParticipant
            CreateParticipant()
        {
            return new TurnBrigadeParticipant(
                Guid.NewGuid(),
                brigadeNumber: 1);
        }

        private sealed class TestBrigadeOrder :
            BrigadeTurnOrder
        {
            public TestBrigadeOrder(
                Guid orderId,
                int requiredPhases,
                Guid brigadeId)
                : base(
                    orderId,
                    requiredPhases,
                    brigadeId)
            {
            }
        }

        private sealed class TestBastionOrder :
            BastionTurnOrder
        {
            public TestBastionOrder(
                Guid orderId,
                int requiredPhases,
                Guid bastionId)
                : base(
                    orderId,
                    requiredPhases,
                    bastionId)
            {
            }
        }
    }
}