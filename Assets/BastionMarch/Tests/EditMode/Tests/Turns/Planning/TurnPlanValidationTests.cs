using System;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanValidationTests
    {
        [Test]
        public void EmptyCurrentPlanningDraftIsValid()
        {
            TurnCycle cycle =
                new TurnCycle();

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.IsValid,
                Is.True);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason.None));
        }

        [Test]
        public void AssessmentRejectsWhenPlanningIsNotActive()
        {
            TurnCycle cycle =
                new TurnCycle();

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            Assert.That(
                cycle.TryConfirmPlanning()
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.IsValid,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .PlanningNotActive));
        }

        [Test]
        public void AssessmentRejectsTurnNumberMismatch()
        {
            TurnCycle cycle =
                new TurnCycle(
                    turnNumber: 2);

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount:
                        cycle.ActionPhaseCount,
                    participants:
                        cycle.ActiveBrigades);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .TurnNumberMismatch));
        }

        [Test]
        public void AssessmentRejectsActionPhaseCountMismatch()
        {
            TurnCycle cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 3);

            var draft =
                new TurnPlanDraft(
                    turnNumber:
                        cycle.TurnNumber,
                    actionPhaseCount: 2,
                    participants:
                        cycle.ActiveBrigades);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .ActionPhaseCountMismatch));
        }

        [Test]
        public void AssessmentRejectsDifferentParticipantId()
        {
            var cycleParticipant =
                new TurnBrigadeParticipant(
                    Guid.NewGuid(),
                    brigadeNumber: 1);

            var draftParticipant =
                new TurnBrigadeParticipant(
                    Guid.NewGuid(),
                    brigadeNumber: 1);

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        new[]
                        {
                            cycleParticipant
                        });

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        new[]
                        {
                            draftParticipant
                        });

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .ParticipantSnapshotMismatch));
        }

        [Test]
        public void AssessmentRejectsDifferentParticipantNumber()
        {
            Guid brigadeId =
                Guid.NewGuid();

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        new[]
                        {
                            new TurnBrigadeParticipant(
                                brigadeId,
                                brigadeNumber: 1)
                        });

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        new[]
                        {
                            new TurnBrigadeParticipant(
                                brigadeId,
                                brigadeNumber: 2)
                        });

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .ParticipantSnapshotMismatch));
        }

        [Test]
        public void AssessmentRejectsBrigadeOrderForNonParticipant()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnCycle cycle =
                CreateCycle(
                    participant);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    brigadeId:
                        Guid.NewGuid());

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .BrigadeOrderParticipantMismatch));

            Assert.That(
                assessment.OrderId,
                Is.EqualTo(
                    order.OrderId));

            Assert.That(
                assessment.BrigadeId,
                Is.EqualTo(
                    order.BrigadeId));
        }

        [Test]
        public void AssessmentRejectsReservationWithoutOrder()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnCycle cycle =
                CreateCycle(
                    participant);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            Guid unknownOrderId =
                Guid.NewGuid();

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            unknownOrderId,
                            actionPhase: 1,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .ReservationOrderNotFound));

            Assert.That(
                assessment.OrderId,
                Is.EqualTo(
                    unknownOrderId));
        }

        [Test]
        public void AssessmentRejectsReservationForWrongBrigadeScope()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            TurnCycle cycle =
                CreateCycle(
                    first,
                    second);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    brigadeId:
                        first.BrigadeId);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            second.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .BrigadeOrderReservationMismatch));

            Assert.That(
                assessment.BrigadeId,
                Is.EqualTo(
                    second.BrigadeId));

            Assert.That(
                assessment.ActionPhase,
                Is.EqualTo(1));
        }

        [Test]
        public void AssessmentRejectsOrderWithoutReservation()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnCycle cycle =
                CreateCycle(
                    participant);

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

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .OrderHasNoReservations));

            Assert.That(
                assessment.OrderId,
                Is.EqualTo(
                    order.OrderId));
        }

        [Test]
        public void AssessmentRejectsOrderScheduledForTooManyPhases()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnCycle cycle =
                CreateCycle(
                    participant);

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

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 2,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .OrderScheduledTooManyPhases));
        }

        [Test]
        public void BastionOrderMayReserveSeveralBrigadesInOnePhase()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            TurnCycle cycle =
                CreateCycle(
                    first,
                    second);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            first.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            second.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.IsValid,
                Is.True);
        }

        [Test]
        public void MultiPhaseOrderMayUseOnlyCurrentTurnPhases()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnCycle cycle =
                CreateCycle(
                    participant);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 5,
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

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 2,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            Assert.That(
                assessment.IsValid,
                Is.True);
        }

        [Test]
        public void AssessmentDoesNotMutateCycleOrDraft()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnCycle cycle =
                CreateCycle(
                    participant);

            TurnPlanDraft draft =
                CreateDraftFromCycle(
                    cycle);

            int originalOrderCount =
                draft.OrderCount;

            int originalReservationCount =
                draft.ReservationCount;

            TurnStage originalStage =
                cycle.Stage;

            TurnPlanValidator.Assess(
                cycle,
                draft);

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(
                    originalOrderCount));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(
                    originalReservationCount));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    originalStage));
        }

        private static TurnCycle CreateCycle(
            params TurnBrigadeParticipant[]
                participants)
        {
            return new TurnCycle(
                turnNumber: 1,
                actionPhaseCount: 2,
                activeBrigades:
                    participants);
        }

        private static TurnPlanDraft
            CreateDraftFromCycle(
                TurnCycle cycle)
        {
            return new TurnPlanDraft(
                turnNumber:
                    cycle.TurnNumber,
                actionPhaseCount:
                    cycle.ActionPhaseCount,
                participants:
                    cycle.ActiveBrigades);
        }

        private static TurnBrigadeParticipant
            CreateParticipant(
                int number = 1)
        {
            return new TurnBrigadeParticipant(
                Guid.NewGuid(),
                number);
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