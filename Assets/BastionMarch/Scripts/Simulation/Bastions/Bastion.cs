using System;
using System.Linq;
using System.Collections.Generic;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Features;
using BastionMarch.Simulation.Power;
using BastionMarch.Simulation.Crew;

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

        private readonly Dictionary<Guid, Brigade> _brigadesById =
            new();

        private readonly Dictionary<Guid, Guid> _moduleIdByBrigadeId =
            new();

        public int BrigadeCount =>
            _brigadesById.Count;

        public IReadOnlyCollection<Brigade> Brigades =>
            _brigadesById.Values.ToArray();

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
            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance existingModule))
            {
                removedModule = null;
                return false;
            }

            Guid[] assignedBrigadeIds =
                existingModule.AssignedBrigadeIds.ToArray();

            foreach (Guid brigadeId in assignedBrigadeIds)
            {
                existingModule.RemoveBrigade(brigadeId);
                _moduleIdByBrigadeId.Remove(brigadeId);
            }

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
            int maximumUsefulPersonnel = 0;

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

                maximumUsefulPersonnel +=
                    definition.CrewRequirement.MaximumUsefulPersonnel;

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
                maximumUsefulPersonnel: maximumUsefulPersonnel,
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

        public bool TryAddBrigade(Brigade brigade)
        {
            if (brigade == null)
            {
                throw new ArgumentNullException(nameof(brigade));
            }

            if (brigade.IsDisbanded)
            {
                return false;
            }

            if (_brigadesById.ContainsKey(brigade.Id))
            {
                return false;
            }

            _brigadesById.Add(
                brigade.Id,
                brigade);

            return true;
        }

        public bool TryGetBrigade(
            Guid brigadeId,
            out Brigade brigade)
        {
            return _brigadesById.TryGetValue(
                brigadeId,
                out brigade);
        }

        public bool TryRemoveBrigade(
            Guid brigadeId,
            out Brigade removedBrigade)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out removedBrigade))
            {
                return false;
            }

            TryUnassignBrigade(
                brigadeId,
                out _);

            _brigadesById.Remove(brigadeId);

            return true;
        }

        public BrigadeAssignmentResult TryAssignBrigadeToModule(
            Guid brigadeId,
            Guid moduleId)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out Brigade brigade))
            {
                return BrigadeAssignmentResult.Failure(
                    BrigadeAssignmentFailureReason.BrigadeNotFound);
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                return BrigadeAssignmentResult.Failure(
                    BrigadeAssignmentFailureReason.ModuleNotFound);
            }

            if (brigade.IsDisbanded)
            {
                return BrigadeAssignmentResult.Failure(
                    BrigadeAssignmentFailureReason.BrigadeDisbanded);
            }

            if (_moduleIdByBrigadeId.ContainsKey(brigadeId))
            {
                return BrigadeAssignmentResult.Failure(
                    BrigadeAssignmentFailureReason.BrigadeAlreadyAssigned);
            }

            module.AssignBrigade(brigadeId);

            _moduleIdByBrigadeId.Add(
                brigadeId,
                moduleId);

            return BrigadeAssignmentResult.Success(
                brigade,
                module);
        }

        public bool TryUnassignBrigade(
            Guid brigadeId,
            out ModuleInstance previousModule)
        {
            if (!_moduleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                previousModule = null;
                return false;
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out previousModule))
            {
                _moduleIdByBrigadeId.Remove(brigadeId);
                return false;
            }

            previousModule.RemoveBrigade(brigadeId);
            _moduleIdByBrigadeId.Remove(brigadeId);

            return true;
        }

        public bool TryGetAssignedModule(
            Guid brigadeId,
            out ModuleInstance module)
        {
            if (!_moduleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                module = null;
                return false;
            }

            return _grid.TryGetModule(
                moduleId,
                out module);
        }

        public ModuleStaffingAssessment CalculateModuleStaffing(
            Guid moduleId)
        {
            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                throw new KeyNotFoundException(
                    $"Module '{moduleId}' was not found.");
            }

            int assignedBrigadeCount = 0;
            int totalPersonnel = 0;

            long totalExperience = 0;
            long totalMorale = 0;
            long totalFatigue = 0;

            foreach (Guid brigadeId in module.AssignedBrigadeIds)
            {
                if (!_brigadesById.TryGetValue(
                        brigadeId,
                        out Brigade brigade))
                {
                    continue;
                }

                assignedBrigadeCount++;

                int personnel =
                    brigade.CurrentPersonnel;

                totalPersonnel += personnel;

                totalExperience +=
                    (long)brigade.Experience * personnel;

                totalMorale +=
                    (long)brigade.Morale * personnel;

                totalFatigue +=
                    (long)brigade.Fatigue * personnel;
            }

            int averageExperience =
                totalPersonnel > 0
                    ? (int)(totalExperience / totalPersonnel)
                    : 0;

            int averageMorale =
                totalPersonnel > 0
                    ? (int)(totalMorale / totalPersonnel)
                    : 0;

            int averageFatigue =
                totalPersonnel > 0
                    ? (int)(totalFatigue / totalPersonnel)
                    : 0;

            CrewRequirement requirement =
                module.Definition.CrewRequirement;

            return new ModuleStaffingAssessment(
                moduleId: module.Id,
                assignedBrigadeCount: assignedBrigadeCount,
                totalPersonnel: totalPersonnel,
                minimumPersonnel:
                    requirement.MinimumPersonnel,
                optimalPersonnel:
                    requirement.OptimalPersonnel,
                maximumPersonnel:
                    requirement.MaximumUsefulPersonnel,
                averageExperience: averageExperience,
                averageMorale: averageMorale,
                averageFatigue: averageFatigue);
        }

        public BastionCrewRequirements CalculateCrewRequirements()
        {
            var totals =
                new Dictionary<
                    WorkType,
                    (int Minimum, int Optimal, int MaximumUseful)>();

            foreach (ModuleInstance module in Modules)
            {
                foreach (
                    ModuleWorkRequirement requirement
                    in module.Definition
                        .CrewRequirement
                        .WorkRequirements)
                {
                    if (!totals.TryGetValue(
                            requirement.WorkType,
                            out var current))
                    {
                        current = (0, 0, 0);
                    }

                    totals[requirement.WorkType] =
                    (
                        current.Minimum +
                            requirement.MinimumPersonnel,

                        current.Optimal +
                            requirement.OptimalPersonnel,

                        current.MaximumUseful +
                            requirement.MaximumUsefulPersonnel
                    );
                }
            }

            IEnumerable<WorkRequirementSummary> summaries =
                totals
                    .OrderBy(pair => pair.Key)
                    .Select(pair =>
                        new WorkRequirementSummary(
                            pair.Key,
                            pair.Value.Minimum,
                            pair.Value.Optimal,
                            pair.Value.MaximumUseful));

            return new BastionCrewRequirements(summaries);
        }

        public BastionCrewCapacity CalculateCrewCapacity()
        {
            int totalBerths = 0;
            int nominalAccommodationCapacity = 0;
            int emergencyAccommodationCapacity = 0;
            int ventilationPersonnelCapacity = 0;

            foreach (ModuleInstance module in Modules)
            {
                IReadOnlyList<CrewAccommodationFeatureDefinition>
                    accommodationFeatures =
                        module.Definition.GetFeatures<
                            CrewAccommodationFeatureDefinition>();

                foreach (
                    CrewAccommodationFeatureDefinition accommodation
                    in accommodationFeatures)
                {
                    totalBerths +=
                        accommodation.BerthCount;

                    nominalAccommodationCapacity +=
                        accommodation.NominalPersonnelCapacity;

                    emergencyAccommodationCapacity +=
                        accommodation.EmergencyPersonnelCapacity;
                }

                IReadOnlyList<VentilationFeatureDefinition>
                    ventilationFeatures =
                        module.Definition.GetFeatures<
                            VentilationFeatureDefinition>();

                foreach (
                    VentilationFeatureDefinition ventilation
                    in ventilationFeatures)
                {
                    ventilationPersonnelCapacity +=
                        ventilation.SupportedPersonnelCapacity;
                }
            }

            return new BastionCrewCapacity(
                totalBerths,
                nominalAccommodationCapacity,
                emergencyAccommodationCapacity,
                ventilationPersonnelCapacity);
        }

        public BastionCrewRosterSummary CalculateCrewRosterSummary()
        {
            IEnumerable<BrigadeTypePersonnelSummary> summaries =
                Brigades
                    .Where(brigade => !brigade.IsDisbanded)
                    .GroupBy(brigade => brigade.Type)
                    .OrderBy(group => group.Key)
                    .Select(group =>
                        new BrigadeTypePersonnelSummary(
                            brigadeType: group.Key,
                            brigadeCount: group.Count(),
                            personnel: group.Sum(
                                brigade => brigade.CurrentPersonnel)));

            return new BastionCrewRosterSummary(summaries);
        }

        public ModuleWorkEfficiencyAssessment
            CalculateModuleWorkEfficiency(
                Guid moduleId,
                BrigadeWorkProfileCatalog profileCatalog)
        {
            if (profileCatalog == null)
            {
                throw new ArgumentNullException(
                    nameof(profileCatalog));
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                throw new KeyNotFoundException(
                    $"Module '{moduleId}' was not found.");
            }

            IEnumerable<Brigade> assignedBrigades =
                module.AssignedBrigadeIds
                    .Select(brigadeId =>
                        _brigadesById.TryGetValue(
                            brigadeId,
                            out Brigade brigade)
                                ? brigade
                                : null)
                    .Where(brigade => brigade != null);

            return ModuleWorkEfficiencyCalculator.Calculate(
                module,
                assignedBrigades,
                profileCatalog);
        }

        public ModuleOperationalAssessment
            CalculateModuleOperationalAssessment(
                Guid moduleId,
                BrigadeWorkProfileCatalog profileCatalog)
        {
            if (profileCatalog == null)
            {
                throw new ArgumentNullException(
                    nameof(profileCatalog));
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                throw new KeyNotFoundException(
                    $"Module '{moduleId}' was not found.");
            }

            ModuleWorkEfficiencyAssessment workEfficiency =
                CalculateModuleWorkEfficiency(
                    moduleId,
                    profileCatalog);

            return ModuleOperationalEfficiencyCalculator.Calculate(
                module,
                workEfficiency);
        }
    }
}