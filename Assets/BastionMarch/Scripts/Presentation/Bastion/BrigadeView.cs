using System;
using BastionMarch.Presentation.Bastions.State;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Техническое визуальное представление бригады.
    ///
    /// Не хранит Brigade и не выполняет
    /// игровые расчёты персонала или работы.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BrigadeView : MonoBehaviour
    {
        private const float MinimumVisualSize = 0.05f;

        [Header("References")]

        [SerializeField]
        private SpriteRenderer _bodyRenderer;

        [Header("Layout")]

        [SerializeField]
        [Min(1)]
        private int _maximumColumns = 4;

        [SerializeField]
        [Range(0.1f, 1f)]
        private float _markerScaleFraction = 0.55f;

        [SerializeField]
        [Min(MinimumVisualSize)]
        private float _minimumMarkerSize = 0.25f;

        [SerializeField]
        private int _sortingOrder = 30;

        [Header("Prototype colors")]

        [SerializeField]
        private Color _idleColor =
            new Color(
                0.72f,
                0.75f,
                0.80f,
                1f);

        [SerializeField]
        private Color _workingColor =
            new Color(
                0.25f,
                0.85f,
                0.35f,
                1f);

        [SerializeField]
        private Color _noPersonnelColor =
            new Color(
                0.85f,
                0.22f,
                0.18f,
                1f);

        [SerializeField]
        private Color _disbandedColor =
            new Color(
                0.18f,
                0.18f,
                0.20f,
                0.65f);

        private BrigadePresentationState _state;

        private ModulePresentationState
            _moduleState;

        private BastionGridLayout _layout;

        public Guid BrigadeId =>
            _state != null
                ? _state.BrigadeId
                : Guid.Empty;

        public BrigadePresentationState State =>
            _state;

        public ModulePresentationState ModuleState =>
            _moduleState;

        public int SlotIndex
        {
            get;
            private set;
        }

        public int SlotCount
        {
            get;
            private set;
        }

        public bool IsBound =>
            _state != null &&
            _moduleState != null &&
            _layout != null;

        /// <summary>
        /// Временное разработческое описание.
        /// Позднее его будет отображать TMP-компонент.
        /// </summary>
        public string TechnicalLabel
        {
            get;
            private set;
        }

        private void Reset()
        {
            ResolveReferences();

            _maximumColumns = 4;
            _markerScaleFraction = 0.55f;
            _minimumMarkerSize = 0.25f;
            _sortingOrder = 30;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void Bind(
            BrigadePresentationState state,
            ModulePresentationState moduleState,
            BastionGridLayout layout,
            int slotIndex,
            int slotCount)
        {
            _layout = layout ??
                throw new ArgumentNullException(
                    nameof(layout));

            ApplyState(
                state,
                moduleState,
                slotIndex,
                slotCount);
        }

        /// <summary>
        /// Обновляет снимок, модуль и слот бригады.
        ///
        /// Одна BrigadeView может перемещаться
        /// между модулями, но не может представлять
        /// другую бригаду.
        /// </summary>
        public void ApplyState(
            BrigadePresentationState state,
            ModulePresentationState moduleState,
            int slotIndex,
            int slotCount)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            if (moduleState == null)
            {
                throw new ArgumentNullException(
                    nameof(moduleState));
            }

            if (!state.CurrentModuleId.HasValue ||
                state.CurrentModuleId.Value !=
                moduleState.ModuleId)
            {
                throw new ArgumentException(
                    "Brigade snapshot does not belong " +
                    "to the supplied module.",
                    nameof(moduleState));
            }

            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotCount));
            }

            if (slotIndex < 0 ||
                slotIndex >= slotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex));
            }

            if (_state != null &&
                _state.BrigadeId != state.BrigadeId)
            {
                throw new InvalidOperationException(
                    "BrigadeView cannot be rebound " +
                    "to another brigade.");
            }

            _state = state;
            _moduleState = moduleState;
            SlotIndex = slotIndex;
            SlotCount = slotCount;

            TechnicalLabel =
                $"#{state.Number} " +
                $"{state.Type} " +
                $"{state.CurrentPersonnel}/" +
                $"{state.MaximumUsefulPersonnel}";

            gameObject.name =
                $"BrigadeView_{state.Number}_" +
                $"{state.Type}_" +
                $"{state.BrigadeId:N}";

            Refresh();
        }

        public void Refresh()
        {
            if (!IsBound)
            {
                throw new InvalidOperationException(
                    "BrigadeView must be bound " +
                    "before refresh.");
            }

            ResolveReferences();

            transform.localPosition =
                _layout.GetModuleSlotCenterLocal(
                    _moduleState.Position,
                    _moduleState.Size,
                    SlotIndex,
                    SlotCount,
                    _maximumColumns);

            transform.localRotation =
                Quaternion.identity;

            Vector2 slotSize =
                _layout.GetModuleSlotSizeLocal(
                    _moduleState.Size,
                    SlotCount,
                    _maximumColumns);

            float markerSize =
                Mathf.Max(
                    _minimumMarkerSize,
                    Mathf.Min(
                        slotSize.x,
                        slotSize.y) *
                    _markerScaleFraction);

            transform.localScale =
                new Vector3(
                    markerSize,
                    markerSize,
                    1f);

            _bodyRenderer.color =
                GetCurrentColor();

            _bodyRenderer.sortingOrder =
                _sortingOrder;
        }

        private Color GetCurrentColor()
        {
            if (_state.IsDisbanded)
            {
                return _disbandedColor;
            }

            if (_state.CurrentPersonnel <= 0)
            {
                return _noPersonnelColor;
            }

            return _state.IsWorking
                ? _workingColor
                : _idleColor;
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
                    "BrigadeView requires SpriteRenderer.");
            }
        }

        private void OnValidate()
        {
            _maximumColumns =
                Mathf.Max(
                    1,
                    _maximumColumns);

            _markerScaleFraction =
                Mathf.Clamp(
                    _markerScaleFraction,
                    0.1f,
                    1f);

            _minimumMarkerSize =
                Mathf.Max(
                    MinimumVisualSize,
                    _minimumMarkerSize);

            ResolveReferences();
        }
    }
}