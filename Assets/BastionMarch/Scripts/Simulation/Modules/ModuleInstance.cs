using System;
using System.Collections.Generic;

namespace BastionMarch.Simulation.Modules
{
    public sealed class ModuleInstance
    {
        private readonly HashSet<Guid> _assignedBrigadeIds = new();

        public Guid Id { get; }
        public ModuleDefinition Definition { get; }

        public GridPosition Position { get; private set; }

        public int CurrentDurability { get; private set; }

        public ModuleControlState ControlState { get; private set; }

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
        }

        public void SetPosition(GridPosition position)
        {
            Position = position;
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