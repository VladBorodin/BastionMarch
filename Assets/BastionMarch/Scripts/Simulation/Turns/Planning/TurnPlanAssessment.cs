using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Read-only результат проверки целостности
    /// TurnPlanDraft относительно текущего TurnCycle.
    ///
    /// Не мутирует ни draft, ни cycle.
    /// </summary>
    public sealed class TurnPlanAssessment
    {
        public bool IsValid =>
            FailureReason ==
            TurnPlanFailureReason.None;

        public TurnPlanFailureReason
            FailureReason
        {
            get;
        }

        public Guid? OrderId
        {
            get;
        }

        public Guid? BrigadeId
        {
            get;
        }

        public int? ActionPhase
        {
            get;
        }

        private TurnPlanAssessment(
            TurnPlanFailureReason failureReason,
            Guid? orderId,
            Guid? brigadeId,
            int? actionPhase)
        {
            FailureReason =
                failureReason;

            OrderId =
                orderId;

            BrigadeId =
                brigadeId;

            ActionPhase =
                actionPhase;
        }

        public static TurnPlanAssessment Valid()
        {
            return new TurnPlanAssessment(
                TurnPlanFailureReason.None,
                orderId: null,
                brigadeId: null,
                actionPhase: null);
        }

        public static TurnPlanAssessment Invalid(
            TurnPlanFailureReason failureReason,
            Guid? orderId = null,
            Guid? brigadeId = null,
            int? actionPhase = null)
        {
            if (failureReason ==
                TurnPlanFailureReason.None)
            {
                throw new ArgumentException(
                    "Invalid assessment must " +
                    "contain a failure reason.",
                    nameof(failureReason));
            }

            return new TurnPlanAssessment(
                failureReason,
                orderId,
                brigadeId,
                actionPhase);
        }
    }
}