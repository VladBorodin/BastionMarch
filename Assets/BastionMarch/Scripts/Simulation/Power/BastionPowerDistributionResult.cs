using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Power
{
    public sealed class BastionPowerDistributionResult
    {
        public long GrossPowerGeneration { get; }

        public long TotalRequestedDemand { get; }

        public long TotalGrantedDemand { get; }

        public long PowerReserve =>
            GrossPowerGeneration - TotalGrantedDemand;

        public long UnservedDemand =>
            TotalRequestedDemand - TotalGrantedDemand;

        public bool HasLoadShedding =>
            Allocations.Any(allocation => allocation.WasReduced);

        public IReadOnlyList<ModulePowerAllocation> Allocations { get; }

        public BastionPowerDistributionResult(
            long grossPowerGeneration,
            long totalRequestedDemand,
            long totalGrantedDemand,
            IEnumerable<ModulePowerAllocation> allocations)
        {
            if (grossPowerGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grossPowerGeneration));
            }

            if (totalRequestedDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalRequestedDemand));
            }

            if (totalGrantedDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalGrantedDemand));
            }

            GrossPowerGeneration = grossPowerGeneration;
            TotalRequestedDemand = totalRequestedDemand;
            TotalGrantedDemand = totalGrantedDemand;

            Allocations =
                new ReadOnlyCollection<ModulePowerAllocation>(
                    (allocations ??
                     throw new ArgumentNullException(nameof(allocations)))
                    .ToList());
        }
    }
}