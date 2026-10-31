using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемое подтверждение участия работающей
    /// бригады в работе модуля в конкретной Action Phase.
    ///
    /// Не представляет создание ресурсов или ремонт.
    /// Эти эффекты появятся в соответствующих системах.
    /// </summary>
    public sealed class ModuleWorkActivityTick
    {
        public Guid BrigadeId { get; }
        public Guid ModuleId { get; }
        public int ActionPhase { get; }
        public int WorkingPersonnel { get; }

        internal ModuleWorkActivityTick(
            Guid brigadeId,
            Guid moduleId,
            int actionPhase,
            int workingPersonnel)
        {
            if (brigadeId == Guid.Empty)
                throw new ArgumentException("Empty brigade id.", nameof(brigadeId));

            if (moduleId == Guid.Empty)
                throw new ArgumentException("Empty module id.", nameof(moduleId));

            if (actionPhase < 1)
                throw new ArgumentOutOfRangeException(nameof(actionPhase));

            if (workingPersonnel < 1)
                throw new ArgumentOutOfRangeException(nameof(workingPersonnel));

            BrigadeId = brigadeId;
            ModuleId = moduleId;
            ActionPhase = actionPhase;
            WorkingPersonnel = workingPersonnel;
        }
    }
}