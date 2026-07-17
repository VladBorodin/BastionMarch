using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BastionGridConnectivityTests
    {
        private ModuleDefinitionCatalog _catalog;
        private BastionGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();

            _grid =
                new BastionGrid(
                    width: 6,
                    deckCount: 3);
        }

        [Test]
        public void BoundarySegmentIgnoresCellOrder()
        {
            var forward =
                new GridBoundarySegment(
                    new GridPosition(0, 0),
                    new GridPosition(1, 0));

            var reversed =
                new GridBoundarySegment(
                    new GridPosition(1, 0),
                    new GridPosition(0, 0));

            Assert.That(
                forward,
                Is.EqualTo(reversed));

            Assert.That(
                forward.IsHorizontalPassage,
                Is.True);

            Assert.That(
                forward.IsVerticalPassage,
                Is.False);
        }

        [Test]
        public void BoundarySegmentRejectsNonAdjacentCells()
        {
            Assert.Throws<ArgumentException>(
                () => new GridBoundarySegment(
                    new GridPosition(0, 0),
                    new GridPosition(2, 0)));
        }

        [Test]
        public void FindsHorizontalAdjacency()
        {
            ModuleInstance left =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            ModuleInstance right =
                PlaceSmallModule(
                    x: 1,
                    deck: 0);

            bool found =
                _grid.TryGetModuleAdjacency(
                    left.Id,
                    right.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(found, Is.True);

            Assert.That(
                adjacency.SourceModuleId,
                Is.EqualTo(left.Id));

            Assert.That(
                adjacency.TargetModuleId,
                Is.EqualTo(right.Id));

            Assert.That(
                adjacency.DirectionFromSource,
                Is.EqualTo(
                    GridDirection.Right));

            Assert.That(
                adjacency.SharedBoundaryCount,
                Is.EqualTo(1));

            Assert.That(
                adjacency.SharedBoundaries[0]
                    .IsHorizontalPassage,
                Is.True);
        }

        [Test]
        public void ReverseQueryReturnsOppositeDirection()
        {
            ModuleInstance left =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            ModuleInstance right =
                PlaceSmallModule(
                    x: 1,
                    deck: 0);

            bool found =
                _grid.TryGetModuleAdjacency(
                    right.Id,
                    left.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(found, Is.True);

            Assert.That(
                adjacency.DirectionFromSource,
                Is.EqualTo(
                    GridDirection.Left));
        }

        [Test]
        public void FindsVerticalAdjacency()
        {
            ModuleInstance lower =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            ModuleInstance upper =
                PlaceSmallModule(
                    x: 0,
                    deck: 1);

            bool found =
                _grid.TryGetModuleAdjacency(
                    lower.Id,
                    upper.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(found, Is.True);

            Assert.That(
                adjacency.DirectionFromSource,
                Is.EqualTo(
                    GridDirection.Up));

            Assert.That(
                adjacency.SharedBoundaries[0]
                    .IsVerticalPassage,
                Is.True);
        }

        [Test]
        public void DiagonalModulesAreNotAdjacent()
        {
            ModuleInstance first =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            ModuleInstance diagonal =
                PlaceSmallModule(
                    x: 1,
                    deck: 1);

            bool found =
                _grid.TryGetModuleAdjacency(
                    first.Id,
                    diagonal.Id,
                    out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void EmptyCellGapPreventsAdjacency()
        {
            ModuleInstance first =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            ModuleInstance separated =
                PlaceSmallModule(
                    x: 2,
                    deck: 0);

            bool found =
                _grid.TryGetModuleAdjacency(
                    first.Id,
                    separated.Id,
                    out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void LargeModulesReportEverySharedBoundarySegment()
        {
            ModuleInstance left =
                PlaceLargeModule(
                    x: 0,
                    deck: 0);

            ModuleInstance right =
                PlaceLargeModule(
                    x: 2,
                    deck: 0);

            bool found =
                _grid.TryGetModuleAdjacency(
                    left.Id,
                    right.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(found, Is.True);

            Assert.That(
                adjacency.DirectionFromSource,
                Is.EqualTo(
                    GridDirection.Right));

            Assert.That(
                adjacency.SharedBoundaryCount,
                Is.EqualTo(2));

            Assert.That(
                adjacency.SharedBoundaries.All(
                    boundary =>
                        boundary.IsHorizontalPassage),
                Is.True);
        }

        [Test]
        public void IsolatedModuleReturnsEmptyAdjacencyList()
        {
            ModuleInstance isolated =
                PlaceSmallModule(
                    x: 0,
                    deck: 0);

            bool found =
                _grid.TryGetModuleAdjacencies(
                    isolated.Id,
                    out IReadOnlyList<ModuleAdjacency>
                        adjacencies);

            Assert.That(found, Is.True);
            Assert.That(adjacencies, Is.Empty);
        }

        [Test]
        public void MissingModuleReturnsFalse()
        {
            bool found =
                _grid.TryGetModuleAdjacencies(
                    Guid.NewGuid(),
                    out IReadOnlyList<ModuleAdjacency>
                        adjacencies);

            Assert.That(found, Is.False);
            Assert.That(adjacencies, Is.Empty);
        }

        private ModuleInstance PlaceSmallModule(
            int x,
            int deck)
        {
            return PlaceModule(
                ModuleDefinitionIds
                    .SmallMachineRoom,
                x,
                deck);
        }

        private ModuleInstance PlaceLargeModule(
            int x,
            int deck)
        {
            return PlaceModule(
                ModuleDefinitionIds
                    .LargeMachineRoom,
                x,
                deck);
        }

        private ModuleInstance PlaceModule(
            string definitionId,
            int x,
            int deck)
        {
            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    _catalog.GetRequired(
                        definitionId),
                    new GridPosition(
                        x,
                        deck));

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Module;
        }
    }
}