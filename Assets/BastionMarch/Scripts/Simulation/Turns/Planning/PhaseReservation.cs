using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Неизменяемая запись о том,
    /// что конкретная бригада занята Order
    /// в конкретной Action Phase.
    /// </summary>
    public sealed class PhaseReservation
    {
        public Guid OrderId
        {
            get;
        }

        public int ActionPhase
        {
            get;
        }

        public Guid BrigadeId
        {
            get;
        }

        public PhaseReservation(
            Guid orderId,
            int actionPhase,
            Guid brigadeId)
        {
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(orderId));
            }

            if (actionPhase < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase),
                    actionPhase,
                    "Action phase must be positive.");
            }

            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            OrderId =
                orderId;

            ActionPhase =
                actionPhase;

            BrigadeId =
                brigadeId;
        }
    }
}