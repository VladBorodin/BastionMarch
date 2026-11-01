
using System;
using System.Collections.Generic;
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
    public sealed class DefaultModuleWorkPhaseIntegrationTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private BrigadeWorkProfileCatalog _profiles;
        private ModuleInstance _source;
        private ModuleInstance _target;
        private ModuleInstance _quarters;
        private ModulePassage _passage;
        private TurnCycle _cycle;
        private OrderExecutionRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();
            ModuleDefinition machine =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _profiles = InitialBrigadeWorkProfiles.CreateCatalog();
            _bastion = new Bastion("default-phase-test", 8, 2);

            _source = Install(machine, 0);
            _target = Install(machine, 1);

            _quarters = Install(
                catalog.GetRequired(
                    ModuleDefinitionIds.StandardCrewQuarters), 3);

            _brigade = AddBrigade(1, BrigadeType.Mechanic, _source);

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
        public void FreePhaseStartsDefaultWorkAndRecordsTick()
        {
            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.EqualTo(1));
            Assert.That(result.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(result.ModuleWorkTicks[0].BrigadeId,
                Is.EqualTo(_brigade.Id));
            Assert.That(result.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_source.Id));
            Assert.That(result.ModuleWorkTicks[0].WorkingPersonnel,
                Is.EqualTo(6));

            AssertWorking(_brigade, _source, true);
            Assert.That(
                _bastion.CalculateModuleStaffing(_source.Id)
                    .TotalWorkingPersonnel,
                Is.EqualTo(6));
            Assert.That(_cycle.CurrentActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void MoveThenDefaultWorkCompletesTurn()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            MoveBrigadeOrder move = CreateMove();
            TurnPhaseResolver resolver = CreateResolver(move: move);

            PhaseResolutionResult first = resolver.ResolvePhase(1);

            Assert.That(first.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTickCount, Is.Zero);
            Assert.That(first.OrderEntries.Single().OutcomeKind,
                Is.EqualTo(OrderExecutionOutcomeKind.Completed));

            AssertWorking(_brigade, _target, false);
            Assert.That(_source.WorkingBrigadeIds,
                Has.No.Member(_brigade.Id));

            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_target.Id));
            AssertWorking(_brigade, _target, true);

            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));

            Assert.That(
                _registry.TryGet(
                    move.OrderId, out OrderExecutionState state),
                Is.True);
            Assert.That(state.IsCompleted, Is.True);
        }

        [Test]
        public void DefaultsAreDisabledWithoutProfiles()
        {
            TurnPhaseResolver resolver =
                CreateResolver(enableDefaults: false);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.Zero);
            Assert.That(result.ModuleWorkTickCount, Is.Zero);
            AssertWorking(_brigade, _source, false);
        }

        [Test]
        public void NonPrimarySpecialistRemainsIdle()
        {
            Assert.That(
                _bastion.TryRemoveBrigade(_brigade.Id, out _),
                Is.True);

            _brigade = AddBrigade(
                1, BrigadeType.Engineer, _source);

            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.ModuleWorkTickCount, Is.Zero);
            Assert.That(result.SelectedActivityCount, Is.Zero);
            AssertWorking(_brigade, _source, false);
        }

        [Test]
        public void ExplicitActivityOverridesDefaultPolicy()
        {
            TurnPhaseResolver resolver = CreateResolver(
                activityResolver: id => new TestActivity(id));

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.EqualTo(1));
            Assert.That(result.ModuleWorkTickCount, Is.Zero);
            AssertWorking(_brigade, _source, false);
        }

        [Test]
        public void ExistingWorkContinuesAcrossFreePhases()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            TurnPhaseResolver resolver = CreateResolver(
                activityResolver: id =>
                    ModuleWorkActivityResolver.Resolve(_bastion, id));

            PhaseResolutionResult first = resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(first.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            AssertWorking(_brigade, _source, true);
            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
        }

        [Test]
        public void FailedMoveAllowsDefaultWorkInNextFreePhase()
        {
            _passage.SetState(ModulePassageState.Locked);

            TurnPhaseResolver resolver =
                CreateResolver(move: CreateMove());

            PhaseResolutionResult first = resolver.ResolvePhase(1);

            Assert.That(first.RejectedOrderCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTickCount, Is.Zero);
            AssertWorking(_brigade, _source, false);

            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_source.Id));
            AssertWorking(_brigade, _source, true);
        }

        [Test]
        public void MultipleBrigadesCanStartInSameModule()
        {
            Brigade second =
                AddBrigade(2, BrigadeType.Mechanic, _source);

            TurnPhaseResolver resolver =
                CreateResolver(second: second);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.ModuleWorkTickCount, Is.EqualTo(2));
            AssertWorking(_brigade, _source, true);
            AssertWorking(second, _source, true);

            CollectionAssert.AreEquivalent(
                new[] { _brigade.Id, second.Id },
                result.ModuleWorkTicks.Select(tick => tick.BrigadeId));
        }

        [Test]
        public void StaleWorkStartDoesNotPartiallyApplyMovement()
        {
            Brigade second =
                AddBrigade(2, BrigadeType.Mechanic, _target);

            BrigadeMoveAssessment move = _bastion.AssessBrigadeMove(
                _brigade.Id,
                _source.Id,
                _target.Id,
                _passage.Id);

            Assert.That(move.IsAllowed, Is.True);

            // После подготовки запроса другая операция
            // уже запустила работу второй бригады.
            Assert.That(
                _bastion.TryStartBrigadeWork(second.Id).IsSuccess,
                Is.True);

            Assert.Throws<InvalidOperationException>(() =>
                _bastion.CommitBrigadePhaseActions(
                    new[] { move },
                    new[]
                    {
                        new BrigadeWorkStartRequest(second.Id, _target.Id)
                    }));

            AssertWorking(_brigade, _source, false);
            AssertWorking(second, _target, true);
        }

        [Test]
        public void DuplicateWorkStartsAreRejectedBeforeMutation()
        {
            var request = new BrigadeWorkStartRequest(
                _brigade.Id, _source.Id);

            Assert.Throws<ArgumentException>(() =>
                _bastion.CommitBrigadePhaseActions(
                    Array.Empty<BrigadeMoveAssessment>(),
                    new[] { request, request }));

            AssertWorking(_brigade, _source, false);
        }

        [Test]
        public void SameBrigadeCannotMoveAndStartWorkInOneBatch()
        {
            BrigadeMoveAssessment move = _bastion.AssessBrigadeMove(
                _brigade.Id,
                _source.Id,
                _target.Id,
                _passage.Id);

            Assert.Throws<ArgumentException>(() =>
                _bastion.CommitBrigadePhaseActions(
                    new[] { move },
                    new[]
                    {
                        new BrigadeWorkStartRequest(
                            _brigade.Id, _target.Id)
                    }));

            AssertWorking(_brigade, _source, false);
        }

        [Test]
        public void CrewQuartersHaveNoAutomaticWork()
        {
            Assert.That(
                _bastion.TryUndeployBrigade(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _brigade.Id, _quarters.Id).IsSuccess,
                Is.True);

            TurnPhaseResolver resolver = CreateResolver();

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.Zero);
            Assert.That(result.ModuleWorkTickCount, Is.Zero);
            AssertWorking(_brigade, _quarters, false);
        }

        private TurnPhaseResolver CreateResolver(
            MoveBrigadeOrder move = null,
            bool enableDefaults = true,
            Func<Guid, IBrigadeActivity> activityResolver = null,
            Brigade second = null)
        {
            var participants = new List<TurnBrigadeParticipant>
            {
                new TurnBrigadeParticipant(_brigade.Id, _brigade.Number)
            };

            if (second != null)
            {
                participants.Add(
                    new TurnBrigadeParticipant(second.Id, second.Number));
            }

            _cycle = new TurnCycle(1, 2, participants);
            _registry = new OrderExecutionRegistry();

            var draft = new TurnPlanDraft(
                _cycle.TurnNumber,
                _cycle.ActionPhaseCount,
                _cycle.ActiveBrigades);

            if (move != null)
            {
                Assert.That(draft.TryAddOrder(move).IsSuccess, Is.True);
                Assert.That(
                    draft.TryAddReservation(
                        new PhaseReservation(
                            move.OrderId, 1, move.BrigadeId)).IsSuccess,
                    Is.True);
            }

            ConfirmedTurnPlan plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            Assert.That(_cycle.TryConfirmPlanning().IsSuccess, Is.True);

            return new TurnPhaseResolver(
                _cycle,
                plan,
                _registry,
                activityResolver ?? (id => null),
                _bastion,
                enableDefaults ? _profiles : null);
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

        private ModuleInstance Install(ModuleDefinition definition, int x)
        {
            ModulePlacementResult result =
                _bastion.TryInstallModule(
                    definition, new GridPosition(x, 0));

            Assert.That(result.IsSuccess, Is.True);
            return result.Module;
        }

        private Brigade AddBrigade(
            int number,
            BrigadeType type,
            ModuleInstance module)
        {
            var brigade = new Brigade(number, type, 6, 6);

            Assert.That(_bastion.TryAddBrigade(brigade), Is.True);
            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    brigade.Id, module.Id).IsSuccess,
                Is.True);

            return brigade;
        }

        private void AssertWorking(
            Brigade brigade,
            ModuleInstance expectedModule,
            bool expectedWorking)
        {
            Assert.That(
                _bastion.TryGetBrigadeOperationalState(
                    brigade.Id, out BrigadeOperationalState state),
                Is.True);

            Assert.That(state.CurrentModuleId,
                Is.EqualTo(expectedModule.Id));
            Assert.That(state.IsWorking, Is.EqualTo(expectedWorking));

            Assert.That(
                expectedModule.WorkingBrigadeIds.Contains(brigade.Id),
                Is.EqualTo(expectedWorking));
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId) : base(brigadeId)
            {
            }
        }
    }
}
