using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемый результат изменения
    /// execution progress конкретного Order.
    ///
    /// Позднее может использоваться
    /// PhaseResolutionResult и журналом.
    /// </summary>
    public sealed class OrderExecutionOutcome
    {
        public Guid OrderId
        {
            get;
        }

        public OrderExecutionOutcomeKind Kind
        {
            get;
        }

        public int CompletedPhases
        {
            get;
        }

        public int RemainingPhases
        {
            get;
        }

        public bool IsTerminal =>
            Kind ==
                OrderExecutionOutcomeKind.Completed ||
            Kind ==
                OrderExecutionOutcomeKind.Failed;

        private OrderExecutionOutcome(
            Guid orderId,
            OrderExecutionOutcomeKind kind,
            int completedPhases,
            int remainingPhases)
        {
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(orderId));
            }

            if (completedPhases < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedPhases));
            }

            if (remainingPhases < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingPhases));
            }

            OrderId =
                orderId;

            Kind =
                kind;

            CompletedPhases =
                completedPhases;

            RemainingPhases =
                remainingPhases;
        }

        internal static OrderExecutionOutcome
            Progressed(
                Guid orderId,
                int completedPhases,
                int remainingPhases)
        {
            return new OrderExecutionOutcome(
                orderId,
                OrderExecutionOutcomeKind.Progressed,
                completedPhases,
                remainingPhases);
        }

        internal static OrderExecutionOutcome
            Completed(
                Guid orderId,
                int completedPhases)
        {
            return new OrderExecutionOutcome(
                orderId,
                OrderExecutionOutcomeKind.Completed,
                completedPhases,
                remainingPhases: 0);
        }

        internal static OrderExecutionOutcome
            Failed(
                Guid orderId,
                int completedPhases,
                int remainingPhases)
        {
            return new OrderExecutionOutcome(
                orderId,
                OrderExecutionOutcomeKind.Failed,
                completedPhases,
                remainingPhases);
        }
    }
}