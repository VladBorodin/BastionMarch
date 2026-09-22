using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Базовая реализация общей identity
    /// неизменяемого Order.
    ///
    /// Конкретные приказы наследуются от этого типа
    /// и добавляют только собственные immutable данные.
    /// </summary>
    public abstract class TurnOrder :
        ITurnOrder
    {
        public Guid OrderId
        {
            get;
        }

        public int RequiredPhases
        {
            get;
        }

        protected TurnOrder(
            Guid orderId,
            int requiredPhases)
        {
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(orderId));
            }

            if (requiredPhases < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredPhases),
                    requiredPhases,
                    "Required phases must be positive.");
            }

            OrderId =
                orderId;

            RequiredPhases =
                requiredPhases;
        }
    }
}