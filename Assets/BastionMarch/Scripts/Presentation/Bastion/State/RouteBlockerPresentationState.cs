using System;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок конкретного препятствия,
    /// не позволившего продолжить маршрут.
    /// </summary>
    public sealed class RouteBlockerPresentationState
    {
        public Guid PassageId { get; }

        public Guid FromModuleId { get; }

        public Guid ToModuleId { get; }

        public ModulePassageTraversalFailureReason
            FailureReason
        {
            get;
        }

        public RouteBlockerPresentationState(
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

            if (fromModuleId == toModuleId)
            {
                throw new ArgumentException(
                    "Blocking traversal must connect " +
                    "different modules.",
                    nameof(toModuleId));
            }

            if (!Enum.IsDefined(
                    typeof(
                        ModulePassageTraversalFailureReason),
                    failureReason) ||
                failureReason ==
                    ModulePassageTraversalFailureReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failureReason));
            }

            PassageId = passageId;
            FromModuleId = fromModuleId;
            ToModuleId = toModuleId;
            FailureReason = failureReason;
        }
    }
}