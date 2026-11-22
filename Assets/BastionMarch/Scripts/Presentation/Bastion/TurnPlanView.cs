using System;
using System.Collections.Generic;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Localization;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    [DisallowMultipleComponent]
    public sealed class TurnPlanView : MonoBehaviour
    {
        private readonly SortedDictionary<int, PhaseJournalPresentationState> _journal =
            new SortedDictionary<int, PhaseJournalPresentationState>();

        public TurnPlanPresentationState State { get; private set; }
        public IReadOnlyCollection<PhaseJournalPresentationState> Journal => _journal.Values;
        public bool IsVisible => gameObject.activeSelf;

        public void Render(TurnPlanPresentationState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (State?.TurnNumber != state.TurnNumber)
                _journal.Clear();
            State = state;
        }

        public void RecordPhase(PhaseJournalPresentationState phase)
        {
            if (phase == null) throw new ArgumentNullException(nameof(phase));
            if (State?.TurnNumber != phase.TurnNumber)
                throw new ArgumentException("Phase belongs to another turn.", nameof(phase));
            _journal[phase.ActionPhase] = phase;
        }

        public void Clear()
        {
            State = null;
            _journal.Clear();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void DrawJournal()
        {
            if (!IsVisible || _journal.Count == 0)
                return;

            GUILayout.Label(BastionTexts.Ui(UiTextKeys.JournalTitle));
            foreach (var phase in _journal.Values)
            {
                GUILayout.Space(4);
                GUILayout.Label(BastionTexts.Ui(UiTextKeys.TimelinePhaseLabel, phase.ActionPhase));
                if (phase.IsEmpty)
                    GUILayout.Label(BastionTexts.Ui(UiTextKeys.JournalEmpty));
                else
                    foreach (string line in phase.GetLocalizedEntries())
                        GUILayout.Label(line);
            }
        }

        public void DrawTimeline()
        {
            if (!IsVisible)
                return;

            GUILayout.Label(BastionTexts.Ui(UiTextKeys.TimelineTitle));

            if (State == null)
            {
                GUILayout.Label(BastionTexts.Ui(UiTextKeys.TimelineNoPlan));
                return;
            }

            foreach (TurnPlanPhasePresentationState phase in State.Phases)
            {
                GUILayout.Space(4);
                GUILayout.Label(BastionTexts.Ui(
                    UiTextKeys.TimelinePhaseLabel, phase.ActionPhase));

                if (phase.Entries.Count == 0)
                {
                    GUILayout.Label(BastionTexts.Ui(UiTextKeys.TimelineNoOrders));
                    continue;
                }

                foreach (TurnPlanEntryPresentationState entry in phase.Entries)
                {
                    string description = entry.Kind == TurnPlanEntryKind.Move
                        ? BastionTexts.Ui(
                            UiTextKeys.TimelineMoveEntry,
                            entry.BrigadeNumber,
                            BastionTexts.ModuleName(entry.TargetModuleNameKey),
                            entry.TargetX,
                            entry.TargetDeck)
                        : BastionTexts.Ui(
                            UiTextKeys.TimelineOtherEntry,
                            entry.BrigadeNumber);

                    GUILayout.Label(description);
                }
            }

            if (State.UnscheduledOrderCount > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label(BastionTexts.Ui(
                    UiTextKeys.TimelineUnassignedOrders,
                    State.UnscheduledOrderCount));
            }
        }
    }
}
