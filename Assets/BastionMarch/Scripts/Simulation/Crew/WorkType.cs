namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Вид работы, создаваемой установленными отсеками.
    /// WorkType описывает работу, а не класс бригады.
    /// BrigadeType определяет пригодность бригады к этой работе.
    /// </summary>
    public enum WorkType
    {
        General,
        Driving,
        Command,
        Gunnery,
        Mechanical,
        Engineering,
        Communications,
        Medical,
        Logistics,
        Assault
    }
}