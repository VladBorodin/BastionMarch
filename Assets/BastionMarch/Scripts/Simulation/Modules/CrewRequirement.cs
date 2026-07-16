using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Crew;

namespace BastionMarch.Simulation.Modules
{
    /// <summary>
    /// Совокупные требования модуля к обслуживающему персоналу.
    ///
    /// Общие значения вычисляются из требований по видам работ.
    /// </summary>
    public sealed class CrewRequirement
    {
        public IReadOnlyList<ModuleWorkRequirement> WorkRequirements
        {
            get;
        }

        public int MinimumPersonnel =>
            WorkRequirements.Sum(
                requirement => requirement.MinimumPersonnel);

        public int OptimalPersonnel =>
            WorkRequirements.Sum(
                requirement => requirement.OptimalPersonnel);

        public int MaximumUsefulPersonnel =>
            WorkRequirements.Sum(
                requirement => requirement.MaximumUsefulPersonnel);

        /// <summary>
        /// Совместимый конструктор для модулей,
        /// у которых вид работы пока не уточнён.
        /// </summary>
        public CrewRequirement(
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumUsefulPersonnel)
            : this(
                CreateGeneralRequirement(
                    minimumPersonnel,
                    optimalPersonnel,
                    maximumUsefulPersonnel))
        {
        }

        public CrewRequirement(
            IEnumerable<ModuleWorkRequirement> workRequirements)
        {
            if (workRequirements == null)
            {
                throw new ArgumentNullException(
                    nameof(workRequirements));
            }

            List<ModuleWorkRequirement> requirements =
                workRequirements.ToList();

            if (requirements.Any(requirement => requirement == null))
            {
                throw new ArgumentException(
                    "Work requirements cannot contain null values.",
                    nameof(workRequirements));
            }

            WorkType duplicateWorkType =
                requirements
                    .GroupBy(requirement => requirement.WorkType)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .FirstOrDefault();

            bool hasDuplicate =
                requirements
                    .GroupBy(requirement => requirement.WorkType)
                    .Any(group => group.Count() > 1);

            if (hasDuplicate)
            {
                throw new ArgumentException(
                    $"Duplicate work requirement: {duplicateWorkType}.",
                    nameof(workRequirements));
            }

            WorkRequirements =
                new ReadOnlyCollection<ModuleWorkRequirement>(
                    requirements);
        }

        public bool TryGetRequirement(
            WorkType workType,
            out ModuleWorkRequirement requirement)
        {
            requirement =
                WorkRequirements.FirstOrDefault(
                    item => item.WorkType == workType);

            return requirement != null;
        }

        private static IEnumerable<ModuleWorkRequirement>
            CreateGeneralRequirement(
                int minimumPersonnel,
                int optimalPersonnel,
                int maximumUsefulPersonnel)
        {
            if (minimumPersonnel == 0 &&
                optimalPersonnel == 0 &&
                maximumUsefulPersonnel == 0)
            {
                return Array.Empty<ModuleWorkRequirement>();
            }

            return new[]
            {
                new ModuleWorkRequirement(
                    WorkType.General,
                    minimumPersonnel,
                    optimalPersonnel,
                    maximumUsefulPersonnel)
            };
        }
    }
}