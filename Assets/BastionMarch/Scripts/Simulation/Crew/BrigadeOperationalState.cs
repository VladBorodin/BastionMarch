using System;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Снимок текущего оперативного положения бригады.
    /// Принадлежность бригады бастиону и её физическое
    /// размещение являются разными состояниями.
    /// </summary>
    public sealed class BrigadeOperationalState
    {
        public Guid BrigadeId { get; }

        public Guid? CurrentModuleId { get; }

        public bool IsDeployed =>
            CurrentModuleId.HasValue;

        public bool IsWorking { get; }

        public BrigadeOperationalState(
            Guid brigadeId,
            Guid? currentModuleId,
            bool isWorking)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            if (isWorking &&
                !currentModuleId.HasValue)
            {
                throw new ArgumentException(
                    "A brigade cannot work without being deployed.",
                    nameof(isWorking));
            }

            BrigadeId = brigadeId;
            CurrentModuleId = currentModuleId;
            IsWorking = isWorking;
        }
    }
}