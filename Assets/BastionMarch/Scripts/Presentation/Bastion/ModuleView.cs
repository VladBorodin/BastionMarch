using System;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Modules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Визуальное представление неизменяемого
    /// снимка одного модуля.
    ///
    /// Не хранит ModuleInstance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(SpriteRenderer),
        typeof(BoxCollider2D))]
    public sealed class ModuleView
        : MonoBehaviour, IPointerClickHandler
    {
        private const float MinimumVisualSize = 0.05f;

        [Header("References")]

        [SerializeField]
        private SpriteRenderer _bodyRenderer;

        [SerializeField]
        private BoxCollider2D _hitCollider;

        [Header("Layout")]

        [SerializeField]
        private Vector2 _inset =
            new Vector2(
                0.08f,
                0.08f);

        [SerializeField]
        private int _sortingOrder;

        [Header("Prototype colors")]

        [SerializeField]
        private Color _operationalColor =
            new Color(
                0.28f,
                0.48f,
                0.68f,
                1f);

        [SerializeField]
        private Color _damagedColor =
            new Color(
                0.72f,
                0.52f,
                0.20f,
                1f);

        [SerializeField]
        private Color _criticalColor =
            new Color(
                0.75f,
                0.24f,
                0.20f,
                1f);

        [SerializeField]
        private Color _destroyedColor =
            new Color(
                0.18f,
                0.18f,
                0.20f,
                1f);

        private ModulePresentationState _state;
        private BastionGridLayout _layout;
        private bool _isSelected;

        public event Action<ModuleView> Clicked;

        public Guid ModuleId =>
            _state != null
                ? _state.ModuleId
                : Guid.Empty;

        public ModulePresentationState State =>
            _state;

        public bool IsBound =>
            _state != null &&
            _layout != null;

        public bool IsSelected =>
            _isSelected;

        private void Reset()
        {
            ResolveReferences();

            _inset =
                new Vector2(
                    0.08f,
                    0.08f);

            _sortingOrder = 0;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void Bind(
            ModulePresentationState state,
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
        /// Обновляет View новым снимком того же модуля.
        /// </summary>
        public void ApplyState(
            ModulePresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            if (_state != null &&
                _state.ModuleId != state.ModuleId)
            {
                throw new InvalidOperationException(
                    "ModuleView cannot be rebound to another module.");
            }

            _state = state;

            gameObject.name =
                $"ModuleView_{state.DefinitionId}_" +
                $"{state.ModuleId:N}";

            Refresh();
        }

        public void Refresh()
        {
            if (!IsBound)
            {
                throw new InvalidOperationException(
                    "ModuleView must be bound before refresh.");
            }

            ResolveReferences();

            Vector2 fullSize =
                _layout.GetModuleSizeLocal(
                    _state.Size);

            float visualWidth =
                Mathf.Max(
                    MinimumVisualSize,
                    fullSize.x -
                    _inset.x * 2f);

            float visualHeight =
                Mathf.Max(
                    MinimumVisualSize,
                    fullSize.y -
                    _inset.y * 2f);

            transform.localPosition =
                _layout.GetModuleCenterLocal(
                    _state.Position,
                    _state.Size);

            transform.localRotation =
                Quaternion.identity;

            transform.localScale =
                new Vector3(
                    visualWidth,
                    visualHeight,
                    1f);

            ApplyCurrentColor();

            _bodyRenderer.sortingOrder =
                _sortingOrder;

            _hitCollider.offset =
                Vector2.zero;

            _hitCollider.size =
                Vector2.one;
        }

        public void SetSelected(
            bool isSelected)
        {
            if (_isSelected == isSelected)
            {
                return;
            }

            _isSelected = isSelected;

            if (IsBound)
            {
                ApplyCurrentColor();
            }
        }

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        private void ApplyCurrentColor()
        {
            Color baseColor =
                GetTechnicalStateColor(
                    _state.TechnicalState);

            _bodyRenderer.color =
                _isSelected
                    ? Color.Lerp(
                        baseColor,
                        Color.white,
                        0.35f)
                    : baseColor;
        }

        private Color GetTechnicalStateColor(
            ModuleTechnicalState state)
        {
            switch (state)
            {
                case ModuleTechnicalState.Operational:
                    return _operationalColor;

                case ModuleTechnicalState.Damaged:
                    return _damagedColor;

                case ModuleTechnicalState.Critical:
                    return _criticalColor;

                case ModuleTechnicalState.Destroyed:
                    return _destroyedColor;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported technical state: {state}.");
            }
        }

        private void ResolveReferences()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer =
                    GetComponent<SpriteRenderer>();
            }

            if (_hitCollider == null)
            {
                _hitCollider =
                    GetComponent<BoxCollider2D>();
            }

            if (_bodyRenderer == null)
            {
                throw new InvalidOperationException(
                    "ModuleView requires SpriteRenderer.");
            }

            if (_hitCollider == null)
            {
                throw new InvalidOperationException(
                    "ModuleView requires BoxCollider2D.");
            }
        }

        private void OnValidate()
        {
            _inset =
                new Vector2(
                    Mathf.Max(0f, _inset.x),
                    Mathf.Max(0f, _inset.y));

            ResolveReferences();
        }
    }
}