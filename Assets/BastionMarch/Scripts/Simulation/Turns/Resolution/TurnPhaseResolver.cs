
using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Базовый orchestration pipeline Action Phase.
    ///
    /// Поддерживает Order execution progress,
    /// Activity selection и NoOp persistent processes.
    ///
    /// Не выполняет конкретных world actions.
    /// </summary>
    public sealed class TurnPhaseResolver
    {
        private readonly Bastion _bastion;
        private readonly TurnCycle _cycle;
        private readonly ConfirmedTurnPlan _plan;
        private readonly OrderExecutionRegistry _registry;
        private readonly Func<Guid, IBrigadeActivity>
            _activityResolver;

        public OrderExecutionRegistry ExecutionRegistry =>
            _registry;

        public TurnPhaseResolver(
            TurnCycle cycle,
            ConfirmedTurnPlan plan,
            OrderExecutionRegistry registry,
            Func<Guid, IBrigadeActivity> activityResolver,
            Bastion bastion = null)
        {
            _cycle = cycle ?? throw new ArgumentNullException(nameof(cycle));

            _plan = plan ?? throw new ArgumentNullException(nameof(plan));

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));

            _activityResolver = activityResolver ?? throw new ArgumentNullException(nameof(activityResolver));
            
            _bastion = bastion;

            ValidateCycleAndPlan();
        }

        public PhaseResolutionResult ResolvePhase(int expectedActionPhase)
        {
            ValidateCycleAndPlan();

            if (_cycle.CurrentActionPhase != expectedActionPhase)
            {
                throw new InvalidOperationException(
                    "Requested action phase is not active.");
            }

            int phase = expectedActionPhase;

            var context = new PersistentWorldPhaseContext(
                _cycle.TurnNumber,
                phase,
                _cycle.ActionPhaseCount);

            // Build Order intents before any mutations.
            IReadOnlyList<OrderPhaseIntent> orderIntents = OrderPhaseIntentFactory.CreateForPhase(_plan, phase);
            foreach (OrderPhaseIntent intent in orderIntents)
            {
                if (intent.Order is MoveBrigadeOrder && _bastion == null)
                {
                    throw new NotSupportedException(
                        "MoveBrigadeOrder requires a Bastion execution context.");
                }
            }

            // Activity selection also happens before mutation.
            IReadOnlyList<BrigadeActivityPhaseIntent>
                activityIntents =
                    BrigadeActivityPhaseIntentFactory
                        .CreateForPhase(
                            _plan,
                            phase,
                            _activityResolver);

            var moduleWorkTicks = new List<ModuleWorkActivityTick>();

            foreach (BrigadeActivityPhaseIntent intent in activityIntents)
            {
                if (!(intent.Activity is ModuleWorkActivity))
                {
                    continue;
                }

                if (_bastion == null)
                {
                    throw new NotSupportedException(
                        "ModuleWorkActivity requires a Bastion context.");
                }

                moduleWorkTicks.Add(ModuleWorkActivityTickProcessor.Prepare(intent, _bastion));
            }

            // All assessments use the original registry.
            var assessed =
                new List<AssessedOrderPhaseIntent>();

            var allowedMoves = new Dictionary<Guid, BrigadeMoveAssessment>();
            foreach (OrderPhaseIntent intent in orderIntents)
            {
                _registry.TryGet(
                    intent.OrderId,
                    out OrderExecutionState state);

                OrderPhaseAssessment assessment = OrderPhaseExecutionAssessor.Assess(intent, state);

                if (assessment.IsAllowed && intent.Order is MoveBrigadeOrder)
                {
                    BrigadeMoveAssessment moveAssessment =
                        MoveBrigadeOrderAssessor.Assess(intent, _bastion);

                    if (moveAssessment.IsAllowed)
                    {
                        allowedMoves.Add(intent.OrderId, moveAssessment);
                    }
                    else
                    {
                        assessment = OrderPhaseAssessment.RejectedMove(
                            intent.OrderId,
                            intent.ActionPhase,
                            moveAssessment);
                    }
                }

                assessed.Add(
                    new AssessedOrderPhaseIntent(
                        intent,
                        assessment));
            }

            IReadOnlyList<ResolvedOrderPhaseIntent>
                resolutions =
                    OrderPhaseConflictResolver.Resolve(
                        assessed);

            // All execution mutations are staged on a copy.
            OrderExecutionRegistry preparedRegistry =
                _registry.CreateCopy();

            var commits = new List<OrderPhaseCommitResult>();
            var movesToCommit = new List<BrigadeMoveAssessment>();

            foreach (ResolvedOrderPhaseIntent resolution in resolutions)
            {
                if (!resolution.IsApproved)
                {
                    if (resolution.Assessment.FailureReason ==
                        OrderPhaseAssessmentFailureReason.MoveDomainRejected)
                    {
                        commits.Add(OrderPhaseCommitter.RecordMoveFailure(
                            resolution, preparedRegistry));
                    }

                    continue;
                }

                commits.Add(OrderPhaseCommitter.Commit(
                    resolution, preparedRegistry));

                if (resolution.Intent.Order is MoveBrigadeOrder)
                {
                    movesToCommit.Add(allowedMoves[resolution.OrderId]);
                }
            }

            // Stage 14 currently has no actual persistent
            // world process implementation.
            PersistentWorldPhaseResult persistentResult =
                NoOpPersistentWorldPhaseProcessor.Instance
                    .AdvancePhase(context);

            // Prepare the expected temporal transition
            // without changing TurnCycle.
            TurnTransitionResult expectedTransition =
                TurnTransitionResult.Success(
                    turnNumber: context.TurnNumber,
                    previousStage: TurnStage.ActionResolution,
                    currentStage: context.IsLastActionPhase
                        ? TurnStage.TurnEnd
                        : TurnStage.ActionResolution,
                    previousActionPhase: phase,
                    currentActionPhase: context.IsLastActionPhase
                        ? (int?)null
                        : phase + 1);

            // Validate and build the complete journal
            // before publishing any mutation.
            PhaseResolutionResult result =
                PhaseResolutionResult.Create(
                    context,
                    resolutions,
                    commits,
                    activityIntents,
                    persistentResult,
                    expectedTransition,
                    moduleWorkTicks);

            // Recheck after all preparation callbacks.
            ValidateCycleAndPlan();

            if (_cycle.CurrentActionPhase != expectedActionPhase)
            {
                throw new InvalidOperationException(
                    "Action phase changed during preparation.");
            }

            if (movesToCommit.Count > 0)
            {
                _bastion.CommitAssessedBrigadeMoves(movesToCommit);
            }

            TurnTransitionResult actualTransition = _cycle.TryAdvanceActionPhase();

            if (!actualTransition.IsSuccess)
            {
                throw new InvalidOperationException(
                    "Could not advance action phase.");
            }

            // Publish prepared execution progress only
            // after the phase transition succeeds.
            _registry.ReplaceFrom(preparedRegistry);

            return result;
        }

        private void ValidateCycleAndPlan()
        {
            if (_cycle.Stage != TurnStage.ActionResolution ||
                !_cycle.CurrentActionPhase.HasValue)
            {
                throw new InvalidOperationException(
                    "Action resolution is not active.");
            }

            if (_cycle.TurnNumber != _plan.TurnNumber ||
                _cycle.ActionPhaseCount != _plan.ActionPhaseCount)
            {
                throw new ArgumentException(
                    "Confirmed plan does not match TurnCycle.");
            }

            if (_cycle.ActiveBrigades.Count !=
                _plan.Participants.Count)
            {
                throw new ArgumentException(
                    "Participant snapshots do not match.");
            }

            for (int i = 0; i < _plan.Participants.Count; i++)
            {
                TurnBrigadeParticipant planned =
                    _plan.Participants[i];

                TurnBrigadeParticipant active =
                    _cycle.ActiveBrigades[i];

                if (planned.BrigadeId != active.BrigadeId ||
                    planned.BrigadeNumber != active.BrigadeNumber)
                {
                    throw new ArgumentException(
                        "Participant snapshots do not match.");
                }
            }
        }
    }
}
