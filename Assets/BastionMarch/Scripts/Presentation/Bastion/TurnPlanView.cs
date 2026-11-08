using System;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Localization;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    [DisallowMultipleComponent]
    public sealed class TurnPlanView : MonoBehaviour
    {
        public TurnPlanPresentationState State { get; private set; }
        public bool IsVisible => gameObject.activeSelf;

        public void Render(TurnPlanPresentationState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Clear()
        {
            State = null;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
