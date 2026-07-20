using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Один разрешённый направленный переход
    /// между двумя модулями.
    ///
    /// Двусторонняя дверь создаёт два ребра.
    /// </summary>
    public sealed class ModuleTraversalEdge
    {
        public Guid PassageId { get; }

        public Guid FromModuleId { get; }

        public Guid ToModuleId { get; }

        public ModulePassageType PassageType { get; }

        public ModulePassageState PassageState { get; }

        public GridBoundarySegment Boundary { get; }

        internal ModuleTraversalEdge(
            ModulePassage passage,
            Guid fromModuleId,
            Guid toModuleId)
        {
            if (passage == null)
            {
                throw new ArgumentNullException(
                    nameof(passage));
            }

            if (!passage.ConnectsModule(
                    fromModuleId) ||
                !passage.ConnectsModule(
                    toModuleId))
            {
                throw new ArgumentException(
                    "Passage does not connect edge modules.");
            }

            if (fromModuleId == toModuleId)
            {
                throw new ArgumentException(
                    "Traversal edge must connect different modules.",
                    nameof(toModuleId));
            }

            PassageId = passage.Id;
            FromModuleId = fromModuleId;
            ToModuleId = toModuleId;
            PassageType = passage.Type;
            PassageState = passage.State;
            Boundary = passage.Boundary;
        }
    }
}