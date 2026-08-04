using System;
using BastionMarch.Presentation.Bastions.State;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Управляет выбором одного ModuleView.
    ///
    /// Наружу передаёт неизменяемый снимок,
    /// а не ModuleInstance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModuleSelectionController
        : MonoBehaviour
    {
        [SerializeField]
        private BastionView _bastionView;

        public ModuleView SelectedView
        {
            get;
            private set;
        }

        public ModulePresentationState SelectedState =>
            SelectedView != null
                ? SelectedView.State
                : null;

        public event Action<ModulePresentationState>
            SelectionChanged;

        private void Reset()
        {
            ResolveBastionView();
        }

        private void Awake()
        {
            ResolveBastionView();
        }

        private void OnEnable()
        {
            if (_bastionView == null)
            {
                return;
            }

            _bastionView.ModuleClicked +=
                HandleModuleClicked;

            _bastionView.StateRendered +=
                HandleStateRendered;
        }

        private void OnDisable()
        {
            if (_bastionView == null)
            {
                return;
            }

            _bastionView.ModuleClicked -=
                HandleModuleClicked;

            _bastionView.StateRendered -=
                HandleStateRendered;
        }

        public void Select(
            ModuleView moduleView)
        {
            if (moduleView == null)
            {
                throw new ArgumentNullException(
                    nameof(moduleView));
            }

            if (SelectedView == moduleView)
            {
                return;
            }

            if (SelectedView != null)
            {
                SelectedView.SetSelected(
                    false);
            }

            SelectedView = moduleView;

            SelectedView.SetSelected(
                true);

            SelectionChanged?.Invoke(
                SelectedState);
        }

        public void ClearSelection()
        {
            if (SelectedView == null)
            {
                return;
            }

            SelectedView.SetSelected(
                false);

            SelectedView = null;

            SelectionChanged?.Invoke(
                null);
        }

        private void HandleModuleClicked(
            ModuleView moduleView)
        {
            Select(moduleView);
        }

        private void HandleStateRendered(
            BastionPresentationState state)
        {
            if (state == null)
            {
                ClearSelection();
                return;
            }

            if (SelectedView == null)
            {
                return;
            }

            Guid selectedModuleId =
                SelectedView.ModuleId;

            if (!_bastionView.TryGetModuleView(
                    selectedModuleId,
                    out ModuleView currentView))
            {
                ClearSelection();
                return;
            }

            if (!ReferenceEquals(
                    SelectedView,
                    currentView))
            {
                SelectedView.SetSelected(
                    false);

                SelectedView = currentView;

                SelectedView.SetSelected(
                    true);
            }

            SelectionChanged?.Invoke(
                SelectedState);
        }

        private void ResolveBastionView()
        {
            if (_bastionView != null)
            {
                return;
            }

            Transform prototypeRoot =
                transform.parent != null
                    ? transform.parent.parent
                    : null;

            if (prototypeRoot != null)
            {
                _bastionView =
                    prototypeRoot
                        .GetComponentInChildren<
                            BastionView>(
                                includeInactive: true);
            }
        }
    }
}