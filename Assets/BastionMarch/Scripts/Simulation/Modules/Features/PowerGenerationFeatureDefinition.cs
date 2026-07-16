using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает способность отсека производить электрическую энергию.
    ///
    /// Значения пока выражаются в условных единицах мощности.
    /// Выбор физических единиц будет сделан после первичной балансировки.
    /// </summary>
    public sealed class PowerGenerationFeatureDefinition
        : IModuleFeatureDefinition
    {
        /// <summary>
        /// Максимальная номинальная мощность генератора.
        ///
        /// Это валовая выработка. Собственное энергопотребление
        /// генераторного отсека учитывается через ModuleDefinition.
        /// </summary>
        public int MaximumPowerOutput { get; }

        /// <summary>
        /// Расход топлива за ход при работе на полной мощности.
        /// </summary>
        public int FuelConsumptionPerTurn { get; }

        public PowerGenerationFeatureDefinition(
            int maximumPowerOutput,
            int fuelConsumptionPerTurn)
        {
            if (maximumPowerOutput <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPowerOutput),
                    "Power output must be greater than zero.");
            }

            if (fuelConsumptionPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fuelConsumptionPerTurn));
            }

            MaximumPowerOutput = maximumPowerOutput;
            FuelConsumptionPerTurn = fuelConsumptionPerTurn;
        }
    }
}