
namespace BastionMarch.Simulation.Turns.Planning
{
    public enum MovePlanningFailureReason
    {
        None,
        PlanningNotActive,
        ActionPhaseOutOfRange,
        BrigadeNotParticipant,
        BrigadeUnavailable,
        BrigadeNotDeployed,
        BrigadeMoveAlreadyPlanned,
        BrigadePhaseReserved,
        TargetModuleNotFound,
        SameModule,
        NoTraversablePassage,
        PlanRegistrationFailed
    }
}
