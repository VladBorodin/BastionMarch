using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Общий неизменяемый контракт приказа.
    ///
    /// Конкретный Order самостоятельно определяет
    /// своего исполнителя, scope, цели и параметры.
    /// </summary>
    public interface ITurnOrder
    {
        Guid OrderId
        {
            get;
        }

        int RequiredPhases
        {
            get;
        }
    }
}