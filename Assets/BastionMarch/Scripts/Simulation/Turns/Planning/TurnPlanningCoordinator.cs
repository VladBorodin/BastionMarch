using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Координирует безопасное подтверждение
    /// плана текущего хода.
    ///
    /// Порядок:
    /// validate
    /// -> freeze
    /// -> confirm TurnCycle.
    ///
    /// Не владеет ни TurnCycle, ни TurnPlanDraft.
    /// </summary>
    public sealed class TurnPlanningCoordinator
    {
        public TurnPlanningConfirmationResult
            TryConfirmPlan(
                TurnCycle cycle,
                TurnPlanDraft draft)
        {
            if (cycle == null)
            {
                throw new ArgumentNullException(
                    nameof(cycle));
            }

            if (draft == null)
            {
                throw new ArgumentNullException(
                    nameof(draft));
            }

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(
                    cycle,
                    draft);

            if (!assessment.IsValid)
            {
                return
                    TurnPlanningConfirmationResult
                        .ValidationFailure(
                            assessment);
            }

            ConfirmedTurnPlan confirmedPlan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            TurnTransitionResult transitionResult =
                cycle.TryConfirmPlanning();

            if (!transitionResult.IsSuccess)
            {
                return
                    TurnPlanningConfirmationResult
                        .TransitionFailure(
                            assessment,
                            transitionResult);
            }

            return
                TurnPlanningConfirmationResult
                    .Success(
                        assessment,
                        confirmedPlan,
                        transitionResult);
        }
    }
}