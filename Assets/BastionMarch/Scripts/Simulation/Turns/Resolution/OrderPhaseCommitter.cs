using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Применяет уже разрешённый Order tick
    /// к runtime execution state.
    ///
    /// Не выполняет world mutation.
    /// </summary>
    public static class OrderPhaseCommitter
    {
        public static OrderPhaseCommitResult Commit(
            ResolvedOrderPhaseIntent resolution,
            OrderExecutionRegistry registry)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(
                    nameof(resolution));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(
                    nameof(registry));
            }

            if (!resolution.IsApproved)
            {
                throw new ArgumentException(
                    "Only approved resolution " +
                    "can be committed.",
                    nameof(resolution));
            }

            OrderPhaseIntent intent =
                resolution.Intent;

            OrderPhaseAssessment assessment =
                resolution.Assessment;

            if (assessment.RequiresExecutionStart)
            {
                return CommitNewExecution(
                    intent,
                    registry);
            }

            return CommitExistingExecution(
                intent,
                registry);
        }

        private static OrderPhaseCommitResult
            CommitNewExecution(
                OrderPhaseIntent intent,
                OrderExecutionRegistry registry)
        {
            if (registry.TryGet(
                    intent.OrderId,
                    out _))
            {
                throw new InvalidOperationException(
                    "Assessment expected a new " +
                    "execution process, but one " +
                    "already exists.");
            }

            OrderExecutionState state =
                OrderExecutionState.Start(
                    intent.Order);

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            registry.Add(
                state);

            return new OrderPhaseCommitResult(
                intent.OrderId,
                intent.ActionPhase,
                startedExecution: true,
                outcome);
        }

        private static OrderPhaseCommitResult
            CommitExistingExecution(
                OrderPhaseIntent intent,
                OrderExecutionRegistry registry)
        {
            if (!registry.TryGet(
                    intent.OrderId,
                    out OrderExecutionState state))
            {
                throw new InvalidOperationException(
                    "Assessment expected an existing " +
                    "execution process, but none " +
                    "was found.");
            }

            if (state.RequiredPhases !=
                intent.Order.RequiredPhases)
            {
                throw new InvalidOperationException(
                    "Existing execution duration " +
                    "does not match the Order.");
            }

            if (!state.IsActive)
            {
                throw new InvalidOperationException(
                    "Terminal execution process " +
                    "cannot receive another tick.");
            }

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            return new OrderPhaseCommitResult(
                intent.OrderId,
                intent.ActionPhase,
                startedExecution: false,
                outcome);
        }
    }
}