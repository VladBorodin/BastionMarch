using System;

namespace BastionMarch.Simulation.Crew
{
    public sealed class ModuleStaffingAssessment
    {
        public Guid ModuleId { get; }

        public int OccupyingBrigadeCount { get; }

        public int WorkingBrigadeCount { get; }

        public int TotalOccupyingPersonnel { get; }

        public int TotalWorkingPersonnel { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        public int AverageExperience { get; }

        public int AverageMorale { get; }

        public int AverageFatigue { get; }

        public ModuleStaffingState State { get; }

        public bool IsMinimumMet =>
            MinimumPersonnel == 0 ||
            TotalWorkingPersonnel >= MinimumPersonnel;

        public bool IsOvercrowded =>
            MaximumUsefulPersonnel > 0 &&
            TotalOccupyingPersonnel >
                MaximumUsefulPersonnel;

        public ModuleStaffingAssessment(
            Guid moduleId,
            int occupyingBrigadeCount,
            int workingBrigadeCount,
            int totalOccupyingPersonnel,
            int totalWorkingPersonnel,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumUsefulPersonnel,
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

            if (occupyingBrigadeCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occupyingBrigadeCount));
            }

            if (workingBrigadeCount < 0 ||
                workingBrigadeCount >
                    occupyingBrigadeCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workingBrigadeCount));
            }

            if (totalOccupyingPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalOccupyingPersonnel));
            }

            if (totalWorkingPersonnel < 0 ||
                totalWorkingPersonnel >
                    totalOccupyingPersonnel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalWorkingPersonnel));
            }

            if (minimumPersonnel < 0 ||
                optimalPersonnel < minimumPersonnel ||
                maximumUsefulPersonnel < optimalPersonnel)
            {
                throw new ArgumentException(
                    "Invalid module personnel requirements.");
            }

            ModuleId = moduleId;

            OccupyingBrigadeCount =
                occupyingBrigadeCount;

            WorkingBrigadeCount =
                workingBrigadeCount;

            TotalOccupyingPersonnel =
                totalOccupyingPersonnel;

            TotalWorkingPersonnel =
                totalWorkingPersonnel;

            MinimumPersonnel =
                minimumPersonnel;

            OptimalPersonnel =
                optimalPersonnel;

            MaximumUsefulPersonnel =
                maximumUsefulPersonnel;

            AverageExperience =
                averageExperience;

            AverageMorale =
                averageMorale;

            AverageFatigue =
                averageFatigue;

            State = ResolveState(
                totalWorkingPersonnel,
                minimumPersonnel,
                optimalPersonnel);
        }

        private static ModuleStaffingState ResolveState(
            int totalWorkingPersonnel,
            int minimumPersonnel,
            int optimalPersonnel)
        {
            if (minimumPersonnel == 0 &&
                optimalPersonnel == 0)
            {
                return ModuleStaffingState.NotRequired;
            }

            if (totalWorkingPersonnel == 0)
            {
                return ModuleStaffingState.Unstaffed;
            }

            if (totalWorkingPersonnel <
                minimumPersonnel)
            {
                return ModuleStaffingState.BelowMinimum;
            }

            if (totalWorkingPersonnel <
                optimalPersonnel)
            {
                return ModuleStaffingState.Functional;
            }

            if (totalWorkingPersonnel ==
                optimalPersonnel)
            {
                return ModuleStaffingState.Optimal;
            }

            return ModuleStaffingState.AboveOptimal;
        }
    }
}