using System;
using BastionMarch.Simulation.Modules;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Временное визуальное представление одного
    /// установленного модуля бастиона.
    ///
    /// Компонент только отображает состояние Simulation
    /// и не изменяет игровые правила.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(SpriteRenderer),
        typeof(BoxCollider2D))]
    public sealed class ModuleView : MonoBehaviour
    {
        private const float MinimumVisualSize = 0.05f;

        [Header("References")]

        [SerializeField]
        private SpriteRenderer _bodyRenderer;

        [SerializeField]
        private BoxCollider2D _hitCollider;

        [Header("Layout")]

        [Tooltip(
            "Отступ прямоугольника модуля от линий сетки.")]
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

        private ModuleInstance _module;
        private BastionGridLayout _layout;

        public Guid ModuleId =>
            _module != null
                ? _module.Id
                : Guid.Empty;

        public ModuleInstance Module =>
            _module;

        public bool IsBound =>
            _module != null &&
            _layout != null;

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

        /// <summary>
        /// Привязывает View к конкретному экземпляру модуля.
        /// </summary>
        public void Bind(
            ModuleInstance module,
            BastionGridLayout layout)
        {
            _module = module ??
                throw new ArgumentNullException(
                    nameof(module));

            _layout = layout ??
                throw new ArgumentNullException(
                    nameof(layout));

            gameObject.name =
                $"ModuleView_{module.Definition.Id}_" +
                $"{module.Id:N}";

            Refresh();
        }

        /// <summary>
        /// Обновляет положение, размер и цвет
        /// из текущего состояния Simulation.
        /// </summary>
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
                    _module.Definition.Size);

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
                    _module.Position,
                    _module.Definition.Size);

            transform.localRotation =
                Quaternion.identity;

            transform.localScale =
                new Vector3(
                    visualWidth,
                    visualHeight,
                    1f);

            _bodyRenderer.color =
                GetTechnicalStateColor(
                    _module.TechnicalState);

            _bodyRenderer.sortingOrder =
                _sortingOrder;

            _hitCollider.offset =
                Vector2.zero;

            // Sprite имеет базовый размер 1 × 1,
            // поэтому Collider масштабируется Transform.
            _hitCollider.size =
                Vector2.one;
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