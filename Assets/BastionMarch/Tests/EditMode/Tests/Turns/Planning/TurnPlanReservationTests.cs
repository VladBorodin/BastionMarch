using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanReservationTests
    {
        [Test]
        public void NewDraftStartsWithoutReservations()
        {
            TurnPlanDraft draft =
                CreateDraft();

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(0));

            Assert.That(
                draft.PhaseReservations,
                Is.Empty);
        }

        [Test]
        public void DraftAddsValidReservation()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            var reservation =
                new PhaseReservation(
                    Guid.NewGuid(),
                    actionPhase: 1,
                    participant.BrigadeId);

            PhaseReservationResult result =
                draft.TryAddReservation(
                    reservation);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    PhaseReservationFailureReason
                        .None));

            Assert.That(
                result.Reservation,
                Is.SameAs(
                    reservation));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                draft.PhaseReservations[0],
                Is.SameAs(
                    reservation));
        }

        [Test]
        public void DraftRejectsPhaseOutsideTurn()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            PhaseReservationResult result =
                draft.TryAddReservation(
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 3,
                        participant.BrigadeId));

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    PhaseReservationFailureReason
                        .ActionPhaseOutOfRange));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(0));
        }

        [Test]
        public void DraftRejectsNonParticipantBrigade()
        {
            TurnPlanDraft draft =
                CreateDraft(
                    CreateParticipant(
                        number: 1));

            PhaseReservationResult result =
                draft.TryAddReservation(
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 1,
                        brigadeId:
                            Guid.NewGuid()));

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    PhaseReservationFailureReason
                        .BrigadeNotParticipant));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(0));
        }

        [Test]
        public void DraftRejectsDuplicateReservation()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            Guid orderId =
                Guid.NewGuid();

            var first =
                new PhaseReservation(
                    orderId,
                    actionPhase: 1,
                    participant.BrigadeId);

            var duplicate =
                new PhaseReservation(
                    orderId,
                    actionPhase: 1,
                    participant.BrigadeId);

            Assert.That(
                draft.TryAddReservation(
                        first)
                    .IsSuccess,
                Is.True);

            PhaseReservationResult result =
                draft.TryAddReservation(
                    duplicate);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    PhaseReservationFailureReason
                        .ReservationAlreadyExists));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(1));
        }

        [Test]
        public void DraftRejectsDifferentOrderInOccupiedBrigadePhase()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            Assert.That(
                draft.TryAddReservation(
                        new PhaseReservation(
                            Guid.NewGuid(),
                            actionPhase: 1,
                            participant.BrigadeId))
                    .IsSuccess,
                Is.True);

            PhaseReservationResult result =
                draft.TryAddReservation(
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 1,
                        participant.BrigadeId));

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    PhaseReservationFailureReason
                        .BrigadePhaseAlreadyReserved));

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(1));
        }

        [Test]
        public void SameOrderCanReserveMultipleBrigadesInSamePhase()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            TurnPlanDraft draft =
                CreateDraft(
                    first,
                    second);

            Guid orderId =
                Guid.NewGuid();

            PhaseReservationResult firstResult =
                draft.TryAddReservation(
                    new PhaseReservation(
                        orderId,
                        actionPhase: 1,
                        first.BrigadeId));

            PhaseReservationResult secondResult =
                draft.TryAddReservation(
                    new PhaseReservation(
                        orderId,
                        actionPhase: 1,
                        second.BrigadeId));

            Assert.That(
                firstResult.IsSuccess,
                Is.True);

            Assert.That(
                secondResult.IsSuccess,
                Is.True);

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(2));
        }

        [Test]
        public void SameBrigadeCanBeReservedInDifferentPhases()
        {
            TurnBrigadeParticipant participant =
                CreateParticipant(
                    number: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    participant);

            PhaseReservationResult first =
                draft.TryAddReservation(
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 1,
                        participant.BrigadeId));

            PhaseReservationResult second =
                draft.TryAddReservation(
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 2,
                        participant.BrigadeId));

            Assert.That(
                first.IsSuccess,
                Is.True);

            Assert.That(
                second.IsSuccess,
                Is.True);

            Assert.That(
                draft.ReservationCount,
                Is.EqualTo(2));
        }

        [Test]
        public void ReservationsUseDeterministicOrder()
        {
            Guid firstId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            var brigadeTwo =
                new TurnBrigadeParticipant(
                    secondId,
                    brigadeNumber: 2);

            var brigadeOne =
                new TurnBrigadeParticipant(
                    firstId,
                    brigadeNumber: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    brigadeTwo,
                    brigadeOne);

            Guid phaseTwoOrder =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000013");

            Guid brigadeTwoOrder =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000012");

            Guid brigadeOneOrder =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000011");

            draft.TryAddReservation(
                new PhaseReservation(
                    phaseTwoOrder,
                    actionPhase: 2,
                    brigadeOne.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    brigadeTwoOrder,
                    actionPhase: 1,
                    brigadeTwo.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    brigadeOneOrder,
                    actionPhase: 1,
                    brigadeOne.BrigadeId));

            CollectionAssert.AreEqual(
                new[]
                {
                    brigadeOneOrder,
                    brigadeTwoOrder,
                    phaseTwoOrder
                },
                draft.PhaseReservations
                    .Select(reservation =>
                        reservation.OrderId)
                    .ToArray());
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
                int number)
        {
            return new TurnBrigadeParticipant(
                Guid.NewGuid(),
                number);
        }
    }
}