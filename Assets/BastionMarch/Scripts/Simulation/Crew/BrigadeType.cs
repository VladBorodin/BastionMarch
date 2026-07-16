namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Основная специализация бригады.
    ///
    /// Тип определяет, для каких работ бригада подходит лучше всего.
    /// Он не запрещает аварийное назначение на другие работы.
    /// </summary>
    public enum BrigadeType
    {
        Recruit,
        Driver,
        Officer,
        Gunner,
        Mechanic,
        Engineer,
        Signal,
        Medical,
        Logistics,
        Assault
    }
}