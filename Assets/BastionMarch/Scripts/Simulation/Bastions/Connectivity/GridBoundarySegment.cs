using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Каноническое описание границы между двумя
    /// соседними клетками сетки.
    ///
    /// Порядок переданных клеток не влияет на равенство.
    /// </summary>
    public readonly struct GridBoundarySegment
        : IEquatable<GridBoundarySegment>
    {
        public GridPosition CellA { get; }

        public GridPosition CellB { get; }

        /// <summary>
        /// Переход между клетками находится на одном этаже.
        /// Разделяющая их стена при этом вертикальна.
        /// </summary>
        public bool IsHorizontalPassage =>
            CellA.Deck == CellB.Deck;

        /// <summary>
        /// Клетки находятся одна над другой.
        /// Для реального прохода позднее потребуется
        /// лестница, люк или лифт.
        /// </summary>
        public bool IsVerticalPassage =>
            CellA.X == CellB.X;

        public GridBoundarySegment(
            GridPosition firstCell,
            GridPosition secondCell)
        {
            int distance =
                Math.Abs(
                    firstCell.X -
                    secondCell.X) +
                Math.Abs(
                    firstCell.Deck -
                    secondCell.Deck);

            if (distance != 1)
            {
                throw new ArgumentException(
                    "Boundary cells must share exactly one side.");
            }

            if (ComesBefore(
                    firstCell,
                    secondCell))
            {
                CellA = firstCell;
                CellB = secondCell;
            }
            else
            {
                CellA = secondCell;
                CellB = firstCell;
            }
        }

        private static bool ComesBefore(
            GridPosition first,
            GridPosition second)
        {
            if (first.Deck != second.Deck)
            {
                return first.Deck < second.Deck;
            }

            return first.X < second.X;
        }

        public bool Equals(
            GridBoundarySegment other)
        {
            return CellA.Equals(other.CellA) &&
                   CellB.Equals(other.CellB);
        }

        public override bool Equals(object obj)
        {
            return obj is GridBoundarySegment other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (CellA.GetHashCode() * 397) ^
                    CellB.GetHashCode();
            }
        }

        public static bool operator ==(
            GridBoundarySegment left,
            GridBoundarySegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            GridBoundarySegment left,
            GridBoundarySegment right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{CellA} <-> {CellB}";
        }
    }
}