using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Базовый immutable Order,
    /// относящийся к Bastion в целом.
    /// </summary>
    public abstract class BastionTurnOrder :
        TurnOrder,
        IBastionScopedOrder
    {
        public Guid BastionId
        {
            get;
        }

        protected BastionTurnOrder(
            Guid orderId,
            int requiredPhases,
            Guid bastionId)
            : base(
                orderId,
                requiredPhases)
        {
            if (bastionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Bastion id cannot be empty.",
                    nameof(bastionId));
            }

            BastionId =
                bastionId;
        }
    }
}