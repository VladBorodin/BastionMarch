namespace BastionMarch.Simulation.Power
{
    /// <summary>
    /// Приоритет модуля при будущем автоматическом
    /// отключении потребителей из-за дефицита энергии.
    /// </summary>
    public enum PowerPriority
    {
        Critical,
        High,
        Normal,
        Low
    }
}