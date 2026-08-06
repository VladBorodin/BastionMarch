using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;

namespace BastionMarch.Presentation.Prototype
{
    /// <summary>
    /// Создаёт временную модель бастиона для разработки
    /// Presentation-слоя.
    ///
    /// Это не каталог игрового контента и не сохранение.
    /// </summary>
    public static class PrototypeBastionFactory
    {
        private const int PrototypeWidth = 8;
        private const int PrototypeDeckCount = 3;

        public static Bastion Create()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            var bastion =
                new Bastion(
                    name: "presentation-prototype",
                    width: PrototypeWidth,
                    deckCount: PrototypeDeckCount);

            var installedModules =
                new List<ModuleInstance>();

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .LargeMachineRoom)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardGeneratorRoom)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardRepairBay)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardAmmoStorage)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardCrewQuarters)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardVentilation)));

            installedModules.Add(
                InstallFirstAvailable(
                    bastion,
                    catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom)));

            PreparePowerState(
                bastion,
                installedModules);

            ApplyPrototypeDamage(
                installedModules);

            InstallPrototypePassages(
                bastion,
                installedModules);

            return bastion;
        }

        private static ModuleInstance InstallFirstAvailable(
            Bastion bastion,
            ModuleDefinition definition)
        {
            for (int deck = 0;
                 deck < bastion.DeckCount;
                 deck++)
            {
                for (int x = 0;
                     x < bastion.Width;
                     x++)
                {
                    ModulePlacementResult result =
                        bastion.TryInstallModule(
                            definition,
                            new GridPosition(
                                x,
                                deck));

                    if (result.IsSuccess)
                    {
                        return result.Module;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Prototype module '{definition.Id}' " +
                "could not be installed.");
        }

        private static void InstallPrototypePassages(
            Bastion bastion,
            IReadOnlyList<ModuleInstance> modules)
        {
            if (bastion == null)
            {
                throw new ArgumentNullException(
                    nameof(bastion));
            }

            if (modules == null)
            {
                throw new ArgumentNullException(
                    nameof(modules));
            }

            int verticalPassageIndex = 0;

            for (int sourceIndex = 0;
                sourceIndex < modules.Count;
                sourceIndex++)
            {
                for (int targetIndex = sourceIndex + 1;
                    targetIndex < modules.Count;
                    targetIndex++)
                {
                    ModuleInstance source =
                        modules[sourceIndex];

                    ModuleInstance target =
                        modules[targetIndex];

                    bool adjacencyFound =
                        bastion.TryGetModuleAdjacency(
                            source.Id,
                            target.Id,
                            out ModuleAdjacency adjacency);

                    if (!adjacencyFound ||
                        adjacency.SharedBoundaries.Count == 0)
                    {
                        continue;
                    }

                    foreach (
                        GridBoundarySegment boundary
                        in adjacency.SharedBoundaries)
                    {
                        ModulePassageType passageType =
                            GetPrototypePassageType(
                                boundary,
                                verticalPassageIndex);

                        ModulePassagePlacementResult result =
                            bastion.TryInstallPassage(
                                source.Id,
                                target.Id,
                                boundary,
                                passageType,
                                ModulePassageTraversalMode
                                    .Bidirectional);

                        if (!result.IsSuccess)
                        {
                            continue;
                        }

                        ApplyPrototypePassageState(
                            result.Passage,
                            bastion.Passages.Count - 1);

                        if (boundary.IsVerticalPassage)
                        {
                            verticalPassageIndex++;
                        }

                        // Для соседних модулей на одном этаже
                        // достаточно одной двери.
                        if (boundary.IsHorizontalPassage)
                        {
                            break;
                        }
                    }
                }
            }

            if (bastion.Passages.Count == 0)
            {
                throw new InvalidOperationException(
                    "Prototype bastion did not produce any passages.");
            }
        }

        private static ModulePassageType GetPrototypePassageType(
            GridBoundarySegment boundary,
            int verticalPassageIndex)
        {
            if (boundary.IsHorizontalPassage)
            {
                return ModulePassageType.Door;
            }

            switch (verticalPassageIndex % 4)
            {
                case 0:
                    return ModulePassageType.Hatch;

                case 1:
                    return ModulePassageType.Ladder;

                case 2:
                    return ModulePassageType.Stairway;

                case 3:
                    return ModulePassageType.Elevator;

                default:
                    throw new InvalidOperationException(
                        "Unsupported prototype passage index.");
            }
        }

        private static void ApplyPrototypePassageState(
            ModulePassage passage,
            int passageIndex)
        {
            if (passage == null)
            {
                throw new ArgumentNullException(
                    nameof(passage));
            }

            switch (passageIndex % 5)
            {
                case 0:
                    passage.SetState(
                        ModulePassageState.Open);
                    break;

                case 1:
                    passage.SetState(
                        ModulePassageState.Closed);
                    break;

                case 2:
                    passage.SetState(
                        ModulePassageState.Locked);
                    break;

                case 3:
                    passage.SetState(
                        ModulePassageState.Blocked);
                    break;

                case 4:
                    passage.SetState(
                        ModulePassageState.Destroyed);
                    break;
            }
        }

        private static void PreparePowerState(
            Bastion bastion,
            IReadOnlyList<ModuleInstance> modules)
        {
            foreach (
                ModuleInstance module
                in modules)
            {
                module.SetPowerMode(
                    ModulePowerMode.Active);
            }

            bastion.ResolvePowerDistribution();
        }

        private static void ApplyPrototypeDamage(
            IReadOnlyList<ModuleInstance> modules)
        {
            if (modules.Count < 3)
            {
                return;
            }

            ModuleInstance damagedModule =
                modules[1];

            int damagedStateDamage =
                Math.Max(
                    0,
                    damagedModule.Definition.MaxDurability -
                    damagedModule.Definition
                        .DamagedDurabilityThreshold);

            damagedModule.ApplyDamage(
                damagedStateDamage);

            ModuleInstance criticalModule =
                modules[2];

            int criticalStateDamage =
                Math.Max(
                    0,
                    criticalModule.Definition.MaxDurability -
                    criticalModule.Definition
                        .CriticalDurabilityThreshold);

            criticalModule.ApplyDamage(
                criticalStateDamage);
        }
    }
}