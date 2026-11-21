
using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanBatchRegistrationTests
    {
        [Test]
        public void TwoPhaseBatchRegistersBothOrders()
        {
            Guid brigadeId = Guid.NewGuid();
            TurnPlanDraft draft = CreateDraft(brigadeId);

            MoveBrigadeOrder first = CreateMove(brigadeId);
            MoveBrigadeOrder second = CreateMove(brigadeId);

            bool added = draft.TryAddOrdersWithReservations(
                new (ITurnOrder Order, PhaseReservation Reservation)[]
                {
                    (first, new PhaseReservation(
                        first.OrderId, 1, brigadeId)),

                    (second, new PhaseReservation(
                        second.OrderId, 2, brigadeId))
                });

            Assert.That(added, Is.True);
            Assert.That(draft.OrderCount, Is.EqualTo(2));
            Assert.That(draft.ReservationCount, Is.EqualTo(2));

            Assert.That(draft.PhaseReservations
                .Select(r => r.ActionPhase),
                Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public void DuplicatePhaseRejectsEntireBatch()
        {
            Guid brigadeId = Guid.NewGuid();
            TurnPlanDraft draft = CreateDraft(brigadeId);

            MoveBrigadeOrder first = CreateMove(brigadeId);
            MoveBrigadeOrder second = CreateMove(brigadeId);

            bool added = draft.TryAddOrdersWithReservations(
                new (ITurnOrder Order, PhaseReservation Reservation)[]
                {
                    (first, new PhaseReservation(
                        first.OrderId, 1, brigadeId)),

                    (second, new PhaseReservation(
                        second.OrderId, 1, brigadeId))
                });

            Assert.That(added, Is.False);

            // Первый приказ тоже не должен попасть в Draft.
            Assert.That(draft.OrderCount, Is.Zero);
            Assert.That(draft.ReservationCount, Is.Zero);
        }

        [Test]
        public void ExistingPhaseConflictPreservesDraft()
        {
            Guid brigadeId = Guid.NewGuid();
            TurnPlanDraft draft = CreateDraft(brigadeId);

            MoveBrigadeOrder existing = CreateMove(brigadeId);

            Assert.That(draft.TryAddOrderWithReservation(
                existing,
                new PhaseReservation(
                    existing.OrderId, 2, brigadeId)),
                Is.True);

            MoveBrigadeOrder first = CreateMove(brigadeId);
            MoveBrigadeOrder second = CreateMove(brigadeId);

            bool added = draft.TryAddOrdersWithReservations(
                new (ITurnOrder Order, PhaseReservation Reservation)[]
                {
                    (first, new PhaseReservation(
                        first.OrderId, 1, brigadeId)),

                    (second, new PhaseReservation(
                        second.OrderId, 2, brigadeId))
                });

            Assert.That(added, Is.False);

            Assert.That(draft.OrderCount, Is.EqualTo(1));
            Assert.That(draft.ReservationCount, Is.EqualTo(1));

            Assert.That(draft.Orders.Single().OrderId,
                Is.EqualTo(existing.OrderId));
        }

        private static TurnPlanDraft CreateDraft(Guid brigadeId)
        {
            return new TurnPlanDraft(
                1,
                2,
                new[]
                {
                    new TurnBrigadeParticipant(brigadeId, 1)
                });
        }

        private static MoveBrigadeOrder CreateMove(Guid brigadeId)
        {
            return new MoveBrigadeOrder(
                Guid.NewGuid(),
                brigadeId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());
        }
    }
}
