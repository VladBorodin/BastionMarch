using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class ModuleRouteFinderTests
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
        public void SameModuleReturnsEmptyRoute()
        {
            ModuleInstance module =
                PlaceSmall(0, 0);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    module.Id,
                    module.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Route.StepCount, Is.Zero);

            Assert.That(
                result.Route.ModuleIds.Count,
                Is.EqualTo(1));

            Assert.That(
                result.Route.ModuleIds[0],
                Is.EqualTo(module.Id));
        }

        [Test]
        public void DirectDoorCreatesSingleStepRoute()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(left, right);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    left.Id,
                    right.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Route.StepCount, Is.EqualTo(1));

            Assert.That(
                result.Route.RequiredMovementActions,
                Is.EqualTo(1));

            Assert.That(
                result.Route.Steps[0].PassageId,
                Is.EqualTo(door.Id));

            Assert.That(
                result.Route.Steps[0].FromModuleId,
                Is.EqualTo(left.Id));

            Assert.That(
                result.Route.Steps[0].ToModuleId,
                Is.EqualTo(right.Id));
        }

        [Test]
        public void RouteContainsContinuousModuleSequence()
        {
            ModuleInstance first =
                PlaceSmall(0, 0);

            ModuleInstance middle =
                PlaceSmall(1, 0);

            ModuleInstance last =
                PlaceSmall(2, 0);

            AddDoor(first, middle);
            AddDoor(middle, last);

            ModuleRoute route =
                _grid.FindModuleRoute(
                        first.Id,
                        last.Id)
                    .Route;

            Assert.That(route.StepCount, Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[]
                {
                    first.Id,
                    middle.Id,
                    last.Id
                },
                route.ModuleIds);
        }

        [Test]
        public void FinderChoosesShortestRoute()
        {
            ModuleInstance source =
                PlaceSmall(0, 0);

            ModuleInstance shortMiddle =
                PlaceSmall(1, 0);

            ModuleInstance target =
                PlaceSmall(2, 0);

            ModuleInstance upperLeft =
                PlaceSmall(0, 1);

            ModuleInstance upperMiddle =
                PlaceSmall(1, 1);

            ModuleInstance upperRight =
                PlaceSmall(2, 1);

            AddDoor(source, shortMiddle);
            AddDoor(shortMiddle, target);

            AddHatch(source, upperLeft);
            AddDoor(upperLeft, upperMiddle);
            AddDoor(upperMiddle, upperRight);
            AddHatch(upperRight, target);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    source.Id,
                    target.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Route.StepCount, Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[]
                {
                    source.Id,
                    shortMiddle.Id,
                    target.Id
                },
                result.Route.ModuleIds);
        }

        [Test]
        public void RepeatedSearchReturnsSameRoute()
        {
            ModuleInstance lowerLeft =
                PlaceSmall(0, 0);

            ModuleInstance lowerRight =
                PlaceSmall(1, 0);

            ModuleInstance upperLeft =
                PlaceSmall(0, 1);

            ModuleInstance upperRight =
                PlaceSmall(1, 1);

            AddDoor(
                lowerLeft,
                lowerRight);

            AddHatch(
                lowerRight,
                upperRight);

            AddHatch(
                lowerLeft,
                upperLeft);

            AddDoor(
                upperLeft,
                upperRight);

            ModuleRoute first =
                _grid.FindModuleRoute(
                        lowerLeft.Id,
                        upperRight.Id)
                    .Route;

            ModuleRoute second =
                _grid.FindModuleRoute(
                        lowerLeft.Id,
                        upperRight.Id)
                    .Route;

            CollectionAssert.AreEqual(
                first.Steps
                    .Select(step => step.PassageId)
                    .ToArray(),
                second.Steps
                    .Select(step => step.PassageId)
                    .ToArray());
        }

        [Test]
        public void ClosedDoorReportsTraversalBlocked()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(left, right);

            door.SetState(
                ModulePassageState.Closed);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    left.Id,
                    right.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            Assert.That(
                result.BlockingAssessments.Count,
                Is.EqualTo(1));

            Assert.That(
                result.BlockingAssessments[0]
                    .FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageClosed));
        }

        [Test]
        public void WrongOneWayDirectionReportsDirectionBlock()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            AddDoor(
                left,
                right,
                ModulePassageTraversalMode
                    .SourceToTargetOnly);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    right.Id,
                    left.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            Assert.That(
                result.BlockingAssessments.Count,
                Is.EqualTo(1));

            Assert.That(
                result.BlockingAssessments[0]
                    .FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .DirectionNotAllowed));
        }

        [Test]
        public void MissingPassageChainReportsNoStructuralConnection()
        {
            ModuleInstance first =
                PlaceSmall(0, 0);

            ModuleInstance separated =
                PlaceSmall(2, 0);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    first.Id,
                    separated.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .NoStructuralConnection));

            Assert.That(
                result.BlockingAssessments,
                Is.Empty);
        }

        [Test]
        public void MissingSourceModuleIsReported()
        {
            ModuleInstance target =
                PlaceSmall(0, 0);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    Guid.NewGuid(),
                    target.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .SourceModuleNotFound));
        }

        [Test]
        public void MissingTargetModuleIsReported()
        {
            ModuleInstance source =
                PlaceSmall(0, 0);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    source.Id,
                    Guid.NewGuid());

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TargetModuleNotFound));
        }

        [Test]
        public void VerticalAndHorizontalPassagesFormRoute()
        {
            ModuleInstance lower =
                PlaceSmall(0, 0);

            ModuleInstance upper =
                PlaceSmall(0, 1);

            ModuleInstance upperRight =
                PlaceSmall(1, 1);

            AddHatch(lower, upper);
            AddDoor(upper, upperRight);

            ModuleRouteSearchResult result =
                _grid.FindModuleRoute(
                    lower.Id,
                    upperRight.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Route.StepCount, Is.EqualTo(2));

            Assert.That(
                result.Route.Steps[0].PassageType,
                Is.EqualTo(
                    ModulePassageType.Hatch));

            Assert.That(
                result.Route.Steps[1].PassageType,
                Is.EqualTo(
                    ModulePassageType.Door));
        }

        [Test]
        public void CustomPolicyCanAllowLockedPassage()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(left, right);

            door.SetState(
                ModulePassageState.Locked);

            ModuleRouteSearchResult defaultResult =
                _grid.FindModuleRoute(
                    left.Id,
                    right.Id);

            ModuleRouteSearchResult customResult =
                _grid.FindModuleRoute(
                    left.Id,
                    right.Id,
                    new AllowEveryPassagePolicy());

            Assert.That(
                defaultResult.IsSuccess,
                Is.False);

            Assert.That(
                customResult.IsSuccess,
                Is.True);
        }

        [Test]
        public void RouteReflectsPassageStateChanges()
        {
            ModuleInstance first =
                PlaceSmall(0, 0);

            ModuleInstance middle =
                PlaceSmall(1, 0);

            ModuleInstance last =
                PlaceSmall(2, 0);

            AddDoor(first, middle);

            ModulePassage secondDoor =
                AddDoor(middle, last);

            ModuleRouteSearchResult before =
                _grid.FindModuleRoute(
                    first.Id,
                    last.Id);

            secondDoor.SetState(
                ModulePassageState.Blocked);

            ModuleRouteSearchResult after =
                _grid.FindModuleRoute(
                    first.Id,
                    last.Id);

            Assert.That(before.IsSuccess, Is.True);
            Assert.That(after.IsSuccess, Is.False);

            Assert.That(
                after.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            Assert.That(
                after.BlockingAssessments.Count,
                Is.EqualTo(1));

            Assert.That(
                after.BlockingAssessments[0]
                    .PassageId,
                Is.EqualTo(secondDoor.Id));
        }

        private ModulePassage AddDoor(
            ModuleInstance source,
            ModuleInstance target,
            ModulePassageTraversalMode traversalMode =
                ModulePassageTraversalMode.Bidirectional)
        {
            return AddPassage(
                source,
                target,
                ModulePassageType.Door,
                traversalMode);
        }

        private ModulePassage AddHatch(
            ModuleInstance source,
            ModuleInstance target)
        {
            return AddPassage(
                source,
                target,
                ModulePassageType.Hatch,
                ModulePassageTraversalMode.Bidirectional);
        }

        private ModulePassage AddPassage(
            ModuleInstance source,
            ModuleInstance target,
            ModulePassageType type,
            ModulePassageTraversalMode traversalMode)
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

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    source.Id,
                    target.Id,
                    adjacency.SharedBoundaries[0],
                    type,
                    traversalMode);

            Assert.That(result.IsSuccess, Is.True);

            return result.Passage;
        }

        private ModuleInstance PlaceSmall(
            int x,
            int deck)
        {
            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom),
                    new GridPosition(
                        x,
                        deck));

            Assert.That(result.IsSuccess, Is.True);

            return result.Module;
        }

        private sealed class
            AllowEveryPassagePolicy
            : IModulePassageTraversalPolicy
        {
            public ModulePassageTraversalAssessment Evaluate(
                ModulePassageTraversalContext context)
            {
                return
                    ModulePassageTraversalAssessment.Allowed(
                        context);
            }
        }
    }
}