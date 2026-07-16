using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Power
{
    public sealed class ModulePowerAllocation
    {
        public Guid ModuleId { get; }

        public ModulePowerMode RequestedMode { get; }

        public ModulePowerMode EffectiveMode { get; }

        public PowerPriority Priority { get; }

        public int RequestedDemand { get; }

        public int GrantedDemand { get; }

        public bool WasReduced =>
            EffectiveMode != RequestedMode;

        public ModulePowerAllocation(
            Guid moduleId,
            ModulePowerMode requestedMode,
            ModulePowerMode effectiveMode,
            PowerPriority priority,
            int requestedDemand,
            int grantedDemand)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            if (requestedDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedDemand));
            }

            if (grantedDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grantedDemand));
            }

            ModuleId = moduleId;
            RequestedMode = requestedMode;
            EffectiveMode = effectiveMode;
            Priority = priority;
            RequestedDemand = requestedDemand;
            GrantedDemand = grantedDemand;
        }
    }
}