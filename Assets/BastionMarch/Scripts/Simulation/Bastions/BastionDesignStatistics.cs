namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Неизменяемый снимок проектных характеристик бастиона.
    ///
    /// Эти значения описывают установленную конструкцию и не учитывают
    /// повреждения, нехватку экипажа, топлива или энергии.
    /// </summary>
    public sealed class BastionDesignStatistics
    {
        public int ModuleCount { get; }

        public int OccupiedCellCount { get; }

        public long TotalMassKg { get; }

        public long TotalCost { get; }

        public long TotalMaxDurability { get; }

        public long TotalIdlePowerConsumption { get; }

        public long TotalActivePowerConsumption { get; }

        public long TotalHeatGeneration { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        public long TotalHorsePower { get; }

        public BastionDesignStatistics(
        int moduleCount,
        int occupiedCellCount,
        long totalMassKg,
        long totalCost,
        long totalMaxDurability,
        long totalIdlePowerConsumption,
        long totalActivePowerConsumption,
        long totalHeatGeneration,
        int minimumPersonnel,
        int optimalPersonnel,
        int maximumUsefulPersonnel,
        long totalHorsePower)
    {
        ModuleCount = moduleCount;
        OccupiedCellCount = occupiedCellCount;
        TotalMassKg = totalMassKg;
        TotalCost = totalCost;
        TotalMaxDurability = totalMaxDurability;
        TotalIdlePowerConsumption = totalIdlePowerConsumption;
        TotalActivePowerConsumption = totalActivePowerConsumption;
        TotalHeatGeneration = totalHeatGeneration;
        MinimumPersonnel = minimumPersonnel;
        OptimalPersonnel = optimalPersonnel;
        MaximumUsefulPersonnel = maximumUsefulPersonnel;
        TotalHorsePower = totalHorsePower;
    }
    }
}