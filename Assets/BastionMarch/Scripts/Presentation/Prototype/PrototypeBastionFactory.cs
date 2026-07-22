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