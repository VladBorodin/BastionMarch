
using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Пустой processor для Simulation,
    /// в которой ещё нет persistent world processes.
    /// </summary>
    public sealed class NoOpPersistentWorldPhaseProcessor :
        IPersistentWorldPhaseProcessor
    {
        public static NoOpPersistentWorldPhaseProcessor
            Instance { get; } =
                new NoOpPersistentWorldPhaseProcessor();

        private NoOpPersistentWorldPhaseProcessor()
        {
        }

        public PersistentWorldPhaseResult AdvancePhase(
            PersistentWorldPhaseContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            return new PersistentWorldPhaseResult(
                context,
                Array.Empty<Guid>());
        }
    }
}
