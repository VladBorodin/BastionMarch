using System.Collections.Generic;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;
using UnityEngine;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class RouteViewTests
    {
        private readonly List<GameObject>
            _createdObjects = new();

        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (
                GameObject createdObject
                in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(
                        createdObject);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void HorizontalRouteUsesModuleAndBoundaryCenters()
        {
            var bastion =
                new Bastion(
                    name: "horizontal-route-view",
                    width: 4,
                    deckCount: 1);

            ModuleInstance first =
                InstallSmallModule(
                    bastion,
                    x: 0,
                    deck: 0);

            ModuleInstance second =
                InstallSmallModule(
                    bastion,
                    x: 1,
                    deck: 0);

            ModuleInstance third =
                InstallSmallModule(
                    bastion,
                    x: 2,
                    deck: 0);

            AddPassage(
                bastion,
                first,
                second,
                ModulePassageType.Door);

            AddPassage(
                bastion,
                second,
                third,
                ModulePassageType.Door);

            RouteView routeView =
                CreateRouteView();

            BastionPresentationState bastionState =
                BastionPresentationStateFactory
                    .Capture(bastion);

            RoutePresentationState routeState =
                CaptureRoute(
                    bastion,
                    first.Id,
                    third.Id);

            routeView.Show(
                bastionState,
                routeState);

            Assert.That(
                routeView.RenderedPointCount,
                Is.EqualTo(5));

            AssertPoint(
                routeView.GetRenderedPoint(0),
                1.5f,
                1f);

            AssertPoint(
                routeView.GetRenderedPoint(1),
                3f,
                1f);

            AssertPoint(
                routeView.GetRenderedPoint(2),
                4.5f,
                1f);

            AssertPoint(
                routeView.GetRenderedPoint(3),
                6f,
                1f);

            AssertPoint(
                routeView.GetRenderedPoint(4),
                7.5f,
                1f);

            Assert.That(
                routeView.IsSuccessfulRoute,
                Is.True);
        }

        [Test]
        public void VerticalRouteUsesSharedDeckBoundary()
        {
            var bastion =
                new Bastion(
                    name: "vertical-route-view",
                    width: 2,
                    deckCount: 2);

            ModuleInstance lower =
                InstallSmallModule(
                    bastion,
                    x: 0,
                    deck: 0);

            ModuleInstance upper =
                InstallSmallModule(
                    bastion,
                    x: 0,
                    deck: 1);

            AddPassage(
                bastion,
                lower,
                upper,
                ModulePassageType.Ladder);

            RouteView routeView =
                CreateRouteView();

            RoutePresentationState routeState =
                CaptureRoute(
                    bastion,
                    lower.Id,
                    upper.Id);

            routeView.Show(
                BastionPresentationStateFactory
                    .Capture(bastion),
                routeState);

            Assert.That(
                routeView.RenderedPointCount,
                Is.EqualTo(3));

            AssertPoint(
                routeView.GetRenderedPoint(0),
                1.5f,
                1f);

            AssertPoint(
                routeView.GetRenderedPoint(1),
                1.5f,
                2f);

            AssertPoint(
                routeView.GetRenderedPoint(2),
                1.5f,
                3f);
        }

        [Test]
        public void FailedRouteClearsRenderedLine()
        {
            var bastion =
                new Bastion(
                    name: "failed-route-view",
                    width: 3,
                    deckCount: 1);

            ModuleInstance first =
                InstallSmallModule(
                    bastion,
                    x: 0,
                    deck: 0);

            ModuleInstance second =
                InstallSmallModule(
                    bastion,
                    x: 1,
                    deck: 0);

            ModulePassage passage =
                AddPassage(
                    bastion,
                    first,
                    second,
                    ModulePassageType.Door);

            passage.SetState(
                ModulePassageState.Locked);

            RoutePresentationState routeState =
                CaptureRoute(
                    bastion,
                    first.Id,
                    second.Id);

            Assert.That(
                routeState.IsSuccess,
                Is.False);

            RouteView routeView =
                CreateRouteView();

            routeView.Show(
                BastionPresentationStateFactory
                    .Capture(bastion),
                routeState);

            Assert.That(
                routeView.HasRoute,
                Is.True);

            Assert.That(
                routeView.IsSuccessfulRoute,
                Is.False);

            Assert.That(
                routeView.RenderedPointCount,
                Is.EqualTo(0));

            Assert.That(
                routeView.GetComponent<
                    LineRenderer>().enabled,
                Is.False);
        }

        private RouteView CreateRouteView()
        {
            var root =
                new GameObject(
                    "TestBastionView");

            _createdObjects.Add(
                root);

            root.AddComponent<
                BastionGridLayout>();

            var routeObject =
                new GameObject(
                    "RouteView");

            routeObject.transform.SetParent(
                root.transform,
                false);

            routeObject.AddComponent<
                LineRenderer>();

            return routeObject.AddComponent<
                RouteView>();
        }

        private ModuleInstance InstallSmallModule(
            Bastion bastion,
            int x,
            int deck)
        {
            ModulePlacementResult result =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom),
                    new GridPosition(
                        x,
                        deck));

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Module;
        }

        private static ModulePassage AddPassage(
            Bastion bastion,
            ModuleInstance source,
            ModuleInstance target,
            ModulePassageType type)
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
                    type,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Passage;
        }

        private static RoutePresentationState
            CaptureRoute(
                Bastion bastion,
                System.Guid sourceModuleId,
                System.Guid targetModuleId)
        {
            ModuleRouteSearchResult result =
                bastion.FindModuleRoute(
                    sourceModuleId,
                    targetModuleId);

            return RoutePresentationStateFactory
                .CaptureSearchResult(
                    sourceModuleId,
                    targetModuleId,
                    result);
        }

        private static void AssertPoint(
            Vector3 actual,
            float expectedX,
            float expectedY)
        {
            Assert.That(
                actual.x,
                Is.EqualTo(expectedX)
                    .Within(0.0001f));

            Assert.That(
                actual.y,
                Is.EqualTo(expectedY)
                    .Within(0.0001f));

            Assert.That(
                actual.z,
                Is.EqualTo(0f)
                    .Within(0.0001f));
        }
    }
}