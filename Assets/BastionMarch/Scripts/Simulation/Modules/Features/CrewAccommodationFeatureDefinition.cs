using System;

namespace BastionMarch.Simulation.Modules.Features
{
    /// <summary>
    /// Описывает возможности жилого отсека.
    ///
    /// Количество коек может быть ниже номинальной вместимости,
    /// поскольку экипаж отдыхает посменно и несёт вахты.
    /// </summary>
    public sealed class CrewAccommodationFeatureDefinition
        : IModuleFeatureDefinition
    {
        public int BerthCount { get; }

        public int NominalPersonnelCapacity { get; }

        public int EmergencyPersonnelCapacity { get; }

        public CrewAccommodationFeatureDefinition(
            int berthCount,
            int nominalPersonnelCapacity,
            int emergencyPersonnelCapacity)
        {
            if (berthCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(berthCount));
            }

            if (nominalPersonnelCapacity < berthCount)
            {
                throw new ArgumentException(
                    "Nominal capacity cannot be lower than berth count.",
                    nameof(nominalPersonnelCapacity));
            }

            if (emergencyPersonnelCapacity <
                nominalPersonnelCapacity)
            {
                throw new ArgumentException(
                    "Emergency capacity cannot be lower than nominal capacity.",
                    nameof(emergencyPersonnelCapacity));
            }

            BerthCount = berthCount;
            NominalPersonnelCapacity =
                nominalPersonnelCapacity;
            EmergencyPersonnelCapacity =
                emergencyPersonnelCapacity;
        }
    }
}