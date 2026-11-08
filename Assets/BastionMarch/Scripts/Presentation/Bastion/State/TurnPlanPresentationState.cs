using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Presentation.Bastions.State
{
    // These values are presentation data, not translated strings.
    public enum TurnPlanEntryKind
    {
        Move,
        Other
    }

    public sealed class TurnPlanEntryPresentationState
    {
        public Guid OrderId { get; }
        public Guid BrigadeId { get; }
        public int BrigadeNumber { get; }
        public TurnPlanEntryKind Kind { get; }
        public string TargetModuleNameKey { get; }
        public int TargetX { get; }
        public int TargetDeck { get; }

        public TurnPlanEntryPresentationState(
            Guid orderId,
            Guid brigadeId,
            int brigadeNumber,
            TurnPlanEntryKind kind,
            string targetModuleNameKey = null,
            int targetX = 0,
            int targetDeck = 0)
        {
            if (orderId == Guid.Empty || brigadeId == Guid.Empty)
                throw new ArgumentException("Order and brigade ids cannot be empty.");

            if (brigadeNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(brigadeNumber));

            if (!Enum.IsDefined(typeof(TurnPlanEntryKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            if (kind == TurnPlanEntryKind.Move &&
                string.IsNullOrWhiteSpace(targetModuleNameKey))
            {
                throw new ArgumentException(
                    "Move entry requires a module localization key.",
                    nameof(targetModuleNameKey));
            }

            OrderId = orderId;
            BrigadeId = brigadeId;
            BrigadeNumber = brigadeNumber;
            Kind = kind;
            TargetModuleNameKey = targetModuleNameKey;
            TargetX = targetX;
            TargetDeck = targetDeck;
        }
    }

    public sealed class TurnPlanPhasePresentationState
    {
        public int ActionPhase { get; }

        public IReadOnlyList<TurnPlanEntryPresentationState> Entries { get; }

        public TurnPlanPhasePresentationState(
            int actionPhase,
            IEnumerable<TurnPlanEntryPresentationState> entries)
        {
            if (actionPhase < 1)
                throw new ArgumentOutOfRangeException(nameof(actionPhase));

            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            TurnPlanEntryPresentationState[] snapshot = entries.ToArray();
            if (snapshot.Any(item => item == null))
                throw new ArgumentException(
                    "Entries cannot contain null.", nameof(entries));

            ActionPhase = actionPhase;
            Entries = new ReadOnlyCollection<TurnPlanEntryPresentationState>(snapshot);
        }
    }

    public sealed class TurnPlanPresentationState
    {
        public int TurnNumber { get; }
        public IReadOnlyList<TurnPlanPhasePresentationState> Phases { get; }
        public int UnscheduledOrderCount { get; }

        public TurnPlanPresentationState(
            int turnNumber,
            IEnumerable<TurnPlanPhasePresentationState> phases,
            int unscheduledOrderCount)
        {
            if (turnNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(turnNumber));

            if (phases == null)
                throw new ArgumentNullException(nameof(phases));

            if (unscheduledOrderCount < 0)
                throw new ArgumentOutOfRangeException(nameof(unscheduledOrderCount));

            TurnPlanPhasePresentationState[] snapshot = phases.ToArray();
            if (snapshot.Length == 0 || snapshot.Any(item => item == null))
                throw new ArgumentException(
                    "At least one non-null phase is required.", nameof(phases));

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].ActionPhase != i + 1)
                    throw new ArgumentException(
                        "Phases must be consecutive.", nameof(phases));
            }

            TurnNumber = turnNumber;
            Phases = new ReadOnlyCollection<TurnPlanPhasePresentationState>(snapshot);
            UnscheduledOrderCount = unscheduledOrderCount;
        }
    }
}
