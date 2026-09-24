using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Read-only результат проверки возможности
    /// выполнить scheduled Order tick.
    /// </summary>
    public sealed class OrderPhaseAssessment
    {
        public Guid OrderId
        {
            get;
        }

        public int ActionPhase
        {
            get;
        }

        public bool RequiresExecutionStart
        {
            get;
        }

        public OrderPhaseAssessmentFailureReason
            FailureReason
        {
            get;
        }

        public bool IsAllowed =>
            FailureReason ==
            OrderPhaseAssessmentFailureReason.None;

        private OrderPhaseAssessment(
            Guid orderId,
            int actionPhase,
            bool requiresExecutionStart,
            OrderPhaseAssessmentFailureReason
                failureReason)
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

            OrderId =
                orderId;

            ActionPhase =
                actionPhase;

            RequiresExecutionStart =
                requiresExecutionStart;

            FailureReason =
                failureReason;
        }

        public static OrderPhaseAssessment Allowed(
            Guid orderId,
            int actionPhase,
            bool requiresExecutionStart)
        {
            return new OrderPhaseAssessment(
                orderId,
                actionPhase,
                requiresExecutionStart,
                OrderPhaseAssessmentFailureReason.None);
        }

        public static OrderPhaseAssessment Rejected(
            Guid orderId,
            int actionPhase,
            OrderPhaseAssessmentFailureReason
                failureReason)
        {
            if (failureReason ==
                OrderPhaseAssessmentFailureReason.None)
            {
                throw new ArgumentException(
                    "Rejected assessment requires " +
                    "a failure reason.",
                    nameof(failureReason));
            }

            return new OrderPhaseAssessment(
                orderId,
                actionPhase,
                requiresExecutionStart: false,
                failureReason);
        }
    }
}