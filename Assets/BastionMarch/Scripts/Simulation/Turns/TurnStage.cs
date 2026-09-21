namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Крупная стадия жизненного цикла хода.
    ///
    /// Planning и TurnEnd не являются
    /// игровыми Action Phase.
    /// </summary>
    public enum TurnStage
    {
        Planning,

        ActionResolution,

        TurnEnd
    }
}