using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Логическая двумерная сетка внутреннего пространства бастиона.
    ///
    /// X определяет горизонтальную позицию.
    /// Deck определяет этаж, начиная с нулевого нижнего этажа.
    /// </summary>
    public sealed class BastionGrid
    {
        private readonly Dictionary<GridPosition, ModuleInstance>
            _modulesByCell = new();

        private readonly Dictionary<Guid, ModulePassage>
            _passagesById = new();

        private readonly Dictionary<
            GridBoundarySegment,
            ModulePassage> _passagesByBoundary = new();

        private readonly Dictionary<Guid, ModuleInstance>
            _modulesById = new();

        public int PassageCount =>
            _passagesById.Count;

        public IReadOnlyCollection<ModulePassage> Passages =>
            _passagesById
                .Values
                .OrderBy(passage => passage.Id)
                .ToArray();

        public int Width { get; }

        public int DeckCount { get; }

        public int ModuleCount => _modulesById.Count;

        public IReadOnlyCollection<ModuleInstance> Modules =>
            _modulesById.Values.ToArray();

        public BastionGrid(int width, int deckCount)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Grid width must be greater than zero.");
            }

            if (deckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deckCount),
                    "Deck count must be greater than zero.");
            }

            Width = width;
            DeckCount = deckCount;
        }

        public ModulePlacementResult TryPlaceModule(
            ModuleDefinition definition,
            GridPosition origin)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!FitsWithinBounds(definition, origin))
            {
                return ModulePlacementResult.Failure(
                    ModulePlacementFailureReason.OutOfBounds);
            }

            IReadOnlyList<GridPosition> occupiedCells =
                CalculateOccupiedCells(definition, origin);

            bool intersectsAnotherModule =
                occupiedCells.Any(_modulesByCell.ContainsKey);

            if (intersectsAnotherModule)
            {
                return ModulePlacementResult.Failure(
                    ModulePlacementFailureReason.Occupied);
            }

            var module = new ModuleInstance(
                definition,
                origin);

            _modulesById.Add(module.Id, module);

            foreach (GridPosition cell in occupiedCells)
            {
                _modulesByCell.Add(cell, module);
            }

            return ModulePlacementResult.Success(module);
        }

        public bool TryGetModuleAt(
            GridPosition position,
            out ModuleInstance module)
        {
            return _modulesByCell.TryGetValue(
                position,
                out module);
        }

        public bool TryGetModule(
            Guid moduleId,
            out ModuleInstance module)
        {
            return _modulesById.TryGetValue(
                moduleId,
                out module);
        }

        public bool TryGetModuleAdjacencies(
            Guid moduleId,
            out IReadOnlyList<ModuleAdjacency> adjacencies)
        {
            if (!_modulesById.TryGetValue(
                    moduleId,
                    out ModuleInstance sourceModule))
            {
                adjacencies =
                    Array.Empty<ModuleAdjacency>();

                return false;
            }

            var boundariesByNeighbor =
                new Dictionary<
                    (Guid TargetModuleId, GridDirection Direction),
                    HashSet<GridBoundarySegment>>();

            IReadOnlyList<GridPosition> sourceCells =
                CalculateOccupiedCells(
                    sourceModule.Definition,
                    sourceModule.Position);

            foreach (GridPosition sourceCell in sourceCells)
            {
                CollectBoundaryContact(
                    sourceModule,
                    sourceCell,
                    new GridPosition(
                        sourceCell.X - 1,
                        sourceCell.Deck),
                    GridDirection.Left,
                    boundariesByNeighbor);

                CollectBoundaryContact(
                    sourceModule,
                    sourceCell,
                    new GridPosition(
                        sourceCell.X + 1,
                        sourceCell.Deck),
                    GridDirection.Right,
                    boundariesByNeighbor);

                CollectBoundaryContact(
                    sourceModule,
                    sourceCell,
                    new GridPosition(
                        sourceCell.X,
                        sourceCell.Deck - 1),
                    GridDirection.Down,
                    boundariesByNeighbor);

                CollectBoundaryContact(
                    sourceModule,
                    sourceCell,
                    new GridPosition(
                        sourceCell.X,
                        sourceCell.Deck + 1),
                    GridDirection.Up,
                    boundariesByNeighbor);
            }

            adjacencies =
                boundariesByNeighbor
                    .OrderBy(pair =>
                        pair.Key.Direction)
                    .ThenBy(pair =>
                        pair.Key.TargetModuleId)
                    .Select(pair =>
                        new ModuleAdjacency(
                            sourceModuleId:
                                sourceModule.Id,
                            targetModuleId:
                                pair.Key.TargetModuleId,
                            directionFromSource:
                                pair.Key.Direction,
                            sharedBoundaries:
                                pair.Value))
                    .ToArray();

            return true;
        }

        public bool TryGetModuleAdjacency(
            Guid sourceModuleId,
            Guid targetModuleId,
            out ModuleAdjacency adjacency)
        {
            adjacency = null;

            if (!TryGetModuleAdjacencies(
                    sourceModuleId,
                    out IReadOnlyList<ModuleAdjacency>
                        adjacencies))
            {
                return false;
            }

            adjacency =
                adjacencies.FirstOrDefault(
                    item =>
                        item.TargetModuleId ==
                        targetModuleId);

            return adjacency != null;
        }

        public bool TryRemoveModule(
            Guid moduleId,
            out ModuleInstance removedModule)
        {
            if (!_modulesById.TryGetValue(
                    moduleId,
                    out removedModule))
            {
                return false;
            }

            RemovePassagesForModule(
                moduleId);

            IReadOnlyList<GridPosition> occupiedCells =
                CalculateOccupiedCells(
                    removedModule.Definition,
                    removedModule.Position);

            foreach (GridPosition cell in occupiedCells)
            {
                if (_modulesByCell.TryGetValue(
                        cell,
                        out ModuleInstance occupant) &&
                    occupant.Id == moduleId)
                {
                    _modulesByCell.Remove(cell);
                }
            }

            _modulesById.Remove(moduleId);

            return true;
        }

        public bool IsInsideGrid(GridPosition position)
        {
            return position.X >= 0 &&
                   position.X < Width &&
                   position.Deck >= 0 &&
                   position.Deck < DeckCount;
        }

        private static readonly
            IModulePassageTraversalPolicy
                DefaultTraversalPolicy =
                    new DefaultModulePassageTraversalPolicy();

        private bool FitsWithinBounds(
            ModuleDefinition definition,
            GridPosition origin)
        {
            if (!IsInsideGrid(origin))
            {
                return false;
            }

            int exclusiveRight =
                origin.X + definition.Size.Width;

            int exclusiveTopDeck =
                origin.Deck + definition.Size.Height;

            return exclusiveRight <= Width &&
                   exclusiveTopDeck <= DeckCount;
        }

        private void CollectBoundaryContact(
            ModuleInstance sourceModule,
            GridPosition sourceCell,
            GridPosition neighborCell,
            GridDirection direction,
            Dictionary<
                (Guid TargetModuleId, GridDirection Direction),
                HashSet<GridBoundarySegment>>
                    boundariesByNeighbor)
        {
            if (!_modulesByCell.TryGetValue(
                    neighborCell,
                    out ModuleInstance targetModule))
            {
                return;
            }

            if (targetModule.Id == sourceModule.Id)
            {
                return;
            }

            var key =
            (
                TargetModuleId: targetModule.Id,
                Direction: direction
            );

            if (!boundariesByNeighbor.TryGetValue(
                    key,
                    out HashSet<GridBoundarySegment>
                        boundaries))
            {
                boundaries =
                    new HashSet<GridBoundarySegment>();

                boundariesByNeighbor.Add(
                    key,
                    boundaries);
            }

            boundaries.Add(
                new GridBoundarySegment(
                    sourceCell,
                    neighborCell));
        }

        private static IReadOnlyList<GridPosition>
            CalculateOccupiedCells(
                ModuleDefinition definition,
                GridPosition origin)
        {
            var cells = new List<GridPosition>(
                definition.Size.Width *
                definition.Size.Height);

            for (int deckOffset = 0;
                 deckOffset < definition.Size.Height;
                 deckOffset++)
            {
                for (int xOffset = 0;
                     xOffset < definition.Size.Width;
                     xOffset++)
                {
                    cells.Add(
                        new GridPosition(
                            origin.X + xOffset,
                            origin.Deck + deckOffset));
                }
            }

            return cells;
        }

        public ModulePassagePlacementResult TryAddPassage(
            Guid sourceModuleId,
            Guid targetModuleId,
            GridBoundarySegment boundary,
            ModulePassageType type,
            ModulePassageTraversalMode traversalMode)
        {
            if (!_modulesById.ContainsKey(
                    sourceModuleId))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .SourceModuleNotFound);
            }

            if (!_modulesById.ContainsKey(
                    targetModuleId))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .TargetModuleNotFound);
            }

            if (sourceModuleId == targetModuleId)
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .SameModule);
            }

            ValidatePassageType(type);
            ValidateTraversalMode(traversalMode);

            if (!TryGetModuleAdjacency(
                    sourceModuleId,
                    targetModuleId,
                    out ModuleAdjacency adjacency))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .ModulesNotAdjacent);
            }

            if (!adjacency.SharedBoundaries.Contains(
                    boundary))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .BoundaryNotShared);
            }

            if (!IsPassageTypeCompatible(
                    type,
                    boundary))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .PassageTypeIncompatibleWithBoundary);
            }

            if (_passagesByBoundary.ContainsKey(
                    boundary))
            {
                return ModulePassagePlacementResult.Failure(
                    ModulePassagePlacementFailureReason
                        .BoundaryAlreadyHasPassage);
            }

            var passage =
                new ModulePassage(
                    sourceModuleId,
                    targetModuleId,
                    boundary,
                    type,
                    traversalMode);

            _passagesById.Add(
                passage.Id,
                passage);

            _passagesByBoundary.Add(
                boundary,
                passage);

            return ModulePassagePlacementResult.Success(
                passage);
        }

        public bool TryGetPassage(
            Guid passageId,
            out ModulePassage passage)
        {
            return _passagesById.TryGetValue(
                passageId,
                out passage);
        }

        public bool TryGetPassageAtBoundary(
            GridBoundarySegment boundary,
            out ModulePassage passage)
        {
            return _passagesByBoundary.TryGetValue(
                boundary,
                out passage);
        }

        public bool TryGetPassagesForModule(
            Guid moduleId,
            out IReadOnlyList<ModulePassage> passages)
        {
            if (!_modulesById.ContainsKey(
                    moduleId))
            {
                passages =
                    Array.Empty<ModulePassage>();

                return false;
            }

            passages =
                _passagesById
                    .Values
                    .Where(passage =>
                        passage.ConnectsModule(
                            moduleId))
                    .OrderBy(passage =>
                        passage.Boundary.CellA.Deck)
                    .ThenBy(passage =>
                        passage.Boundary.CellA.X)
                    .ThenBy(passage =>
                        passage.Id)
                    .ToArray();

            return true;
        }

        public bool TryRemovePassage(
            Guid passageId,
            out ModulePassage removedPassage)
        {
            if (!_passagesById.TryGetValue(
                    passageId,
                    out removedPassage))
            {
                return false;
            }

            _passagesById.Remove(
                passageId);

            _passagesByBoundary.Remove(
                removedPassage.Boundary);

            return true;
        }

        private static bool IsPassageTypeCompatible(
            ModulePassageType type,
            GridBoundarySegment boundary)
        {
            switch (type)
            {
                case ModulePassageType.Door:
                    return boundary.IsHorizontalPassage;

                case ModulePassageType.Hatch:
                case ModulePassageType.Ladder:
                case ModulePassageType.Stairway:
                case ModulePassageType.Elevator:
                    return boundary.IsVerticalPassage;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type));
            }
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

        private void RemovePassagesForModule(
            Guid moduleId)
        {
            Guid[] passageIds =
                _passagesById
                    .Values
                    .Where(passage =>
                        passage.ConnectsModule(
                            moduleId))
                    .Select(passage =>
                        passage.Id)
                    .ToArray();

            foreach (Guid passageId in passageIds)
            {
                TryRemovePassage(
                    passageId,
                    out _);
            }
        }

        public ModulePassageTraversalAssessment
            AssessPassageTraversal(
                Guid passageId,
                Guid fromModuleId,
                Guid toModuleId)
        {
            return AssessPassageTraversal(
                passageId,
                fromModuleId,
                toModuleId,
                DefaultTraversalPolicy);
        }

        public ModulePassageTraversalAssessment
            AssessPassageTraversal(
                Guid passageId,
                Guid fromModuleId,
                Guid toModuleId,
                IModulePassageTraversalPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy));
            }

            if (!_passagesById.TryGetValue(
                    passageId,
                    out ModulePassage passage))
            {
                return
                    ModulePassageTraversalAssessment.Failure(
                        passageId,
                        fromModuleId,
                        toModuleId,
                        ModulePassageTraversalFailureReason
                            .PassageNotFound);
            }

            if (!_modulesById.TryGetValue(
                    fromModuleId,
                    out ModuleInstance fromModule))
            {
                return
                    ModulePassageTraversalAssessment.Failure(
                        passageId,
                        fromModuleId,
                        toModuleId,
                        ModulePassageTraversalFailureReason
                            .SourceModuleNotFound);
            }

            if (!_modulesById.TryGetValue(
                    toModuleId,
                    out ModuleInstance toModule))
            {
                return
                    ModulePassageTraversalAssessment.Failure(
                        passageId,
                        fromModuleId,
                        toModuleId,
                        ModulePassageTraversalFailureReason
                            .TargetModuleNotFound);
            }

            if (fromModuleId == toModuleId)
            {
                return
                    ModulePassageTraversalAssessment.Failure(
                        passageId,
                        fromModuleId,
                        toModuleId,
                        ModulePassageTraversalFailureReason
                            .SameModule);
            }

            if (!passage.ConnectsModule(
                    fromModuleId) ||
                !passage.ConnectsModule(
                    toModuleId))
            {
                return
                    ModulePassageTraversalAssessment.Failure(
                        passageId,
                        fromModuleId,
                        toModuleId,
                        ModulePassageTraversalFailureReason
                            .PassageDoesNotConnectModules);
            }

            var context =
                new ModulePassageTraversalContext(
                    passage,
                    fromModule,
                    toModule);

            ModulePassageTraversalAssessment assessment =
                policy.Evaluate(context);

            if (assessment == null)
            {
                throw new InvalidOperationException(
                    "Traversal policy returned null.");
            }

            return assessment;
        }

        public ModuleConnectivityGraph
            BuildTraversalGraph()
        {
            return BuildTraversalGraph(
                DefaultTraversalPolicy);
        }

        public ModuleConnectivityGraph
            BuildTraversalGraph(
                IModulePassageTraversalPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy));
            }

            var edges =
                new List<ModuleTraversalEdge>();

            IEnumerable<ModulePassage> orderedPassages =
                _passagesById
                    .Values
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
                AddTraversalEdgeIfAllowed(
                    passage,
                    passage.SourceModuleId,
                    passage.TargetModuleId,
                    policy,
                    edges);

                AddTraversalEdgeIfAllowed(
                    passage,
                    passage.TargetModuleId,
                    passage.SourceModuleId,
                    policy,
                    edges);
            }

            return new ModuleConnectivityGraph(
                _modulesById.Keys,
                edges);
        }

        private void AddTraversalEdgeIfAllowed(
            ModulePassage passage,
            Guid fromModuleId,
            Guid toModuleId,
            IModulePassageTraversalPolicy policy,
            ICollection<ModuleTraversalEdge> edges)
        {
            ModulePassageTraversalAssessment assessment =
                AssessPassageTraversal(
                    passage.Id,
                    fromModuleId,
                    toModuleId,
                    policy);

            if (!assessment.IsAllowed)
            {
                return;
            }

            edges.Add(
                new ModuleTraversalEdge(
                    passage,
                    fromModuleId,
                    toModuleId));
        }

        public ModuleRouteSearchResult FindModuleRoute(
            Guid sourceModuleId,
            Guid targetModuleId)
        {
            return ModuleRouteFinder.Find(
                this,
                sourceModuleId,
                targetModuleId,
                DefaultTraversalPolicy);
        }

        public ModuleRouteSearchResult FindModuleRoute(
            Guid sourceModuleId,
            Guid targetModuleId,
            IModulePassageTraversalPolicy policy)
        {
            return ModuleRouteFinder.Find(
                this,
                sourceModuleId,
                targetModuleId,
                policy);
        }
    }
}