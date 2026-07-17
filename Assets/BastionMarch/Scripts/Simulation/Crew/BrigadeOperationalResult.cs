using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Crew
{
    public sealed class BrigadeOperationalResult
    {
        public bool IsSuccess =>
            FailureReason ==
            BrigadeOperationalFailureReason.None;

        public BrigadeOperationalFailureReason FailureReason
        {
            get;
        }

        public Brigade Brigade { get; }

        public ModuleInstance Module { get; }

        private BrigadeOperationalResult(
            BrigadeOperationalFailureReason failureReason,
            Brigade brigade,
            ModuleInstance module)
        {
            FailureReason = failureReason;
            Brigade = brigade;
            Module = module;
        }

        public static BrigadeOperationalResult Success(
            Brigade brigade,
            ModuleInstance module)
        {
            if (brigade == null)
            {
                throw new ArgumentNullException(
                    nameof(brigade));
            }

            if (module == null)
            {
                throw new ArgumentNullException(
                    nameof(module));
            }

            return new BrigadeOperationalResult(
                BrigadeOperationalFailureReason.None,
                brigade,
                module);
        }

        public static BrigadeOperationalResult Failure(
            BrigadeOperationalFailureReason reason)
        {
            if (reason ==
                BrigadeOperationalFailureReason.None)
            {
                throw new ArgumentException(
                    "Failure result must contain a reason.",
                    nameof(reason));
            }

            return new BrigadeOperationalResult(
                reason,
                brigade: null,
                module: null);
        }
    }
}