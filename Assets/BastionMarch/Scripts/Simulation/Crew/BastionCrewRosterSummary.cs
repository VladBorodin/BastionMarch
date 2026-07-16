using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Crew
{
    public sealed class BastionCrewRosterSummary
    {
        public int ActiveBrigadeCount { get; }

        public int TotalPersonnel { get; }

        public IReadOnlyList<BrigadeTypePersonnelSummary> ByType
        {
            get;
        }

        public BastionCrewRosterSummary(
            IEnumerable<BrigadeTypePersonnelSummary> byType)
        {
            List<BrigadeTypePersonnelSummary> items =
                byType.ToList();

            ByType =
                new ReadOnlyCollection<BrigadeTypePersonnelSummary>(
                    items);

            ActiveBrigadeCount =
                items.Sum(item => item.BrigadeCount);

            TotalPersonnel =
                items.Sum(item => item.Personnel);
        }
    }
}