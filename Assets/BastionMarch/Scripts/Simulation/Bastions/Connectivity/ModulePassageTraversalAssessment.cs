using System;

namespace BastionMarch.Simulation.Bastions
{
    public sealed class ModulePassageTraversalAssessment
    {
        public Guid PassageId { get; }

        public Guid FromModuleId { get; }

        public Guid ToModuleId { get; }

        public ModulePassageTraversalFailureReason
            FailureReason
        {
            get;
        }

        public bool IsAllowed =>
            FailureReason ==
            ModulePassageTraversalFailureReason.None;

        private ModulePassageTraversalAssessment(
            Guid passageId,
            Guid fromModuleId,
            Guid toModuleId,
            ModulePassageTraversalFailureReason
                failureReason)
        {
            if (passageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Passage id cannot be empty.",
                    nameof(passageId));
            }

            if (fromModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Source module id cannot be empty.",
                    nameof(fromModuleId));
            }

            if (toModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Target module id cannot be empty.",
                    nameof(toModuleId));
            }

            PassageId = passageId;
            FromModuleId = fromModuleId;
            ToModuleId = toModuleId;
            FailureReason = failureReason;
        }

        public static
            ModulePassageTraversalAssessment Allowed(
                ModulePassageTraversalContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            return new ModulePassageTraversalAssessment(
                passageId: context.Passage.Id,
                fromModuleId: context.FromModuleId,
                toModuleId: context.ToModuleId,
                failureReason:
                    ModulePassageTraversalFailureReason.None);
        }

        public static
            ModulePassageTraversalAssessment Rejected(
                ModulePassageTraversalContext context,
                ModulePassageTraversalFailureReason
                    failureReason)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            return Failure(
                context.Passage.Id,
                context.FromModuleId,
                context.ToModuleId,
                failureReason);
        }

        public static
            ModulePassageTraversalAssessment Failure(
                Guid passageId,
                Guid fromModuleId,
                Guid toModuleId,
                ModulePassageTraversalFailureReason
                    failureReason)
        {
            if (failureReason ==
                ModulePassageTraversalFailureReason.None)
            {
                throw new ArgumentException(
                    "Rejected traversal must contain a failure reason.",
                    nameof(failureReason));
            }

            return new ModulePassageTraversalAssessment(
                passageId,
                fromModuleId,
                toModuleId,
                failureReason);
        }
    }
}