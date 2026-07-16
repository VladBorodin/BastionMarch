using System;

namespace BastionMarch.Simulation.Modules
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Deck { get; }

        public GridPosition(int x, int deck)
        {
            X = x;
            Deck = deck;
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X &&
                   Deck == other.Deck;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Deck;
            }
        }

        public static bool operator ==(
            GridPosition left,
            GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            GridPosition left,
            GridPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, deck {Deck})";
        }
    }
}