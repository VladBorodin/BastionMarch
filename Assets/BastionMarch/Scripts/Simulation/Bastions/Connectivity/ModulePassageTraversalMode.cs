namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Разрешённое направление движения через переход.
    ///
    /// Source и Target соответствуют идентификаторам,
    /// сохранённым в ModulePassage.
    /// </summary>
    public enum ModulePassageTraversalMode
    {
        Bidirectional,

        SourceToTargetOnly,

        TargetToSourceOnly
    }
}