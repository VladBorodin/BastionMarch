using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Crew
{
    public sealed class BrigadeWorkProfileCatalog
    {
        private readonly Dictionary<
            BrigadeType,
            BrigadeWorkProfile> _profilesByType;

        public IReadOnlyList<BrigadeWorkProfile> All { get; }

        public BrigadeWorkProfileCatalog(
            IEnumerable<BrigadeWorkProfile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(
                    nameof(profiles));
            }

            List<BrigadeWorkProfile> items =
                profiles.ToList();

            if (items.Any(item => item == null))
            {
                throw new ArgumentException(
                    "Profile list cannot contain null values.",
                    nameof(profiles));
            }

            BrigadeType? duplicateType =
                items
                    .GroupBy(item => item.BrigadeType)
                    .Where(group => group.Count() > 1)
                    .Select(group => (BrigadeType?)group.Key)
                    .FirstOrDefault();

            if (duplicateType.HasValue)
            {
                throw new ArgumentException(
                    $"Duplicate brigade profile: {duplicateType.Value}.",
                    nameof(profiles));
            }

            _profilesByType =
                items.ToDictionary(
                    item => item.BrigadeType);

            All =
                new ReadOnlyCollection<BrigadeWorkProfile>(
                    items);
        }

        public BrigadeWorkProfile GetRequired(
            BrigadeType brigadeType)
        {
            if (!_profilesByType.TryGetValue(
                    brigadeType,
                    out BrigadeWorkProfile profile))
            {
                throw new KeyNotFoundException(
                    $"Work profile for brigade type " +
                    $"'{brigadeType}' was not found.");
            }

            return profile;
        }
    }
}