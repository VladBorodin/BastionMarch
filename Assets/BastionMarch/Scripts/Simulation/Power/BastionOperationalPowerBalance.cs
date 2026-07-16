using System;

namespace BastionMarch.Simulation.Power
{
    /// <summary>
    /// Текущий непрерывный энергетический баланс бастиона.
    /// В отличие от проектного баланса учитывает режимы питания,
    /// уничтожение модулей и контроль над отсеками.
    /// Пока не учитывает экипаж, топливо и перегрев.
    /// </summary>
    public sealed class BastionOperationalPowerBalance
    {
        public long AvailablePowerGeneration { get; }

        public long CurrentPowerDemand { get; }

        public long PowerReserve =>
            AvailablePowerGeneration - CurrentPowerDemand;

        public bool IsBalanced =>
            PowerReserve >= 0;

        public BastionOperationalPowerBalance(
            long availablePowerGeneration,
            long currentPowerDemand)
        {
            if (availablePowerGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availablePowerGeneration));
            }

            if (currentPowerDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentPowerDemand));
            }

            AvailablePowerGeneration =
                availablePowerGeneration;

            CurrentPowerDemand =
                currentPowerDemand;
        }
    }
}