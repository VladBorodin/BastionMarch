namespace BastionMarch.Simulation.Turns.Planning
{
    public enum PhaseReservationFailureReason
    {
        None,

        ActionPhaseOutOfRange,

        BrigadeNotParticipant,

        ReservationAlreadyExists,

        BrigadePhaseAlreadyReserved
    }
}