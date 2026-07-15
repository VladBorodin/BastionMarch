using System;

namespace BastionMarch.Simulation.Modules
{
    public sealed class CrewRequirement
    {
        public int MinimumPersonnel { get; }
        public int OptimalPersonnel { get; }
        public int MaximumPersonnel { get; }

        public CrewRequirement(
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumPersonnel)
        {
            if (minimumPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPersonnel));
            }

            if (optimalPersonnel < minimumPersonnel)
            {
                throw new ArgumentException(
                    "Optimal personnel cannot be lower than minimum personnel.");
            }

            if (maximumPersonnel < optimalPersonnel)
            {
                throw new ArgumentException(
                    "Maximum personnel cannot be lower than optimal personnel.");
            }

            MinimumPersonnel = minimumPersonnel;
            OptimalPersonnel = optimalPersonnel;
            MaximumPersonnel = maximumPersonnel;
        }
    }
}