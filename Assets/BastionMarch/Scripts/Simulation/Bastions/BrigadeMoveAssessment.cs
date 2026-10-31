
using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Неизменяемый результат проверки перемещения
    /// бригады между соседними отсеками.
    /// </summary>
    public sealed class BrigadeMoveAssessment
    {
        public Guid BrigadeId { get; }
        public Guid SourceModuleId { get; }
        public Guid TargetModuleId { get; }
        public Guid PassageId { get; }

        public BrigadeMoveFailureReason FailureReason { get; }

        public ModulePassageTraversalFailureReason TraversalFailureReason
        {
            get;
        }

        public bool IsAllowed =>
            FailureReason == BrigadeMoveFailureReason.None;

        private BrigadeMoveAssessment(
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId,
            BrigadeMoveFailureReason failureReason,
            ModulePassageTraversalFailureReason traversalFailureReason)
        {
            BrigadeId = brigadeId;
            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;
            PassageId = passageId;
            FailureReason = failureReason;
            TraversalFailureReason = traversalFailureReason;
        }

        internal static BrigadeMoveAssessment Allowed(
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId)
        {
            return new BrigadeMoveAssessment(
                brigadeId,
                sourceModuleId,
                targetModuleId,
                passageId,
                BrigadeMoveFailureReason.None,
                ModulePassageTraversalFailureReason.None);
        }

        internal static BrigadeMoveAssessment Rejected(
            Guid brigadeId,
            Guid sourceModuleId,
            Guid targetModuleId,
            Guid passageId,
            BrigadeMoveFailureReason failureReason,
            ModulePassageTraversalFailureReason traversalFailureReason =
                ModulePassageTraversalFailureReason.None)
        {
            if (failureReason == BrigadeMoveFailureReason.None)
            {
                throw new ArgumentException(
                    "Rejected move requires a failure reason.",
                    nameof(failureReason));
            }

            return new BrigadeMoveAssessment(
                brigadeId,
                sourceModuleId,
                targetModuleId,
                passageId,
                failureReason,
                traversalFailureReason);
        }
    }
}
