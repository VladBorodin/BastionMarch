using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Проектная потребность всей конструкции в персонале.
    /// </summary>
    public sealed class BastionCrewRequirements
    {
        private readonly Dictionary<WorkType, WorkRequirementSummary>
            _byWorkType;

        public IReadOnlyList<WorkRequirementSummary> ByWorkType
        {
            get;
        }

        public int MinimumPersonnel =>
            ByWorkType.Sum(item => item.MinimumPersonnel);

        public int OptimalPersonnel =>
            ByWorkType.Sum(item => item.OptimalPersonnel);

        public int MaximumUsefulPersonnel =>
            ByWorkType.Sum(item => item.MaximumUsefulPersonnel);

        public BastionCrewRequirements(
            IEnumerable<WorkRequirementSummary> requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(
                    nameof(requirements));
            }

            List<WorkRequirementSummary> items =
                requirements.ToList();

            _byWorkType =
                items.ToDictionary(
                    item => item.WorkType);

            ByWorkType =
                new ReadOnlyCollection<WorkRequirementSummary>(
                    items);
        }

        public WorkRequirementSummary Get(
            WorkType workType)
        {
            if (_byWorkType.TryGetValue(
                    workType,
                    out WorkRequirementSummary requirement))
            {
                return requirement;
            }

            return new WorkRequirementSummary(
                workType,
                minimumPersonnel: 0,
                optimalPersonnel: 0,
                maximumUsefulPersonnel: 0);
        }
    }
}