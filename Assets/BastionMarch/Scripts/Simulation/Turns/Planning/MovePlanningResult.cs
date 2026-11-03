
using System;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Planning
{
    public sealed class MovePlanningResult
    {
        public MoveBrigadeOrder Order { get; }

        public MovePlanningFailureReason FailureReason { get; }

        public bool IsSuccess => FailureReason == MovePlanningFailureReason.None;

        private MovePlanningResult(
            MoveBrigadeOrder order,
            MovePlanningFailureReason failureReason)
        {
            Order = order;
            FailureReason = failureReason;
        }

        public static MovePlanningResult Success(
            MoveBrigadeOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new MovePlanningResult(
                order, MovePlanningFailureReason.None);
        }

        public static MovePlanningResult Rejected(
            MovePlanningFailureReason reason)
        {
            if (reason == MovePlanningFailureReason.None)
                throw new ArgumentException(
                    "Rejection requires a reason.",
                    nameof(reason));

            return new MovePlanningResult(null, reason);
        }
    }
}
