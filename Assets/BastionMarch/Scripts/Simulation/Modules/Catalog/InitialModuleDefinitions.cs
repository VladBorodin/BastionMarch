using System.Collections.Generic;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules.Features;

namespace BastionMarch.Simulation.Modules.Catalog
{
    /// <summary>
    /// Начальный тестовый каталог.
    ///
    /// Числа пока являются прототипными и нужны для проверки
    /// архитектуры, а не для окончательного баланса.
    /// </summary>
    public static class InitialModuleDefinitions
    {
        public static ModuleDefinitionCatalog CreateCatalog()
        {
            return new ModuleDefinitionCatalog(
                CreateDefinitions());
        }

        public static IReadOnlyList<ModuleDefinition>
            CreateDefinitions()
        {
            return new[]
            {
                CreateSmallMachineRoom(),
                CreateLargeMachineRoom(),
                CreateStandardGeneratorRoom(),
                CreateStandardRepairBay(),
                CreateStandardAmmoStorage(),
                CreateStandardCrewQuarters(),
                CreateStandardVentilation()
            };
        }

        private static ModuleDefinition CreateSmallMachineRoom()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.SmallMachineRoom,
                nameLocalizationKey:
                    ModuleLocalizationKeys.SmallMachineRoomName,
                category: ModuleCategory.Mobility,
                type: ModuleType.MachineRoom,
                size: new GridSize(1, 1),
                massKg: 18_000,
                cost: 1_000,
                maxDurability: 100,
                damagedDurabilityThreshold: 60,
                criticalDurabilityThreshold: 25,
                idlePowerConsumption: 1,
                activePowerConsumption: 4,
                heatGeneration: 12,
                crewRequirement: new CrewRequirement(
                    new[]
                    {
                        new ModuleWorkRequirement(
                            WorkType.Mechanical,
                            minimumPersonnel: 2,
                            optimalPersonnel: 3,
                            maximumUsefulPersonnel: 4)
                    }),
                features: new IModuleFeatureDefinition[]
                {
                    new PropulsionFeatureDefinition(
                        horsePower: 1_200,
                        fuelConsumptionPerTurn: 20,
                        baseWearPerTurn: 2)
                });
        }

        private static ModuleDefinition CreateLargeMachineRoom()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.LargeMachineRoom,
                nameLocalizationKey:
                    ModuleLocalizationKeys.LargeMachineRoomName,
                category: ModuleCategory.Mobility,
                type: ModuleType.MachineRoom,
                size: new GridSize(2, 2),
                massKg: 70_000,
                cost: 3_600,
                maxDurability: 280,
                damagedDurabilityThreshold: 170,
                criticalDurabilityThreshold: 70,
                idlePowerConsumption: 3,
                activePowerConsumption: 10,
                heatGeneration: 40,
                crewRequirement: new CrewRequirement(
                    new[]
                    {
                        new ModuleWorkRequirement(
                            WorkType.Mechanical,
                            minimumPersonnel: 4,
                            optimalPersonnel: 8,
                            maximumUsefulPersonnel: 12)
                    }),
                features: new IModuleFeatureDefinition[]
                {
                    new PropulsionFeatureDefinition(
                        horsePower: 5_400,
                        fuelConsumptionPerTurn: 85,
                        baseWearPerTurn: 5)
                });
        }

        private static ModuleDefinition CreateStandardRepairBay()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.StandardRepairBay,
                nameLocalizationKey:
                    ModuleLocalizationKeys.StandardRepairBayName,
                category: ModuleCategory.Maintenance,
                type: ModuleType.RepairBay,
                size: new GridSize(2, 1),
                massKg: 24_000,
                cost: 2_200,
                maxDurability: 150,
                damagedDurabilityThreshold: 90,
                criticalDurabilityThreshold: 40,
                idlePowerConsumption: 3,
                activePowerConsumption: 12,
                heatGeneration: 8,
                crewRequirement: new CrewRequirement(
                    new[]
                    {
                        new ModuleWorkRequirement(
                            WorkType.Mechanical,
                            minimumPersonnel: 2,
                            optimalPersonnel: 4,
                            maximumUsefulPersonnel: 6),

                        new ModuleWorkRequirement(
                            WorkType.General,
                            minimumPersonnel: 1,
                            optimalPersonnel: 2,
                            maximumUsefulPersonnel: 3)
                    }),
                features: new IModuleFeatureDefinition[]
                {
                    new RepairSupportFeatureDefinition(
                        repairPointsPerTurn: 20,
                        maximumConcurrentJobs: 2,
                        sparePartsCapacityUnits: 100)
                });
        }

        private static ModuleDefinition CreateStandardAmmoStorage()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.StandardAmmoStorage,
                nameLocalizationKey:
                    ModuleLocalizationKeys.StandardAmmoStorageName,
                category: ModuleCategory.Logistics,
                type: ModuleType.AmmoStorage,
                size: new GridSize(2, 1),
                massKg: 12_000,
                cost: 1_400,
                maxDurability: 120,
                damagedDurabilityThreshold: 70,
                criticalDurabilityThreshold: 25,
                idlePowerConsumption: 0,
                activePowerConsumption: 1,
                heatGeneration: 0,
                crewRequirement: new CrewRequirement(
                    new[]
                    {
                        new ModuleWorkRequirement(
                            WorkType.Logistics,
                            minimumPersonnel: 1,
                            optimalPersonnel: 2,
                            maximumUsefulPersonnel: 4),

                        new ModuleWorkRequirement(
                            WorkType.General,
                            minimumPersonnel: 0,
                            optimalPersonnel: 1,
                            maximumUsefulPersonnel: 2)
                    }),
                features: new IModuleFeatureDefinition[]
                {
                    new AmmoStorageFeatureDefinition(
                        capacityVolumeUnits: 120,
                        outputThroughputPerTurn: 24)
                });
        }

        private static ModuleDefinition CreateStandardGeneratorRoom()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.StandardGeneratorRoom,
                nameLocalizationKey:
                    ModuleLocalizationKeys.StandardGeneratorRoomName,
                category: ModuleCategory.Power,
                type: ModuleType.GeneratorRoom,
                size: new GridSize(2, 1),
                massKg: 20_000,
                cost: 1_800,
                maxDurability: 130,
                damagedDurabilityThreshold: 80,
                criticalDurabilityThreshold: 30,
                idlePowerConsumption: 1,
                activePowerConsumption: 2,
                heatGeneration: 15,
                crewRequirement: new CrewRequirement(
                new[]
                {
                    new ModuleWorkRequirement(
                        WorkType.Engineering,
                        minimumPersonnel: 2,
                        optimalPersonnel: 4,
                        maximumUsefulPersonnel: 6)
                }),
                features: new IModuleFeatureDefinition[]
                {
                    new PowerGenerationFeatureDefinition(
                        maximumPowerOutput: 40,
                        fuelConsumptionPerTurn: 18)
                });
        }

        private static ModuleDefinition CreateStandardCrewQuarters()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.StandardCrewQuarters,
                nameLocalizationKey:
                    ModuleLocalizationKeys.StandardCrewQuartersName,
                category: ModuleCategory.CrewSupport,
                type: ModuleType.CrewQuarters,
                size: new GridSize(2, 1),
                massKg: 10_000,
                cost: 900,
                maxDurability: 100,
                damagedDurabilityThreshold: 60,
                criticalDurabilityThreshold: 25,
                idlePowerConsumption: 1,
                activePowerConsumption: 2,
                heatGeneration: 3,
                crewRequirement: new CrewRequirement(
                    minimumPersonnel: 0,
                    optimalPersonnel: 0,
                    maximumUsefulPersonnel: 0),
                features: new IModuleFeatureDefinition[]
                {
                    new CrewAccommodationFeatureDefinition(
                        berthCount: 8,
                        nominalPersonnelCapacity: 12,
                        emergencyPersonnelCapacity: 18)
                });
        }

        private static ModuleDefinition CreateStandardVentilation()
        {
            return new ModuleDefinition(
                id: ModuleDefinitionIds.StandardVentilation,
                nameLocalizationKey:
                    ModuleLocalizationKeys.StandardVentilationName,
                category: ModuleCategory.CrewSupport,
                type: ModuleType.Ventilation,
                size: new GridSize(1, 1),
                massKg: 8_000,
                cost: 1_100,
                maxDurability: 90,
                damagedDurabilityThreshold: 55,
                criticalDurabilityThreshold: 20,
                idlePowerConsumption: 2,
                activePowerConsumption: 6,
                heatGeneration: 2,
                crewRequirement: new CrewRequirement(
                    minimumPersonnel: 0,
                    optimalPersonnel: 0,
                    maximumUsefulPersonnel: 0),
                features: new IModuleFeatureDefinition[]
                {
                    new VentilationFeatureDefinition(
                        supportedPersonnelCapacity: 24,
                        smokeExtractionPerTurn: 8,
                        heatRemovalPerTurn: 10)
                });
        }
    }
}