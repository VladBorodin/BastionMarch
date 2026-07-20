namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Физическое состояние перехода.
    ///
    /// Само по себе состояние пока не определяет
    /// окончательную возможность перемещения.
    /// Это будет задачей системы проходимости.
    /// </summary>
    public enum ModulePassageState
    {
        Open,

        Closed,

        Locked,

        Blocked,

        Destroyed
    }
}