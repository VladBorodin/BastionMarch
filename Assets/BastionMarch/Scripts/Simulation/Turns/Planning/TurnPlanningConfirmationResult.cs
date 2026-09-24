using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Результат высокоуровневого подтверждения
    /// TurnPlanDraft.
    ///
    /// ConfirmedPlan публикуется только если
    /// validation и переход TurnCycle успешны.
    /// </summary>
    public sealed class TurnPlanningConfirmationResult
    {
        public bool IsSuccess =>
            Assessment.IsValid &&
            ConfirmedPlan != null &&
            TransitionResult != null &&
            TransitionResult.IsSuccess;

        public TurnPlanAssessment Assessment
        {
            get;
        }

        public ConfirmedTurnPlan ConfirmedPlan
        {
            get;
        }

        public TurnTransitionResult TransitionResult
        {
            get;
        }

        private TurnPlanningConfirmationResult(
            TurnPlanAssessment assessment,
            ConfirmedTurnPlan confirmedPlan,
            TurnTransitionResult transitionResult)
        {
            Assessment =
                assessment ??
                throw new ArgumentNullException(
                    nameof(assessment));

            ConfirmedPlan =
                confirmedPlan;

            TransitionResult =
                transitionResult;
        }

        public static TurnPlanningConfirmationResult
            ValidationFailure(
                TurnPlanAssessment assessment)
        {
            if (assessment == null)
            {
                throw new ArgumentNullException(
                    nameof(assessment));
            }

            if (assessment.IsValid)
            {
                throw new ArgumentException(
                    "Validation failure requires " +
                    "an invalid assessment.",
                    nameof(assessment));
            }

            return new TurnPlanningConfirmationResult(
                assessment,
                confirmedPlan: null,
                transitionResult: null);
        }

        public static TurnPlanningConfirmationResult
            TransitionFailure(
                TurnPlanAssessment assessment,
                TurnTransitionResult transitionResult)
        {
            if (assessment == null)
            {
                throw new ArgumentNullException(
                    nameof(assessment));
            }

            if (!assessment.IsValid)
            {
                throw new ArgumentException(
                    "Transition failure requires " +
                    "a valid plan assessment.",
                    nameof(assessment));
            }

            if (transitionResult == null)
            {
                throw new ArgumentNullException(
                    nameof(transitionResult));
            }

            if (transitionResult.IsSuccess)
            {
                throw new ArgumentException(
                    "Transition failure requires " +
                    "a failed transition result.",
                    nameof(transitionResult));
            }

            return new TurnPlanningConfirmationResult(
                assessment,
                confirmedPlan: null,
                transitionResult);
        }

        public static TurnPlanningConfirmationResult
            Success(
                TurnPlanAssessment assessment,
                ConfirmedTurnPlan confirmedPlan,
                TurnTransitionResult transitionResult)
        {
            if (assessment == null)
            {
                throw new ArgumentNullException(
                    nameof(assessment));
            }

            if (!assessment.IsValid)
            {
                throw new ArgumentException(
                    "Successful confirmation requires " +
                    "a valid plan assessment.",
                    nameof(assessment));
            }

            if (confirmedPlan == null)
            {
                throw new ArgumentNullException(
                    nameof(confirmedPlan));
            }

            if (transitionResult == null)
            {
                throw new ArgumentNullException(
                    nameof(transitionResult));
            }

            if (!transitionResult.IsSuccess)
            {
                throw new ArgumentException(
                    "Successful confirmation requires " +
                    "a successful turn transition.",
                    nameof(transitionResult));
            }

            return new TurnPlanningConfirmationResult(
                assessment,
                confirmedPlan,
                transitionResult);
        }
    }
}