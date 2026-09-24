using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Order, чья доменная область относится
    /// к одной конкретной бригаде.
    /// </summary>
    public interface IBrigadeScopedOrder :
        ITurnOrder
    {
        Guid BrigadeId
        {
            get;
        }
    }
}