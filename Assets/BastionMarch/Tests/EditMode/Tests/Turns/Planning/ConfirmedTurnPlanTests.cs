using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class ConfirmedTurnPlanTests
    {
        [Test]
        public void SnapshotStoresTurnIdentity()
        {
            TurnPlanDraft draft =
                CreateDraft();

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                confirmed.TurnNumber,
                Is.EqualTo(
                    draft.TurnNumber));

            Assert.That(
                confirmed.ActionPhaseCount,
                Is.EqualTo(
                    draft.ActionPhaseCount));
        }

        [Test]
        public void EmptyDraftProducesEmptySnapshot()
        {
            TurnPlanDraft draft =
                CreateDraft();

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                confirmed.ParticipantCount,
                Is.EqualTo(0));

            Assert.That(
                confirmed.OrderCount,
                Is.EqualTo(0));

            Assert.That(
                confirmed.ReservationCount,
                Is.EqualTo(0));
        }

        [Test]
        public void SnapshotCopiesParticipants()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                confirmed.ParticipantCount,
                Is.EqualTo(1));

            Assert.That(
                confirmed.Participants[0],
                Is.SameAs(participant));

            Assert.That(
                confirmed.Participants,
                Is.Not.SameAs(
                    draft.Participants));
        }

        [Test]
        public void SnapshotCopiesOrders()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                confirmed.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                confirmed.Orders[0],
                Is.SameAs(order));

            Assert.That(
                confirmed.Orders,
                Is.Not.SameAs(
                    draft.Orders));
        }

        [Test]
        public void SnapshotCopiesReservations()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            var reservation =
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddReservation(
                        reservation)
                    .IsSuccess,
                Is.True);

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                confirmed.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                confirmed.PhaseReservations[0],
                Is.SameAs(reservation));

            Assert.That(
                confirmed.PhaseReservations,
                Is.Not.SameAs(
                    draft.PhaseReservations));
        }

        [Test]
        public void AddingOrderToDraftAfterSnapshotDoesNotChangeConfirmedPlan()
        {
            TurnPlanDraft draft =
                CreateDraft();

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

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
                confirmed.OrderCount,
                Is.EqualTo(0));
        }

        [Test]
        public void AddingReservationToDraftAfterSnapshotDoesNotChangeConfirmedPlan()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                confirmed.ReservationCount,
                Is.EqualTo(0));
        }

        [Test]
        public void SnapshotCollectionsAreReadOnly()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.That(
                ((IList<
                    TurnBrigadeParticipant>)
                        confirmed.Participants)
                    .IsReadOnly,
                Is.True);

            Assert.That(
                ((IList<ITurnOrder>)
                    confirmed.Orders)
                    .IsReadOnly,
                Is.True);

            Assert.That(
                ((IList<PhaseReservation>)
                    confirmed.PhaseReservations)
                    .IsReadOnly,
                Is.True);
        }

        [Test]
        public void SnapshotPreservesDeterministicDraftOrder()
        {
            Guid firstId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            TurnPlanDraft draft =
                CreateDraft();

            draft.TryAddOrder(
                new TestBastionOrder(
                    secondId,
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid()));

            draft.TryAddOrder(
                new TestBastionOrder(
                    firstId,
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid()));

            ConfirmedTurnPlan confirmed =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            CollectionAssert.AreEqual(
                new[]
                {
                    firstId,
                    secondId
                },
                confirmed.Orders
                    .Select(order =>
                        order.OrderId)
                    .ToArray());
        }

        [Test]
        public void SnapshotRejectsNullDraft()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    ConfirmedTurnPlan
                        .CreateSnapshot(
                            null));
        }

        private static TurnPlanDraft CreateDraft(
            params TurnBrigadeParticipant[]
                participants)
        {
            return new TurnPlanDraft(
                turnNumber: 1,
                actionPhaseCount: 2,
                participants:
                    participants);
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