using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает способность машинного отсека создавать тяговую мощность.
    /// </summary>
    public sealed class PropulsionFeatureDefinition
        : IModuleFeatureDefinition
    {
        public int HorsePower { get; }

        /// <summary>
        /// Расход топлива при активной работе за один ход.
        /// Конкретные единицы топлива определим в системе ресурсов.
        /// </summary>
        public int FuelConsumptionPerTurn { get; }

        /// <summary>
        /// Базовый износ при работе за один ход.
        /// Фактический износ позднее будет зависеть от нагрузки,
        /// состояния ходовой и условий движения.
        /// </summary>
        public int BaseWearPerTurn { get; }

        public PropulsionFeatureDefinition(
            int horsePower,
            int fuelConsumptionPerTurn,
            int baseWearPerTurn)
        {
            if (horsePower <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(horsePower),
                    "Horse power must be greater than zero.");
            }

            if (fuelConsumptionPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fuelConsumptionPerTurn));
            }

            if (baseWearPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseWearPerTurn));
            }

            HorsePower = horsePower;
            FuelConsumptionPerTurn = fuelConsumptionPerTurn;
            BaseWearPerTurn = baseWearPerTurn;
        }
    }
}