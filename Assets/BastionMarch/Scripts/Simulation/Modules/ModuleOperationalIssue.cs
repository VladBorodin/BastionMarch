using System;

namespace BastionMarch.Simulation.Modules
{
    /// <summary>
    /// Причины, влияющие на выполнение модулем активной работы.
    ///
    /// Несколько причин могут действовать одновременно.
    /// </summary>
    [Flags]
    public enum ModuleOperationalIssue
    {
        None = 0,

        Damaged = 1 << 0,

        CriticalDamage = 1 << 1,

        Destroyed = 1 << 2,

        NotFriendlyControlled = 1 << 3,

        InactivePowerMode = 1 << 4,

        InsufficientQualifiedPersonnel = 1 << 5,

        Overcrowded = 1 << 6
    }
}