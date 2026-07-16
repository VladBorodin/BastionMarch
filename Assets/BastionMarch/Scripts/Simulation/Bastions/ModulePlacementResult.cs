using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Bastions
{
    public sealed class ModulePlacementResult
    {
        public bool IsSuccess =>
            FailureReason == ModulePlacementFailureReason.None;

        public ModulePlacementFailureReason FailureReason { get; }

        public ModuleInstance Module { get; }

        private ModulePlacementResult(
            ModulePlacementFailureReason failureReason,
            ModuleInstance module)
        {
            FailureReason = failureReason;
            Module = module;
        }

        public static ModulePlacementResult Success(
            ModuleInstance module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            return new ModulePlacementResult(
                ModulePlacementFailureReason.None,
                module);
        }

        public static ModulePlacementResult Failure(
            ModulePlacementFailureReason reason)
        {
            if (reason == ModulePlacementFailureReason.None)
            {
                throw new ArgumentException(
                    "A failed placement must have a failure reason.",
                    nameof(reason));
            }

            return new ModulePlacementResult(reason, null);
        }
    }
}