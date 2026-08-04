using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Presentation.Bastions.State;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Корневое представление снимка состояния бастиона.
    ///
    /// Не хранит прямую ссылку на Simulation Bastion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BastionView : MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private BastionGridLayout _layout;

        [SerializeField]
        private BastionGridView _gridView;

        [SerializeField]
        private Transform _moduleContainer;

        [SerializeField]
        private ModuleView _moduleViewPrefab;

        private readonly Dictionary<Guid, ModuleView>
            _moduleViewsById = new();

        public BastionPresentationState State
        {
            get;
            private set;
        }

        public bool IsBound =>
            State != null;

        public IReadOnlyCollection<ModuleView> ModuleViews =>
            _moduleViewsById
                .Values
                .OrderBy(view =>
                    view.State.Position.Deck)
                .ThenBy(view =>
                    view.State.Position.X)
                .ThenBy(view =>
                    view.ModuleId)
                .ToArray();

        public event Action<ModuleView> ModuleClicked;

        public event Action<BastionPresentationState>
            StateRendered;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void Render(
            BastionPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            ResolveReferences();

            bool gridMustBeRendered =
                State == null ||
                State.Width != state.Width ||
                State.DeckCount != state.DeckCount ||
                _gridView.RenderedWidth != state.Width ||
                _gridView.RenderedDeckCount !=
                    state.DeckCount;

            State = state;

            if (gridMustBeRendered)
            {
                _gridView.RenderGrid(
                    state.Width,
                    state.DeckCount);
            }

            var incomingModuleIds =
                new HashSet<Guid>(
                    state.Modules.Select(
                        module =>
                            module.ModuleId));

            RemoveMissingModuleViews(
                incomingModuleIds);

            foreach (
                ModulePresentationState moduleState
                in state.Modules)
            {
                if (_moduleViewsById.TryGetValue(
                        moduleState.ModuleId,
                        out ModuleView existingView))
                {
                    existingView.ApplyState(
                        moduleState);

                    continue;
                }

                CreateModuleView(
                    moduleState);
            }

            StateRendered?.Invoke(State);
        }

        public bool TryGetModuleView(
            Guid moduleId,
            out ModuleView moduleView)
        {
            return _moduleViewsById.TryGetValue(
                moduleId,
                out moduleView);
        }

        public void Clear()
        {
            ClearModuleViews();
            _gridView.ClearGrid();

            State = null;

            StateRendered?.Invoke(null);
        }

        private void CreateModuleView(
            ModulePresentationState state)
        {
            ModuleView moduleView =
                Instantiate(
                    _moduleViewPrefab,
                    _moduleContainer);

            moduleView.Bind(
                state,
                _layout);

            moduleView.Clicked +=
                HandleModuleViewClicked;

            _moduleViewsById.Add(
                state.ModuleId,
                moduleView);
        }

        private void RemoveMissingModuleViews(
            ISet<Guid> incomingModuleIds)
        {
            Guid[] removedModuleIds =
                _moduleViewsById
                    .Keys
                    .Where(moduleId =>
                        !incomingModuleIds.Contains(
                            moduleId))
                    .ToArray();

            foreach (Guid moduleId in removedModuleIds)
            {
                ModuleView moduleView =
                    _moduleViewsById[moduleId];

                moduleView.Clicked -=
                    HandleModuleViewClicked;

                _moduleViewsById.Remove(
                    moduleId);

                if (moduleView != null)
                {
                    Destroy(
                        moduleView.gameObject);
                }
            }
        }

        private void ClearModuleViews()
        {
            foreach (
                ModuleView moduleView
                in _moduleViewsById.Values)
            {
                if (moduleView == null)
                {
                    continue;
                }

                moduleView.Clicked -=
                    HandleModuleViewClicked;

                Destroy(
                    moduleView.gameObject);
            }

            _moduleViewsById.Clear();
        }

        private void HandleModuleViewClicked(
            ModuleView moduleView)
        {
            if (moduleView == null)
            {
                return;
            }

            ModuleClicked?.Invoke(
                moduleView);
        }

        private void ResolveReferences()
        {
            if (_layout == null)
            {
                _layout =
                    GetComponent<BastionGridLayout>();
            }

            if (_gridView == null)
            {
                _gridView =
                    GetComponentInChildren<
                        BastionGridView>(
                            includeInactive: true);
            }

            if (_moduleContainer == null)
            {
                Transform child =
                    transform.Find(
                        "ModuleContainer");

                if (child != null)
                {
                    _moduleContainer = child;
                }
            }

            if (_layout == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires BastionGridLayout.");
            }

            if (_gridView == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires BastionGridView.");
            }

            if (_moduleContainer == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires ModuleContainer.");
            }

            if (_moduleViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires ModuleView prefab.");
            }
        }
    }
}