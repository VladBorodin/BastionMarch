using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Presentation.Localization;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;

namespace BastionMarch.Presentation.Bastions.State
{
    public sealed class PhaseJournalPresentationState
    {
        private readonly (OrderPhaseJournalEntry Result, int? BrigadeNumber,
            bool IsMove, ModulePresentationState Target)[] _orders;
        private readonly (int BrigadeNumber, ModulePresentationState Module, int Personnel)[] _work;

        public int TurnNumber { get; }
        public int ActionPhase { get; }
        public bool IsEmpty => _orders.Length == 0 && _work.Length == 0;

        public PhaseJournalPresentationState(PhaseResolutionResult result,
            ConfirmedTurnPlan plan, BastionPresentationState bastion)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (bastion == null) throw new ArgumentNullException(nameof(bastion));
            if (plan.TurnNumber != result.TurnNumber)
                throw new ArgumentException("Plan and result belong to different turns.", nameof(plan));

            TurnNumber = result.TurnNumber;
            ActionPhase = result.ActionPhase;
            var orders = plan.Orders.ToDictionary(order => order.OrderId);
            var brigadeNumbers = plan.Participants.ToDictionary(p => p.BrigadeId, p => p.BrigadeNumber);

            _orders = result.OrderEntries.Select(outcome =>
            {
                ITurnOrder order = orders[outcome.OrderId];
                int? number = order is IBrigadeScopedOrder scoped
                    ? brigadeNumbers[scoped.BrigadeId] : (int?)null;
                ModulePresentationState target = null;
                if (order is MoveBrigadeOrder move)
                    bastion.TryGetModule(move.TargetModuleId, out target);
                return (outcome, number, order is MoveBrigadeOrder, target);
            }).ToArray();

            _work = result.ModuleWorkTicks.Select(tick =>
            {
                bastion.TryGetModule(tick.ModuleId, out var module);
                return (brigadeNumbers[tick.BrigadeId], module, tick.WorkingPersonnel);
            }).ToArray();
        }

        // Resolve labels on display so changing locale also updates earlier phases.
        public IEnumerable<string> GetLocalizedEntries()
        {
            foreach (var order in _orders)
            {
                string description;
                if (order.IsMove)
                    description = BastionTexts.Ui(UiTextKeys.JournalMove,
                        order.BrigadeNumber.Value, Location(order.Target));
                else if (order.BrigadeNumber.HasValue)
                    description = BastionTexts.Ui(UiTextKeys.TimelineOtherEntry, order.BrigadeNumber.Value);
                else
                    description = BastionTexts.Ui(UiTextKeys.JournalOrder);

                if (!order.Result.WasCommitted)
                {
                    yield return BastionTexts.Ui(UiTextKeys.JournalRejected,
                        description, FailureReason(order.Result));
                    continue;
                }

                string key = order.Result.OutcomeKind == OrderExecutionOutcomeKind.Completed
                    ? UiTextKeys.JournalCompleted : UiTextKeys.JournalProgressed;
                yield return BastionTexts.Ui(key, description);
            }

            foreach (var work in _work)
                yield return BastionTexts.Ui(UiTextKeys.JournalWork,
                    work.BrigadeNumber, Location(work.Module), work.Personnel);
        }

        private static string Location(ModulePresentationState module)
        {
            if (module == null)
                return BastionTexts.Ui(UiTextKeys.JournalUnknownModule);

            return BastionTexts.Ui(UiTextKeys.JournalLocation,
                BastionTexts.ModuleName(module.NameLocalizationKey), module.Position.X, module.Position.Deck);
        }

        private static string FailureReason(OrderPhaseJournalEntry entry)
        {
            if (entry.MoveTraversalFailureReason is ModulePassageTraversalFailureReason passage &&
                passage != ModulePassageTraversalFailureReason.None)
                return BastionTexts.EnumLabel(passage);
            if (entry.MoveFailureReason is BrigadeMoveFailureReason move && move != BrigadeMoveFailureReason.None)
                return BastionTexts.EnumLabel(move);

            return BastionTexts.EnumLabel(entry.AssessmentFailureReason);
        }
    }
}
