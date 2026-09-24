using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Неизменяемый snapshot подтверждаемого плана хода.
    ///
    /// Не хранит ссылку на TurnPlanDraft.
    /// Все коллекции копируются при создании.
    ///
    /// CreateSnapshot является низкоуровневой
    /// операцией freeze и сам по себе не выполняет
    /// planning validation.
    /// </summary>
    public sealed class ConfirmedTurnPlan
    {
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

        public IReadOnlyList<ITurnOrder>
            Orders
        {
            get;
        }

        public IReadOnlyList<PhaseReservation>
            PhaseReservations
        {
            get;
        }

        public int ParticipantCount =>
            Participants.Count;

        public int OrderCount =>
            Orders.Count;

        public int ReservationCount =>
            PhaseReservations.Count;

        private ConfirmedTurnPlan(
            int turnNumber,
            int actionPhaseCount,
            IReadOnlyList<
                TurnBrigadeParticipant>
                    participants,
            IReadOnlyList<ITurnOrder> orders,
            IReadOnlyList<
                PhaseReservation>
                    phaseReservations)
        {
            TurnNumber =
                turnNumber;

            ActionPhaseCount =
                actionPhaseCount;

            Participants =
                participants;

            Orders =
                orders;

            PhaseReservations =
                phaseReservations;
        }

        public static ConfirmedTurnPlan
            CreateSnapshot(
                TurnPlanDraft draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(
                    nameof(draft));
            }

            TurnBrigadeParticipant[]
                participantSnapshot =
                    draft.Participants
                        .ToArray();

            ITurnOrder[] orderSnapshot =
                draft.Orders
                    .ToArray();

            PhaseReservation[]
                reservationSnapshot =
                    draft.PhaseReservations
                        .ToArray();

            return new ConfirmedTurnPlan(
                turnNumber:
                    draft.TurnNumber,
                actionPhaseCount:
                    draft.ActionPhaseCount,
                participants:
                    new ReadOnlyCollection<
                        TurnBrigadeParticipant>(
                            participantSnapshot),
                orders:
                    new ReadOnlyCollection<
                        ITurnOrder>(
                            orderSnapshot),
                phaseReservations:
                    new ReadOnlyCollection<
                        PhaseReservation>(
                            reservationSnapshot));
        }
    }
}