using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Один переход маршрута между двумя соседними
    /// модулями.
    /// </summary>
    public sealed class ModuleRouteStep
    {
        public Guid PassageId { get; }

        public Guid FromModuleId { get; }

        public Guid ToModuleId { get; }

        public ModulePassageType PassageType { get; }

        public GridBoundarySegment Boundary { get; }

        internal ModuleRouteStep(
            ModuleTraversalEdge edge)
        {
            if (edge == null)
            {
                throw new ArgumentNullException(
                    nameof(edge));
            }

            PassageId = edge.PassageId;
            FromModuleId = edge.FromModuleId;
            ToModuleId = edge.ToModuleId;
            PassageType = edge.PassageType;
            Boundary = edge.Boundary;
        }
    }
}