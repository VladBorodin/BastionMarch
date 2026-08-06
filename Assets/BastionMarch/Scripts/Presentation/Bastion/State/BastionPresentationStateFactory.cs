using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Crew;

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

            IEnumerable<PassagePresentationState>
                passageStates =
                    bastion.Passages
                        .OrderBy(passage =>
                            passage.Boundary.CellA.Deck)
                        .ThenBy(passage =>
                            passage.Boundary.CellA.X)
                        .ThenBy(passage =>
                            passage.Boundary.CellB.Deck)
                        .ThenBy(passage =>
                            passage.Boundary.CellB.X)
                        .ThenBy(passage =>
                            passage.Id)
                        .Select(CapturePassage);

            IEnumerable<BrigadePresentationState>
                brigadeStates =
                    bastion.Brigades
                        .OrderBy(brigade =>
                            brigade.Number)
                        .ThenBy(brigade =>
                            brigade.Id)
                        .Select(brigade =>
                            CaptureBrigade(
                                bastion,
                                brigade));

            return new BastionPresentationState(
                bastionId: bastion.Id,
                name: bastion.Name,
                width: bastion.Width,
                deckCount: bastion.DeckCount,
                modules: moduleStates,
                passages: passageStates,
                brigades: brigadeStates);
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

        public static PassagePresentationState
            CapturePassage(
                ModulePassage passage)
        {
            if (passage == null)
            {
                throw new ArgumentNullException(
                    nameof(passage));
            }

            return new PassagePresentationState(
                passageId: passage.Id,
                sourceModuleId:
                    passage.SourceModuleId,
                targetModuleId:
                    passage.TargetModuleId,
                boundary:
                    passage.Boundary,
                type:
                    passage.Type,
                traversalMode:
                    passage.TraversalMode,
                state:
                    passage.State);
        }

        public static BrigadePresentationState
            CaptureBrigade(
                Bastion bastion,
                Brigade brigade)
        {
            if (bastion == null)
            {
                throw new ArgumentNullException(
                    nameof(bastion));
            }

            if (brigade == null)
            {
                throw new ArgumentNullException(
                    nameof(brigade));
            }

            Guid? currentModuleId = null;
            bool isWorking = false;

            if (bastion.TryGetBrigadeLocation(
                    brigade.Id,
                    out ModuleInstance currentModule))
            {
                currentModuleId =
                    currentModule.Id;

                isWorking =
                    currentModule
                        .WorkingBrigadeIds
                        .Contains(
                            brigade.Id);
            }

            return new BrigadePresentationState(
                brigadeId:
                    brigade.Id,
                number:
                    brigade.Number,
                type:
                    brigade.Type,
                currentPersonnel:
                    brigade.CurrentPersonnel,
                maximumUsefulPersonnel:
                    brigade.MaximumPersonnel,
                experience:
                    brigade.Experience,
                peakExperience:
                    brigade.PeakExperience,
                morale:
                    brigade.Morale,
                fatigue:
                    brigade.Fatigue,
                nickname:
                    brigade.Nickname,
                hasVeteranTradition:
                    brigade.HasVeteranTradition,
                isDisbanded:
                    brigade.IsDisbanded,
                currentModuleId:
                    currentModuleId,
                isWorking:
                    isWorking);
        }
    }
}