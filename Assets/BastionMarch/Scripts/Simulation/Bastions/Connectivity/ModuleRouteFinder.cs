using System;
using System.Collections.Generic;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Выполняет детерминированный поиск кратчайшего
    /// маршрута по доступным переходам.
    /// </summary>
    public static class ModuleRouteFinder
    {
        public static ModuleRouteSearchResult Find(
            BastionGrid grid,
            Guid sourceModuleId,
            Guid targetModuleId,
            IModulePassageTraversalPolicy policy)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(
                    nameof(grid));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy));
            }

            if (!grid.TryGetModule(
                    sourceModuleId,
                    out _))
            {
                return ModuleRouteSearchResult.Failure(
                    ModuleRouteFailureReason
                        .SourceModuleNotFound);
            }

            if (!grid.TryGetModule(
                    targetModuleId,
                    out _))
            {
                return ModuleRouteSearchResult.Failure(
                    ModuleRouteFailureReason
                        .TargetModuleNotFound);
            }

            if (sourceModuleId == targetModuleId)
            {
                return ModuleRouteSearchResult.Success(
                    new ModuleRoute(
                        sourceModuleId,
                        targetModuleId,
                        Array.Empty<ModuleRouteStep>()));
            }

            ModuleConnectivityGraph graph =
                grid.BuildTraversalGraph(
                    policy);

            if (TryFindTraversableRoute(
                    graph,
                    sourceModuleId,
                    targetModuleId,
                    out ModuleRoute route))
            {
                return ModuleRouteSearchResult.Success(
                    route);
            }

            if (!HasStructuralConnection(
                    grid,
                    sourceModuleId,
                    targetModuleId))
            {
                return ModuleRouteSearchResult.Failure(
                    ModuleRouteFailureReason
                        .NoStructuralConnection);
            }

            IReadOnlyList<
                ModulePassageTraversalAssessment>
                    blockers =
                        FindBlockingFrontier(
                            grid,
                            graph,
                            sourceModuleId,
                            policy);

            return ModuleRouteSearchResult.Failure(
                ModuleRouteFailureReason
                    .TraversalBlocked,
                blockers);
        }

        private static bool TryFindTraversableRoute(
            ModuleConnectivityGraph graph,
            Guid sourceModuleId,
            Guid targetModuleId,
            out ModuleRoute route)
        {
            var visited =
                new HashSet<Guid>
                {
                    sourceModuleId
                };

            var queue =
                new Queue<Guid>();

            var predecessorByModuleId =
                new Dictionary<
                    Guid,
                    ModuleTraversalEdge>();

            queue.Enqueue(
                sourceModuleId);

            while (queue.Count > 0)
            {
                Guid currentModuleId =
                    queue.Dequeue();

                graph.TryGetOutgoingEdges(
                    currentModuleId,
                    out IReadOnlyList<
                        ModuleTraversalEdge> outgoing);

                IEnumerable<ModuleTraversalEdge>
                    orderedOutgoing =
                        outgoing
                            .OrderBy(edge =>
                                edge.ToModuleId)
                            .ThenBy(edge =>
                                edge.Boundary.CellA.Deck)
                            .ThenBy(edge =>
                                edge.Boundary.CellA.X)
                            .ThenBy(edge =>
                                edge.Boundary.CellB.Deck)
                            .ThenBy(edge =>
                                edge.Boundary.CellB.X)
                            .ThenBy(edge =>
                                edge.PassageId);

                foreach (
                    ModuleTraversalEdge edge
                    in orderedOutgoing)
                {
                    if (!visited.Add(
                            edge.ToModuleId))
                    {
                        continue;
                    }

                    predecessorByModuleId.Add(
                        edge.ToModuleId,
                        edge);

                    if (edge.ToModuleId ==
                        targetModuleId)
                    {
                        route =
                            ReconstructRoute(
                                sourceModuleId,
                                targetModuleId,
                                predecessorByModuleId);

                        return true;
                    }

                    queue.Enqueue(
                        edge.ToModuleId);
                }
            }

            route = null;
            return false;
        }

        private static ModuleRoute ReconstructRoute(
            Guid sourceModuleId,
            Guid targetModuleId,
            IReadOnlyDictionary<
                Guid,
                ModuleTraversalEdge>
                    predecessorByModuleId)
        {
            var reversedEdges =
                new List<ModuleTraversalEdge>();

            Guid currentModuleId =
                targetModuleId;

            while (currentModuleId !=
                   sourceModuleId)
            {
                if (!predecessorByModuleId.TryGetValue(
                        currentModuleId,
                        out ModuleTraversalEdge edge))
                {
                    throw new InvalidOperationException(
                        "Route predecessor chain is incomplete.");
                }

                reversedEdges.Add(edge);

                currentModuleId =
                    edge.FromModuleId;
            }

            reversedEdges.Reverse();

            IEnumerable<ModuleRouteStep> steps =
                reversedEdges.Select(
                    edge =>
                        new ModuleRouteStep(edge));

            return new ModuleRoute(
                sourceModuleId,
                targetModuleId,
                steps);
        }

        /// <summary>
        /// Проверяет существование цепочки физических
        /// переходов без учёта состояния и направления.
        /// </summary>
        private static bool HasStructuralConnection(
            BastionGrid grid,
            Guid sourceModuleId,
            Guid targetModuleId)
        {
            var visited =
                new HashSet<Guid>
                {
                    sourceModuleId
                };

            var queue =
                new Queue<Guid>();

            queue.Enqueue(
                sourceModuleId);

            while (queue.Count > 0)
            {
                Guid currentModuleId =
                    queue.Dequeue();

                if (!grid.TryGetPassagesForModule(
                        currentModuleId,
                        out IReadOnlyList<
                            ModulePassage> passages))
                {
                    continue;
                }

                IEnumerable<ModulePassage>
                    orderedPassages =
                        passages
                            .OrderBy(passage =>
                                passage.Boundary.CellA.Deck)
                            .ThenBy(passage =>
                                passage.Boundary.CellA.X)
                            .ThenBy(passage =>
                                passage.Id);

                foreach (
                    ModulePassage passage
                    in orderedPassages)
                {
                    Guid otherModuleId =
                        passage.GetOtherModuleId(
                            currentModuleId);

                    if (otherModuleId ==
                        targetModuleId)
                    {
                        return true;
                    }

                    if (visited.Add(
                            otherModuleId))
                    {
                        queue.Enqueue(
                            otherModuleId);
                    }
                }
            }

            return false;
        }

        private static IReadOnlyList<
            ModulePassageTraversalAssessment>
                FindBlockingFrontier(
                    BastionGrid grid,
                    ModuleConnectivityGraph graph,
                    Guid sourceModuleId,
                    IModulePassageTraversalPolicy policy)
        {
            HashSet<Guid> reachableModuleIds =
                CollectReachableModules(
                    graph,
                    sourceModuleId);

            var blockerAssessments =
                new List<
                    ModulePassageTraversalAssessment>();

            var assessedDirections =
                new HashSet<
                    (Guid PassageId, Guid FromModuleId)>();

            foreach (
                Guid reachableModuleId
                in reachableModuleIds.OrderBy(id => id))
            {
                if (!grid.TryGetPassagesForModule(
                        reachableModuleId,
                        out IReadOnlyList<
                            ModulePassage> passages))
                {
                    continue;
                }

                foreach (
                    ModulePassage passage
                    in passages)
                {
                    Guid otherModuleId =
                        passage.GetOtherModuleId(
                            reachableModuleId);

                    if (reachableModuleIds.Contains(
                            otherModuleId))
                    {
                        continue;
                    }

                    var key =
                    (
                        PassageId: passage.Id,
                        FromModuleId:
                            reachableModuleId
                    );

                    if (!assessedDirections.Add(key))
                    {
                        continue;
                    }

                    ModulePassageTraversalAssessment
                        assessment =
                            grid.AssessPassageTraversal(
                                passage.Id,
                                reachableModuleId,
                                otherModuleId,
                                policy);

                    if (!assessment.IsAllowed)
                    {
                        blockerAssessments.Add(
                            assessment);
                    }
                }
            }

            return blockerAssessments
                .OrderBy(item =>
                    item.FailureReason)
                .ThenBy(item =>
                    item.FromModuleId)
                .ThenBy(item =>
                    item.ToModuleId)
                .ThenBy(item =>
                    item.PassageId)
                .ToArray();
        }

        private static HashSet<Guid>
            CollectReachableModules(
                ModuleConnectivityGraph graph,
                Guid sourceModuleId)
        {
            var visited =
                new HashSet<Guid>
                {
                    sourceModuleId
                };

            var queue =
                new Queue<Guid>();

            queue.Enqueue(
                sourceModuleId);

            while (queue.Count > 0)
            {
                Guid currentModuleId =
                    queue.Dequeue();

                graph.TryGetOutgoingEdges(
                    currentModuleId,
                    out IReadOnlyList<
                        ModuleTraversalEdge> outgoing);

                foreach (
                    ModuleTraversalEdge edge
                    in outgoing)
                {
                    if (visited.Add(
                            edge.ToModuleId))
                    {
                        queue.Enqueue(
                            edge.ToModuleId);
                    }
                }
            }

            return visited;
        }
    }
}