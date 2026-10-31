using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Получает текущую ModuleWorkActivity
    /// из фактического operational state Bastion.
    ///
    /// Не хранит назначения и не изменяет мир.
    /// </summary>
    public static class ModuleWorkActivityResolver
    {
        public static ModuleWorkActivity Resolve(Bastion bastion, Guid brigadeId)
        {
            if (bastion == null)
            {
                throw new ArgumentNullException(nameof(bastion));
            }

            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            if (!bastion.TryGetBrigade(brigadeId, out Brigade brigade) ||
                brigade.IsDisbanded)
            {
                return null;
            }

            if (!bastion.TryGetBrigadeOperationalState(
                    brigadeId, out BrigadeOperationalState state))
            {
                throw new InvalidOperationException(
                    "Registered brigade has no operational state.");
            }

            if (!state.IsWorking)
            {
                return null;
            }

            if (!state.CurrentModuleId.HasValue ||
                !bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance module) ||
                module.Id != state.CurrentModuleId.Value ||
                !module.OccupyingBrigadeIds.Contains(brigadeId) ||
                !module.WorkingBrigadeIds.Contains(brigadeId))
            {
                throw new InvalidOperationException(
                    "Brigade work state is inconsistent.");
            }

            return new ModuleWorkActivity(brigadeId, module.Id);
        }
    }
}