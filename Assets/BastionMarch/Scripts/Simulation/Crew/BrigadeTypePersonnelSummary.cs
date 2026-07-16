namespace BastionMarch.Simulation.Crew
{
    public sealed class BrigadeTypePersonnelSummary
    {
        public BrigadeType BrigadeType { get; }

        public int BrigadeCount { get; }

        public int Personnel { get; }

        public BrigadeTypePersonnelSummary(
            BrigadeType brigadeType,
            int brigadeCount,
            int personnel)
        {
            BrigadeType = brigadeType;
            BrigadeCount = brigadeCount;
            Personnel = personnel;
        }
    }
}