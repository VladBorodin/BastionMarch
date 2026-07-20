using System;

namespace BastionMarch.Simulation.Bastions
{
    public sealed class ModulePassagePlacementResult
    {
        public bool IsSuccess =>
            FailureReason ==
            ModulePassagePlacementFailureReason.None;

        public ModulePassagePlacementFailureReason
            FailureReason
        {
            get;
        }

        public ModulePassage Passage { get; }

        private ModulePassagePlacementResult(
            ModulePassagePlacementFailureReason
                failureReason,
            ModulePassage passage)
        {
            FailureReason = failureReason;
            Passage = passage;
        }

        public static ModulePassagePlacementResult Success(
            ModulePassage passage)
        {
            if (passage == null)
            {
                throw new ArgumentNullException(
                    nameof(passage));
            }

            return new ModulePassagePlacementResult(
                ModulePassagePlacementFailureReason.None,
                passage);
        }

        public static ModulePassagePlacementResult Failure(
            ModulePassagePlacementFailureReason reason)
        {
            if (reason ==
                ModulePassagePlacementFailureReason.None)
            {
                throw new ArgumentException(
                    "Failure result must contain a reason.",
                    nameof(reason));
            }

            return new ModulePassagePlacementResult(
                reason,
                passage: null);
        }
    }
}