using System;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок одного шага маршрута.
    /// </summary>
    public sealed class RouteStepPresentationState
    {
        public Guid PassageId { get; }

        public Guid FromModuleId { get; }

        public Guid ToModuleId { get; }

        public ModulePassageType PassageType { get; }

        public GridBoundarySegment Boundary { get; }

        public bool IsHorizontal =>
            Boundary.IsHorizontalPassage;

        public bool IsVertical =>
            Boundary.IsVerticalPassage;

        public RouteStepPresentationState(
            Guid passageId,
            Guid fromModuleId,
            Guid toModuleId,
            ModulePassageType passageType,
            GridBoundarySegment boundary)
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
                    "Route step must connect " +
                    "different modules.",
                    nameof(toModuleId));
            }

            if (!Enum.IsDefined(
                    typeof(ModulePassageType),
                    passageType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(passageType));
            }

            PassageId = passageId;
            FromModuleId = fromModuleId;
            ToModuleId = toModuleId;
            PassageType = passageType;
            Boundary = boundary;
        }
    }
}