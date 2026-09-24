namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Причины, по которым целостный TurnPlanDraft
    /// нельзя подтвердить для текущего TurnCycle.
    /// </summary>
    public enum TurnPlanFailureReason
    {
        None,

        PlanningNotActive,

        TurnNumberMismatch,

        ActionPhaseCountMismatch,

        ParticipantSnapshotMismatch,

        BrigadeOrderParticipantMismatch,

        ReservationOrderNotFound,

        BrigadeOrderReservationMismatch,

        OrderHasNoReservations,

        OrderScheduledTooManyPhases
    }
}