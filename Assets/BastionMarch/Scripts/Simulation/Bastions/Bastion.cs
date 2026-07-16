using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Features;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Главная доменная сущность боевой машины.
    ///
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
    }
}