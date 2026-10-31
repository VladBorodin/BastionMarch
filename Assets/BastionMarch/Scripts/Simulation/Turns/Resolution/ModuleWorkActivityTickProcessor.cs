using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns.Activities;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Read-only проверка и фиксация одного
    /// логического tick текущей работы.
    ///
    /// Источник истины — Bastion.
    /// </summary>
    public static class ModuleWorkActivityTickProcessor
    {
        public static ModuleWorkActivityTick Prepare(
            BrigadeActivityPhaseIntent intent,
            Bastion bastion)
        {
            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            if (bastion == null)
                throw new ArgumentNullException(nameof(bastion));

            if (!(intent.Activity is ModuleWorkActivity activity))
            {
                throw new ArgumentException(
                    "Expected ModuleWorkActivity.",
                    nameof(intent));
            }

            ModuleWorkActivity current =
                ModuleWorkActivityResolver.Resolve(bastion, activity.BrigadeId);

            if (current == null || current.ModuleId != activity.ModuleId)
            {
                throw new InvalidOperationException(
                    "Selected ModuleWorkActivity is no longer valid.");
            }

            if (!bastion.TryGetBrigade(activity.BrigadeId, out Brigade brigade) ||
                brigade.IsDisbanded)
            {
                throw new InvalidOperationException(
                    "Working brigade is no longer available.");
            }

            return new ModuleWorkActivityTick(
                activity.BrigadeId,
                activity.ModuleId,
                intent.ActionPhase,
                brigade.CurrentPersonnel);
        }
    }
}