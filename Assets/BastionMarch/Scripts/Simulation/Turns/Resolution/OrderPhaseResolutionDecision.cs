namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Решение после текущего этапа
    /// conflict resolution.
    ///
    /// Реальные conflict-specific варианты
    /// добавляются только вместе с конкретными
    /// игровыми конфликтами.
    /// </summary>
    public enum OrderPhaseResolutionDecision
    {
        Approved,

        RejectedByAssessment
    }
}