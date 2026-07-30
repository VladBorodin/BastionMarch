using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Корневое представление одного бастиона.
    ///
    /// Получает готовую модель из Simulation и создаёт
    /// визуальные представления её модулей.
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

        public event Action<ModuleView> ModuleClicked;

        private readonly Dictionary<Guid, ModuleView>
            _moduleViewsById = new();

        public Bastion Bastion { get; private set; }

        public bool IsBound =>
            Bastion != null;

        public IReadOnlyCollection<ModuleView> ModuleViews =>
            _moduleViewsById.Values.ToArray();

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>
        /// Привязывает View к готовой модели бастиона.
        /// </summary>
        public void Bind(
            Bastion bastion)
        {
            Bastion = bastion ??
                throw new ArgumentNullException(
                    nameof(bastion));

            ResolveReferences();
            ClearModuleViews();

            _gridView.RenderGrid(
                bastion.Width,
                bastion.DeckCount);

            IEnumerable<ModuleInstance> orderedModules =
                bastion.Modules
                    .OrderBy(module =>
                        module.Position.Deck)
                    .ThenBy(module =>
                        module.Position.X)
                    .ThenBy(module =>
                        module.Id);

            foreach (
                ModuleInstance module
                in orderedModules)
            {
                CreateModuleView(module);
            }
        }

        /// <summary>
        /// Обновляет уже созданные представления
        /// из текущего состояния Simulation.
        /// </summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                throw new InvalidOperationException(
                    "BastionView must be bound before refresh.");
            }

            foreach (
                ModuleView moduleView
                in _moduleViewsById.Values)
            {
                moduleView.Refresh();
            }
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

            Bastion = null;
        }

        private void CreateModuleView(
            ModuleInstance module)
        {
            ModuleView moduleView =
                Instantiate(
                    _moduleViewPrefab,
                    _moduleContainer);

            moduleView.Bind(
                module,
                _layout);

            moduleView.Clicked += HandleModuleViewClicked;

            _moduleViewsById.Add(
                module.Id,
                moduleView);
        }

        private void ClearModuleViews()
        {
            foreach (
                ModuleView moduleView
                in _moduleViewsById.Values)
            {
                if (moduleView != null)
                {
                    moduleView.Clicked -=
                        HandleModuleViewClicked;

                    Destroy(
                        moduleView.gameObject);
                }
            }

            _moduleViewsById.Clear();
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
    }
}