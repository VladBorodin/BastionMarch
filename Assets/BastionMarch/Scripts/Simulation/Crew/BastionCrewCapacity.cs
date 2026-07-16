namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Проектные возможности бастиона по размещению
    /// и жизнеобеспечению экипажа.
    /// </summary>
    public sealed class BastionCrewCapacity
    {
        public int TotalBerths { get; }

        public int NominalAccommodationCapacity { get; }

        public int EmergencyAccommodationCapacity { get; }

        public int VentilationPersonnelCapacity { get; }

        public int NominalSupportedPersonnel =>
            System.Math.Min(
                NominalAccommodationCapacity,
                VentilationPersonnelCapacity);

        public int EmergencySupportedPersonnel =>
            System.Math.Min(
                EmergencyAccommodationCapacity,
                VentilationPersonnelCapacity);

        public BastionCrewCapacity(
            int totalBerths,
            int nominalAccommodationCapacity,
            int emergencyAccommodationCapacity,
            int ventilationPersonnelCapacity)
        {
            TotalBerths = totalBerths;

            NominalAccommodationCapacity =
                nominalAccommodationCapacity;

            EmergencyAccommodationCapacity =
                emergencyAccommodationCapacity;

            VentilationPersonnelCapacity =
                ventilationPersonnelCapacity;
        }
    }
}