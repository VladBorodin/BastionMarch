using System.Linq;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class RoutePresentationStateTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void CapturesSuccessfulRoute()
        {
            Bastion bastion =
                CreateThreeModuleChain(
                    out ModuleInstance first,
                    out ModuleInstance second,
                    out ModuleInstance third,
                    out ModulePassage firstPassage,
                    out ModulePassage secondPassage);

            ModuleRouteSearchResult result =
                bastion.FindModuleRoute(
                    first.Id,
                    third.Id);

            Assert.That(
                result.IsSuccess,
                Is.True);

            RoutePresentationState state =
                RoutePresentationStateFactory
                    .CaptureSearchResult(
                        first.Id,
                        third.Id,
                        result);

            Assert.That(
                state.IsSuccess,
                Is.True);

            Assert.That(
                state.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason.None));

            Assert.That(
                state.StepCount,
                Is.EqualTo(2));

            Assert.That(
                state.RequiredMovementActions,
                Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[]
                {
                    first.Id,
                    second.Id,
                    third.Id
                },
                state.ModuleIds);

            Assert.That(
                state.Steps[0].PassageId,
                Is.EqualTo(
                    firstPassage.Id));

            Assert.That(
                state.Steps[1].PassageId,
                Is.EqualTo(
                    secondPassage.Id));

            Assert.That(
                state.HasBlockingAssessments,
                Is.False);
        }

        [Test]
        public void CapturesSameModuleRouteWithoutSteps()
        {
            var bastion =
                new Bastion(
                    name: "same-module-route",
                    width: 3,
                    deckCount: 1);

            ModuleInstance module =
                InstallSmallModule(
                    bastion,
                    x: 0);

            ModuleRouteSearchResult result =
                bastion.FindModuleRoute(
                    module.Id,
                    module.Id);

            RoutePresentationState state =
                RoutePresentationStateFactory
                    .CaptureSearchResult(
                        module.Id,
                        module.Id,
                        result);

            Assert.That(
                state.IsSuccess,
                Is.True);

            Assert.That(
                state.StepCount,
                Is.EqualTo(0));

            Assert.That(
                state.RequiredMovementActions,
                Is.EqualTo(0));

            CollectionAssert.AreEqual(
                new[]
                {
                    module.Id
                },
                state.ModuleIds);
        }

        [Test]
        public void CapturesTraversalBlockedResult()
        {
            Bastion bastion =
                CreateThreeModuleChain(
                    out ModuleInstance first,
                    out ModuleInstance second,
                    out ModuleInstance third,
                    out _,
                    out ModulePassage blockedPassage);

            blockedPassage.SetState(
                ModulePassageState.Locked);

            ModuleRouteSearchResult result =
                bastion.FindModuleRoute(
                    first.Id,
                    third.Id);

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            RoutePresentationState state =
                RoutePresentationStateFactory
                    .CaptureSearchResult(
                        first.Id,
                        third.Id,
                        result);

            Assert.That(
                state.IsSuccess,
                Is.False);

            Assert.That(
                state.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            Assert.That(
                state.StepCount,
                Is.EqualTo(0));

            Assert.That(
                state.ModuleIds,
                Is.Empty);

            Assert.That(
                state.HasBlockingAssessments,
                Is.True);

            RouteBlockerPresentationState blocker =
                state.BlockingAssessments
                    .Single();

            Assert.That(
                blocker.PassageId,
                Is.EqualTo(
                    blockedPassage.Id));

            Assert.That(
                blocker.FromModuleId,
                Is.EqualTo(
                    second.Id));

            Assert.That(
                blocker.ToModuleId,
                Is.EqualTo(
                    third.Id));

            Assert.That(
                blocker.FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageLocked));
        }

        [Test]
        public void CapturedBlockedRouteDoesNotChangeWithSimulation()
        {
            Bastion bastion =
                CreateThreeModuleChain(
                    out ModuleInstance first,
                    out _,
                    out ModuleInstance third,
                    out _,
                    out ModulePassage blockedPassage);

            blockedPassage.SetState(
                ModulePassageState.Locked);

            ModuleRouteSearchResult initialResult =
                bastion.FindModuleRoute(
                    first.Id,
                    third.Id);

            RoutePresentationState captured =
                RoutePresentationStateFactory
                    .CaptureSearchResult(
                        first.Id,
                        third.Id,
                        initialResult);

            blockedPassage.SetState(
                ModulePassageState.Open);

            ModuleRouteSearchResult newResult =
                bastion.FindModuleRoute(
                    first.Id,
                    third.Id);

            Assert.That(
                newResult.IsSuccess,
                Is.True);

            Assert.That(
                captured.IsSuccess,
                Is.False);

            Assert.That(
                captured.FailureReason,
                Is.EqualTo(
                    ModuleRouteFailureReason
                        .TraversalBlocked));

            Assert.That(
                captured.BlockingAssessments
                    .Single()
                    .FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageLocked));
        }

        private Bastion CreateThreeModuleChain(
            out ModuleInstance first,
            out ModuleInstance second,
            out ModuleInstance third,
            out ModulePassage firstPassage,
            out ModulePassage secondPassage)
        {
            var bastion =
                new Bastion(
                    name: "route-presentation-test",
                    width: 4,
                    deckCount: 1);

            first =
                InstallSmallModule(
                    bastion,
                    x: 0);

            second =
                InstallSmallModule(
                    bastion,
                    x: 1);

            third =
                InstallSmallModule(
                    bastion,
                    x: 2);

            firstPassage =
                AddDoor(
                    bastion,
                    first,
                    second);

            secondPassage =
                AddDoor(
                    bastion,
                    second,
                    third);

            return bastion;
        }

        private ModuleInstance InstallSmallModule(
            Bastion bastion,
            int x)
        {
            return bastion
                .TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom),
                    new GridPosition(
                        x,
                        0))
                .Module;
        }

        private static ModulePassage AddDoor(
            Bastion bastion,
            ModuleInstance source,
            ModuleInstance target)
        {
            bool adjacencyFound =
                bastion.TryGetModuleAdjacency(
                    source.Id,
                    target.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(
                adjacencyFound,
                Is.True);

            ModulePassagePlacementResult result =
                bastion.TryInstallPassage(
                    source.Id,
                    target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Passage;
        }
    }
}