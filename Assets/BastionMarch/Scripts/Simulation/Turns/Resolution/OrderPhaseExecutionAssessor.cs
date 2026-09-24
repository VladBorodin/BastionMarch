using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Проверяет универсальную execution-семантику
    /// scheduled Order tick.
    ///
    /// Не проверяет конкретные world preconditions
    /// Move/Weapon/Repair Orders.
    /// </summary>
    public static class OrderPhaseExecutionAssessor
    {
        public static OrderPhaseAssessment Assess(
            OrderPhaseIntent intent,
            OrderExecutionState executionState)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(
                    nameof(intent));
            }

            if (executionState == null)
            {
                return OrderPhaseAssessment.Allowed(
                    intent.OrderId,
                    intent.ActionPhase,
                    requiresExecutionStart: true);
            }

            if (executionState.OrderId !=
                intent.OrderId)
            {
                return OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionOrderMismatch);
            }

            if (executionState.RequiredPhases !=
                intent.Order.RequiredPhases)
            {
                return OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionDurationMismatch);
            }

            if (executionState.IsCompleted)
            {
                return OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyCompleted);
            }

            if (executionState.IsFailed)
            {
                return OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed);
            }

            return OrderPhaseAssessment.Allowed(
                intent.OrderId,
                intent.ActionPhase,
                requiresExecutionStart: false);
        }
    }
}