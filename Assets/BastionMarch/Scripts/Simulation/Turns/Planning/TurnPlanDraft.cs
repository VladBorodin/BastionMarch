using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Редактируемый план одного игрового хода.
    ///
    /// На этапе 13.1 содержит только
    /// временную identity хода и immutable snapshot
    /// его участников.
    ///
    /// Orders и reservations добавляются
    /// последующими подэтапами Stage 13.
    /// </summary>
    public sealed class TurnPlanDraft
    {

        private readonly List<PhaseReservation>
            _phaseReservations;

        private readonly ReadOnlyCollection<
            PhaseReservation>
                _phaseReservationsView;

        private readonly List<ITurnOrder>
            _orders;

        private readonly ReadOnlyCollection<
            ITurnOrder>
                _ordersView;

        public int TurnNumber
        {
            get;
        }

        public int ActionPhaseCount
        {
            get;
        }

        public IReadOnlyList<
            TurnBrigadeParticipant>
                Participants
        {
            get;
        }

        public int ParticipantCount =>
            Participants.Count;

        public IReadOnlyList<ITurnOrder>
            Orders =>
                _ordersView;

        public int OrderCount =>
            _orders.Count;

        public IReadOnlyList<PhaseReservation>
            PhaseReservations =>
                _phaseReservationsView;

        public int ReservationCount =>
            _phaseReservations.Count;

        public TurnPlanDraft(
            int turnNumber,
            int actionPhaseCount,
            IEnumerable<
                TurnBrigadeParticipant>
                    participants)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber),
                    turnNumber,
                    "Turn number must be positive.");
            }

            if (actionPhaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhaseCount),
                    actionPhaseCount,
                    "Action phase count must be positive.");
            }

            if (participants == null)
            {
                throw new ArgumentNullException(
                    nameof(participants));
            }

            TurnBrigadeParticipant[] participantArray =
                participants.ToArray();

            if (participantArray.Any(
                    participant =>
                        participant == null))
            {
                throw new ArgumentException(
                    "Participant collection " +
                    "cannot contain null.",
                    nameof(participants));
            }

            bool containsDuplicateIds =
                participantArray
                    .GroupBy(participant =>
                        participant.BrigadeId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateIds)
            {
                throw new ArgumentException(
                    "Participant collection " +
                    "contains duplicate brigade ids.",
                    nameof(participants));
            }

            TurnBrigadeParticipant[]
                orderedParticipants =
                    participantArray
                        .OrderBy(participant =>
                            participant.BrigadeNumber)
                        .ThenBy(participant =>
                            participant.BrigadeId)
                        .ToArray();

            TurnNumber =
                turnNumber;

            ActionPhaseCount =
                actionPhaseCount;

            Participants =
                new ReadOnlyCollection<
                    TurnBrigadeParticipant>(
                        orderedParticipants);

            _phaseReservations =
                new List<PhaseReservation>();

            _phaseReservationsView =
                _phaseReservations.AsReadOnly();

            _orders =
                new List<ITurnOrder>();

            _ordersView =
                _orders.AsReadOnly();
        }

        public PhaseReservationResult
            TryAddReservation(
                PhaseReservation reservation)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(
                    nameof(reservation));
            }

            if (reservation.ActionPhase >
                ActionPhaseCount)
            {
                return PhaseReservationResult.Failure(
                    PhaseReservationFailureReason
                        .ActionPhaseOutOfRange);
            }

            bool brigadeIsParticipant =
                Participants.Any(
                    participant =>
                        participant.BrigadeId ==
                        reservation.BrigadeId);

            if (!brigadeIsParticipant)
            {
                return PhaseReservationResult.Failure(
                    PhaseReservationFailureReason
                        .BrigadeNotParticipant);
            }

            PhaseReservation existingReservation =
                _phaseReservations
                    .FirstOrDefault(
                        existing =>
                            existing.BrigadeId ==
                                reservation.BrigadeId &&
                            existing.ActionPhase ==
                                reservation.ActionPhase);

            if (existingReservation != null)
            {
                if (existingReservation.OrderId ==
                    reservation.OrderId)
                {
                    return PhaseReservationResult.Failure(
                        PhaseReservationFailureReason
                            .ReservationAlreadyExists);
                }

                return PhaseReservationResult.Failure(
                    PhaseReservationFailureReason
                        .BrigadePhaseAlreadyReserved);
            }

            _phaseReservations.Add(
                reservation);

            SortReservations();

            return PhaseReservationResult.Success(
                reservation);
        }

        private void SortReservations()
        {
            _phaseReservations.Sort(
                CompareReservations);
        }

        private int CompareReservations(
            PhaseReservation left,
            PhaseReservation right)
        {
            int phaseComparison =
                left.ActionPhase.CompareTo(
                    right.ActionPhase);

            if (phaseComparison != 0)
            {
                return phaseComparison;
            }

            TurnBrigadeParticipant leftParticipant =
                Participants.First(
                    participant =>
                        participant.BrigadeId ==
                        left.BrigadeId);

            TurnBrigadeParticipant rightParticipant =
                Participants.First(
                    participant =>
                        participant.BrigadeId ==
                        right.BrigadeId);

            int numberComparison =
                leftParticipant.BrigadeNumber.CompareTo(
                    rightParticipant.BrigadeNumber);

            if (numberComparison != 0)
            {
                return numberComparison;
            }

            int brigadeComparison =
                left.BrigadeId.CompareTo(
                    right.BrigadeId);

            if (brigadeComparison != 0)
            {
                return brigadeComparison;
            }

            return left.OrderId.CompareTo(
                right.OrderId);
        }

        public TurnPlanOrderResult TryAddOrder(
            ITurnOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(
                    nameof(order));
            }

            if (order.OrderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(order));
            }

            if (order.RequiredPhases < 1)
            {
                throw new ArgumentException(
                    "Order required phases " +
                    "must be positive.",
                    nameof(order));
            }

            bool duplicateOrderId =
                _orders.Any(
                    existing =>
                        existing.OrderId ==
                        order.OrderId);

            if (duplicateOrderId)
            {
                return TurnPlanOrderResult.Failure(
                    TurnPlanOrderFailureReason
                        .DuplicateOrderId);
            }

            _orders.Add(
                order);

            _orders.Sort(
                (left, right) =>
                    left.OrderId.CompareTo(
                        right.OrderId));

            return TurnPlanOrderResult.Success(
                order);
        }
    }
}