using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Связывает OrderPhaseIntent с результатом
    /// его read-only runtime assessment.
    ///
    /// Не является commit decision.
    /// </summary>
    public sealed class AssessedOrderPhaseIntent
    {
        public OrderPhaseIntent Intent
        {
            get;
        }

        public OrderPhaseAssessment Assessment
        {
            get;
        }

        public Guid OrderId =>
            Intent.OrderId;

        public int ActionPhase =>
            Intent.ActionPhase;

        public bool PassedAssessment =>
            Assessment.IsAllowed;

        public AssessedOrderPhaseIntent(
            OrderPhaseIntent intent,
            OrderPhaseAssessment assessment)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(
                    nameof(intent));
            }

            if (assessment == null)
            {
                throw new ArgumentNullException(
                    nameof(assessment));
            }

            if (intent.OrderId !=
                assessment.OrderId)
            {
                throw new ArgumentException(
                    "Intent and assessment must " +
                    "refer to the same order.",
                    nameof(assessment));
            }

            if (intent.ActionPhase !=
                assessment.ActionPhase)
            {
                throw new ArgumentException(
                    "Intent and assessment must " +
                    "refer to the same action phase.",
                    nameof(assessment));
            }

            Intent =
                intent;

            Assessment =
                assessment;
        }
    }
}