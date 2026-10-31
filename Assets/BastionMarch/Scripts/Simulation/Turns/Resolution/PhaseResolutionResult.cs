
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемый структурированный журнал
    /// успешно завершённой Action Phase.
    ///
    /// Не владеет TurnCycle, OrderExecutionRegistry
    /// или игровым миром.
    /// </summary>
    public sealed class PhaseResolutionResult
    {
        public IReadOnlyList<ModuleWorkActivityTick> ModuleWorkTicks { get; }
        public int ModuleWorkTickCount => ModuleWorkTicks.Count;
        public int TurnNumber { get; }

        public int ActionPhase { get; }

        public IReadOnlyList<OrderPhaseJournalEntry>
            OrderEntries { get; }

        /// <summary>
        /// Бригады, для которых выбрана Activity.
        /// Это не подтверждение выполнения Activity.
        /// </summary>
        public IReadOnlyList<Guid>
            SelectedActivityBrigadeIds { get; }

        public IReadOnlyList<Guid>
            ProcessedWorldProcessIds { get; }

        public TurnTransitionResult Transition { get; }

        public int OrderEntryCount => OrderEntries.Count;

        public int CommittedOrderCount =>
            OrderEntries.Count(entry => entry.WasCommitted);

        public int RejectedOrderCount =>
            OrderEntries.Count(entry => !entry.WasCommitted);

        public int SelectedActivityCount =>
            SelectedActivityBrigadeIds.Count;

        public int ProcessedWorldProcessCount =>
            ProcessedWorldProcessIds.Count;

        private PhaseResolutionResult(
            PersistentWorldPhaseContext context,
            IReadOnlyList<OrderPhaseJournalEntry> orderEntries,
            IReadOnlyList<Guid> activityIds,
            IReadOnlyList<Guid> processIds,
            TurnTransitionResult transition,
            IReadOnlyList<ModuleWorkActivityTick> moduleWorkTicks)
        {
            TurnNumber = context.TurnNumber;
            ActionPhase = context.ActionPhase;
            OrderEntries = orderEntries;
            SelectedActivityBrigadeIds = activityIds;
            ProcessedWorldProcessIds = processIds;
            Transition = transition;
            ModuleWorkTicks = moduleWorkTicks;
        }

        public static PhaseResolutionResult Create(
            PersistentWorldPhaseContext context,
            IEnumerable<ResolvedOrderPhaseIntent> resolutions,
            IEnumerable<OrderPhaseCommitResult> commits,
            IEnumerable<BrigadeActivityPhaseIntent> activities,
            PersistentWorldPhaseResult persistentResult,
            TurnTransitionResult transition,
            IEnumerable<ModuleWorkActivityTick> moduleWorkTicks = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (resolutions == null)
                throw new ArgumentNullException(nameof(resolutions));

            if (commits == null)
                throw new ArgumentNullException(nameof(commits));

            if (activities == null)
                throw new ArgumentNullException(nameof(activities));

            if (persistentResult == null)
                throw new ArgumentNullException(nameof(persistentResult));

            if (transition == null)
                throw new ArgumentNullException(nameof(transition));

            ValidateTransition(context, transition);

            if (persistentResult.TurnNumber != context.TurnNumber ||
                persistentResult.ActionPhase != context.ActionPhase)
            {
                throw new ArgumentException(
                    "Persistent result belongs to another phase.",
                    nameof(persistentResult));
            }

            ResolvedOrderPhaseIntent[] resolutionArray =
                resolutions.ToArray();

            OrderPhaseCommitResult[] commitArray =
                commits.ToArray();

            BrigadeActivityPhaseIntent[] activityArray =
                activities.ToArray();

            if (resolutionArray.Any(item => item == null) ||
                commitArray.Any(item => item == null) ||
                activityArray.Any(item => item == null))
            {
                throw new ArgumentException(
                    "Phase result collections cannot contain null.");
            }

            if (resolutionArray.Any(item =>
                    item.ActionPhase != context.ActionPhase) ||
                commitArray.Any(item =>
                    item.ActionPhase != context.ActionPhase) ||
                activityArray.Any(item =>
                    item.ActionPhase != context.ActionPhase))
            {
                throw new ArgumentException(
                    "All entries must belong to the resolved phase.");
            }

            if (resolutionArray.Select(item => item.OrderId)
                    .Distinct().Count() != resolutionArray.Length)
            {
                throw new ArgumentException(
                    "Duplicate order resolutions.");
            }

            if (commitArray.Select(item => item.OrderId)
                    .Distinct().Count() != commitArray.Length)
            {
                throw new ArgumentException(
                    "Duplicate order commits.");
            }

            Dictionary<Guid, OrderPhaseCommitResult> commitsById =
                commitArray.ToDictionary(item => item.OrderId);

            HashSet<Guid> resolutionIds =
                resolutionArray.Select(item => item.OrderId).ToHashSet();

            if (commitArray.Any(item =>
                    !resolutionIds.Contains(item.OrderId)))
            {
                throw new ArgumentException(
                    "Commit without a corresponding resolution.");
            }

            var entries = new List<OrderPhaseJournalEntry>();

            foreach (ResolvedOrderPhaseIntent resolution in
                     resolutionArray.OrderBy(item => item.OrderId))
            {
                commitsById.TryGetValue(
                    resolution.OrderId,
                    out OrderPhaseCommitResult commit);

                entries.Add(
                    new OrderPhaseJournalEntry(resolution, commit));
            }

            Guid[] activityIds = activityArray
                .Select(item => item.BrigadeId)
                .ToArray();

            if (activityIds.Distinct().Count() != activityIds.Length)
            {
                throw new ArgumentException(
                    "Duplicate activity selection.");
            }

            ModuleWorkActivityTick[] ticks = moduleWorkTicks?.ToArray() ?? Array.Empty<ModuleWorkActivityTick>();

            if (ticks.Any(tick => tick == null))
            {
                throw new ArgumentException("Module work ticks cannot contain null.");
            }

            if (ticks.Any(tick => tick.ActionPhase != context.ActionPhase))
            {
                throw new ArgumentException("Module work tick phase mismatch.");
            }

            if (ticks.Select(tick => tick.BrigadeId).Distinct().Count() != ticks.Length)
            {
                throw new ArgumentException("Duplicate module work tick.");
            }

            Dictionary<Guid, ModuleWorkActivity> selectedWork =
                activityArray
                    .Where(intent => intent.Activity is ModuleWorkActivity)
                    .ToDictionary(
                        intent => intent.BrigadeId,
                        intent => (ModuleWorkActivity)intent.Activity);

            if (ticks.Length != selectedWork.Count ||
                ticks.Any(tick =>
                    !selectedWork.TryGetValue(tick.BrigadeId, out ModuleWorkActivity work) ||
                    work.ModuleId != tick.ModuleId))
            {
                throw new ArgumentException(
                    "Module work ticks must match selected activities.");
            }

            HashSet<Guid> reservedBrigadeIds =
                resolutionArray
                    .SelectMany(item =>
                        item.Intent.ReservedBrigadeIds)
                    .ToHashSet();

            if (activityIds.Any(reservedBrigadeIds.Contains))
            {
                throw new ArgumentException(
                    "Reserved brigade cannot receive Activity " +
                    "in the same phase.");
            }

            return new PhaseResolutionResult(
                context,
                new ReadOnlyCollection<OrderPhaseJournalEntry>(entries),
                new ReadOnlyCollection<Guid>(activityIds.OrderBy(id => id).ToArray()),
                new ReadOnlyCollection<Guid>(persistentResult.ProcessedProcessIds.ToArray()),
                transition,
                new ReadOnlyCollection<ModuleWorkActivityTick>(
                    ticks.OrderBy(tick => tick.BrigadeId).ToArray()));
                
        }

        private static void ValidateTransition(
            PersistentWorldPhaseContext context,
            TurnTransitionResult transition)
        {
            if (!transition.IsSuccess ||
                transition.PreviousTurnNumber != context.TurnNumber ||
                transition.CurrentTurnNumber != context.TurnNumber ||
                transition.PreviousStage != TurnStage.ActionResolution ||
                transition.PreviousActionPhase != context.ActionPhase)
            {
                throw new ArgumentException(
                    "Transition does not complete the specified phase.",
                    nameof(transition));
            }

            bool expectedTransition =
                context.IsLastActionPhase
                    ? transition.CurrentStage == TurnStage.TurnEnd &&
                      transition.CurrentActionPhase == null
                    : transition.CurrentStage ==
                          TurnStage.ActionResolution &&
                      transition.CurrentActionPhase ==
                          context.ActionPhase + 1;

            if (!expectedTransition)
            {
                throw new ArgumentException(
                    "Unexpected next turn stage or action phase.",
                    nameof(transition));
            }
        }
    }
}
