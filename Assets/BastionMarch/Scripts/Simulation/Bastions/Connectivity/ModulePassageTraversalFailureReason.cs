namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Стабильные доменные причины невозможности прохода.
    ///
    /// Пользовательские формулировки должны находиться
    /// в локализации Presentation-слоя.
    /// </summary>
    public enum ModulePassageTraversalFailureReason
    {
        None,

        PassageNotFound,

        SourceModuleNotFound,

        TargetModuleNotFound,

        SameModule,

        PassageDoesNotConnectModules,

        DirectionNotAllowed,

        PassageClosed,

        PassageLocked,

        PassageBlocked,

        PassageDestroyed
    }
}