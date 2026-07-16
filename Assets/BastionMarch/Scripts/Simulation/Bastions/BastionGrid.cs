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