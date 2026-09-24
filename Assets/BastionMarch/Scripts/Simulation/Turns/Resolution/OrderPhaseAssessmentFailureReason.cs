namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Универсальные причины невозможности
    /// execution tick конкретного Order.
    ///
    /// Конкретные игровые Orders позднее добавляют
    /// собственные domain assessments.
    /// </summary>
    public enum OrderPhaseAssessmentFailureReason
    {
        None,

        ExecutionOrderMismatch,

        ExecutionDurationMismatch,

        ExecutionAlreadyCompleted,

        ExecutionAlreadyFailed
    }
}