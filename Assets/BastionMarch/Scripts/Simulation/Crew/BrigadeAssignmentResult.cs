using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Crew
{
    public sealed class BrigadeAssignmentResult
    {
        public bool IsSuccess =>
            FailureReason == BrigadeAssignmentFailureReason.None;

        public BrigadeAssignmentFailureReason FailureReason { get; }

        public Brigade Brigade { get; }

        public ModuleInstance Module { get; }

        private BrigadeAssignmentResult(
            BrigadeAssignmentFailureReason failureReason,
            Brigade brigade,
            ModuleInstance module)
        {
            FailureReason = failureReason;
            Brigade = brigade;
            Module = module;
        }

        public static BrigadeAssignmentResult Success(
            Brigade brigade,
            ModuleInstance module)
        {
            if (brigade == null)
            {
                throw new ArgumentNullException(nameof(brigade));
            }

            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            return new BrigadeAssignmentResult(
                BrigadeAssignmentFailureReason.None,
                brigade,
                module);
        }

        public static BrigadeAssignmentResult Failure(
            BrigadeAssignmentFailureReason reason)
        {
            if (reason == BrigadeAssignmentFailureReason.None)
            {
                throw new ArgumentException(
                    "Failed assignment must contain a failure reason.",
                    nameof(reason));
            }

            return new BrigadeAssignmentResult(
                reason,
                brigade: null,
                module: null);
        }
    }
}