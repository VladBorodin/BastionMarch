using System;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок одного перехода
    /// между двумя модулями.
    /// </summary>
    public sealed class PassagePresentationState
    {
        public Guid PassageId { get; }

        public Guid SourceModuleId { get; }

        public Guid TargetModuleId { get; }

        public GridBoundarySegment Boundary { get; }

        public ModulePassageType Type { get; }

        public ModulePassageTraversalMode TraversalMode
        {
            get;
        }

        public ModulePassageState State { get; }

        public bool IsHorizontal =>
            Boundary.IsHorizontalPassage;

        public bool IsVertical =>
            Boundary.IsVerticalPassage;

        public PassagePresentationState(
            Guid passageId,
            Guid sourceModuleId,
            Guid targetModuleId,
            GridBoundarySegment boundary,
            ModulePassageType type,
            ModulePassageTraversalMode traversalMode,
            ModulePassageState state)
        {
            if (passageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Passage id cannot be empty.",
                    nameof(passageId));
            }

            if (sourceModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Source module id cannot be empty.",
                    nameof(sourceModuleId));
            }

            if (targetModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Target module id cannot be empty.",
                    nameof(targetModuleId));
            }

            if (sourceModuleId == targetModuleId)
            {
                throw new ArgumentException(
                    "Passage must connect different modules.",
                    nameof(targetModuleId));
            }

            if (!Enum.IsDefined(
                    typeof(ModulePassageType),
                    type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type));
            }

            if (!Enum.IsDefined(
                    typeof(ModulePassageTraversalMode),
                    traversalMode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(traversalMode));
            }

            if (!Enum.IsDefined(
                    typeof(ModulePassageState),
                    state))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state));
            }

            PassageId = passageId;
            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;
            Boundary = boundary;
            Type = type;
            TraversalMode = traversalMode;
            State = state;
        }

        public bool ConnectsModule(
            Guid moduleId)
        {
            return moduleId == SourceModuleId ||
                   moduleId == TargetModuleId;
        }
    }
}