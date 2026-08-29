using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Преобразует координаты Simulation-сетки
    /// в локальные и мировые координаты Unity.
    ///
    /// GridPosition обозначает нижнюю левую клетку.
    /// Визуальные объекты модулей располагаются по центру.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BastionGridLayout : MonoBehaviour
    {
        private const float MinimumCellDimension = 0.01f;

        [Header("Cell dimensions")]

        [SerializeField]
        [Min(MinimumCellDimension)]
        private float _cellWidth = 3f;

        [SerializeField]
        [Min(MinimumCellDimension)]
        private float _deckHeight = 2f;

        [Header("Grid origin")]

        [Tooltip(
            "Локальная позиция нижнего левого угла клетки X=0, Deck=0.")]
        [SerializeField]
        private Vector2 _localOrigin = Vector2.zero;

        public float CellWidth =>
            _cellWidth;

        public float DeckHeight =>
            _deckHeight;

        public Vector2 LocalOrigin =>
            _localOrigin;

        /// <summary>
        /// Нижний левый угол указанной клетки
        /// в локальных координатах BastionView.
        /// </summary>
        public Vector3 GetCellBottomLeftLocal(
            GridPosition position)
        {
            return new Vector3(
                _localOrigin.x +
                    position.X * _cellWidth,
                _localOrigin.y +
                    position.Deck * _deckHeight,
                0f);
        }

        /// <summary>
        /// Центр указанной клетки
        /// в локальных координатах BastionView.
        /// </summary>
        public Vector3 GetCellCenterLocal(
            GridPosition position)
        {
            Vector3 bottomLeft =
                GetCellBottomLeftLocal(position);

            return bottomLeft +
                   new Vector3(
                       _cellWidth * 0.5f,
                       _deckHeight * 0.5f,
                       0f);
        }

        /// <summary>
        /// Центр модуля с учётом его размера.
        /// </summary>
        public Vector3 GetModuleCenterLocal(
            GridPosition position,
            GridSize size)
        {
            Vector3 bottomLeft =
                GetCellBottomLeftLocal(position);

            return bottomLeft +
                   new Vector3(
                       size.Width *
                           _cellWidth *
                           0.5f,
                       size.Height *
                           _deckHeight *
                           0.5f,
                       0f);
        }

        /// <summary>
        /// Визуальный размер модуля
        /// в локальных единицах Unity.
        /// </summary>
        public Vector2 GetModuleSizeLocal(
            GridSize size)
        {
            return new Vector2(
                size.Width * _cellWidth,
                size.Height * _deckHeight);
        }

        /// <summary>
        /// Размер одного технического слота внутри модуля.
        ///
        /// Несколько объектов размещаются в строки,
        /// содержащие не более maxColumns элементов.
        /// </summary>
        public Vector2 GetModuleSlotSizeLocal(
            GridSize moduleSize,
            int slotCount,
            int maxColumns)
        {
            ValidateSlotGridArguments(
                slotCount,
                maxColumns);

            int columnCount =
                Math.Min(
                    slotCount,
                    maxColumns);

            int rowCount =
                (int)Math.Ceiling(
                    slotCount /
                    (double)columnCount);

            Vector2 moduleVisualSize =
                GetModuleSizeLocal(
                    moduleSize);

            return new Vector2(
                moduleVisualSize.x /
                    columnCount,
                moduleVisualSize.y /
                    rowCount);
        }

        /// <summary>
        /// Центр одного технического слота
        /// внутри указанного модуля.
        /// </summary>
        public Vector3 GetModuleSlotCenterLocal(
            GridPosition modulePosition,
            GridSize moduleSize,
            int slotIndex,
            int slotCount,
            int maxColumns)
        {
            ValidateSlotGridArguments(
                slotCount,
                maxColumns);

            if (slotIndex < 0 ||
                slotIndex >= slotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex));
            }

            int columnCount =
                Math.Min(
                    slotCount,
                    maxColumns);

            int columnIndex =
                slotIndex %
                columnCount;

            int rowIndex =
                slotIndex /
                columnCount;

            Vector2 slotSize =
                GetModuleSlotSizeLocal(
                    moduleSize,
                    slotCount,
                    maxColumns);

            Vector3 moduleBottomLeft =
                GetCellBottomLeftLocal(
                    modulePosition);

            return moduleBottomLeft +
                new Vector3(
                    (columnIndex + 0.5f) *
                        slotSize.x,
                    (rowIndex + 0.5f) *
                        slotSize.y,
                    0f);
        }

        private static void ValidateSlotGridArguments(
            int slotCount,
            int maxColumns)
        {
            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotCount));
            }

            if (maxColumns <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxColumns));
            }
        }

        /// <summary>
        /// Центр общей границы двух соседних клеток
        /// в локальных координатах BastionView.
        /// </summary>
        public Vector3 GetBoundaryCenterLocal(
            GridBoundarySegment boundary)
        {
            Vector3 firstCellCenter =
                GetCellCenterLocal(
                    boundary.CellA);

            Vector3 secondCellCenter =
                GetCellCenterLocal(
                    boundary.CellB);

            return
                (firstCellCenter +
                secondCellCenter) *
                0.5f;
        }

        /// <summary>
        /// Центр общей границы двух соседних клеток
        /// в мировых координатах.
        /// </summary>
        public Vector3 GetBoundaryCenterWorld(
            GridBoundarySegment boundary)
        {
            return transform.TransformPoint(
                GetBoundaryCenterLocal(
                    boundary));
        }

        public Vector3 GetCellCenterWorld(
            GridPosition position)
        {
            return transform.TransformPoint(
                GetCellCenterLocal(position));
        }

        public Vector3 GetModuleCenterWorld(
            GridPosition position,
            GridSize size)
        {
            return transform.TransformPoint(
                GetModuleCenterLocal(
                    position,
                    size));
        }

        private void Reset()
        {
            _cellWidth = 3f;
            _deckHeight = 2f;
            _localOrigin = Vector2.zero;
        }

        private void OnValidate()
        {
            _cellWidth =
                Math.Max(
                    MinimumCellDimension,
                    _cellWidth);

            _deckHeight =
                Math.Max(
                    MinimumCellDimension,
                    _deckHeight);
        }
    }
}