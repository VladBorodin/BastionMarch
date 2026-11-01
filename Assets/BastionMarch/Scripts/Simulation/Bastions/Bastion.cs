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

        public int PassageCount =>
            _grid.PassageCount;

        public IReadOnlyCollection<ModulePassage> Passages =>
            _grid.Passages;

        public int Width => _grid.Width;

        public int DeckCount => _grid.DeckCount;

        public int ModuleCount => _grid.ModuleCount;

        public IReadOnlyCollection<ModuleInstance> Modules =>
            _grid.Modules;

        private readonly Dictionary<Guid, Brigade> _brigadesById =
            new();

        private readonly Dictionary<Guid, Guid>
            _locationModuleIdByBrigadeId = new();

        private readonly HashSet<Guid>
            _workingBrigadeIds = new();

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

            Guid[] occupyingBrigadeIds =
                existingModule
                    .OccupyingBrigadeIds
                    .ToArray();

            foreach (Guid brigadeId in occupyingBrigadeIds)
            {
                ClearBrigadePlacement(
                    brigadeId,
                    existingModule);
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

        public bool TryGetModuleAdjacencies(
            Guid moduleId,
            out IReadOnlyList<ModuleAdjacency> adjacencies)
        {
            return _grid.TryGetModuleAdjacencies(
                moduleId,
                out adjacencies);
        }

        public bool TryGetModuleAdjacency(
            Guid sourceModuleId,
            Guid targetModuleId,
            out ModuleAdjacency adjacency)
        {
            return _grid.TryGetModuleAdjacency(
                sourceModuleId,
                targetModuleId,
                out adjacency);
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

            if (_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                if (_grid.TryGetModule(
                        moduleId,
                        out ModuleInstance module))
                {
                    ClearBrigadePlacement(
                        brigadeId,
                        module);
                }
                else
                {
                    ClearStaleBrigadePlacement(
                        brigadeId);
                }
            }

            _brigadesById.Remove(
                brigadeId);

            return true;
        }

        public BrigadeOperationalResult TryDeployBrigadeToModule(
            Guid brigadeId,
            Guid moduleId)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out Brigade brigade))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotFound);
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .ModuleNotFound);
            }

            if (brigade.IsDisbanded)
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeDisbanded);
            }

            if (_locationModuleIdByBrigadeId.ContainsKey(
                    brigadeId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeAlreadyDeployed);
            }

            if (!module.AddOccupyingBrigade(
                    brigadeId))
            {
                throw new InvalidOperationException(
                    "Module occupancy state is inconsistent.");
            }

            _locationModuleIdByBrigadeId.Add(
                brigadeId,
                moduleId);

            return BrigadeOperationalResult.Success(
                brigade,
                module);
        }

        public BrigadeOperationalResult TryStartBrigadeWork(
            Guid brigadeId)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out Brigade brigade))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotFound);
            }

            if (brigade.IsDisbanded)
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeDisbanded);
            }

            if (!_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotDeployed);
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                ClearStaleBrigadePlacement(
                    brigadeId);

                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .ModuleNotFound);
            }

            if (_workingBrigadeIds.Contains(
                    brigadeId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeAlreadyWorking);
            }

            if (!module.StartBrigadeWork(
                    brigadeId))
            {
                throw new InvalidOperationException(
                    "Module work state is inconsistent.");
            }

            _workingBrigadeIds.Add(
                brigadeId);

            return BrigadeOperationalResult.Success(
                brigade,
                module);
        }

        public BrigadeOperationalResult TryStopBrigadeWork(
            Guid brigadeId)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out Brigade brigade))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotFound);
            }

            if (!_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotDeployed);
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                ClearStaleBrigadePlacement(
                    brigadeId);

                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .ModuleNotFound);
            }

            if (!_workingBrigadeIds.Contains(
                    brigadeId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotWorking);
            }

            module.StopBrigadeWork(
                brigadeId);

            _workingBrigadeIds.Remove(
                brigadeId);

            return BrigadeOperationalResult.Success(
                brigade,
                module);
        }

        public BrigadeOperationalResult TryUndeployBrigade(
            Guid brigadeId)
        {
            if (!_brigadesById.TryGetValue(
                    brigadeId,
                    out Brigade brigade))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotFound);
            }

            if (!_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .BrigadeNotDeployed);
            }

            if (!_grid.TryGetModule(
                    moduleId,
                    out ModuleInstance module))
            {
                ClearStaleBrigadePlacement(
                    brigadeId);

                return BrigadeOperationalResult.Failure(
                    BrigadeOperationalFailureReason
                        .ModuleNotFound);
            }

            ClearBrigadePlacement(
                brigadeId,
                module);

            return BrigadeOperationalResult.Success(
                brigade,
                module);
        }

        
        public BrigadeMoveAssessment AssessBrigadeMove(
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId)
        {
            if (brigadeId == Guid.Empty ||
                sourceModuleId == Guid.Empty ||
                targetModuleId == Guid.Empty ||
                passageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Move identities cannot be empty.");
            }

            BrigadeMoveAssessment Reject(
                BrigadeMoveFailureReason reason,
                ModulePassageTraversalFailureReason traversalReason =
                    ModulePassageTraversalFailureReason.None)
            {
                return BrigadeMoveAssessment.Rejected(
                    brigadeId,
                    sourceModuleId,
                    targetModuleId,
                    passageId,
                    reason,
                    traversalReason);
            }

            if (!_brigadesById.TryGetValue(brigadeId, out Brigade brigade))
            {
                return Reject(BrigadeMoveFailureReason.BrigadeNotFound);
            }

            if (brigade.IsDisbanded)
            {
                return Reject(BrigadeMoveFailureReason.BrigadeDisbanded);
            }

            if (!_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId, out Guid actualModuleId))
            {
                return Reject(BrigadeMoveFailureReason.BrigadeNotDeployed);
            }

            if (actualModuleId != sourceModuleId)
            {
                return Reject(BrigadeMoveFailureReason.SourceMismatch);
            }

            if (!_grid.TryGetModule(sourceModuleId, out ModuleInstance source))
            {
                return Reject(BrigadeMoveFailureReason.SourceModuleNotFound);
            }

            if (!_grid.TryGetModule(targetModuleId, out ModuleInstance target))
            {
                return Reject(BrigadeMoveFailureReason.TargetModuleNotFound);
            }

            if (sourceModuleId == targetModuleId)
            {
                return Reject(BrigadeMoveFailureReason.SameModule);
            }

            if (!_grid.TryGetModuleAdjacency(sourceModuleId, targetModuleId, out _))
            {
                return Reject(BrigadeMoveFailureReason.ModulesNotAdjacent);
            }

            if (!source.OccupyingBrigadeIds.Contains(brigadeId))
            {
                return Reject(BrigadeMoveFailureReason.SourceOccupancyMissing);
            }

            bool registeredWorking = _workingBrigadeIds.Contains(brigadeId);
            bool sourceWorking = source.WorkingBrigadeIds.Contains(brigadeId);

            if (registeredWorking != sourceWorking)
            {
                return Reject(BrigadeMoveFailureReason.WorkingStateInconsistent);
            }

            if (target.OccupyingBrigadeIds.Contains(brigadeId) ||
                target.WorkingBrigadeIds.Contains(brigadeId))
            {
                return Reject(BrigadeMoveFailureReason.TargetAlreadyOccupied);
            }

            if (!_grid.TryGetPassage(passageId, out _))
            {
                return Reject(BrigadeMoveFailureReason.PassageNotFound);
            }

            ModulePassageTraversalAssessment traversal =
                _grid.AssessPassageTraversal(
                    passageId,
                    sourceModuleId,
                    targetModuleId);

            if (!traversal.IsAllowed)
            {
                return Reject(
                    BrigadeMoveFailureReason.PassageTraversalRejected,
                    traversal.FailureReason);
            }

            return BrigadeMoveAssessment.Allowed(
                brigadeId,
                sourceModuleId,
                targetModuleId,
                passageId);
        }

        /// <summary>
        /// Перемещает бригаду через проверенный соседний Passage.
        ///
        /// Успешное перемещение прекращает текущую работу,
        /// переносит occupancy и обновляет location.
        ///
        /// Работа в целевом модуле не запускается автоматически.
        /// </summary>
        public BrigadeMoveResult TryMoveBrigade(
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId)
        {
            BrigadeMoveAssessment assessment = AssessBrigadeMove(
                brigadeId,
                sourceModuleId,
                targetModuleId,
                passageId);

            if (!assessment.IsAllowed)
            {
                return new BrigadeMoveResult(assessment);
            }

            ApplyAssessedBrigadeMove(assessment);
            return new BrigadeMoveResult(assessment);
        }
           
        public IReadOnlyList<BrigadeMoveResult> CommitAssessedBrigadeMoves(
            IEnumerable<BrigadeMoveAssessment> assessments)
        {
            return CommitBrigadePhaseActions(
                assessments,
                Array.Empty<BrigadeWorkStartRequest>());
        }

        /// <summary>
        /// Проверяет весь пакет перемещений и начал работы
        /// относительно общего исходного состояния Bastion.
        ///
        /// В M1 разные бригады не конкурируют за физическую
        /// вместимость рабочего места.
        /// </summary>
        public IReadOnlyList<BrigadeMoveResult> CommitBrigadePhaseActions(
            IEnumerable<BrigadeMoveAssessment> assessments,
            IEnumerable<BrigadeWorkStartRequest> workStarts)
        {
            if (assessments == null)
            {
                throw new ArgumentNullException(nameof(assessments));
            }

            if (workStarts == null)
            {
                throw new ArgumentNullException(nameof(workStarts));
            }

            BrigadeMoveAssessment[] requestedMoves = assessments.ToArray();
            BrigadeWorkStartRequest[] requestedStarts = workStarts.ToArray();

            if (requestedMoves.Any(item => item == null) ||
                requestedMoves.Any(item => !item.IsAllowed))
            {
                throw new ArgumentException(
                    "Move assessments must be non-null and allowed.",
                    nameof(assessments));
            }

            if (requestedStarts.Any(item => item == null))
            {
                throw new ArgumentException(
                    "Work start requests cannot contain null.",
                    nameof(workStarts));
            }

            if (requestedMoves.Select(item => item.BrigadeId)
                    .Distinct().Count() != requestedMoves.Length)
            {
                throw new ArgumentException(
                    "Duplicate brigade movement.",
                    nameof(assessments));
            }

            if (requestedStarts.Select(item => item.BrigadeId)
                    .Distinct().Count() != requestedStarts.Length)
            {
                throw new ArgumentException(
                    "Duplicate brigade work start.",
                    nameof(workStarts));
            }

            HashSet<Guid> movingBrigades =
                requestedMoves.Select(item => item.BrigadeId).ToHashSet();

            if (requestedStarts.Any(item => movingBrigades.Contains(item.BrigadeId)))
            {
                throw new ArgumentException(
                    "A brigade cannot move and start work in the same phase.",
                    nameof(workStarts));
            }

            // Полная повторная проверка всех перемещений.
            BrigadeMoveAssessment[] validatedMoves = requestedMoves
                .Select(item => AssessBrigadeMove(
                    item.BrigadeId,
                    item.SourceModuleId,
                    item.TargetModuleId,
                    item.PassageId))
                .ToArray();

            if (validatedMoves.Any(item => !item.IsAllowed))
            {
                throw new InvalidOperationException(
                    "Move preconditions changed after assessment.");
            }

            // Полная проверка всех рабочих назначений до mutation.
            foreach (BrigadeWorkStartRequest request in requestedStarts)
            {
                if (!_brigadesById.TryGetValue(
                        request.BrigadeId, out Brigade brigade) ||
                    brigade.IsDisbanded)
                {
                    throw new InvalidOperationException(
                        "Work-start brigade is unavailable.");
                }

                if (!_locationModuleIdByBrigadeId.TryGetValue(
                        request.BrigadeId, out Guid actualModuleId) ||
                    actualModuleId != request.ModuleId)
                {
                    throw new InvalidOperationException(
                        "Work-start brigade is not in the expected module.");
                }

                if (!_grid.TryGetModule(
                        request.ModuleId, out ModuleInstance module) ||
                    !module.OccupyingBrigadeIds.Contains(request.BrigadeId) ||
                    _workingBrigadeIds.Contains(request.BrigadeId) ||
                    module.WorkingBrigadeIds.Contains(request.BrigadeId))
                {
                    throw new InvalidOperationException(
                        "Work-start occupancy or working state is invalid.");
                }
            }

            var results = new List<BrigadeMoveResult>(validatedMoves.Length);

            foreach (BrigadeMoveAssessment assessment in
                    validatedMoves.OrderBy(item => item.BrigadeId))
            {
                ApplyAssessedBrigadeMove(assessment);
                results.Add(new BrigadeMoveResult(assessment));
            }

            foreach (BrigadeWorkStartRequest request in
                    requestedStarts.OrderBy(item => item.BrigadeId))
            {
                BrigadeOperationalResult result =
                    TryStartBrigadeWork(request.BrigadeId);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        "Prepared work start could not be committed.");
                }
            }

            return results.AsReadOnly();
        }

        private void ApplyAssessedBrigadeMove(BrigadeMoveAssessment assessment)
        {
            if (!_grid.TryGetModule(
                    assessment.SourceModuleId, out ModuleInstance source))
            {
                throw new InvalidOperationException(
                    "Source module disappeared during move.");
            }

            if (!_grid.TryGetModule(
                    assessment.TargetModuleId, out ModuleInstance target))
            {
                throw new InvalidOperationException(
                    "Target module disappeared during move.");
            }

            Guid brigadeId = assessment.BrigadeId;

            if (!target.AddOccupyingBrigade(brigadeId))
            {
                throw new InvalidOperationException(
                    "Target occupancy state is inconsistent.");
            }

            if (!source.RemoveOccupyingBrigade(brigadeId))
            {
                target.RemoveOccupyingBrigade(brigadeId);

                throw new InvalidOperationException(
                    "Source occupancy state changed during move.");
            }

            _workingBrigadeIds.Remove(brigadeId);
            _locationModuleIdByBrigadeId[brigadeId] =
                assessment.TargetModuleId;
        }

        public bool TryGetBrigadeLocation(
            Guid brigadeId,
            out ModuleInstance module)
        {
            if (!_locationModuleIdByBrigadeId.TryGetValue(
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

        public bool TryGetBrigadeOperationalState(
            Guid brigadeId,
            out BrigadeOperationalState state)
        {
            if (!_brigadesById.ContainsKey(
                    brigadeId))
            {
                state = null;
                return false;
            }

            Guid? currentModuleId = null;

            if (_locationModuleIdByBrigadeId.TryGetValue(
                    brigadeId,
                    out Guid moduleId))
            {
                currentModuleId = moduleId;
            }

            state = new BrigadeOperationalState(
                brigadeId,
                currentModuleId,
                _workingBrigadeIds.Contains(
                    brigadeId));

            return true;
        }

        private void ClearBrigadePlacement(
            Guid brigadeId,
            ModuleInstance module)
        {
            module.StopBrigadeWork(
                brigadeId);

            module.RemoveOccupyingBrigade(
                brigadeId);

            _workingBrigadeIds.Remove(
                brigadeId);

            _locationModuleIdByBrigadeId.Remove(
                brigadeId);
        }

        private void ClearStaleBrigadePlacement(
            Guid brigadeId)
        {
            _workingBrigadeIds.Remove(
                brigadeId);

            _locationModuleIdByBrigadeId.Remove(
                brigadeId);
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

            int occupyingBrigadeCount = 0;
            int workingBrigadeCount = 0;

            int totalOccupyingPersonnel = 0;
            int totalWorkingPersonnel = 0;

            long totalExperience = 0;
            long totalMorale = 0;
            long totalFatigue = 0;

            foreach (
                Guid brigadeId
                in module.OccupyingBrigadeIds)
            {
                if (!_brigadesById.TryGetValue(
                        brigadeId,
                        out Brigade brigade) ||
                    brigade.IsDisbanded)
                {
                    continue;
                }

                occupyingBrigadeCount++;
                totalOccupyingPersonnel +=
                    brigade.CurrentPersonnel;
            }

            foreach (
                Guid brigadeId
                in module.WorkingBrigadeIds)
            {
                if (!_brigadesById.TryGetValue(
                        brigadeId,
                        out Brigade brigade) ||
                    brigade.IsDisbanded)
                {
                    continue;
                }

                workingBrigadeCount++;

                int personnel =
                    brigade.CurrentPersonnel;

                totalWorkingPersonnel +=
                    personnel;

                totalExperience +=
                    (long)brigade.Experience *
                    personnel;

                totalMorale +=
                    (long)brigade.Morale *
                    personnel;

                totalFatigue +=
                    (long)brigade.Fatigue *
                    personnel;
            }

            int averageExperience =
                totalWorkingPersonnel > 0
                    ? (int)(
                        totalExperience /
                        totalWorkingPersonnel)
                    : 0;

            int averageMorale =
                totalWorkingPersonnel > 0
                    ? (int)(
                        totalMorale /
                        totalWorkingPersonnel)
                    : 0;

            int averageFatigue =
                totalWorkingPersonnel > 0
                    ? (int)(
                        totalFatigue /
                        totalWorkingPersonnel)
                    : 0;

            CrewRequirement requirement =
                module.Definition.CrewRequirement;

            return new ModuleStaffingAssessment(
                moduleId: module.Id,
                occupyingBrigadeCount:
                    occupyingBrigadeCount,
                workingBrigadeCount:
                    workingBrigadeCount,
                totalOccupyingPersonnel:
                    totalOccupyingPersonnel,
                totalWorkingPersonnel:
                    totalWorkingPersonnel,
                minimumPersonnel:
                    requirement.MinimumPersonnel,
                optimalPersonnel:
                    requirement.OptimalPersonnel,
                maximumUsefulPersonnel:
                    requirement.MaximumUsefulPersonnel,
                averageExperience:
                    averageExperience,
                averageMorale:
                    averageMorale,
                averageFatigue:
                    averageFatigue);
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

            List<Brigade> workingBrigades =
                module.WorkingBrigadeIds
                    .Select(brigadeId =>
                        _brigadesById.TryGetValue(
                            brigadeId,
                            out Brigade brigade)
                                ? brigade
                                : null)
                    .Where(brigade =>
                        brigade != null &&
                        !brigade.IsDisbanded)
                    .ToList();

            int totalOccupyingPersonnel =
                module.OccupyingBrigadeIds
                    .Select(brigadeId =>
                        _brigadesById.TryGetValue(
                            brigadeId,
                            out Brigade brigade)
                                ? brigade
                                : null)
                    .Where(brigade =>
                        brigade != null &&
                        !brigade.IsDisbanded)
                    .Sum(brigade =>
                        brigade.CurrentPersonnel);

            return ModuleWorkEfficiencyCalculator.Calculate(
                module,
                workingBrigades,
                totalOccupyingPersonnel,
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

        public ModulePassagePlacementResult
            TryInstallPassage(
                Guid sourceModuleId,
                Guid targetModuleId,
                GridBoundarySegment boundary,
                ModulePassageType type,
                ModulePassageTraversalMode traversalMode)
        {
            return _grid.TryAddPassage(
                sourceModuleId,
                targetModuleId,
                boundary,
                type,
                traversalMode);
        }

        public bool TryGetPassage(
            Guid passageId,
            out ModulePassage passage)
        {
            return _grid.TryGetPassage(
                passageId,
                out passage);
        }

        public bool TryGetPassageAtBoundary(
            GridBoundarySegment boundary,
            out ModulePassage passage)
        {
            return _grid.TryGetPassageAtBoundary(
                boundary,
                out passage);
        }

        public bool TryGetPassagesForModule(
            Guid moduleId,
            out IReadOnlyList<ModulePassage> passages)
        {
            return _grid.TryGetPassagesForModule(
                moduleId,
                out passages);
        }

        public bool TryRemovePassage(
            Guid passageId,
            out ModulePassage removedPassage)
        {
            return _grid.TryRemovePassage(
                passageId,
                out removedPassage);
        }

        public ModulePassageTraversalAssessment
            AssessPassageTraversal(
                Guid passageId,
                Guid fromModuleId,
                Guid toModuleId)
        {
            return _grid.AssessPassageTraversal(
                passageId,
                fromModuleId,
                toModuleId);
        }

        public ModulePassageTraversalAssessment
            AssessPassageTraversal(
                Guid passageId,
                Guid fromModuleId,
                Guid toModuleId,
                IModulePassageTraversalPolicy policy)
        {
            return _grid.AssessPassageTraversal(
                passageId,
                fromModuleId,
                toModuleId,
                policy);
        }

        public ModuleConnectivityGraph
            BuildTraversalGraph()
        {
            return _grid.BuildTraversalGraph();
        }

        public ModuleConnectivityGraph
            BuildTraversalGraph(
                IModulePassageTraversalPolicy policy)
        {
            return _grid.BuildTraversalGraph(
                policy);
        }

        public ModuleRouteSearchResult FindModuleRoute(
            Guid sourceModuleId,
            Guid targetModuleId)
        {
            return _grid.FindModuleRoute(
                sourceModuleId,
                targetModuleId);
        }

        public ModuleRouteSearchResult FindModuleRoute(
            Guid sourceModuleId,
            Guid targetModuleId,
            IModulePassageTraversalPolicy policy)
        {
            return _grid.FindModuleRoute(
                sourceModuleId,
                targetModuleId,
                policy);
        }
    }
}