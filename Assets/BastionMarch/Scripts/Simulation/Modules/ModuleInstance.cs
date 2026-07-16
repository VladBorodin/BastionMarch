using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Power;

namespace BastionMarch.Simulation.Modules
{
    public sealed class ModuleInstance
    {
        private readonly HashSet<Guid> _assignedBrigadeIds = new();

        public Guid Id { get; }
        public ModuleDefinition Definition { get; }

        public GridPosition Position { get; }

        public int CurrentDurability { get; private set; }

        public ModuleControlState ControlState { get; private set; }

        public ModulePowerMode RequestedPowerMode { get; private set; }

        public ModulePowerMode EffectivePowerMode { get; private set; }

        /// <summary>
        /// Текущий фактический режим.
        /// Оставлено как короткое представление EffectivePowerMode.
        /// </summary>
        public ModulePowerMode PowerMode =>
            EffectivePowerMode;

        public PowerPriority PowerPriority { get; private set; }

        public bool IsManuallyPoweredOff =>
            RequestedPowerMode == ModulePowerMode.Offline;

        public int RequestedContinuousPowerDemand =>
            GetContinuousPowerDemand(RequestedPowerMode);

        public int CurrentContinuousPowerDemand =>
            GetContinuousPowerDemand(EffectivePowerMode);
        
        public IReadOnlyCollection<Guid> AssignedBrigadeIds =>
            _assignedBrigadeIds;

        public ModuleTechnicalState TechnicalState
        {
            get
            {
                if (CurrentDurability <= 0)
                {
                    return ModuleTechnicalState.Destroyed;
                }

                if (CurrentDurability <=
                    Definition.CriticalDurabilityThreshold)
                {
                    return ModuleTechnicalState.Critical;
                }

                if (CurrentDurability <=
                    Definition.DamagedDurabilityThreshold)
                {
                    return ModuleTechnicalState.Damaged;
                }

                return ModuleTechnicalState.Operational;
            }
        }

        public ModuleInstance(
            ModuleDefinition definition,
            GridPosition position)
        {
            Definition = definition ??
                throw new ArgumentNullException(nameof(definition));

            Id = Guid.NewGuid();
            Position = position;
            CurrentDurability = definition.MaxDurability;
            ControlState = ModuleControlState.Friendly;
            RequestedPowerMode = ModulePowerMode.Standby;
            EffectivePowerMode = ModulePowerMode.Standby;
            PowerPriority = PowerPriority.Normal;
        }

        /// <summary>
        /// Устанавливает желаемый режим модуля.
        ///
        /// Это команда игрока. Автоматическое распределение энергии
        /// может понизить эффективный режим, но не должно изменять запрос.
        /// </summary>
        public void SetPowerMode(ModulePowerMode powerMode)
        {
            ValidatePowerMode(powerMode);

            RequestedPowerMode = powerMode;

            // Сразу отражаем приказ игрока.
            // После этого распределитель энергии может понизить режим.
            EffectivePowerMode = powerMode;
        }

        internal void ApplyPowerAllocation(
            ModulePowerMode effectivePowerMode)
        {
            ValidatePowerMode(effectivePowerMode);

            if (effectivePowerMode > RequestedPowerMode)
            {
                throw new InvalidOperationException(
                    "Effective power mode cannot exceed requested power mode.");
            }

            EffectivePowerMode = effectivePowerMode;
        }

        private int GetContinuousPowerDemand(
            ModulePowerMode powerMode)
        {
            switch (powerMode)
            {
                case ModulePowerMode.Offline:
                    return 0;

                case ModulePowerMode.Standby:
                    return Definition.IdlePowerConsumption;

                case ModulePowerMode.Active:
                    return Definition.ActivePowerConsumption;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported power mode: {powerMode}.");
            }
        }

        private static void ValidatePowerMode(
            ModulePowerMode powerMode)
        {
            if (!Enum.IsDefined(
                    typeof(ModulePowerMode),
                    powerMode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(powerMode));
            }
        }

        public void SetPowerPriority(PowerPriority powerPriority)
        {
            if (!Enum.IsDefined(
                    typeof(PowerPriority),
                    powerPriority))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(powerPriority));
            }

            PowerPriority = powerPriority;
        }

        public void ApplyDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            CurrentDurability = Math.Max(
                0,
                CurrentDurability - damage);
        }

        public void RestoreDurability(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentDurability = Math.Min(
                Definition.MaxDurability,
                CurrentDurability + amount);
        }

        public void SetControlState(ModuleControlState controlState)
        {
            ControlState = controlState;
        }

        public bool AssignBrigade(Guid brigadeId)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            return _assignedBrigadeIds.Add(brigadeId);
        }

        public bool RemoveBrigade(Guid brigadeId)
        {
            return _assignedBrigadeIds.Remove(brigadeId);
        }
    }
}