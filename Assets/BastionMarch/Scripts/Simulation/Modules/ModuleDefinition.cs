using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Modules
{
    public sealed class ModuleDefinition
    {
        public string Id { get; }
        public string Name { get; }

        public ModuleCategory Category { get; }
        public ModuleType Type { get; }

        public GridSize Size { get; }

        public int MassKg { get; }
        public int Cost { get; }

        public int MaxDurability { get; }
        public int DamagedDurabilityThreshold { get; }
        public int CriticalDurabilityThreshold { get; }

        public int IdlePowerConsumption { get; }
        public int ActivePowerConsumption { get; }
        public int HeatGeneration { get; }

        public CrewRequirement CrewRequirement { get; }

        public IReadOnlyList<IModuleFeatureDefinition> Features { get; }

        public ModuleDefinition(
            string id,
            string name,
            ModuleCategory category,
            ModuleType type,
            GridSize size,
            int massKg,
            int cost,
            int maxDurability,
            int damagedDurabilityThreshold,
            int criticalDurabilityThreshold,
            int idlePowerConsumption,
            int activePowerConsumption,
            int heatGeneration,
            CrewRequirement crewRequirement,
            IEnumerable<IModuleFeatureDefinition> features)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Module name cannot be empty.",
                    nameof(name));
            }

            if (massKg < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(massKg));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            if (maxDurability <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDurability));
            }

            if (criticalDurabilityThreshold < 0 ||
                criticalDurabilityThreshold > damagedDurabilityThreshold)
            {
                throw new ArgumentException(
                    "Critical threshold must be between zero and damaged threshold.");
            }

            if (damagedDurabilityThreshold > maxDurability)
            {
                throw new ArgumentException(
                    "Damaged threshold cannot exceed maximum durability.");
            }

            if (idlePowerConsumption < 0 ||
                activePowerConsumption < idlePowerConsumption)
            {
                throw new ArgumentException(
                    "Active power consumption cannot be lower than idle consumption.");
            }

            if (heatGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heatGeneration));
            }

            Id = id;
            Name = name;
            Category = category;
            Type = type;
            Size = size;
            MassKg = massKg;
            Cost = cost;

            MaxDurability = maxDurability;
            DamagedDurabilityThreshold = damagedDurabilityThreshold;
            CriticalDurabilityThreshold = criticalDurabilityThreshold;

            IdlePowerConsumption = idlePowerConsumption;
            ActivePowerConsumption = activePowerConsumption;
            HeatGeneration = heatGeneration;

            CrewRequirement = crewRequirement ??
                throw new ArgumentNullException(nameof(crewRequirement));

            var featureList = features == null
                ? new List<IModuleFeatureDefinition>()
                : features.ToList();

            Features = new ReadOnlyCollection<IModuleFeatureDefinition>(
                featureList);
        }

        public bool TryGetFeature<TFeature>(out TFeature feature)
            where TFeature : class, IModuleFeatureDefinition
        {
            feature = Features.OfType<TFeature>().FirstOrDefault();
            return feature != null;
        }
<<<<<<< HEAD
=======

        public IReadOnlyList<TFeature> GetFeatures<TFeature>()
            where TFeature : class, IModuleFeatureDefinition
        {
            return Features
                .OfType<TFeature>()
                .ToArray();
        }
>>>>>>> 4949ad8 (feat: add initial module features and catalog)
    }
}