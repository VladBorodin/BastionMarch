using System;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемая запись результата одного
    /// scheduled Order tick.
    ///
    /// Хранит значения, а не mutable execution state.
    /// </summary>
    public sealed class OrderPhaseJournalEntry
    {
        public BrigadeMoveFailureReason? MoveFailureReason 
        { 
            get; 
        }

        public ModulePassageTraversalFailureReason? MoveTraversalFailureReason
        {
            get;
        }

        public Guid OrderId { get; }

        public OrderPhaseResolutionDecision Decision { get; }

        public OrderPhaseAssessmentFailureReason
            AssessmentFailureReason { get; }

        public bool WasCommitted { get; }

        public bool StartedExecution { get; }

        public OrderExecutionOutcomeKind? OutcomeKind { get; }

        public int? CompletedPhases { get; }

        public int? RemainingPhases { get; }

        internal OrderPhaseJournalEntry(
            ResolvedOrderPhaseIntent resolution,
            OrderPhaseCommitResult commit)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(
                    nameof(resolution));
            }

            if (resolution.IsApproved && commit == null)
            {
                throw new ArgumentException(
                    "Approved order requires a commit result.",
                    nameof(commit));
            }

            if (!resolution.IsApproved && commit != null &&
                (resolution.Assessment.FailureReason !=
                    OrderPhaseAssessmentFailureReason.MoveDomainRejected ||
                commit.Outcome.Kind != OrderExecutionOutcomeKind.Failed))
            {
                throw new ArgumentException(
                    "Rejected order may only record a supported failure outcome.",
                    nameof(commit));
            }

            if (resolution.IsApproved && commit != null &&
                commit.Outcome.Kind == OrderExecutionOutcomeKind.Failed)
            {
                throw new ArgumentException(
                    "Approved order cannot record failed execution.",
                    nameof(commit));
            }

            if (commit != null &&
                commit.OrderId != resolution.OrderId)
            {
                throw new ArgumentException(
                    "Commit result belongs to another order.",
                    nameof(commit));
            }

            OrderId = resolution.OrderId;
            Decision = resolution.Decision;
            AssessmentFailureReason = resolution.Assessment.FailureReason;
            MoveFailureReason = resolution.Assessment.MoveAssessment?.FailureReason;
            MoveTraversalFailureReason = resolution.Assessment.MoveAssessment?.TraversalFailureReason;
            WasCommitted = resolution.IsApproved && commit != null;

            if (commit != null)
            {
                StartedExecution = commit.StartedExecution;
                OutcomeKind = commit.Outcome.Kind;
                CompletedPhases =
                    commit.Outcome.CompletedPhases;
                RemainingPhases =
                    commit.Outcome.RemainingPhases;
            }
        }
    }
}
