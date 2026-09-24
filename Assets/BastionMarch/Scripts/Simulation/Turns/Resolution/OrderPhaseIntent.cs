using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемое намерение выполнить
    /// один tick конкретного Order
    /// в конкретной Action Phase.
    ///
    /// Несколько Brigade reservations одного Order
    /// в одной фазе остаются одним execution intent.
    /// </summary>
    public sealed class OrderPhaseIntent
    {
        public ITurnOrder Order
        {
            get;
        }

        public Guid OrderId =>
            Order.OrderId;

        public int ActionPhase
        {
            get;
        }

        public IReadOnlyList<Guid>
            ReservedBrigadeIds
        {
            get;
        }

        public int ReservedBrigadeCount =>
            ReservedBrigadeIds.Count;

        public OrderPhaseIntent(
            ITurnOrder order,
            int actionPhase,
            IEnumerable<Guid>
                reservedBrigadeIds)
        {
            if (order == null)
            {
                throw new ArgumentNullException(
                    nameof(order));
            }

            if (actionPhase < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase),
                    actionPhase,
                    "Action phase must be positive.");
            }

            if (reservedBrigadeIds == null)
            {
                throw new ArgumentNullException(
                    nameof(reservedBrigadeIds));
            }

            Guid[] brigadeIds =
                reservedBrigadeIds.ToArray();

            if (brigadeIds.Length == 0)
            {
                throw new ArgumentException(
                    "Order phase intent requires " +
                    "at least one reserved brigade.",
                    nameof(reservedBrigadeIds));
            }

            if (brigadeIds.Any(
                    brigadeId =>
                        brigadeId == Guid.Empty))
            {
                throw new ArgumentException(
                    "Reserved brigade id " +
                    "cannot be empty.",
                    nameof(reservedBrigadeIds));
            }

            bool containsDuplicates =
                brigadeIds
                    .Distinct()
                    .Count() !=
                brigadeIds.Length;

            if (containsDuplicates)
            {
                throw new ArgumentException(
                    "Reserved brigade collection " +
                    "contains duplicate ids.",
                    nameof(reservedBrigadeIds));
            }

            Order =
                order;

            ActionPhase =
                actionPhase;

            ReservedBrigadeIds =
                new ReadOnlyCollection<Guid>(
                    brigadeIds);
        }
    }
}