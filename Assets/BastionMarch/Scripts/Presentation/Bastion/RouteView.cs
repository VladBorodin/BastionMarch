using System;
using System.Collections.Generic;
using BastionMarch.Presentation.Bastions.State;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Техническое визуальное представление
    /// рассчитанного маршрута между модулями.
    ///
    /// Не выполняет поиск пути и не хранит
    /// объекты Simulation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class RouteView : MonoBehaviour
    {
        private const float MinimumLineWidth = 0.01f;

        [Header("References")]

        [SerializeField]
        private BastionGridLayout _layout;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [Header("Prototype appearance")]

        [SerializeField]
        [Min(MinimumLineWidth)]
        private float _lineWidth = 0.12f;

        [SerializeField]
        private Color _routeColor =
            new Color(
                1f,
                0.82f,
                0.18f,
                1f);

        [SerializeField]
        private int _sortingOrder = 25;

        private BastionPresentationState
            _bastionState;

        private RoutePresentationState
            _routeState;

        public BastionPresentationState BastionState =>
            _bastionState;

        public RoutePresentationState State =>
            _routeState;

        public bool HasRoute =>
            _routeState != null;

        public bool IsSuccessfulRoute =>
            _routeState != null &&
            _routeState.IsSuccess;

        public int RenderedPointCount =>
            _lineRenderer != null
                ? _lineRenderer.positionCount
                : 0;

        private void Reset()
        {
            ResolveReferences();

            _lineWidth = 0.12f;
            _sortingOrder = 25;

            ConfigureRenderer();
            Clear();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureRenderer();

            if (_routeState == null)
            {
                ClearRenderer();
            }
        }

        /// <summary>
        /// Показывает результат поиска маршрута.
        ///
        /// Для неуспешного результата линия очищается.
        /// Причины блокировки будут отображаться
        /// отдельным Presentation-механизмом.
        /// </summary>
        public void Show(
            BastionPresentationState bastionState,
            RoutePresentationState routeState)
        {
            if (bastionState == null)
            {
                throw new ArgumentNullException(
                    nameof(bastionState));
            }

            if (routeState == null)
            {
                throw new ArgumentNullException(
                    nameof(routeState));
            }

            ResolveReferences();
            ConfigureRenderer();

            _bastionState = bastionState;
            _routeState = routeState;

            if (!routeState.IsSuccess)
            {
                ClearRenderer();
                return;
            }

            Vector3[] points =
                BuildSuccessfulRoutePoints(
                    bastionState,
                    routeState);

            _lineRenderer.positionCount =
                points.Length;

            _lineRenderer.SetPositions(
                points);

            // Маршрут в тот же отсек корректен,
            // но линии из одной точки визуально нет.
            _lineRenderer.enabled =
                points.Length >= 2;
        }

        public void Clear()
        {
            _bastionState = null;
            _routeState = null;

            ResolveReferences();
            ClearRenderer();
        }

        public Vector3 GetRenderedPoint(
            int index)
        {
            ResolveReferences();

            if (index < 0 ||
                index >=
                    _lineRenderer.positionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return _lineRenderer.GetPosition(
                index);
        }

        private Vector3[] BuildSuccessfulRoutePoints(
            BastionPresentationState bastionState,
            RoutePresentationState routeState)
        {
            if (!bastionState.TryGetModule(
                    routeState.SourceModuleId,
                    out ModulePresentationState
                        sourceModule))
            {
                throw new InvalidOperationException(
                    "Route source module is missing " +
                    "from BastionPresentationState.");
            }

            var points =
                new List<Vector3>(
                    routeState.StepCount * 2 + 1);

            points.Add(
                GetModuleCenterInRouteSpace(
                    sourceModule));

            Guid currentModuleId =
                routeState.SourceModuleId;

            foreach (
                RouteStepPresentationState step
                in routeState.Steps)
            {
                if (step.FromModuleId !=
                    currentModuleId)
                {
                    throw new InvalidOperationException(
                        "Route presentation steps " +
                        "are not continuous.");
                }

                if (!bastionState.TryGetModule(
                        step.ToModuleId,
                        out ModulePresentationState
                            destinationModule))
                {
                    throw new InvalidOperationException(
                        "Route destination module " +
                        "is missing from " +
                        "BastionPresentationState.");
                }

                Vector3 boundaryWorld =
                    _layout.GetBoundaryCenterWorld(
                        step.Boundary);

                points.Add(
                    transform.InverseTransformPoint(
                        boundaryWorld));

                points.Add(
                    GetModuleCenterInRouteSpace(
                        destinationModule));

                currentModuleId =
                    step.ToModuleId;
            }

            if (currentModuleId !=
                routeState.TargetModuleId)
            {
                throw new InvalidOperationException(
                    "Rendered route does not end " +
                    "at its target module.");
            }

            return points.ToArray();
        }

        private Vector3 GetModuleCenterInRouteSpace(
            ModulePresentationState moduleState)
        {
            Vector3 moduleWorld =
                _layout.GetModuleCenterWorld(
                    moduleState.Position,
                    moduleState.Size);

            return transform.InverseTransformPoint(
                moduleWorld);
        }

        private void ClearRenderer()
        {
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }

        private void ConfigureRenderer()
        {
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = false;

            _lineRenderer.widthMultiplier =
                _lineWidth;

            _lineRenderer.startColor =
                _routeColor;

            _lineRenderer.endColor =
                _routeColor;

            _lineRenderer.sortingOrder =
                _sortingOrder;

            _lineRenderer.numCapVertices = 4;
            _lineRenderer.numCornerVertices = 4;
        }

        private void ResolveReferences()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer =
                    GetComponent<LineRenderer>();
            }

            if (_layout == null)
            {
                _layout =
                    GetComponentInParent<
                        BastionGridLayout>(
                            includeInactive: true);
            }

            if (_lineRenderer == null)
            {
                throw new InvalidOperationException(
                    "RouteView requires LineRenderer.");
            }

            if (_layout == null)
            {
                throw new InvalidOperationException(
                    "RouteView requires " +
                    "BastionGridLayout in its parent hierarchy.");
            }
        }

        private void OnValidate()
        {
            _lineWidth =
                Mathf.Max(
                    MinimumLineWidth,
                    _lineWidth);

            ResolveReferences();
            ConfigureRenderer();
        }
    }
}