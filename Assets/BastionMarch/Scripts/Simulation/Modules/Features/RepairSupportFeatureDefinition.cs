using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает общую ремонтную мощность мастерской.
    /// Мастерская поддерживает ремонт по всему бастиону,
    /// если до повреждённого отсека можно доставить людей,
    /// инструменты и запасные части.
    /// </summary>
    public sealed class RepairSupportFeatureDefinition
        : IModuleFeatureDefinition
    {
        /// <summary>
        /// Количество ремонтных очков, создаваемых за ход
        /// при оптимальном обслуживании.
        /// </summary>
        public int RepairPointsPerTurn { get; }

        /// <summary>
        /// Максимальное количество независимых ремонтных работ,
        /// которые мастерская может поддерживать одновременно.
        /// </summary>
        public int MaximumConcurrentJobs { get; }

        /// <summary>
        /// Внутренний запас деталей и расходных материалов.
        /// </summary>
        public int SparePartsCapacityUnits { get; }

        public RepairSupportFeatureDefinition(
            int repairPointsPerTurn,
            int maximumConcurrentJobs,
            int sparePartsCapacityUnits)
        {
            if (repairPointsPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(repairPointsPerTurn));
            }

            if (maximumConcurrentJobs <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumConcurrentJobs));
            }

            if (sparePartsCapacityUnits < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sparePartsCapacityUnits));
            }

            RepairPointsPerTurn = repairPointsPerTurn;
            MaximumConcurrentJobs = maximumConcurrentJobs;
            SparePartsCapacityUnits = sparePartsCapacityUnits;
        }
    }
}