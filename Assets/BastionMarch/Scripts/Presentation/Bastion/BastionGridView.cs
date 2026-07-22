using System;
using System.Collections.Generic;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Отображает двумерную сетку внутреннего пространства бастиона.
    ///
    /// Компонент не определяет размеры конструкции самостоятельно.
    /// Он получает ширину и количество этажей извне и использует
    /// BastionGridLayout только для преобразования координат.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BastionGridView : MonoBehaviour
    {
        private const int MinimumGridSize = 1;
        private const float MinimumLineWidth = 0.001f;

        [Header("References")]

        [SerializeField]
        private BastionGridLayout _layout;

        [Header("Prototype preview")]

        [SerializeField]
        private bool _renderPrototypeGridOnStart = true;

        [SerializeField]
        [Min(MinimumGridSize)]
        private int _prototypeWidth = 8;

        [SerializeField]
        [Min(MinimumGridSize)]
        private int _prototypeDeckCount = 3;

        [Header("Line appearance")]

        [SerializeField]
        [Min(MinimumLineWidth)]
        private float _lineWidth = 0.035f;

        [SerializeField]
        private Color _lineColor =
            new Color(
                r: 0.75f,
                g: 0.82f,
                b: 0.90f,
                a: 0.35f);

        [SerializeField]
        private int _sortingOrder = -100;

        private readonly List<LineRenderer> _createdLines =
            new();

        private Transform _generatedRoot;
        private Material _lineMaterial;

        public int RenderedWidth { get; private set; }

        public int RenderedDeckCount { get; private set; }

        private void Reset()
        {
            _layout =
                GetComponentInParent<BastionGridLayout>();

            _renderPrototypeGridOnStart = true;
            _prototypeWidth = 8;
            _prototypeDeckCount = 3;
            _lineWidth = 0.035f;
            _sortingOrder = -100;
        }

        private void Awake()
        {
            ResolveLayout();
        }

        private void Start()
        {
            if (_renderPrototypeGridOnStart)
            {
                RenderGrid(
                    _prototypeWidth,
                    _prototypeDeckCount);
            }
        }

        public void RenderGrid(
            int width,
            int deckCount)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Grid width must be greater than zero.");
            }

            if (deckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deckCount),
                    "Deck count must be greater than zero.");
            }

            ResolveLayout();
            ClearGrid();

            _generatedRoot =
                CreateGeneratedRoot();

            _lineMaterial =
                CreateLineMaterial();

            DrawVerticalLines(
                width,
                deckCount);

            DrawHorizontalLines(
                width,
                deckCount);

            RenderedWidth = width;
            RenderedDeckCount = deckCount;
        }

        public void ClearGrid()
        {
            _createdLines.Clear();

            if (_generatedRoot != null)
            {
                Destroy(
                    _generatedRoot.gameObject);

                _generatedRoot = null;
            }

            if (_lineMaterial != null)
            {
                Destroy(
                    _lineMaterial);

                _lineMaterial = null;
            }

            RenderedWidth = 0;
            RenderedDeckCount = 0;
        }

        private void DrawVerticalLines(
            int width,
            int deckCount)
        {
            float originX =
                _layout.LocalOrigin.x;

            float bottomY =
                _layout.LocalOrigin.y;

            float topY =
                bottomY +
                deckCount *
                _layout.DeckHeight;

            for (int x = 0;
                 x <= width;
                 x++)
            {
                float lineX =
                    originX +
                    x *
                    _layout.CellWidth;

                CreateLine(
                    name: $"Vertical_{x}",
                    start: new Vector3(
                        lineX,
                        bottomY,
                        0f),
                    end: new Vector3(
                        lineX,
                        topY,
                        0f));
            }
        }

        private void DrawHorizontalLines(
            int width,
            int deckCount)
        {
            float originX =
                _layout.LocalOrigin.x;

            float leftX =
                originX;

            float rightX =
                originX +
                width *
                _layout.CellWidth;

            for (int deck = 0;
                 deck <= deckCount;
                 deck++)
            {
                float lineY =
                    _layout.LocalOrigin.y +
                    deck *
                    _layout.DeckHeight;

                CreateLine(
                    name: $"Horizontal_{deck}",
                    start: new Vector3(
                        leftX,
                        lineY,
                        0f),
                    end: new Vector3(
                        rightX,
                        lineY,
                        0f));
            }
        }

        private void CreateLine(
            string name,
            Vector3 start,
            Vector3 end)
        {
            var lineObject =
                new GameObject(name);

            lineObject.transform.SetParent(
                _generatedRoot,
                worldPositionStays: false);

            var lineRenderer =
                lineObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;

            lineRenderer.positionCount = 2;

            lineRenderer.SetPosition(
                index: 0,
                position: start);

            lineRenderer.SetPosition(
                index: 1,
                position: end);

            lineRenderer.startWidth =
                _lineWidth;

            lineRenderer.endWidth =
                _lineWidth;

            lineRenderer.startColor =
                _lineColor;

            lineRenderer.endColor =
                _lineColor;

            lineRenderer.material =
                _lineMaterial;

            lineRenderer.numCapVertices = 0;
            lineRenderer.numCornerVertices = 0;

            lineRenderer.sortingOrder =
                _sortingOrder;

            _createdLines.Add(
                lineRenderer);
        }

        private Transform CreateGeneratedRoot()
        {
            var root =
                new GameObject("GeneratedGrid");

            root.transform.SetParent(
                transform,
                worldPositionStays: false);

            return root.transform;
        }

        private void ResolveLayout()
        {
            if (_layout == null)
            {
                _layout =
                    GetComponentInParent<BastionGridLayout>();
            }

            if (_layout == null)
            {
                throw new InvalidOperationException(
                    "BastionGridView requires BastionGridLayout " +
                    "on itself or a parent object.");
            }
        }

        private static Material CreateLineMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "A compatible unlit sprite shader was not found.");
            }

            return new Material(shader)
            {
                name = "Runtime Bastion Grid Material"
            };
        }

        private void OnValidate()
        {
            _prototypeWidth =
                Mathf.Max(
                    MinimumGridSize,
                    _prototypeWidth);

            _prototypeDeckCount =
                Mathf.Max(
                    MinimumGridSize,
                    _prototypeDeckCount);

            _lineWidth =
                Mathf.Max(
                    MinimumLineWidth,
                    _lineWidth);
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(
                    _lineMaterial);
            }
        }
    }
}