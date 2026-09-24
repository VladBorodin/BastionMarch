using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Базовый immutable Order,
    /// относящийся к одной Brigade.
    /// </summary>
    public abstract class BrigadeTurnOrder :
        TurnOrder,
        IBrigadeScopedOrder
    {
        public Guid BrigadeId
        {
            get;
        }

        protected BrigadeTurnOrder(
            Guid orderId,
            int requiredPhases,
            Guid brigadeId)
            : base(
                orderId,
                requiredPhases)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            BrigadeId =
                brigadeId;
        }
    }
}