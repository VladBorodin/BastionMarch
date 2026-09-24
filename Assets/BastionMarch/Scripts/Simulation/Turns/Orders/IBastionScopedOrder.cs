using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Order, чья доменная область относится
    /// к одному Bastion.
    ///
    /// Затронутые этим Order бригады
    /// определяются PhaseReservation,
    /// а не хранятся здесь отдельным mutable списком.
    /// </summary>
    public interface IBastionScopedOrder :
        ITurnOrder
    {
        Guid BastionId
        {
            get;
        }
    }
}