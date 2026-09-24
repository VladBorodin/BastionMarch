namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Текущее runtime-состояние процесса
    /// исполнения одного Order.
    /// </summary>
    public enum OrderExecutionStatus
    {
        Active,

        Completed,

        Failed
    }
}