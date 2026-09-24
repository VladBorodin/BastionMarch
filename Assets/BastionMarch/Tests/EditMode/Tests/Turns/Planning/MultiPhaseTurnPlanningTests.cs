using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class MultiPhaseTurnPlanningTests
    {
        [Test]
        public void OrderMayRequireMorePhasesThanCurrentTurnContains()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 5,
                    brigade.BrigadeId);

            TurnPlanDraft draft =
                CreateDraft(
                    brigade);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                order.RequiredPhases,
                Is.EqualTo(5));

            Assert.That(
                draft.ActionPhaseCount,
                Is.EqualTo(2));
        }

        [Test]
        public void SameOrderMayReserveSameBrigadeAcrossMultiplePhases()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2,
                    brigade.BrigadeId);

            TurnPlanDraft draft =
                CreateDraft(
                    brigade);

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 2,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.PhaseReservations
                    .Count(reservation =>
                        reservation.OrderId ==
                        order.OrderId),
                Is.EqualTo(2));
        }

        [Test]
        public void MultipleBrigadeReservationsInSamePhaseRepresentOneOrderPhase()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            TurnPlanDraft draft =
                CreateDraft(
                    first,
                    second);

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

            int distinctScheduledPhases =
                draft.PhaseReservations
                    .Where(reservation =>
                        reservation.OrderId ==
                        order.OrderId)
                    .Select(reservation =>
                        reservation.ActionPhase)
                    .Distinct()
                    .Count();

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(2));

            Assert.That(
                distinctScheduledPhases,
                Is.EqualTo(1));
        }

        [Test]
        public void SameImmutableOrderCanBeRepresentedInNextTurnDraft()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 5,
                    brigade.BrigadeId);

            TurnPlanDraft firstTurn =
                new TurnPlanDraft(
                    turnNumber: 10,
                    actionPhaseCount: 2,
                    participants:
                        new[]
                        {
                            brigade
                        });

            Assert.That(
                firstTurn.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                firstTurn.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                firstTurn.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 2,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            TurnPlanDraft secondTurn =
                new TurnPlanDraft(
                    turnNumber: 11,
                    actionPhaseCount: 2,
                    participants:
                        new[]
                        {
                            brigade
                        });

            Assert.That(
                secondTurn.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                secondTurn.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                secondTurn.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 2,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                firstTurn.Orders[0],
                Is.SameAs(order));

            Assert.That(
                secondTurn.Orders[0],
                Is.SameAs(order));

            Assert.That(
                firstTurn.TurnNumber,
                Is.EqualTo(10));

            Assert.That(
                secondTurn.TurnNumber,
                Is.EqualTo(11));
        }

        [Test]
        public void CarryOverRepresentationDoesNotRequirePreviousDraft()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 5,
                    brigade.BrigadeId);

            TurnPlanDraft nextTurn =
                new TurnPlanDraft(
                    turnNumber: 12,
                    actionPhaseCount: 2,
                    participants:
                        new[]
                        {
                            brigade
                        });

            Assert.That(
                nextTurn.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                nextTurn.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            actionPhase: 1,
                            brigade.BrigadeId))
                    .IsSuccess,
                Is.True);

            Assert.That(
                nextTurn.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                nextTurn.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                nextTurn.Orders[0].RequiredPhases,
                Is.EqualTo(5));
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