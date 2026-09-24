using System;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Базовая immutable identity деятельности Brigade.
    ///
    /// Не хранит текущее operational состояние
    /// и не является registry текущих Activities.
    /// </summary>
    public abstract class BrigadeActivity :
        IBrigadeActivity
    {
        public Guid BrigadeId
        {
            get;
        }

        protected BrigadeActivity(
            Guid brigadeId)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            BrigadeId =
                brigadeId;
        }
    }
}