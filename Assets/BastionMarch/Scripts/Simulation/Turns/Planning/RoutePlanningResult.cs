
using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    public sealed class RoutePlanningResult
    {
        public BrigadeRoutePlan RoutePlan { get; }

        public MovePlanningFailureReason FailureReason { get; }

        public bool IsSuccess =>
            FailureReason == MovePlanningFailureReason.None;

        private RoutePlanningResult(
            BrigadeRoutePlan routePlan,
            MovePlanningFailureReason failureReason)
        {
            RoutePlan = routePlan;
            FailureReason = failureReason;
        }

        public static RoutePlanningResult Success(
            BrigadeRoutePlan routePlan)
        {
            if (routePlan == null)
                throw new ArgumentNullException(nameof(routePlan));

            return new RoutePlanningResult(
                routePlan,
                MovePlanningFailureReason.None);
        }

        public static RoutePlanningResult Rejected(
            MovePlanningFailureReason reason)
        {
            if (reason == MovePlanningFailureReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));

            return new RoutePlanningResult(null, reason);
        }
    }
}
