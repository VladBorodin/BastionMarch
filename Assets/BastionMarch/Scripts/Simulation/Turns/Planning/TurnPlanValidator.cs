using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Выполняет read-only проверку целостности
    /// TurnPlanDraft относительно текущего TurnCycle.
    /// </summary>
    public static class TurnPlanValidator
    {
        public static TurnPlanAssessment Assess(
            TurnCycle cycle,
            TurnPlanDraft draft)
        {
            if (cycle == null)
            {
                throw new ArgumentNullException(
                    nameof(cycle));
            }

            if (draft == null)
            {
                throw new ArgumentNullException(
                    nameof(draft));
            }

            if (cycle.Stage !=
                TurnStage.Planning)
            {
                return TurnPlanAssessment.Invalid(
                    TurnPlanFailureReason
                        .PlanningNotActive);
            }

            if (draft.TurnNumber !=
                cycle.TurnNumber)
            {
                return TurnPlanAssessment.Invalid(
                    TurnPlanFailureReason
                        .TurnNumberMismatch);
            }

            if (draft.ActionPhaseCount !=
                cycle.ActionPhaseCount)
            {
                return TurnPlanAssessment.Invalid(
                    TurnPlanFailureReason
                        .ActionPhaseCountMismatch);
            }

            if (!ParticipantSnapshotsMatch(
                    cycle.ActiveBrigades,
                    draft.Participants))
            {
                return TurnPlanAssessment.Invalid(
                    TurnPlanFailureReason
                        .ParticipantSnapshotMismatch);
            }

            HashSet<Guid> participantIds =
                draft.Participants
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToHashSet();

            foreach (ITurnOrder order in
                     draft.Orders)
            {
                if (order is
                        IBrigadeScopedOrder
                            brigadeOrder &&
                    !participantIds.Contains(
                        brigadeOrder.BrigadeId))
                {
                    return TurnPlanAssessment.Invalid(
                        TurnPlanFailureReason
                            .BrigadeOrderParticipantMismatch,
                        orderId:
                            order.OrderId,
                        brigadeId:
                            brigadeOrder.BrigadeId);
                }
            }

            Dictionary<Guid, ITurnOrder>
                ordersById =
                    draft.Orders.ToDictionary(
                        order =>
                            order.OrderId);

            foreach (PhaseReservation reservation in
                     draft.PhaseReservations)
            {
                if (!ordersById.TryGetValue(
                        reservation.OrderId,
                        out ITurnOrder order))
                {
                    return TurnPlanAssessment.Invalid(
                        TurnPlanFailureReason
                            .ReservationOrderNotFound,
                        orderId:
                            reservation.OrderId,
                        brigadeId:
                            reservation.BrigadeId,
                        actionPhase:
                            reservation.ActionPhase);
                }

                if (order is
                        IBrigadeScopedOrder
                            brigadeOrder &&
                    brigadeOrder.BrigadeId !=
                        reservation.BrigadeId)
                {
                    return TurnPlanAssessment.Invalid(
                        TurnPlanFailureReason
                            .BrigadeOrderReservationMismatch,
                        orderId:
                            order.OrderId,
                        brigadeId:
                            reservation.BrigadeId,
                        actionPhase:
                            reservation.ActionPhase);
                }
            }

            foreach (ITurnOrder order in
                     draft.Orders)
            {
                PhaseReservation[] reservations =
                    draft.PhaseReservations
                        .Where(reservation =>
                            reservation.OrderId ==
                            order.OrderId)
                        .ToArray();

                if (reservations.Length == 0)
                {
                    return TurnPlanAssessment.Invalid(
                        TurnPlanFailureReason
                            .OrderHasNoReservations,
                        orderId:
                            order.OrderId);
                }

                int scheduledPhaseCount =
                    reservations
                        .Select(reservation =>
                            reservation.ActionPhase)
                        .Distinct()
                        .Count();

                if (scheduledPhaseCount >
                    order.RequiredPhases)
                {
                    return TurnPlanAssessment.Invalid(
                        TurnPlanFailureReason
                            .OrderScheduledTooManyPhases,
                        orderId:
                            order.OrderId);
                }
            }

            return TurnPlanAssessment.Valid();
        }

        private static bool ParticipantSnapshotsMatch(
            IReadOnlyList<
                TurnBrigadeParticipant>
                    left,
            IReadOnlyList<
                TurnBrigadeParticipant>
                    right)
        {
            if (left.Count !=
                right.Count)
            {
                return false;
            }

            for (int index = 0;
                 index < left.Count;
                 index++)
            {
                TurnBrigadeParticipant
                    leftParticipant =
                        left[index];

                TurnBrigadeParticipant
                    rightParticipant =
                        right[index];

                if (leftParticipant.BrigadeId !=
                        rightParticipant.BrigadeId ||
                    leftParticipant.BrigadeNumber !=
                        rightParticipant.BrigadeNumber)
                {
                    return false;
                }
            }

            return true;
        }
    }
}