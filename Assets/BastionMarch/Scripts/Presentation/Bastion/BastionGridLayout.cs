using System;
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