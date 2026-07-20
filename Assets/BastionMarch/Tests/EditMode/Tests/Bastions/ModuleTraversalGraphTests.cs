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
    public sealed class ModuleTraversalGraphTests
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
        public void OpenBidirectionalDoorCreatesTwoEdges()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(
                    left,
                    right,
                    ModulePassageTraversalMode
                        .Bidirectional);

            ModuleConnectivityGraph graph =
                _grid.BuildTraversalGraph();

            Assert.That(
                graph.EdgeCount,
                Is.EqualTo(2));

            Assert.That(
                graph.HasTraversal(
                    left.Id,
                    right.Id),
                Is.True);

            Assert.That(
                graph.HasTraversal(
                    right.Id,
                    left.Id),
                Is.True);

            Assert.That(
                graph.Edges.All(
                    edge =>
                        edge.PassageId ==
                        door.Id),
                Is.True);
        }

        [Test]
        public void OneWayDoorCreatesOnlyAllowedEdge()
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

            ModuleConnectivityGraph graph =
                _grid.BuildTraversalGraph();

            Assert.That(
                graph.EdgeCount,
                Is.EqualTo(1));

            Assert.That(
                graph.HasTraversal(
                    left.Id,
                    right.Id),
                Is.True);

            Assert.That(
                graph.HasTraversal(
                    right.Id,
                    left.Id),
                Is.False);
        }

        [TestCase(
            ModulePassageState.Closed,
            ModulePassageTraversalFailureReason
                .PassageClosed)]
        [TestCase(
            ModulePassageState.Locked,
            ModulePassageTraversalFailureReason
                .PassageLocked)]
        [TestCase(
            ModulePassageState.Blocked,
            ModulePassageTraversalFailureReason
                .PassageBlocked)]
        [TestCase(
            ModulePassageState.Destroyed,
            ModulePassageTraversalFailureReason
                .PassageDestroyed)]
        public void UnavailablePassageDoesNotCreateEdges(
            ModulePassageState state,
            ModulePassageTraversalFailureReason
                expectedFailureReason)
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(
                    left,
                    right,
                    ModulePassageTraversalMode
                        .Bidirectional);

            door.SetState(state);

            ModulePassageTraversalAssessment assessment =
                _grid.AssessPassageTraversal(
                    door.Id,
                    left.Id,
                    right.Id);

            ModuleConnectivityGraph graph =
                _grid.BuildTraversalGraph();

            Assert.That(
                assessment.IsAllowed,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    expectedFailureReason));

            Assert.That(
                graph.EdgeCount,
                Is.Zero);
        }

        [Test]
        public void IsolatedModuleRemainsGraphNode()
        {
            ModuleInstance isolated =
                PlaceSmall(0, 0);

            ModuleConnectivityGraph graph =
                _grid.BuildTraversalGraph();

            Assert.That(
                graph.ModuleCount,
                Is.EqualTo(1));

            Assert.That(
                graph.ContainsModule(
                    isolated.Id),
                Is.True);

            bool found =
                graph.TryGetOutgoingEdges(
                    isolated.Id,
                    out IReadOnlyList<
                        ModuleTraversalEdge> edges);

            Assert.That(found, Is.True);
            Assert.That(edges, Is.Empty);
        }

        [Test]
        public void MultipleDoorsCreateMultipleEdges()
        {
            ModuleInstance left =
                PlaceLarge(0, 0);

            ModuleInstance right =
                PlaceLarge(2, 0);

            _grid.TryGetModuleAdjacency(
                left.Id,
                right.Id,
                out ModuleAdjacency adjacency);

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

            ModuleConnectivityGraph graph =
                _grid.BuildTraversalGraph();

            graph.TryGetOutgoingEdges(
                left.Id,
                out IReadOnlyList<
                    ModuleTraversalEdge> outgoing);

            Assert.That(
                outgoing.Count(
                    edge =>
                        edge.ToModuleId ==
                        right.Id),
                Is.EqualTo(2));

            Assert.That(
                graph.EdgeCount,
                Is.EqualTo(4));
        }

        [Test]
        public void MissingPassageReturnsDiagnosticReason()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassageTraversalAssessment assessment =
                _grid.AssessPassageTraversal(
                    Guid.NewGuid(),
                    left.Id,
                    right.Id);

            Assert.That(
                assessment.IsAllowed,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageNotFound));
        }

        [Test]
        public void PassageCannotBeUsedForOtherModules()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance middle =
                PlaceSmall(1, 0);

            ModuleInstance right =
                PlaceSmall(2, 0);

            ModulePassage door =
                AddDoor(
                    left,
                    middle,
                    ModulePassageTraversalMode
                        .Bidirectional);

            ModulePassageTraversalAssessment assessment =
                _grid.AssessPassageTraversal(
                    door.Id,
                    left.Id,
                    right.Id);

            Assert.That(
                assessment.IsAllowed,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageDoesNotConnectModules));
        }

        [Test]
        public void CustomPolicyCanOverrideStateInterpretation()
        {
            ModuleInstance left =
                PlaceSmall(0, 0);

            ModuleInstance right =
                PlaceSmall(1, 0);

            ModulePassage door =
                AddDoor(
                    left,
                    right,
                    ModulePassageTraversalMode
                        .Bidirectional);

            door.SetState(
                ModulePassageState.Locked);

            ModuleConnectivityGraph defaultGraph =
                _grid.BuildTraversalGraph();

            ModuleConnectivityGraph customGraph =
                _grid.BuildTraversalGraph(
                    new AllowEveryDirectionPolicy());

            Assert.That(
                defaultGraph.EdgeCount,
                Is.Zero);

            Assert.That(
                customGraph.EdgeCount,
                Is.EqualTo(2));
        }

        private ModulePassage AddDoor(
            ModuleInstance source,
            ModuleInstance target,
            ModulePassageTraversalMode traversalMode)
        {
            _grid.TryGetModuleAdjacency(
                source.Id,
                target.Id,
                out ModuleAdjacency adjacency);

            Assert.That(
                adjacency.SharedBoundaryCount,
                Is.EqualTo(1));

            ModulePassagePlacementResult result =
                _grid.TryAddPassage(
                    source.Id,
                    target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    traversalMode);

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Passage;
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

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Module;
        }

        private sealed class
            AllowEveryDirectionPolicy
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