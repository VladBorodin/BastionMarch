using System;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Техническое визуальное представление
    /// перехода между модулями.
    ///
    /// Не хранит ModulePassage и не определяет
    /// правила проходимости.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PassageView : MonoBehaviour
    {
        private const float MinimumVisualSize = 0.01f;

        [Header("References")]

        [SerializeField]
        private SpriteRenderer _bodyRenderer;

        [Header("Prototype dimensions")]

        [Tooltip(
            "Толщина технического маркера перехода " +
            "в локальных единицах Unity.")]
        [SerializeField]
        [Min(MinimumVisualSize)]
        private float _markerThickness = 0.22f;

        [Tooltip(
            "Доля высоты или ширины клетки, " +
            "занимаемая маркером перехода.")]
        [SerializeField]
        [Range(0.1f, 1f)]
        private float _markerLengthFraction = 0.55f;

        [SerializeField]
        private int _sortingOrder = 20;

        [Header("Prototype type colors")]

        [SerializeField]
        private Color _doorColor =
            new Color(
                0.30f,
                0.85f,
                0.95f,
                1f);

        [SerializeField]
        private Color _hatchColor =
            new Color(
                0.35f,
                0.58f,
                0.90f,
                1f);

        [SerializeField]
        private Color _ladderColor =
            new Color(
                0.90f,
                0.78f,
                0.25f,
                1f);

        [SerializeField]
        private Color _stairwayColor =
            new Color(
                0.92f,
                0.52f,
                0.22f,
                1f);

        [SerializeField]
        private Color _elevatorColor =
            new Color(
                0.67f,
                0.40f,
                0.88f,
                1f);

        private PassagePresentationState _state;
        private BastionGridLayout _layout;

        public Guid PassageId =>
            _state != null
                ? _state.PassageId
                : Guid.Empty;

        public PassagePresentationState State =>
            _state;

        public bool IsBound =>
            _state != null &&
            _layout != null;

        private void Reset()
        {
            ResolveReferences();

            _markerThickness = 0.22f;
            _markerLengthFraction = 0.55f;
            _sortingOrder = 20;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void Bind(
            PassagePresentationState state,
            BastionGridLayout layout)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            _layout = layout ??
                throw new ArgumentNullException(
                    nameof(layout));

            ApplyState(state);
        }

        /// <summary>
        /// Обновляет View новым снимком
        /// того же перехода.
        /// </summary>
        public void ApplyState(
            PassagePresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            if (_state != null &&
                _state.PassageId != state.PassageId)
            {
                throw new InvalidOperationException(
                    "PassageView cannot be rebound " +
                    "to another passage.");
            }

            _state = state;

            gameObject.name =
                $"PassageView_{state.Type}_" +
                $"{state.PassageId:N}";

            Refresh();
        }

        public void Refresh()
        {
            if (!IsBound)
            {
                throw new InvalidOperationException(
                    "PassageView must be bound " +
                    "before refresh.");
            }

            ResolveReferences();

            transform.localPosition =
                _layout.GetBoundaryCenterLocal(
                    _state.Boundary);

            transform.localRotation =
                Quaternion.identity;

            Vector2 markerSize =
                GetMarkerSize();

            transform.localScale =
                new Vector3(
                    markerSize.x,
                    markerSize.y,
                    1f);

            _bodyRenderer.color =
                GetCurrentColor();

            _bodyRenderer.sortingOrder =
                _sortingOrder;
        }

        private Vector2 GetMarkerSize()
        {
            if (_state.IsHorizontal)
            {
                // Клетки находятся слева и справа.
                // Общая стена между ними вертикальна.
                return new Vector2(
                    _markerThickness,
                    Mathf.Max(
                        MinimumVisualSize,
                        _layout.DeckHeight *
                        _markerLengthFraction));
            }

            // Клетки находятся одна над другой.
            // Общая граница между ними горизонтальна.
            return new Vector2(
                Mathf.Max(
                    MinimumVisualSize,
                    _layout.CellWidth *
                    _markerLengthFraction),
                _markerThickness);
        }

        private Color GetCurrentColor()
        {
            Color typeColor =
                GetTypeColor(
                    _state.Type);

            switch (_state.State)
            {
                case ModulePassageState.Open:
                    return typeColor;

                case ModulePassageState.Closed:
                    return Color.Lerp(
                        typeColor,
                        Color.black,
                        0.45f);

                case ModulePassageState.Locked:
                    return Color.Lerp(
                        typeColor,
                        new Color(
                            1f,
                            0.78f,
                            0.15f,
                            1f),
                        0.55f);

                case ModulePassageState.Blocked:
                    return new Color(
                        0.82f,
                        0.20f,
                        0.12f,
                        1f);

                case ModulePassageState.Destroyed:
                    return new Color(
                        0.15f,
                        0.15f,
                        0.17f,
                        0.85f);

                default:
                    throw new InvalidOperationException(
                        "Unsupported passage state: " +
                        _state.State);
            }
        }

        private Color GetTypeColor(
            ModulePassageType type)
        {
            switch (type)
            {
                case ModulePassageType.Door:
                    return _doorColor;

                case ModulePassageType.Hatch:
                    return _hatchColor;

                case ModulePassageType.Ladder:
                    return _ladderColor;

                case ModulePassageType.Stairway:
                    return _stairwayColor;

                case ModulePassageType.Elevator:
                    return _elevatorColor;

                default:
                    throw new InvalidOperationException(
                        "Unsupported passage type: " +
                        type);
            }
        }

        private void ResolveReferences()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer =
                    GetComponent<SpriteRenderer>();
            }

            if (_bodyRenderer == null)
            {
                throw new InvalidOperationException(
                    "PassageView requires SpriteRenderer.");
            }
        }

        private void OnValidate()
        {
            _markerThickness =
                Mathf.Max(
                    MinimumVisualSize,
                    _markerThickness);

            _markerLengthFraction =
                Mathf.Clamp(
                    _markerLengthFraction,
                    0.1f,
                    1f);

            ResolveReferences();
        }
    }
}