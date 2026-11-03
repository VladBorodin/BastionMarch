
using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;

namespace BastionMarch.Presentation.Bastions.State
{
    public static class TurnPlanPresentationStateFactory
    {
        public static TurnPlanPresentationState Capture(
            TurnPlanDraft draft,
            BastionPresentationState bastionState)
        {
            if (draft == null)
                throw new ArgumentNullException(nameof(draft));

            if (bastionState == null)
                throw new ArgumentNullException(nameof(bastionState));

            var orders = draft.Orders.ToDictionary(
                order => order.OrderId);

            var participants = draft.Participants.ToDictionary(
                participant => participant.BrigadeId);

            var phases =
                new List<TurnPlanPhasePresentationState>();

            for (int phase = 1;
                 phase <= draft.ActionPhaseCount;
                 phase++)
            {
                var entries =
                    new List<TurnPlanEntryPresentationState>();

                foreach (PhaseReservation reservation in
                    draft.PhaseReservations.Where(
                        item => item.ActionPhase == phase))
                {
                    if (!orders.TryGetValue(
                            reservation.OrderId,
                            out ITurnOrder order))
                    {
                        throw new InvalidOperationException(
                            "Reservation references a missing order.");
                    }

                    if (!participants.TryGetValue(
                            reservation.BrigadeId,
                            out TurnBrigadeParticipant participant))
                    {
                        throw new InvalidOperationException(
                            "Reservation references a missing brigade.");
                    }

                    string description;

                    if (order is MoveBrigadeOrder move)
                    {
                        if (!bastionState.TryGetModule(
                                move.TargetModuleId,
                                out ModulePresentationState target))
                        {
                            throw new InvalidOperationException(
                                "Move target is missing from the snapshot.");
                        }

                        description =
                            $"#{participant.BrigadeNumber} " +
                            $"переход → {target.DefinitionId} " +
                            $"(X:{target.Position.X}, " +
                            $"палуба:{target.Position.Deck})";
                    }
                    else
                    {
                        description =
                            $"#{participant.BrigadeNumber} " +
                            order.GetType().Name;
                    }

                    entries.Add(
                        new TurnPlanEntryPresentationState(
                            order.OrderId,
                            reservation.BrigadeId,
                            participant.BrigadeNumber,
                            description));
                }

                phases.Add(
                    new TurnPlanPhasePresentationState(
                        phase,
                        entries
                            .OrderBy(item => item.BrigadeNumber)
                            .ThenBy(item => item.BrigadeId)
                            .ThenBy(item => item.OrderId)));
            }

            int unscheduledCount =
                draft.Orders.Count(order =>
                    !draft.PhaseReservations.Any(
                        reservation =>
                            reservation.OrderId == order.OrderId));

            return new TurnPlanPresentationState(
                draft.TurnNumber,
                phases,
                unscheduledCount);
        }
    }
}