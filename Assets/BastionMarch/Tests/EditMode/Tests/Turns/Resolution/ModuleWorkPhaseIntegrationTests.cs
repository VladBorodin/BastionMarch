using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class ModuleWorkPhaseIntegrationTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance _source;
        private ModuleInstance _target;
        private ModulePassage _passage;
        private TurnCycle _cycle;
        private OrderExecutionRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            ModuleDefinition definition = InitialModuleDefinitions
                .CreateCatalog()
                .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("work-phase-test", 8, 2);

            _source = _bastion.TryInstallModule(
                definition, new GridPosition(0, 0)).Module;

            _target = _bastion.TryInstallModule(
                definition, new GridPosition(1, 0)).Module;

            _brigade = new Brigade(1, BrigadeType.Mechanic, 6, 6);

            Assert.That(_bastion.TryAddBrigade(_brigade), Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _brigade.Id, _source.Id).IsSuccess,
                Is.True);

            Assert.That(
                _bastion.TryGetModuleAdjacency(
                    _source.Id, _target.Id,
                    out ModuleAdjacency adjacency),
                Is.True);

            ModulePassagePlacementResult placement =
                _bastion.TryInstallPassage(
                    _source.Id,
                    _target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    ModulePassageTraversalMode.Bidirectional);

            Assert.That(placement.IsSuccess, Is.True);

            _passage = placement.Passage;
        }

        [Test]
        public void WorkingBrigadeTicksInBothFreePhases()
        {
            StartWork();

            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult first = resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(first.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_source.Id));
            Assert.That(second.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_source.Id));

            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void MoveClearsWorkAndNextFreePhaseStandsBy()
        {
            StartWork();

            TurnPhaseResolver resolver = CreateResolver(CreateMove());

            PhaseResolutionResult first = resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(first.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTickCount, Is.Zero);
            Assert.That(second.ModuleWorkTickCount, Is.Zero);
            Assert.That(second.SelectedActivityCount, Is.Zero);

            AssertLocation(_target);

            _bastion.TryGetBrigadeOperationalState(
                _brigade.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.False);
            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
        }

        [Test]
        public void ExplicitWorkStartAfterMoveProducesNextPhaseTick()
        {
            TurnPhaseResolver resolver = CreateResolver(CreateMove());

            resolver.ResolvePhase(1);

            // Отдельное решение о начале работы после перемещения.
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_target.Id));
            Assert.That(second.ModuleWorkTicks[0].WorkingPersonnel,
                Is.EqualTo(6));
            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
        }

        [Test]
        public void FailedMoveSuppressesWorkOnlyInReservedPhase()
        {
            StartWork();
            _passage.SetState(ModulePassageState.Locked);

            TurnPhaseResolver resolver = CreateResolver(CreateMove());

            PhaseResolutionResult first = resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(first.RejectedOrderCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTickCount, Is.Zero);
            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));

            AssertLocation(_source);
            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
        }

        [Test]
        public void WorkTickSnapshotsRemainImmutableBetweenPhases()
        {
            StartWork();

            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult first = resolver.ResolvePhase(1);

            _brigade.ApplyCasualties(2);

            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(first.ModuleWorkTicks[0].WorkingPersonnel,
                Is.EqualTo(6));
            Assert.That(second.ModuleWorkTicks[0].WorkingPersonnel,
                Is.EqualTo(4));
        }

        [Test]
        public void UnknownActivityIsSelectedButNotCountedAsModuleWork()
        {
            TurnPhaseResolver resolver = CreateResolver(
                null,
                id => new TestActivity(id));

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.EqualTo(1));
            Assert.That(result.ModuleWorkTickCount, Is.Zero);
        }

        [Test]
        public void MissingBastionRejectsModuleWorkBeforeAdvancingTime()
        {
            StartWork();

            TurnPhaseResolver resolver = CreateResolver(
                null,
                id => ModuleWorkActivityResolver.Resolve(_bastion, id),
                includeBastion: false);

            Assert.Throws<NotSupportedException>(() =>
                resolver.ResolvePhase(1));

            Assert.That(_cycle.CurrentActionPhase, Is.EqualTo(1));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void WorkTickCannotBeRecordedTwiceForSamePhase()
        {
            StartWork();

            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult first = resolver.ResolvePhase(1);

            Assert.That(first.ModuleWorkTickCount, Is.EqualTo(1));

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(1));

            Assert.That(_cycle.CurrentActionPhase, Is.EqualTo(2));
        }

        private TurnPhaseResolver CreateResolver(
            MoveBrigadeOrder order = null,
            Func<Guid, IBrigadeActivity> activityResolver = null,
            bool includeBastion = true)
        {
            var participant = new TurnBrigadeParticipant(
                _brigade.Id, _brigade.Number);

            _cycle = new TurnCycle(1, 2, new[] { participant });
            _registry = new OrderExecutionRegistry();

            var draft = new TurnPlanDraft(
                _cycle.TurnNumber,
                _cycle.ActionPhaseCount,
                _cycle.ActiveBrigades);

            if (order != null)
            {
                Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

                Assert.That(
                    draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId, 1, _brigade.Id)).IsSuccess,
                    Is.True);
            }

            ConfirmedTurnPlan plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            Assert.That(_cycle.TryConfirmPlanning().IsSuccess, Is.True);

            return new TurnPhaseResolver(
                _cycle,
                plan,
                _registry,
                activityResolver ??
                    (id => ModuleWorkActivityResolver.Resolve(_bastion, id)),
                includeBastion ? _bastion : null);
        }

        private MoveBrigadeOrder CreateMove()
        {
            return new MoveBrigadeOrder(
                Guid.NewGuid(),
                _brigade.Id,
                _source.Id,
                _target.Id,
                _passage.Id);
        }

        private void StartWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);
        }

        private void AssertLocation(ModuleInstance expected)
        {
            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    _brigade.Id, out ModuleInstance location),
                Is.True);

            Assert.That(location.Id, Is.EqualTo(expected.Id));
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId) : base(brigadeId)
            {
            }
        }
    }
}