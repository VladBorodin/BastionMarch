using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Описывает пригодность одного BrigadeType
    /// к различным WorkType.
    /// </summary>
    public sealed class BrigadeWorkProfile
    {
        private readonly Dictionary<WorkType, int>
            _efficiencyByWorkType;

        public BrigadeType BrigadeType { get; }

        /// <summary>
        /// Используется для работ, которые не были
        /// явно перечислены в профиле.
        /// </summary>
        public int DefaultEfficiencyPercent { get; }

        public IReadOnlyList<WorkAffinityDefinition> Affinities
        {
            get;
        }

        public BrigadeWorkProfile(
            BrigadeType brigadeType,
            int defaultEfficiencyPercent,
            IEnumerable<WorkAffinityDefinition> affinities)
        {
            if (!Enum.IsDefined(
                    typeof(BrigadeType),
                    brigadeType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(brigadeType));
            }

            if (defaultEfficiencyPercent < 0 ||
                defaultEfficiencyPercent > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(defaultEfficiencyPercent));
            }

            if (affinities == null)
            {
                throw new ArgumentNullException(
                    nameof(affinities));
            }

            List<WorkAffinityDefinition> items =
                affinities.ToList();

            if (items.Any(item => item == null))
            {
                throw new ArgumentException(
                    "Affinity list cannot contain null values.",
                    nameof(affinities));
            }

            WorkType? duplicateWorkType =
                items
                    .GroupBy(item => item.WorkType)
                    .Where(group => group.Count() > 1)
                    .Select(group => (WorkType?)group.Key)
                    .FirstOrDefault();

            if (duplicateWorkType.HasValue)
            {
                throw new ArgumentException(
                    $"Duplicate work affinity: {duplicateWorkType.Value}.",
                    nameof(affinities));
            }

            BrigadeType = brigadeType;

            DefaultEfficiencyPercent =
                defaultEfficiencyPercent;

            _efficiencyByWorkType =
                items.ToDictionary(
                    item => item.WorkType,
                    item => item.EfficiencyPercent);

            Affinities =
                new ReadOnlyCollection<WorkAffinityDefinition>(
                    items);
        }

        public int GetEfficiencyPercent(
            WorkType workType)
        {
            if (!Enum.IsDefined(typeof(WorkType), workType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workType));
            }

            return _efficiencyByWorkType.TryGetValue(
                workType,
                out int efficiency)
                    ? efficiency
                    : DefaultEfficiencyPercent;
        }
    }
}