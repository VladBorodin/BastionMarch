using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Проверяет возможность выполнения MoveBrigadeOrder
    /// относительно актуального состояния Bastion.
    ///
    /// Не изменяет мир и execution progress.
    /// </summary>
    public static class MoveBrigadeOrderAssessor
    {
        public static BrigadeMoveAssessment Assess(
            OrderPhaseIntent intent,
            Bastion bastion)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (bastion == null)
            {
                throw new ArgumentNullException(nameof(bastion));
            }

            if (!(intent.Order is MoveBrigadeOrder order))
            {
                throw new ArgumentException(
                    "Intent must contain MoveBrigadeOrder.",
                    nameof(intent));
            }

            if (intent.ReservedBrigadeCount != 1 ||
                intent.ReservedBrigadeIds[0] != order.BrigadeId)
            {
                throw new ArgumentException(
                    "MoveBrigadeOrder requires exactly one " +
                    "reservation for its own brigade.",
                    nameof(intent));
            }

            return bastion.AssessBrigadeMove(
                order.BrigadeId,
                order.SourceModuleId,
                order.TargetModuleId,
                order.PassageId);
        }
    }
}