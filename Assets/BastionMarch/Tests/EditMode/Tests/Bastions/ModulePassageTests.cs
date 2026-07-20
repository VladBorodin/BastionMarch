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
    public sealed class ModulePassageTests
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
                    width: 8,
                    deckCount: 4);
        }

        [Test]
        public void InstallsDoorOnHorizontalBoundary()
        {
            ModuleInstance left =
                PlaceSmall(
                    x: 0,
                    deck: 0);

            ModuleInstance right =
                PlaceSmall(
                    x: 1,
                    deck: 0);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    left,
                    right);

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    left.Id,
                    right.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Passage, Is.Not.Null);
            Assert.That(_grid.PassageCount, Is.EqualTo(1));

            Assert.That(
                result.Passage.Type,
                Is.EqualTo(
                    ModulePassageType.Door));

            Assert.That(
                result.Passage.State,
                Is.EqualTo(
                    ModulePassageState.Open));

            Assert.That(
                result.Passage.AllowsDirection(
                    left.Id,
                    right.Id),
                Is.True);

            Assert.That(
                result.Passage.AllowsDirection(
                    right.Id,
                    left.Id),
                Is.True);
        }

        [Test]
        public void InstallsHatchOnVerticalBoundary()
        {
            ModuleInstance lower =
                PlaceSmall(
                    x: 0,
                    deck: 0);

            ModuleInstance upper =
                PlaceSmall(
                    x: 0,
                    deck: 1);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    lower,
                    upper);

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    lower.Id,
                    upper.Id,
                    boundary,
                    ModulePassageType.Hatch,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(
                result.Passage.Type,
                Is.EqualTo(
                    ModulePassageType.Hatch));
        }

        [Test]
        public void RejectsDoorOnVerticalBoundary()
        {
            ModuleInstance lower =
                PlaceSmall(0, 0);

            ModuleInstance upper =
                PlaceSmall(0, 1);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    lower,
                    upper);

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    lower.Id,
                    upper.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePassagePlacementFailureReason
                        .PassageTypeIncompatibleWithBoundary));
        }

        [Test]
        public void RejectsHatchOnHorizontalBoundary()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    left,
                    right);

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    left.Id,
                    right.Id,
                    boundary,
                    ModulePassageType.Hatch,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePassagePlacementFailureReason
                        .PassageTypeIncompatibleWithBoundary));
        }

        [Test]
        public void RejectsPassageBetweenSeparatedModules()
        {
            ModuleInstance first =
                PlaceSmall(0, 0);

            ModuleInstance second =
                PlaceSmall(2, 0);

            var boundary =
                new GridBoundarySegment(
                    new GridPosition(0, 0),
                    new GridPosition(1, 0));

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    first.Id,
                    second.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePassagePlacementFailureReason
                        .ModulesNotAdjacent));
        }

        [Test]
        public void RejectsBoundaryNotSharedByRequestedModules()
        {
            ModuleInstance left =
                PlaceLarge(0, 0);

            ModuleInstance right =
                PlaceLarge(2, 0);

            var internalBoundary =
                new GridBoundarySegment(
                    new GridPosition(0, 0),
                    new GridPosition(0, 1));

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    left.Id,
                    right.Id,
                    internalBoundary,
                    ModulePassageType.Hatch,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePassagePlacementFailureReason
                        .BoundaryNotShared));
        }

        [Test]
        public void RejectsSecondPassageOnSameBoundary()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    left,
                    right);

            ModulePassagePlacementResult first =
                _grid.TryAddPassage(
                    left.Id,
                    right.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            ModulePassagePlacementResult second =
                _grid.TryAddPassage(
                    left.Id,
                    right.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.False);

            Assert.That(
                second.FailureReason,
                Is.EqualTo(
                    ModulePassagePlacementFailureReason
                        .BoundaryAlreadyHasPassage));
        }

        [Test]
        public void LargeModulesCanHaveMultipleSeparateDoors()
        {
            ModuleInstance left =
                PlaceLarge(0, 0);

            ModuleInstance right =
                PlaceLarge(2, 0);

            _grid.TryGetModuleAdjacency(
                left.Id,
                right.Id,
                out ModuleAdjacency adjacency);

            Assert.That(
                adjacency.SharedBoundaryCount,
                Is.EqualTo(2));

            foreach (
                GridBoundarySegment boundary
                in adjacency.SharedBoundaries)
            {
                ModulePassagePlacementResult result =
                    _grid.TryAddPassage(
                        left.Id,
                        right.Id,
                        boundary,
                        ModulePassageType.Door,
                        ModulePassageTraversalMode
                            .Bidirectional);

                Assert.That(
                    result.IsSuccess,
                    Is.True);
            }

            Assert.That(
                _grid.PassageCount,
                Is.EqualTo(2));
        }

        [Test]
        public void OneWayPassagePreservesRequestedDirection()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    left,
                    right);

            ModulePassage passage =
                _grid.TryAddPassage(
                        sourceModuleId: right.Id,
                        targetModuleId: left.Id,
                        boundary: boundary,
                        type: ModulePassageType.Door,
                        traversalMode:
                            ModulePassageTraversalMode
                                .SourceToTargetOnly)
                    .Passage;

            Assert.That(
                passage.AllowsDirection(
                    right.Id,
                    left.Id),
                Is.True);

            Assert.That(
                passage.AllowsDirection(
                    left.Id,
                    right.Id),
                Is.False);
        }

        [Test]
        public void PassageStateCanChange()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage passage =
                AddDoor(
                    left,
                    right);

            passage.SetState(
                ModulePassageState.Locked);

            Assert.That(
                passage.State,
                Is.EqualTo(
                    ModulePassageState.Locked));
        }

        [Test]
        public void RemovingModuleRemovesConnectedPassages()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance middle =
                PlaceSmall(1, 0);

            ModuleInstance right =
                PlaceSmall(2, 0);

            AddDoor(left, middle);
            AddDoor(middle, right);

            Assert.That(
                _grid.PassageCount,
                Is.EqualTo(2));

            _grid.TryRemoveModule(
                middle.Id,
                out _);

            Assert.That(
                _grid.PassageCount,
                Is.Zero);
        }

        [Test]
        public void ReturnsOnlyPassagesConnectedToModule()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance middle =
                PlaceSmall(1, 0);

            ModuleInstance right =
                PlaceSmall(2, 0);

            AddDoor(left, middle);
            AddDoor(middle, right);

            bool found =
                _grid.TryGetPassagesForModule(
                    left.Id,
                    out IReadOnlyList<ModulePassage>
                        passages);

            Assert.That(found, Is.True);
            Assert.That(passages.Count, Is.EqualTo(1));

            Assert.That(
                passages[0].ConnectsModule(
                    left.Id),
                Is.True);
        }

        private ModulePassage AddDoor(
            ModuleInstance source,
            ModuleInstance target)
        {
            GridBoundarySegment boundary =
                GetOnlyBoundary(
                    source,
                    target);

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    source.Id,
                    target.Id,
                    boundary,
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(result.IsSuccess, Is.True);

            return result.Passage;
        }

        private GridBoundarySegment GetOnlyBoundary(
            ModuleInstance source,
            ModuleInstance target)
        {
            bool found =
                _grid.TryGetModuleAdjacency(
                    source.Id,
                    target.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(found, Is.True);

            Assert.That(
                adjacency.SharedBoundaryCount,
                Is.EqualTo(1));

            return adjacency.SharedBoundaries[0];
        }

        private ModuleInstance PlaceSmall(
            int x,
            int deck)
        {
            return Place(
                ModuleDefinitionIds
                    .SmallMachineRoom,
                x,
                deck);
        }

        private ModuleInstance PlaceLarge(
            int x,
            int deck)
        {
            return Place(
                ModuleDefinitionIds
                    .LargeMachineRoom,
                x,
                deck);
        }

        private ModuleInstance Place(
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

            Assert.That(result.IsSuccess, Is.True);

            return result.Module;
        }
    }
}