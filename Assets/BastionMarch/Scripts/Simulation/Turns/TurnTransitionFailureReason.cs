namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Причина отказа операции,
    /// изменяющей временное состояние TurnCycle.
    /// </summary>
    public enum TurnTransitionFailureReason
    {
        None,

        PlanningAlreadyConfirmed,

        ActionResolutionNotActive
    }
}