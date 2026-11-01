
using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Неизменяемое намерение начать работу
    /// в ожидаемом модуле.
    ///
    /// Не изменяет operational state.
    /// </summary>
    public sealed class BrigadeWorkStartRequest
    {
        public Guid BrigadeId { get; }
        public Guid ModuleId { get; }

        public BrigadeWorkStartRequest(Guid brigadeId, Guid moduleId)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            BrigadeId = brigadeId;
            ModuleId = moduleId;
        }
    }
}
