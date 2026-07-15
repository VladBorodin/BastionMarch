namespace BastionMarch.Simulation.Modules
{
    public enum ModuleCategory
    {
        Structure,
        Mobility,
        Power,
        Combat,
        Logistics,
        Maintenance,
        Command,
        CrewSupport,
        Medical,
        Utility
    }

    public enum ModuleType
    {
        MachineRoom,
        RepairBay,
        AmmoStorage,
        CommandPost,
        CrewQuarters,
        Ventilation,
        WeaponPlatform,
        MedicalBay,
        Corridor,
        Stairwell
    }

    public enum ModuleTechnicalState
    {
        Operational,
        Damaged,
        Critical,
        Destroyed
    }

    public enum ModuleControlState
    {
        Friendly,
        Contested,
        Occupied
    }
}