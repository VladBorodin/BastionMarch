using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Конкретный переход, установленный на участке
    /// общей границы двух модулей.
    /// </summary>
    public sealed class ModulePassage
    {
        public Guid Id { get; }

        public Guid SourceModuleId { get; }

        public Guid TargetModuleId { get; }

        public GridBoundarySegment Boundary { get; }

        public ModulePassageType Type { get; }

        public ModulePassageTraversalMode TraversalMode
        {
            get;
        }

        public ModulePassageState State
        {
            get;
            private set;
        }

        internal ModulePassage(
            Guid sourceModuleId,
            Guid targetModuleId,
            GridBoundarySegment boundary,
            ModulePassageType type,
            ModulePassageTraversalMode traversalMode)
        {
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
                    "A passage must connect two different modules.",
                    nameof(targetModuleId));
            }

            ValidatePassageType(type);
            ValidateTraversalMode(traversalMode);

            Id = Guid.NewGuid();

            SourceModuleId =
                sourceModuleId;

            TargetModuleId =
                targetModuleId;

            Boundary = boundary;
            Type = type;
            TraversalMode = traversalMode;

            State =
                ModulePassageState.Open;
        }

        public bool ConnectsModule(
            Guid moduleId)
        {
            return moduleId == SourceModuleId ||
                   moduleId == TargetModuleId;
        }

        public Guid GetOtherModuleId(
            Guid moduleId)
        {
            if (moduleId == SourceModuleId)
            {
                return TargetModuleId;
            }

            if (moduleId == TargetModuleId)
            {
                return SourceModuleId;
            }

            throw new ArgumentException(
                "The module is not connected by this passage.",
                nameof(moduleId));
        }

        /// <summary>
        /// Проверяет только конструктивное направление.
        ///
        /// Состояние, питание, огонь и другие блокировки
        /// здесь намеренно не учитываются.
        /// </summary>
        public bool AllowsDirection(
            Guid fromModuleId,
            Guid toModuleId)
        {
            if (fromModuleId == SourceModuleId &&
                toModuleId == TargetModuleId)
            {
                return TraversalMode !=
                    ModulePassageTraversalMode
                        .TargetToSourceOnly;
            }

            if (fromModuleId == TargetModuleId &&
                toModuleId == SourceModuleId)
            {
                return TraversalMode !=
                    ModulePassageTraversalMode
                        .SourceToTargetOnly;
            }

            return false;
        }

        public void SetState(
            ModulePassageState state)
        {
            if (!Enum.IsDefined(
                    typeof(ModulePassageState),
                    state))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state));
            }

            State = state;
        }

        private static void ValidatePassageType(
            ModulePassageType type)
        {
            if (!Enum.IsDefined(
                    typeof(ModulePassageType),
                    type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type));
            }
        }

        private static void ValidateTraversalMode(
            ModulePassageTraversalMode traversalMode)
        {
            if (!Enum.IsDefined(
                    typeof(ModulePassageTraversalMode),
                    traversalMode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(traversalMode));
            }
        }
    }
}