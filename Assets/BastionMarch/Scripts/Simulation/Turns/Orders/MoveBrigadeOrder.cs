
using System;

namespace BastionMarch.Simulation.Turns.Orders
{
    /// <summary>
    /// Неизменяемый приказ переместить одну бригаду
    /// через конкретный переход между соседними модулями.
    ///
    /// M1: один переход занимает одну Action Phase.
    ///
    /// Не проверяет состояние Bastion и не выполняет
    /// перемещение. Runtime revalidation и mutation
    /// принадлежат Bastion.
    /// </summary>
    public sealed class MoveBrigadeOrder : BrigadeTurnOrder
    {
        public const int MovementRequiredPhases = 1;

        public Guid SourceModuleId { get; }
        public Guid TargetModuleId { get; }
        public Guid PassageId { get; }

        public MoveBrigadeOrder(
            Guid orderId,
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId)
            : base(orderId, MovementRequiredPhases, brigadeId)
        {
            if (sourceModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Source module id cannot be empty.",
                    nameof(sourceModuleId));
            }

            if (targetModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Target module id cannot be empty.",
                    nameof(targetModuleId));
            }

            if (sourceModuleId == targetModuleId)
            {
                throw new ArgumentException(
                    "Move must connect different modules.",
                    nameof(targetModuleId));
            }

            if (passageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Passage id cannot be empty.",
                    nameof(passageId));
            }

            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;
            PassageId = passageId;
        }
    }
}
