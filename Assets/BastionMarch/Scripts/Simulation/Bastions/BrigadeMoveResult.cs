
using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Результат попытки выполнить atomic brigade move.
    /// </summary>
    public sealed class BrigadeMoveResult
    {
        public BrigadeMoveAssessment Assessment { get; }

        public bool IsSuccess => Assessment.IsAllowed;

        public BrigadeMoveFailureReason FailureReason =>
            Assessment.FailureReason;

        public Guid BrigadeId => Assessment.BrigadeId;
        public Guid SourceModuleId => Assessment.SourceModuleId;
        public Guid TargetModuleId => Assessment.TargetModuleId;
        public Guid PassageId => Assessment.PassageId;

        internal BrigadeMoveResult(BrigadeMoveAssessment assessment)
        {
            Assessment = assessment ??
                throw new ArgumentNullException(nameof(assessment));
        }
    }
}
