using System;
using BastionMarch.Simulation.Modules;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Управляет выбором одного визуального модуля.
    ///
    /// Контроллер не изменяет состояние Simulation.
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

        public ModuleInstance SelectedModule =>
            SelectedView != null
                ? SelectedView.Module
                : null;

        public event Action<ModuleInstance>
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
            if (_bastionView != null)
            {
                _bastionView.ModuleClicked +=
                    HandleModuleClicked;
            }
        }

        private void OnDisable()
        {
            if (_bastionView != null)
            {
                _bastionView.ModuleClicked -=
                    HandleModuleClicked;
            }
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
                SelectedModule);
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