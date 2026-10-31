using System;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Неизменяемое представление работы бригады
    /// в конкретном модуле.
    ///
    /// Не хранит состояние выполнения и не запускает работу.
    /// Источник истины — operational state Bastion.
    /// </summary>
    public sealed class ModuleWorkActivity : BrigadeActivity
    {
        public Guid ModuleId { get; }

        public ModuleWorkActivity(Guid brigadeId, Guid moduleId)
            : base(brigadeId)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            ModuleId = moduleId;
        }
    }
}