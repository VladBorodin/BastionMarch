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

        private readonly Dictionary<Guid, ModuleInstance>
            _modulesById = new();

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
    }
}