
using System;
using BastionMarch.Presentation.Bastions.State;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Read-only представление плана Action Phase.
    ///
    /// Не хранит TurnPlanDraft и не выполняет Orders.
    /// IMGUI используется только в техническом прототипе.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurnPlanView : MonoBehaviour
    {
        public TurnPlanPresentationState State { get; private set; }

        public bool IsVisible => gameObject.activeSelf;

        public void Render(TurnPlanPresentationState state)
        {
            State = state ??
                throw new ArgumentNullException(nameof(state));
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

        /// <summary>
        /// Вызывается внутри GUILayout-панели контроллера.
        /// Самостоятельный OnGUI не нужен.
        /// </summary>
        public void DrawTimeline()
        {
            if (!IsVisible)
                return;

            GUILayout.Label("План по Action Phase:");

            if (State == null)
            {
                GUILayout.Label("План ещё не загружен.");
                return;
            }

            foreach (TurnPlanPhasePresentationState phase
                     in State.Phases)
            {
                GUILayout.Space(4);
                GUILayout.Label($"Фаза {phase.ActionPhase}:");

                if (phase.Entries.Count == 0)
                {
                    GUILayout.Label("  Нет назначенных приказов");
                    continue;
                }

                foreach (TurnPlanEntryPresentationState entry
                         in phase.Entries)
                {
                    GUILayout.Label("  " + entry.Description);
                }
            }

            if (State.UnscheduledOrderCount > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label(
                    $"Без назначенной фазы: " +
                    $"{State.UnscheduledOrderCount}");
            }
        }
    }
}
