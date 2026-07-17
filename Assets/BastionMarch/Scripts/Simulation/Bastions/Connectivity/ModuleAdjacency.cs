using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Описывает геометрическое соприкосновение
    /// двух установленных модулей.
    ///
    /// Наличие соседства не означает наличие прохода.
    /// </summary>
    public sealed class ModuleAdjacency
    {
        public Guid SourceModuleId { get; }

        public Guid TargetModuleId { get; }

        /// <summary>
        /// Направление от SourceModule к TargetModule.
        /// </summary>
        public GridDirection DirectionFromSource { get; }

        /// <summary>
        /// Все единичные участки общей границы.
        ///
        /// Два модуля высотой в две клетки могут иметь,
        /// например, два общих участка стены.
        /// </summary>
        public IReadOnlyList<GridBoundarySegment>
            SharedBoundaries { get; }

        public int SharedBoundaryCount =>
            SharedBoundaries.Count;

        public ModuleAdjacency(
            Guid sourceModuleId,
            Guid targetModuleId,
            GridDirection directionFromSource,
            IEnumerable<GridBoundarySegment>
                sharedBoundaries)
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
                    "A module cannot be adjacent to itself.",
                    nameof(targetModuleId));
            }

            if (!Enum.IsDefined(
                    typeof(GridDirection),
                    directionFromSource))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directionFromSource));
            }

            if (sharedBoundaries == null)
            {
                throw new ArgumentNullException(
                    nameof(sharedBoundaries));
            }

            List<GridBoundarySegment> boundaries =
                sharedBoundaries
                    .Distinct()
                    .OrderBy(
                        boundary =>
                            boundary.CellA.Deck)
                    .ThenBy(
                        boundary =>
                            boundary.CellA.X)
                    .ThenBy(
                        boundary =>
                            boundary.CellB.Deck)
                    .ThenBy(
                        boundary =>
                            boundary.CellB.X)
                    .ToList();

            if (boundaries.Count == 0)
            {
                throw new ArgumentException(
                    "Adjacent modules must share at least one boundary.",
                    nameof(sharedBoundaries));
            }

            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;

            DirectionFromSource =
                directionFromSource;

            SharedBoundaries =
                new ReadOnlyCollection<
                    GridBoundarySegment>(
                        boundaries);
        }
    }
}