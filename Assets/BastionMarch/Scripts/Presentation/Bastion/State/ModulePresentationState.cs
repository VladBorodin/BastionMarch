using System;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Power;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок одного установленного модуля.
    ///
    /// View получает готовые данные и не хранит ссылку
    /// на изменяемый ModuleInstance.
    /// </summary>
    public sealed class ModulePresentationState
    {
        public Guid ModuleId { get; }

        public string DefinitionId { get; }

        public string NameLocalizationKey { get; }

        public ModuleCategory Category { get; }

        public ModuleType Type { get; }

        public GridPosition Position { get; }

        public GridSize Size { get; }

        public int CurrentDurability { get; }

        public int MaximumDurability { get; }

        public ModuleTechnicalState TechnicalState { get; }

        public ModuleControlState ControlState { get; }

        public ModulePowerMode RequestedPowerMode { get; }

        public ModulePowerMode EffectivePowerMode { get; }

        public PowerPriority PowerPriority { get; }

        public int OccupyingBrigadeCount { get; }

        public int WorkingBrigadeCount { get; }

        public ModulePresentationState(
            Guid moduleId,
            string definitionId,
            string nameLocalizationKey,
            ModuleCategory category,
            ModuleType type,
            GridPosition position,
            GridSize size,
            int currentDurability,
            int maximumDurability,
            ModuleTechnicalState technicalState,
            ModuleControlState controlState,
            ModulePowerMode requestedPowerMode,
            ModulePowerMode effectivePowerMode,
            PowerPriority powerPriority,
            int occupyingBrigadeCount,
            int workingBrigadeCount)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new ArgumentException(
                    "Definition id cannot be empty.",
                    nameof(definitionId));
            }

            if (string.IsNullOrWhiteSpace(
                    nameLocalizationKey))
            {
                throw new ArgumentException(
                    "Name localization key cannot be empty.",
                    nameof(nameLocalizationKey));
            }

            if (maximumDurability <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDurability));
            }

            if (currentDurability < 0 ||
                currentDurability > maximumDurability)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentDurability));
            }

            if (occupyingBrigadeCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occupyingBrigadeCount));
            }

            if (workingBrigadeCount < 0 ||
                workingBrigadeCount >
                occupyingBrigadeCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workingBrigadeCount));
            }

            ModuleId = moduleId;
            DefinitionId = definitionId;

            NameLocalizationKey =
                nameLocalizationKey;

            Category = category;
            Type = type;
            Position = position;
            Size = size;

            CurrentDurability =
                currentDurability;

            MaximumDurability =
                maximumDurability;

            TechnicalState =
                technicalState;

            ControlState =
                controlState;

            RequestedPowerMode =
                requestedPowerMode;

            EffectivePowerMode =
                effectivePowerMode;

            PowerPriority =
                powerPriority;

            OccupyingBrigadeCount =
                occupyingBrigadeCount;

            WorkingBrigadeCount =
                workingBrigadeCount;
        }
    }
}