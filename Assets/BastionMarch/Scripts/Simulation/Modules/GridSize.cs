using System;

namespace BastionMarch.Simulation.Modules
{
    public readonly struct GridSize
    {
        public int Width { get; }
        public int Height { get; }

        public int CellCount => checked(Width * Height);

        public GridSize(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Module width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    "Module height must be greater than zero.");
            }

            Width = width;
            Height = height;
        }
    }
}