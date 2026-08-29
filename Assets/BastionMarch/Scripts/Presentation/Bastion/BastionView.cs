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
        [Header("Common references")]

        [SerializeField]
        private BastionGridLayout _layout;

        [SerializeField]
        private BastionGridView _gridView;

        [Header("Module presentation")]

        [SerializeField]
        private Transform _moduleContainer;

        [SerializeField]
        private ModuleView _moduleViewPrefab;

        [Header("Passage presentation")]

        [SerializeField]
        private Transform _passageContainer;

        [SerializeField]
        private PassageView _passageViewPrefab;

        [Header("Brigade presentation")]

        [SerializeField]
        private Transform _brigadeContainer;

        [SerializeField]
        private BrigadeView _brigadeViewPrefab;

        private readonly Dictionary<Guid, ModuleView>
            _moduleViewsById = new();

        private readonly Dictionary<Guid, PassageView>
            _passageViewsById = new();

        private readonly Dictionary<Guid, BrigadeView>
            _brigadeViewsById = new();

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

        public IReadOnlyCollection<PassageView> PassageViews =>
            _passageViewsById
                .Values
                .OrderBy(view =>
                    view.State.Boundary.CellA.Deck)
                .ThenBy(view =>
                    view.State.Boundary.CellA.X)
                .ThenBy(view =>
                    view.State.Boundary.CellB.Deck)
                .ThenBy(view =>
                    view.State.Boundary.CellB.X)
                .ThenBy(view =>
                    view.PassageId)
                .ToArray();

        public IReadOnlyCollection<BrigadeView> BrigadeViews =>
            _brigadeViewsById
                .Values
                .OrderBy(view =>
                    view.State.CurrentModuleId)
                .ThenBy(view =>
                    view.State.Number)
                .ThenBy(view =>
                    view.BrigadeId)
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

            RenderModules(
                state.Modules);

            RenderPassages(
                state.Passages);

            RenderBrigades(
                state);

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

        public bool TryGetPassageView(
            Guid passageId,
            out PassageView passageView)
        {
            return _passageViewsById.TryGetValue(
                passageId,
                out passageView);
        }

        public bool TryGetBrigadeView(
            Guid brigadeId,
            out BrigadeView brigadeView)
        {
            return _brigadeViewsById.TryGetValue(
                brigadeId,
                out brigadeView);
        }

        public void Clear()
        {
            ClearBrigadeViews();
            ClearPassageViews();
            ClearModuleViews();
            _gridView.ClearGrid();

            State = null;

            StateRendered?.Invoke(null);
        }

        private void RenderModules(
            IReadOnlyList<ModulePresentationState>
                moduleStates)
        {
            var incomingModuleIds =
                new HashSet<Guid>(
                    moduleStates.Select(
                        module =>
                            module.ModuleId));

            RemoveMissingModuleViews(
                incomingModuleIds);

            foreach (
                ModulePresentationState moduleState
                in moduleStates)
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
        }

        private void RenderPassages(
            IReadOnlyList<PassagePresentationState>
                passageStates)
        {
            var incomingPassageIds =
                new HashSet<Guid>(
                    passageStates.Select(
                        passage =>
                            passage.PassageId));

            RemoveMissingPassageViews(
                incomingPassageIds);

            foreach (
                PassagePresentationState passageState
                in passageStates)
            {
                if (_passageViewsById.TryGetValue(
                        passageState.PassageId,
                        out PassageView existingView))
                {
                    existingView.ApplyState(
                        passageState);

                    continue;
                }

                CreatePassageView(
                    passageState);
            }
        }

        private void RenderBrigades(
            BastionPresentationState state)
        {
            BrigadePresentationState[] deployedBrigades =
                state.Brigades
                    .Where(brigade =>
                        brigade.IsDeployed)
                    .ToArray();

            var incomingBrigadeIds =
                new HashSet<Guid>(
                    deployedBrigades.Select(
                        brigade =>
                            brigade.BrigadeId));

            RemoveMissingBrigadeViews(
                incomingBrigadeIds);

            var brigadesByModule =
                deployedBrigades
                    .GroupBy(brigade =>
                        brigade.CurrentModuleId.Value)
                    .OrderBy(group =>
                        group.Key);

            foreach (var moduleGroup in brigadesByModule)
            {
                if (!state.TryGetModule(
                        moduleGroup.Key,
                        out ModulePresentationState
                            moduleState))
                {
                    throw new InvalidOperationException(
                        "Deployed brigade references " +
                        "a module missing from presentation state.");
                }

                BrigadePresentationState[] moduleBrigades =
                    moduleGroup
                        .OrderBy(brigade =>
                            brigade.Number)
                        .ThenBy(brigade =>
                            brigade.BrigadeId)
                        .ToArray();

                int slotCount =
                    moduleBrigades.Length;

                for (int slotIndex = 0;
                    slotIndex < slotCount;
                    slotIndex++)
                {
                    BrigadePresentationState brigadeState =
                        moduleBrigades[slotIndex];

                    if (_brigadeViewsById.TryGetValue(
                            brigadeState.BrigadeId,
                            out BrigadeView existingView))
                    {
                        existingView.ApplyState(
                            brigadeState,
                            moduleState,
                            slotIndex,
                            slotCount);

                        continue;
                    }

                    CreateBrigadeView(
                        brigadeState,
                        moduleState,
                        slotIndex,
                        slotCount);
                }
            }
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

        private void CreatePassageView(
            PassagePresentationState state)
        {
            PassageView passageView =
                Instantiate(
                    _passageViewPrefab,
                    _passageContainer);

            passageView.Bind(
                state,
                _layout);

            _passageViewsById.Add(
                state.PassageId,
                passageView);
        }

        private void CreateBrigadeView(
            BrigadePresentationState brigadeState,
            ModulePresentationState moduleState,
            int slotIndex,
            int slotCount)
        {
            BrigadeView brigadeView =
                Instantiate(
                    _brigadeViewPrefab,
                    _brigadeContainer);

            brigadeView.Bind(
                brigadeState,
                moduleState,
                _layout,
                slotIndex,
                slotCount);

            _brigadeViewsById.Add(
                brigadeState.BrigadeId,
                brigadeView);
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

            foreach (
                Guid moduleId
                in removedModuleIds)
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

        private void RemoveMissingPassageViews(
            ISet<Guid> incomingPassageIds)
        {
            Guid[] removedPassageIds =
                _passageViewsById
                    .Keys
                    .Where(passageId =>
                        !incomingPassageIds.Contains(
                            passageId))
                    .ToArray();

            foreach (
                Guid passageId
                in removedPassageIds)
            {
                PassageView passageView =
                    _passageViewsById[passageId];

                _passageViewsById.Remove(
                    passageId);

                if (passageView != null)
                {
                    Destroy(
                        passageView.gameObject);
                }
            }
        }

        private void RemoveMissingBrigadeViews(
            ISet<Guid> incomingBrigadeIds)
        {
            Guid[] removedBrigadeIds =
                _brigadeViewsById
                    .Keys
                    .Where(brigadeId =>
                        !incomingBrigadeIds.Contains(
                            brigadeId))
                    .ToArray();

            foreach (
                Guid brigadeId
                in removedBrigadeIds)
            {
                BrigadeView brigadeView =
                    _brigadeViewsById[brigadeId];

                _brigadeViewsById.Remove(
                    brigadeId);

                if (brigadeView != null)
                {
                    Destroy(
                        brigadeView.gameObject);
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

        private void ClearBrigadeViews()
        {
            foreach (
                BrigadeView brigadeView
                in _brigadeViewsById.Values)
            {
                if (brigadeView == null)
                {
                    continue;
                }

                Destroy(
                    brigadeView.gameObject);
            }

            _brigadeViewsById.Clear();
        }

        private void ClearPassageViews()
        {
            foreach (
                PassageView passageView
                in _passageViewsById.Values)
            {
                if (passageView == null)
                {
                    continue;
                }

                Destroy(
                    passageView.gameObject);
            }

            _passageViewsById.Clear();
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
                _moduleContainer =
                    FindDirectChild(
                        "ModuleContainer");
            }

            if (_passageContainer == null)
            {
                _passageContainer =
                    FindDirectChild(
                        "PassageContainer");
            }

            if (_brigadeContainer == null)
            {
                _brigadeContainer =
                    FindDirectChild(
                        "BrigadeContainer");
            }

            if (_layout == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "BastionGridLayout.");
            }

            if (_gridView == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "BastionGridView.");
            }

            if (_moduleContainer == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "ModuleContainer.");
            }

            if (_moduleViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "ModuleView prefab.");
            }

            if (_passageContainer == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "PassageContainer.");
            }

            if (_passageViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "PassageView prefab.");
            }

            if (_brigadeContainer == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "BrigadeContainer.");
            }

            if (_brigadeViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "BastionView requires " +
                    "BrigadeView prefab.");
            }
        }

        private Transform FindDirectChild(
            string childName)
        {
            Transform child =
                transform.Find(
                    childName);

            return child;
        }
    }
}