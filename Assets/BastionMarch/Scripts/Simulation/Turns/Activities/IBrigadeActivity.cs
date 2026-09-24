using System;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Общий контракт постоянной деятельности Brigade.
    ///
    /// Activity не является Order и не имеет
    /// фиксированной длительности в Action Phase.
    ///
    /// Выполнение Activity будет определяться
    /// resolver'ом в свободную от blocking Order фазу.
    /// </summary>
    public interface IBrigadeActivity
    {
        Guid BrigadeId
        {
            get;
        }
    }
}