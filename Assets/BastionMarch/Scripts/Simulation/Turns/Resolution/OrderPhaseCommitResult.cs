using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемый результат успешного
    /// commit одного OrderPhaseIntent.
    ///
    /// Не содержит mutable ссылку
    /// на OrderExecutionState.
    /// </summary>
    public sealed class OrderPhaseCommitResult
    {
        public Guid OrderId
        {
            get;
        }

        public int ActionPhase
        {
            get;
        }

        public bool StartedExecution
        {
            get;
        }

        public OrderExecutionOutcome Outcome
        {
            get;
        }

        public bool IsTerminal =>
            Outcome.IsTerminal;

        internal OrderPhaseCommitResult(
            Guid orderId,
            int actionPhase,
            bool startedExecution,
            OrderExecutionOutcome outcome)
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
                    nameof(actionPhase));
            }

            if (outcome == null)
            {
                throw new ArgumentNullException(
                    nameof(outcome));
            }

            if (outcome.OrderId !=
                orderId)
            {
                throw new ArgumentException(
                    "Commit result and outcome must " +
                    "refer to the same order.",
                    nameof(outcome));
            }

            OrderId =
                orderId;

            ActionPhase =
                actionPhase;

            StartedExecution =
                startedExecution;

            Outcome =
                outcome;
        }
    }
}