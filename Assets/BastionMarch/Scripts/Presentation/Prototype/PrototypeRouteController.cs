using System;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using UnityEngine;

namespace BastionMarch.Presentation.Prototype
{
    /// <summary>
    /// Временный контроллер для проверки маршрутов
    /// в Presentation-прототипе.
    ///
    /// Выполняет запрос к Simulation и передаёт
    /// его неизменяемый результат в RouteView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeRouteController
        : MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private BastionPrototypeBootstrap _bootstrap;

        [SerializeField]
        private BastionPresenter _presenter;

        [SerializeField]
        private RouteView _routeView;

        [Header("Prototype behaviour")]

        [SerializeField]
        private bool _showRouteOnStart = true;

        public Guid? CurrentBrigadeId
        {
            get;
            private set;
        }

        public RoutePresentationState CurrentRoute
        {
            get;
            private set;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (!_showRouteOnStart)
            {
                return;
            }

            ShowLongestReachableBrigadeRoute();
        }

        /// <summary>
        /// Строит маршрут конкретной бригады
        /// из её текущего отсека к указанной цели.
        /// </summary>
        public RoutePresentationState
            ShowRouteForBrigade(
                Guid brigadeId,
                Guid targetModuleId)
        {
            ResolveReferences();

            Bastion bastion =
                _bootstrap.Bastion;

            if (bastion == null)
            {
                throw new InvalidOperationException(
                    "Prototype bastion is not initialized.");
            }

            BastionPresentationState bastionState =
                _presenter.CurrentState;

            if (bastionState == null)
            {
                throw new InvalidOperationException(
                    "Presenter has no current state.");
            }

            if (!bastionState.TryGetBrigade(
                    brigadeId,
                    out BrigadePresentationState
                        brigadeState))
            {
                throw new InvalidOperationException(
                    "Brigade is missing from " +
                    "BastionPresentationState.");
            }

            if (!brigadeState.CurrentModuleId.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot build a module route " +
                    "for an undeployed brigade.");
            }

            if (!bastionState.TryGetModule(
                    targetModuleId,
                    out _))
            {
                throw new InvalidOperationException(
                    "Target module is missing from " +
                    "BastionPresentationState.");
            }

            Guid sourceModuleId =
                brigadeState.CurrentModuleId.Value;

            ModuleRouteSearchResult result =
                bastion.FindModuleRoute(
                    sourceModuleId,
                    targetModuleId);

            RoutePresentationState routeState =
                RoutePresentationStateFactory
                    .CaptureSearchResult(
                        sourceModuleId,
                        targetModuleId,
                        result);

            CurrentBrigadeId =
                brigadeId;

            CurrentRoute =
                routeState;

            _routeView.Show(
                bastionState,
                routeState);

            return routeState;
        }

        /// <summary>
        /// Для технической сцены выбирает самый
        /// длинный доступный маршрут среди всех
        /// развёрнутых бригад.
        /// </summary>
        public bool ShowLongestReachableBrigadeRoute()
        {
            ResolveReferences();

            Bastion bastion =
                _bootstrap.Bastion;

            BastionPresentationState bastionState =
                _presenter.CurrentState;

            if (bastion == null ||
                bastionState == null)
            {
                ClearRoute();
                return false;
            }

            BrigadePresentationState
                selectedBrigade = null;

            RoutePresentationState
                selectedRoute = null;

            foreach (
                BrigadePresentationState brigade
                in bastionState.Brigades
                    .Where(item =>
                        item.IsDeployed)
                    .OrderBy(item =>
                        item.Number)
                    .ThenBy(item =>
                        item.BrigadeId))
            {
                Guid sourceModuleId =
                    brigade.CurrentModuleId.Value;

                foreach (
                    ModulePresentationState targetModule
                    in bastionState.Modules)
                {
                    if (targetModule.ModuleId ==
                        sourceModuleId)
                    {
                        continue;
                    }

                    ModuleRouteSearchResult result =
                        bastion.FindModuleRoute(
                            sourceModuleId,
                            targetModule.ModuleId);

                    if (!result.IsSuccess)
                    {
                        continue;
                    }

                    RoutePresentationState candidate =
                        RoutePresentationStateFactory
                            .CaptureSearchResult(
                                sourceModuleId,
                                targetModule.ModuleId,
                                result);

                    if (candidate.StepCount <= 0)
                    {
                        continue;
                    }

                    if (selectedRoute != null &&
                        candidate.StepCount <=
                            selectedRoute.StepCount)
                    {
                        continue;
                    }

                    selectedBrigade =
                        brigade;

                    selectedRoute =
                        candidate;
                }
            }

            if (selectedBrigade == null ||
                selectedRoute == null)
            {
                ClearRoute();
                return false;
            }

            CurrentBrigadeId =
                selectedBrigade.BrigadeId;

            CurrentRoute =
                selectedRoute;

            _routeView.Show(
                bastionState,
                selectedRoute);

            return true;
        }

        public void ClearRoute()
        {
            CurrentBrigadeId = null;
            CurrentRoute = null;

            if (_routeView != null)
            {
                _routeView.Clear();
            }
        }

        private void ResolveReferences()
        {
            if (_bootstrap == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRouteController requires " +
                    "BastionPrototypeBootstrap.");
            }

            if (_presenter == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRouteController requires " +
                    "BastionPresenter.");
            }

            if (_routeView == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRouteController requires " +
                    "RouteView.");
            }
        }
    }
}