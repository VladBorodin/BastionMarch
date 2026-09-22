using System;
using BastionMarch.Simulation.Turns.Orders;

namespace BastionMarch.Simulation.Turns.Planning
{
    public sealed class TurnPlanOrderResult
    {
        public bool IsSuccess =>
            FailureReason ==
            TurnPlanOrderFailureReason.None;

        public TurnPlanOrderFailureReason
            FailureReason
        {
            get;
        }

        public ITurnOrder Order
        {
            get;
        }

        private TurnPlanOrderResult(
            TurnPlanOrderFailureReason
                failureReason,
            ITurnOrder order)
        {
            FailureReason =
                failureReason;

            Order =
                order;
        }

        public static TurnPlanOrderResult Success(
            ITurnOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(
                    nameof(order));
            }

            return new TurnPlanOrderResult(
                TurnPlanOrderFailureReason.None,
                order);
        }

        public static TurnPlanOrderResult Failure(
            TurnPlanOrderFailureReason
                failureReason)
        {
            if (failureReason ==
                TurnPlanOrderFailureReason.None)
            {
                throw new ArgumentException(
                    "Failure result must contain " +
                    "a failure reason.",
                    nameof(failureReason));
            }

            return new TurnPlanOrderResult(
                failureReason,
                order: null);
        }
    }
}