using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает вместимость и пропускную способность
    /// склада боеприпасов.
    /// </summary>
    public sealed class AmmoStorageFeatureDefinition
        : IModuleFeatureDefinition
    {
        /// <summary>
        /// Вместимость выражается в единицах объёма,
        /// а не в количестве снарядов.
        ///
        /// Разные боеприпасы позднее будут занимать
        /// разный объём.
        /// </summary>
        public int CapacityVolumeUnits { get; }

        /// <summary>
        /// Максимальный объём боеприпасов, который склад
        /// может передать в логистическую сеть за ход.
        /// </summary>
        public int OutputThroughputPerTurn { get; }

        public AmmoStorageFeatureDefinition(
            int capacityVolumeUnits,
            int outputThroughputPerTurn)
        {
            if (capacityVolumeUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacityVolumeUnits));
            }

            if (outputThroughputPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outputThroughputPerTurn));
            }

            if (outputThroughputPerTurn > capacityVolumeUnits)
            {
                throw new ArgumentException(
                    "Storage throughput cannot exceed its total capacity.");
            }

            CapacityVolumeUnits = capacityVolumeUnits;
            OutputThroughputPerTurn = outputThroughputPerTurn;
        }
    }
}