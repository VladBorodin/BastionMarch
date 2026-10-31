
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Planning;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Строит Activity intents для бригад,
    /// не занятых blocking Orders в текущей фазе.
    ///
    /// Activity запрашивается из внешнего
    /// read-only источника phase-start state.
    ///
    /// Не хранит Activity и не изменяет мир.
    /// </summary>
    public static class BrigadeActivityPhaseIntentFactory
    {
        public static IReadOnlyList<BrigadeActivityPhaseIntent>
            CreateForPhase(
                ConfirmedTurnPlan plan,
                int actionPhase,
                Func<Guid, IBrigadeActivity> activityResolver)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(
                    nameof(plan));
            }

            if (activityResolver == null)
            {
                throw new ArgumentNullException(
                    nameof(activityResolver));
            }

            if (actionPhase < 1 ||
                actionPhase > plan.ActionPhaseCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase),
                    actionPhase,
                    "Action phase must be inside the plan.");
            }

            HashSet<Guid> participantIds =
                plan.Participants
                    .Select(participant => participant.BrigadeId)
                    .ToHashSet();

            HashSet<Guid> orderIds =
                plan.Orders
                    .Select(order => order.OrderId)
                    .ToHashSet();

            HashSet<Guid> reservedBrigadeIds =
                new HashSet<Guid>();

            foreach (PhaseReservation reservation in
                     plan.PhaseReservations)
            {
                if (reservation.ActionPhase != actionPhase)
                {
                    continue;
                }

                if (!orderIds.Contains(reservation.OrderId))
                {
                    throw new InvalidOperationException(
                        "Reservation references an unknown order.");
                }

                if (!participantIds.Contains(reservation.BrigadeId))
                {
                    throw new InvalidOperationException(
                        "Reservation references an unknown brigade.");
                }

                if (!reservedBrigadeIds.Add(reservation.BrigadeId))
                {
                    throw new InvalidOperationException(
                        "Brigade is reserved more than once " +
                        "in the same action phase.");
                }
            }

            var intents =
                new List<BrigadeActivityPhaseIntent>();

            foreach (TurnBrigadeParticipant participant in
                     plan.Participants)
            {
                Guid brigadeId = participant.BrigadeId;

                if (reservedBrigadeIds.Contains(brigadeId))
                {
                    continue;
                }

                IBrigadeActivity activity =
                    activityResolver(brigadeId);

                if (activity == null)
                {
                    continue;
                }

                if (activity.BrigadeId != brigadeId)
                {
                    throw new InvalidOperationException(
                        "Resolved activity belongs to " +
                        "a different brigade.");
                }

                intents.Add(
                    new BrigadeActivityPhaseIntent(
                        activity,
                        actionPhase));
            }

            return new ReadOnlyCollection<BrigadeActivityPhaseIntent>(
                intents);
        }
    }
}
