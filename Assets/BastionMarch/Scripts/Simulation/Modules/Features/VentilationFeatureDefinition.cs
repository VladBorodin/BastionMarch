using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает возможности вентиляционного отсека.
    ///
    /// На текущем этапе используется для расчёта допустимой
    /// численности экипажа. Позднее также будет участвовать
    /// в удалении дыма и тепла.
    /// </summary>
    public sealed class VentilationFeatureDefinition
        : IModuleFeatureDefinition
    {
        public int SupportedPersonnelCapacity { get; }

        public int SmokeExtractionPerTurn { get; }

        public int HeatRemovalPerTurn { get; }

        public VentilationFeatureDefinition(
            int supportedPersonnelCapacity,
            int smokeExtractionPerTurn,
            int heatRemovalPerTurn)
        {
            if (supportedPersonnelCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supportedPersonnelCapacity));
            }

            if (smokeExtractionPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(smokeExtractionPerTurn));
            }

            if (heatRemovalPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heatRemovalPerTurn));
            }

            SupportedPersonnelCapacity =
                supportedPersonnelCapacity;

            SmokeExtractionPerTurn =
                smokeExtractionPerTurn;

            HeatRemovalPerTurn =
                heatRemovalPerTurn;
        }
    }
}