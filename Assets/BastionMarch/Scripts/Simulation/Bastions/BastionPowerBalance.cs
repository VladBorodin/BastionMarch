using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Номинальный энергетический баланс конструкции бастиона.
    /// Не учитывает повреждения, наличие топлива, экипаж и фактическое включение отдельных модулей.
    /// </summary>
    public sealed class BastionPowerBalance
    {
        public long TotalPowerGeneration { get; }

        public long TotalIdlePowerDemand { get; }

        public long TotalActivePowerDemand { get; }

        public long IdlePowerReserve =>
            TotalPowerGeneration - TotalIdlePowerDemand;

        public long ActivePowerReserve =>
            TotalPowerGeneration - TotalActivePowerDemand;

        public bool CanSustainIdleLoad =>
            IdlePowerReserve >= 0;

        public bool CanSustainFullLoad =>
            ActivePowerReserve >= 0;

        public BastionPowerBalance(
            long totalPowerGeneration,
            long totalIdlePowerDemand,
            long totalActivePowerDemand)
        {
            if (totalPowerGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalPowerGeneration));
            }

            if (totalIdlePowerDemand < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalIdlePowerDemand));
            }

            if (totalActivePowerDemand < totalIdlePowerDemand)
            {
                throw new ArgumentException(
                    "Active power demand cannot be lower than idle demand.",
                    nameof(totalActivePowerDemand));
            }

            TotalPowerGeneration = totalPowerGeneration;
            TotalIdlePowerDemand = totalIdlePowerDemand;
            TotalActivePowerDemand = totalActivePowerDemand;
        }
    }
}