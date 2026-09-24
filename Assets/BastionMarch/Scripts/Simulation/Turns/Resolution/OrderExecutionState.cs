using System;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Runtime-состояние процесса исполнения
    /// одного immutable Order.
    ///
    /// Один OrderId соответствует одному
    /// execution process.
    ///
    /// Execution state может переживать
    /// границы Action Phase и Turn.
    /// </summary>
    public sealed class OrderExecutionState
    {
        public Guid OrderId
        {
            get;
        }

        /// <summary>
        /// Зафиксированная длительность Order
        /// на момент начала execution process.
        /// </summary>
        public int RequiredPhases
        {
            get;
        }

        public int CompletedPhases
        {
            get;
            private set;
        }

        public int RemainingPhases =>
            RequiredPhases -
            CompletedPhases;

        public OrderExecutionStatus Status
        {
            get;
            private set;
        }

        public bool IsActive =>
            Status ==
            OrderExecutionStatus.Active;

        public bool IsCompleted =>
            Status ==
            OrderExecutionStatus.Completed;

        public bool IsFailed =>
            Status ==
            OrderExecutionStatus.Failed;

        public bool IsTerminal =>
            !IsActive;

        private OrderExecutionState(
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

            CompletedPhases =
                0;

            Status =
                OrderExecutionStatus.Active;
        }

        public static OrderExecutionState Start(
            ITurnOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(
                    nameof(order));
            }

            if (order.OrderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(order));
            }

            if (order.RequiredPhases < 1)
            {
                throw new ArgumentException(
                    "Order required phases " +
                    "must be positive.",
                    nameof(order));
            }

            return new OrderExecutionState(
                order.OrderId,
                order.RequiredPhases);
        }

        /// <summary>
        /// Засчитывает один успешно выполненный
        /// execution tick данного Order.
        ///
        /// Метод ничего не знает о TurnNumber
        /// или ActionPhase. Гарантия
        /// "не более одного tick Order за фазу"
        /// принадлежит phase resolver.
        /// </summary>
        public OrderExecutionOutcome
            ApplyProgressTick()
        {
            EnsureActive();

            CompletedPhases =
                checked(
                    CompletedPhases + 1);

            if (CompletedPhases ==
                RequiredPhases)
            {
                Status =
                    OrderExecutionStatus.Completed;

                return
                    OrderExecutionOutcome.Completed(
                        OrderId,
                        CompletedPhases);
            }

            return
                OrderExecutionOutcome.Progressed(
                    OrderId,
                    CompletedPhases,
                    RemainingPhases);
        }

        /// <summary>
        /// Терминально завершает процесс исполнения
        /// неуспехом без добавления progress tick.
        /// </summary>
        public OrderExecutionOutcome Fail()
        {
            EnsureActive();

            Status =
                OrderExecutionStatus.Failed;

            return
                OrderExecutionOutcome.Failed(
                    OrderId,
                    CompletedPhases,
                    RemainingPhases);
        }

        private void EnsureActive()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException(
                    "Terminal order execution " +
                    "cannot be changed.");
            }
        }
    }
}