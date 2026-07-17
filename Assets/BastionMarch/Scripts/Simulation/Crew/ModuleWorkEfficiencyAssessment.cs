using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Crew
{
    public sealed class ModuleWorkEfficiencyAssessment
    {
        private readonly Dictionary<
            WorkType,
            ModuleWorkTypeAssessment> _byWorkType;

        public Guid ModuleId { get; }

        public int WorkingBrigadeCount { get; }

        public int TotalWorkingPersonnel { get; }

        public int TotalOccupyingPersonnel { get; }

        public double OvercrowdingMultiplier { get; }

        public double OverallEfficiencyRatio { get; }

        public int OverallEfficiencyPercent =>
            (int)Math.Round(
                OverallEfficiencyRatio * 100);

        public bool CanOperate { get; }

        public IReadOnlyList<ModuleWorkTypeAssessment>
            ByWorkType { get; }

        public ModuleWorkEfficiencyAssessment(
            Guid moduleId,
            int workingBrigadeCount,
            int totalWorkingPersonnel,
            int totalOccupyingPersonnel,
            double overcrowdingMultiplier,
            double overallEfficiencyRatio,
            IEnumerable<ModuleWorkTypeAssessment> byWorkType)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            ModuleId = moduleId;
            WorkingBrigadeCount = workingBrigadeCount;
            TotalWorkingPersonnel = totalWorkingPersonnel;
            TotalOccupyingPersonnel = totalOccupyingPersonnel;

            OvercrowdingMultiplier =
                overcrowdingMultiplier;

            OverallEfficiencyRatio =
                overallEfficiencyRatio;

            List<ModuleWorkTypeAssessment> items =
                byWorkType.ToList();

            ByWorkType =
                new ReadOnlyCollection<ModuleWorkTypeAssessment>(
                    items);

            _byWorkType =
                items.ToDictionary(
                    item => item.WorkType);

            CanOperate =
                items.All(item => item.IsMinimumMet);
        }

        public ModuleWorkTypeAssessment Get(
            WorkType workType)
        {
            if (_byWorkType.TryGetValue(
                    workType,
                    out ModuleWorkTypeAssessment assessment))
            {
                return assessment;
            }

            throw new KeyNotFoundException(
                $"Work type '{workType}' is not required " +
                $"by module '{ModuleId}'.");
        }
    }
}