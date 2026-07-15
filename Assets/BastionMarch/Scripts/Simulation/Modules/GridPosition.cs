namespace BastionMarch.Simulation.Modules
{
    public readonly struct GridPosition
    {
        public int X { get; }
        public int Deck { get; }

        public GridPosition(int x, int deck)
        {
            X = x;
            Deck = deck;
        }
    }
}