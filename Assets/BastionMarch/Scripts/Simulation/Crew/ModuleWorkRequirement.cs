using System;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Потребность одного модуля в людях для конкретного вида работы.
    /// </summary>
    public sealed class ModuleWorkRequirement
    {
        public WorkType WorkType { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        public ModuleWorkRequirement(
            WorkType workType,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumUsefulPersonnel)
        {
            if (!Enum.IsDefined(typeof(WorkType), workType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workType));
            }

            if (minimumPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPersonnel));
            }

            if (optimalPersonnel < minimumPersonnel)
            {
                throw new ArgumentException(
                    "Optimal personnel cannot be lower than minimum personnel.",
                    nameof(optimalPersonnel));
            }

            if (maximumUsefulPersonnel < optimalPersonnel)
            {
                throw new ArgumentException(
                    "Maximum useful personnel cannot be lower than optimal personnel.",
                    nameof(maximumUsefulPersonnel));
            }

            WorkType = workType;
            MinimumPersonnel = minimumPersonnel;
            OptimalPersonnel = optimalPersonnel;
            MaximumUsefulPersonnel = maximumUsefulPersonnel;
        }
    }
}