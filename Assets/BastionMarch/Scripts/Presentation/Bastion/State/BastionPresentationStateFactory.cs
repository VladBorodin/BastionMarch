using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Преобразует текущее изменяемое состояние Simulation
    /// в неизменяемый снимок для Presentation.
    /// </summary>
    public static class BastionPresentationStateFactory
    {
        public static BastionPresentationState Capture(
            Bastion bastion)
        {
            if (bastion == null)
            {
                throw new ArgumentNullException(
                    nameof(bastion));
            }

            IEnumerable<ModulePresentationState>
                moduleStates =
                    bastion.Modules
                        .OrderBy(module =>
                            module.Position.Deck)
                        .ThenBy(module =>
                            module.Position.X)
                        .ThenBy(module =>
                            module.Id)
                        .Select(CaptureModule);

            return new BastionPresentationState(
                bastionId: bastion.Id,
                name: bastion.Name,
                width: bastion.Width,
                deckCount: bastion.DeckCount,
                modules: moduleStates);
        }

        public static ModulePresentationState CaptureModule(
            ModuleInstance module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(
                    nameof(module));
            }

            return new ModulePresentationState(
                moduleId: module.Id,
                definitionId:
                    module.Definition.Id,
                nameLocalizationKey:
                    module.Definition
                        .NameLocalizationKey,
                category:
                    module.Definition.Category,
                type:
                    module.Definition.Type,
                position:
                    module.Position,
                size:
                    module.Definition.Size,
                currentDurability:
                    module.CurrentDurability,
                maximumDurability:
                    module.Definition.MaxDurability,
                technicalState:
                    module.TechnicalState,
                controlState:
                    module.ControlState,
                requestedPowerMode:
                    module.RequestedPowerMode,
                effectivePowerMode:
                    module.EffectivePowerMode,
                powerPriority:
                    module.PowerPriority,
                occupyingBrigadeCount:
                    module.OccupyingBrigadeIds.Count,
                workingBrigadeCount:
                    module.WorkingBrigadeIds.Count);
        }
    }
}