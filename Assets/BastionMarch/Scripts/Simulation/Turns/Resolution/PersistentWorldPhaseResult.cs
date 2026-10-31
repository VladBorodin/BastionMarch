
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемый минимальный результат обработки
    /// persistent world processes в Action Phase.
    ///
    /// Содержит только identity обработанных
    /// процессов, но не их mutable world state.
    /// </summary>
    public sealed class PersistentWorldPhaseResult
    {
        public int TurnNumber { get; }

        public int ActionPhase { get; }

        public IReadOnlyList<Guid> ProcessedProcessIds
        {
            get;
        }

        public int ProcessedProcessCount =>
            ProcessedProcessIds.Count;

        public PersistentWorldPhaseResult(
            PersistentWorldPhaseContext context,
            IEnumerable<Guid> processedProcessIds)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            if (processedProcessIds == null)
            {
                throw new ArgumentNullException(
                    nameof(processedProcessIds));
            }

            Guid[] ids = processedProcessIds.ToArray();

            if (ids.Any(id => id == Guid.Empty))
            {
                throw new ArgumentException(
                    "Process id cannot be empty.",
                    nameof(processedProcessIds));
            }

            if (ids.Distinct().Count() != ids.Length)
            {
                throw new ArgumentException(
                    "Processed process ids must be unique.",
                    nameof(processedProcessIds));
            }

            TurnNumber = context.TurnNumber;
            ActionPhase = context.ActionPhase;

            ProcessedProcessIds =
                new ReadOnlyCollection<Guid>(
                    ids.OrderBy(id => id).ToArray());
        }
    }
}
