namespace BastionMarch.Simulation.Bastions
{
    public enum ModulePassagePlacementFailureReason
    {
        None,

        SourceModuleNotFound,

        TargetModuleNotFound,

        SameModule,

        ModulesNotAdjacent,

        BoundaryNotShared,

        PassageTypeIncompatibleWithBoundary,

        BoundaryAlreadyHasPassage
    }
}