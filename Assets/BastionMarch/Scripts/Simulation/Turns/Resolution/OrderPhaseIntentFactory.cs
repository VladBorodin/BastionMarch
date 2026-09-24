using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Строит детерминированную read-only проекцию
    /// scheduled Order ticks конкретной Action Phase.
    ///
    /// Один OrderId в одной фазе всегда создаёт
    /// не более одного OrderPhaseIntent.
    /// </summary>
    public static class OrderPhaseIntentFactory
    {
        public static IReadOnlyList<
            OrderPhaseIntent>
                CreateForPhase(
                    ConfirmedTurnPlan plan,
                    int actionPhase)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(
                    nameof(plan));
            }

            if (actionPhase < 1 ||
                actionPhase >
                    plan.ActionPhaseCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase),
                    actionPhase,
                    "Action phase must be " +
                    "inside the confirmed plan.");
            }

            Dictionary<Guid, ITurnOrder>
                ordersById =
                    plan.Orders.ToDictionary(
                        order =>
                            order.OrderId);

            Dictionary<Guid, int>
                participantOrder =
                    plan.Participants
                        .Select(
                            (participant, index) =>
                                new
                                {
                                    participant
                                        .BrigadeId,
                                    Index = index
                                })
                        .ToDictionary(
                            item =>
                                item.BrigadeId,
                            item =>
                                item.Index);

            PhaseReservation[]
                phaseReservations =
                    plan.PhaseReservations
                        .Where(reservation =>
                            reservation.ActionPhase ==
                            actionPhase)
                        .ToArray();

            foreach (PhaseReservation reservation in
                     phaseReservations)
            {
                if (!ordersById.ContainsKey(
                        reservation.OrderId))
                {
                    throw new InvalidOperationException(
                        "Confirmed plan contains " +
                        "a reservation for an " +
                        "unknown order.");
                }

                if (!participantOrder.ContainsKey(
                        reservation.BrigadeId))
                {
                    throw new InvalidOperationException(
                        "Confirmed plan contains " +
                        "a reservation for an " +
                        "unknown brigade.");
                }
            }

            OrderPhaseIntent[] intents =
                phaseReservations
                    .GroupBy(reservation =>
                        reservation.OrderId)
                    .Select(group =>
                    {
                        ITurnOrder order =
                            ordersById[
                                group.Key];

                        Guid[] brigadeIds =
                            group
                                .Select(reservation =>
                                    reservation.BrigadeId)
                                .OrderBy(brigadeId =>
                                    participantOrder[
                                        brigadeId])
                                .ToArray();

                        return new OrderPhaseIntent(
                            order,
                            actionPhase,
                            brigadeIds);
                    })
                    .OrderBy(intent =>
                        intent.OrderId)
                    .ToArray();

            return new ReadOnlyCollection<
                OrderPhaseIntent>(
                    intents);
        }
    }
}