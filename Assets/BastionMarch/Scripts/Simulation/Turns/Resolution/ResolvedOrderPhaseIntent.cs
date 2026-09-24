using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемое решение о допуске
    /// assessed Order intent к commit stage.
    /// </summary>
    public sealed class ResolvedOrderPhaseIntent
    {
        public AssessedOrderPhaseIntent Candidate
        {
            get;
        }

        public OrderPhaseIntent Intent =>
            Candidate.Intent;

        public OrderPhaseAssessment Assessment =>
            Candidate.Assessment;

        public Guid OrderId =>
            Candidate.OrderId;

        public int ActionPhase =>
            Candidate.ActionPhase;

        public OrderPhaseResolutionDecision Decision
        {
            get;
        }

        public bool IsApproved =>
            Decision ==
            OrderPhaseResolutionDecision.Approved;

        private ResolvedOrderPhaseIntent(
            AssessedOrderPhaseIntent candidate,
            OrderPhaseResolutionDecision decision)
        {
            Candidate =
                candidate ??
                throw new ArgumentNullException(
                    nameof(candidate));

            Decision =
                decision;
        }

        public static ResolvedOrderPhaseIntent
            Approved(
                AssessedOrderPhaseIntent candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(
                    nameof(candidate));
            }

            if (!candidate.PassedAssessment)
            {
                throw new ArgumentException(
                    "Approved resolution requires " +
                    "a passed assessment.",
                    nameof(candidate));
            }

            return new ResolvedOrderPhaseIntent(
                candidate,
                OrderPhaseResolutionDecision.Approved);
        }

        public static ResolvedOrderPhaseIntent
            RejectedByAssessment(
                AssessedOrderPhaseIntent candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(
                    nameof(candidate));
            }

            if (candidate.PassedAssessment)
            {
                throw new ArgumentException(
                    "Assessment rejection requires " +
                    "a failed assessment.",
                    nameof(candidate));
            }

            return new ResolvedOrderPhaseIntent(
                candidate,
                OrderPhaseResolutionDecision
                    .RejectedByAssessment);
        }
    }
}