using System;

namespace BastionMarch.Simulation.Crew
{
    public sealed class ModuleStaffingAssessment
    {
        public Guid ModuleId { get; }

        public int AssignedBrigadeCount { get; }

        public int TotalPersonnel { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumPersonnel { get; }

        public int AverageExperience { get; }

        public int AverageMorale { get; }

        public int AverageFatigue { get; }

        public ModuleStaffingState State { get; }

        public bool IsMinimumMet =>
            MinimumPersonnel == 0 ||
            TotalPersonnel >= MinimumPersonnel;

        public bool IsOvercrowded =>
            MaximumPersonnel > 0 &&
            TotalPersonnel > MaximumPersonnel;

        public ModuleStaffingAssessment(
            Guid moduleId,
            int assignedBrigadeCount,
            int totalPersonnel,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumPersonnel,
            int averageExperience,
            int averageMorale,
            int averageFatigue)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            if (assignedBrigadeCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(assignedBrigadeCount));
            }

            if (totalPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalPersonnel));
            }

            if (minimumPersonnel < 0 ||
                optimalPersonnel < minimumPersonnel ||
                maximumPersonnel < optimalPersonnel)
            {
                throw new ArgumentException(
                    "Invalid module personnel requirements.");
            }

            ModuleId = moduleId;
            AssignedBrigadeCount = assignedBrigadeCount;
            TotalPersonnel = totalPersonnel;

            MinimumPersonnel = minimumPersonnel;
            OptimalPersonnel = optimalPersonnel;
            MaximumPersonnel = maximumPersonnel;

            AverageExperience = averageExperience;
            AverageMorale = averageMorale;
            AverageFatigue = averageFatigue;

            State = ResolveState(
                totalPersonnel,
                minimumPersonnel,
                optimalPersonnel,
                maximumPersonnel);
        }

        private static ModuleStaffingState ResolveState(
            int totalPersonnel,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumPersonnel)
        {
            if (maximumPersonnel == 0)
            {
                return ModuleStaffingState.NotRequired;
            }

            if (totalPersonnel == 0)
            {
                return ModuleStaffingState.Unstaffed;
            }

            if (totalPersonnel < minimumPersonnel)
            {
                return ModuleStaffingState.BelowMinimum;
            }

            if (totalPersonnel < optimalPersonnel)
            {
                return ModuleStaffingState.Functional;
            }

            if (totalPersonnel == optimalPersonnel)
            {
                return ModuleStaffingState.Optimal;
            }

            if (totalPersonnel <= maximumPersonnel)
            {
                return ModuleStaffingState.AboveOptimal;
            }

            return ModuleStaffingState.Overcrowded;
        }
    }
}