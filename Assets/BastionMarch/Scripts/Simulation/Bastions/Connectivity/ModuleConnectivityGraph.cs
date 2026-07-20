using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Неизменяемый снимок доступных направленных
    /// переходов между модулями.
    /// </summary>
    public sealed class ModuleConnectivityGraph
    {
        private readonly Dictionary<
            Guid,
            IReadOnlyList<ModuleTraversalEdge>>
                _outgoingEdgesByModuleId;

        public IReadOnlyList<Guid> ModuleIds { get; }

        public IReadOnlyList<ModuleTraversalEdge> Edges
        {
            get;
        }

        public int ModuleCount =>
            ModuleIds.Count;

        public int EdgeCount =>
            Edges.Count;

        internal ModuleConnectivityGraph(
            IEnumerable<Guid> moduleIds,
            IEnumerable<ModuleTraversalEdge> edges)
        {
            if (moduleIds == null)
            {
                throw new ArgumentNullException(
                    nameof(moduleIds));
            }

            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            Guid[] orderedModuleIds =
                moduleIds
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();

            ModuleTraversalEdge[] orderedEdges =
                edges
                    .OrderBy(edge =>
                        edge.FromModuleId)
                    .ThenBy(edge =>
                        edge.ToModuleId)
                    .ThenBy(edge =>
                        edge.PassageId)
                    .ToArray();

            var moduleIdSet =
                new HashSet<Guid>(
                    orderedModuleIds);

            foreach (
                ModuleTraversalEdge edge
                in orderedEdges)
            {
                if (!moduleIdSet.Contains(
                        edge.FromModuleId) ||
                    !moduleIdSet.Contains(
                        edge.ToModuleId))
                {
                    throw new ArgumentException(
                        "Traversal edge references a module outside the graph.",
                        nameof(edges));
                }
            }

            ModuleIds =
                new ReadOnlyCollection<Guid>(
                    orderedModuleIds);

            Edges =
                new ReadOnlyCollection<
                    ModuleTraversalEdge>(
                        orderedEdges);

            _outgoingEdgesByModuleId =
                orderedModuleIds.ToDictionary(
                    moduleId => moduleId,
                    moduleId =>
                        (IReadOnlyList<
                            ModuleTraversalEdge>)
                        new ReadOnlyCollection<
                            ModuleTraversalEdge>(
                                orderedEdges
                                    .Where(edge =>
                                        edge.FromModuleId ==
                                        moduleId)
                                    .ToArray()));
        }

        public bool ContainsModule(
            Guid moduleId)
        {
            return _outgoingEdgesByModuleId.ContainsKey(
                moduleId);
        }

        public bool TryGetOutgoingEdges(
            Guid moduleId,
            out IReadOnlyList<ModuleTraversalEdge>
                edges)
        {
            if (!_outgoingEdgesByModuleId.TryGetValue(
                    moduleId,
                    out edges))
            {
                edges =
                    Array.Empty<ModuleTraversalEdge>();

                return false;
            }

            return true;
        }

        public bool HasTraversal(
            Guid fromModuleId,
            Guid toModuleId)
        {
            return
                _outgoingEdgesByModuleId.TryGetValue(
                    fromModuleId,
                    out IReadOnlyList<
                        ModuleTraversalEdge> edges) &&
                edges.Any(
                    edge =>
                        edge.ToModuleId ==
                        toModuleId);
        }
    }
}