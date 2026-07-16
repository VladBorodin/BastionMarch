using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Features;
using BastionMarch.Simulation.Power;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Главная доменная сущность боевой машины.
    /// Bastion владеет сеткой и предоставляет операции над установленными
    /// модулями. Внешний код не должен самостоятельно поддерживать
    /// согласованность сетки и характеристик машины.
    /// </summary>
    public sealed class Bastion
    {
        private readonly BastionGrid _grid;

        public Guid Id { get; }

        public string Name { get; }

        public int Width => _grid.Width;

        public int DeckCount => _grid.DeckCount;

        public int ModuleCount => _grid.ModuleCount;

        public IReadOnlyCollection<ModuleInstance> Modules =>
            _grid.Modules;

        public Bastion(
            string name,
            int width,
            int deckCount)
            : this(
                Guid.NewGuid(),
                name,
                width,
                deckCount)
        {
        }

        public Bastion(
            Guid id,
            string name,
            int width,
            int deckCount)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Bastion id cannot be empty.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Bastion name cannot be empty.",
                    nameof(name));
            }

            Id = id;
            Name = name;
            _grid = new BastionGrid(width, deckCount);
        }

        public ModulePlacementResult TryInstallModule(
            ModuleDefinition definition,
            GridPosition origin)
        {
            return _grid.TryPlaceModule(
                definition,
                origin);
        }

        public bool TryRemoveModule(
            Guid moduleId,
            out ModuleInstance removedModule)
        {
            return _grid.TryRemoveModule(
                moduleId,
                out removedModule);
        }

        public bool TryGetModuleAt(
            GridPosition position,
            out ModuleInstance module)
        {
            return _grid.TryGetModuleAt(
                position,
                out module);
        }

        public bool TryGetModule(
            Guid moduleId,
            out ModuleInstance module)
        {
            return _grid.TryGetModule(
                moduleId,
                out module);
        }

        public bool IsInsideGrid(GridPosition position)
        {
            return _grid.IsInsideGrid(position);
        }

        public BastionDesignStatistics CalculateDesignStatistics()
        {
            int occupiedCellCount = 0;

            long totalMassKg = 0;
            long totalCost = 0;
            long totalMaxDurability = 0;

            long totalIdlePowerConsumption = 0;
            long totalActivePowerConsumption = 0;
            long totalHeatGeneration = 0;

            int minimumPersonnel = 0;
            int optimalPersonnel = 0;
            int maximumPersonnel = 0;

            long totalHorsePower = 0;

            foreach (ModuleInstance module in Modules)
            {
                ModuleDefinition definition =
                    module.Definition;

                occupiedCellCount +=
                    definition.Size.CellCount;

                totalMassKg +=
                    definition.MassKg;

                totalCost +=
                    definition.Cost;

                totalMaxDurability +=
                    definition.MaxDurability;

                totalIdlePowerConsumption +=
                    definition.IdlePowerConsumption;

                totalActivePowerConsumption +=
                    definition.ActivePowerConsumption;

                totalHeatGeneration +=
                    definition.HeatGeneration;

                minimumPersonnel +=
                    definition.CrewRequirement.MinimumPersonnel;

                optimalPersonnel +=
                    definition.CrewRequirement.OptimalPersonnel;

                maximumPersonnel +=
                    definition.CrewRequirement.MaximumPersonnel;

                IReadOnlyList<PropulsionFeatureDefinition>
                    propulsionFeatures =
                        definition.GetFeatures<
                            PropulsionFeatureDefinition>();

                foreach (
                    PropulsionFeatureDefinition propulsion
                    in propulsionFeatures)
                {
                    totalHorsePower +=
                        propulsion.HorsePower;
                }
            }

            return new BastionDesignStatistics(
                moduleCount: ModuleCount,
                occupiedCellCount: occupiedCellCount,
                totalMassKg: totalMassKg,
                totalCost: totalCost,
                totalMaxDurability: totalMaxDurability,
                totalIdlePowerConsumption:
                    totalIdlePowerConsumption,
                totalActivePowerConsumption:
                    totalActivePowerConsumption,
                totalHeatGeneration: totalHeatGeneration,
                minimumPersonnel: minimumPersonnel,
                optimalPersonnel: optimalPersonnel,
                maximumPersonnel: maximumPersonnel,
                totalHorsePower: totalHorsePower);
        }

        public BastionPowerBalance CalculateDesignPowerBalance()
        {
            long totalPowerGeneration = 0;
            long totalIdlePowerDemand = 0;
            long totalActivePowerDemand = 0;

            foreach (ModuleInstance module in Modules)
            {
                ModuleDefinition definition =
                    module.Definition;

                totalIdlePowerDemand +=
                    definition.IdlePowerConsumption;

                totalActivePowerDemand +=
                    definition.ActivePowerConsumption;

                IReadOnlyList<PowerGenerationFeatureDefinition>
                    generationFeatures =
                        definition.GetFeatures<
                            PowerGenerationFeatureDefinition>();

                foreach (
                    PowerGenerationFeatureDefinition generation
                    in generationFeatures)
                {
                    totalPowerGeneration +=
                        generation.MaximumPowerOutput;
                }
            }

            return new BastionPowerBalance(
                totalPowerGeneration:
                    totalPowerGeneration,
                totalIdlePowerDemand:
                    totalIdlePowerDemand,
                totalActivePowerDemand:
                    totalActivePowerDemand);
        }

        public BastionOperationalPowerBalance
            CalculateOperationalPowerBalance()
        {
            long availablePowerGeneration = 0;
            long currentPowerDemand = 0;

            foreach (ModuleInstance module in Modules)
            {
                if (!IsAvailableForPowerOperation(module))
                {
                    continue;
                }

                currentPowerDemand +=
                    module.CurrentContinuousPowerDemand;

                if (module.PowerMode != ModulePowerMode.Active)
                {
                    continue;
                }

                IReadOnlyList<PowerGenerationFeatureDefinition>
                    generationFeatures =
                        module.Definition.GetFeatures<
                            PowerGenerationFeatureDefinition>();

                foreach (
                    PowerGenerationFeatureDefinition generation
                    in generationFeatures)
                {
                    availablePowerGeneration +=
                        generation.MaximumPowerOutput;
                }
            }

            return new BastionOperationalPowerBalance(
                availablePowerGeneration:
                    availablePowerGeneration,
                currentPowerDemand:
                    currentPowerDemand);
        }

        private static bool IsAvailableForPowerOperation(
            ModuleInstance module)
        {
            return module.TechnicalState !=
                    ModuleTechnicalState.Destroyed &&
                module.ControlState ==
                    ModuleControlState.Friendly;
        }

        public BastionPowerDistributionResult ResolvePowerDistribution()
        {
            var allocations =
                new List<ModulePowerAllocation>();

            List<ModuleInstance> availableModules =
                Modules
                    .Where(IsAvailableForPowerOperation)
                    .ToList();

            // Недоступные модули физически не могут получать питание.
            foreach (ModuleInstance module in Modules)
            {
                if (!IsAvailableForPowerOperation(module))
                {
                    module.ApplyPowerAllocation(
                        ModulePowerMode.Offline);
                }
            }

            long grossPowerGeneration = 0;
            long totalRequestedDemand = 0;
            long totalGrantedDemand = 0;

            var activeGeneratorIds =
                new HashSet<Guid>();

            foreach (ModuleInstance module in availableModules)
            {
                totalRequestedDemand +=
                    module.RequestedContinuousPowerDemand;

                if (module.RequestedPowerMode ==
                    ModulePowerMode.Offline)
                {
                    module.ApplyPowerAllocation(
                        ModulePowerMode.Offline);

                    allocations.Add(
                        CreatePowerAllocation(module));

                    continue;
                }

                IReadOnlyList<PowerGenerationFeatureDefinition>
                    generationFeatures =
                        module.Definition.GetFeatures<
                            PowerGenerationFeatureDefinition>();

                bool isActiveGenerator =
                    module.RequestedPowerMode ==
                        ModulePowerMode.Active &&
                    generationFeatures.Count > 0;

                if (!isActiveGenerator)
                {
                    continue;
                }

                module.ApplyPowerAllocation(
                    ModulePowerMode.Active);

                activeGeneratorIds.Add(module.Id);

                foreach (
                    PowerGenerationFeatureDefinition generation
                    in generationFeatures)
                {
                    grossPowerGeneration +=
                        generation.MaximumPowerOutput;
                }

                totalGrantedDemand +=
                    module.CurrentContinuousPowerDemand;

                allocations.Add(
                    CreatePowerAllocation(module));
            }

            long remainingPower =
                grossPowerGeneration - totalGrantedDemand;

            IEnumerable<ModuleInstance> consumers =
                availableModules
                    .Where(module =>
                        module.RequestedPowerMode !=
                            ModulePowerMode.Offline &&
                        !activeGeneratorIds.Contains(module.Id))
                    .OrderBy(module => module.PowerPriority)
                    .ThenBy(module => module.Id);

            foreach (ModuleInstance module in consumers)
            {
                ModulePowerMode grantedMode =
                    ResolveGrantedPowerMode(
                        module,
                        remainingPower);

                module.ApplyPowerAllocation(grantedMode);

                int grantedDemand =
                    module.CurrentContinuousPowerDemand;

                totalGrantedDemand += grantedDemand;
                remainingPower -= grantedDemand;

                allocations.Add(
                    CreatePowerAllocation(module));
            }

            return new BastionPowerDistributionResult(
                grossPowerGeneration:
                    grossPowerGeneration,
                totalRequestedDemand:
                    totalRequestedDemand,
                totalGrantedDemand:
                    totalGrantedDemand,
                allocations:
                    allocations);
        }

        private static ModulePowerMode ResolveGrantedPowerMode(
            ModuleInstance module,
            long remainingPower)
        {
            int requestedDemand =
                module.RequestedContinuousPowerDemand;

            // Орудия и другие механические системы с нулевым
            // потреблением могут работать без электроснабжения.
            if (requestedDemand == 0)
            {
                return module.RequestedPowerMode;
            }

            if (remainingPower >= requestedDemand)
            {
                return module.RequestedPowerMode;
            }

            if (module.RequestedPowerMode ==
                ModulePowerMode.Active)
            {
                int standbyDemand =
                    module.Definition.IdlePowerConsumption;

                if (standbyDemand == 0 ||
                    remainingPower >= standbyDemand)
                {
                    return ModulePowerMode.Standby;
                }
            }

            return ModulePowerMode.Offline;
        }

        private static ModulePowerAllocation CreatePowerAllocation(
            ModuleInstance module)
        {
            return new ModulePowerAllocation(
                moduleId: module.Id,
                requestedMode: module.RequestedPowerMode,
                effectiveMode: module.EffectivePowerMode,
                priority: module.PowerPriority,
                requestedDemand:
                    module.RequestedContinuousPowerDemand,
                grantedDemand:
                    module.CurrentContinuousPowerDemand);
        }
    }
}