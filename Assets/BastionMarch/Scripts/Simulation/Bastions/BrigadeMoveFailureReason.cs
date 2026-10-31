
namespace BastionMarch.Simulation.Bastions
{
    public enum BrigadeMoveFailureReason
    {
        None,
        BrigadeNotFound,
        BrigadeDisbanded,
        BrigadeNotDeployed,
        SourceMismatch,
        SourceModuleNotFound,
        TargetModuleNotFound,
        SameModule,
        ModulesNotAdjacent,
        SourceOccupancyMissing,
        WorkingStateInconsistent,
        TargetAlreadyOccupied,
        PassageNotFound,
        PassageTraversalRejected
    }
}
