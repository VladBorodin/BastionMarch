using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class MoveBrigadeOrderAssessorTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance _left;
        private ModuleInstance _right;
        private ModulePassage _passage;
        private ModuleDefinition _moduleDefinition;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();

            _moduleDefinition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("move-assessment-test", 8, 2);

            _left = _bastion.TryInstallModule(
                _moduleDefinition, new GridPosition(0, 0)).Module;

            _right = _bastion.TryInstallModule(
                _moduleDefinition, new GridPosition(1, 0)).Module;

            _brigade = new Brigade(
                number: 1,
                type: BrigadeType.Mechanic,
                currentPersonnel: 6,
                maximumPersonnel: 6);

            Assert.That(_bastion.TryAddBrigade(_brigade), Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _brigade.Id, _left.Id).IsSuccess,
                Is.True);

            _passage = InstallDoor(
                _left,
                _right,
                ModulePassageTraversalMode.Bidirectional);
        }

        [Test]
        public void ValidMovePassesDomainAssessment()
        {
            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(), _bastion);

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.BrigadeId, Is.EqualTo(_brigade.Id));
            Assert.That(result.SourceModuleId, Is.EqualTo(_left.Id));
            Assert.That(result.TargetModuleId, Is.EqualTo(_right.Id));
            Assert.That(result.PassageId, Is.EqualTo(_passage.Id));
        }

        [Test]
        public void AssessmentDoesNotModifyWorldOrWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(), _bastion);

            Assert.That(result.IsAllowed, Is.True);

            Assert.That(
                _left.OccupyingBrigadeIds,
                Has.Member(_brigade.Id));

            Assert.That(
                _left.WorkingBrigadeIds,
                Has.Member(_brigade.Id));

            Assert.That(_right.OccupyingBrigadeIds, Is.Empty);

            _bastion.TryGetBrigadeOperationalState(
                _brigade.Id, out BrigadeOperationalState state);

            Assert.That(state.CurrentModuleId, Is.EqualTo(_left.Id));
            Assert.That(state.IsWorking, Is.True);
        }

        [TestCase(ModulePassageState.Closed)]
        [TestCase(ModulePassageState.Locked)]
        [TestCase(ModulePassageState.Blocked)]
        [TestCase(ModulePassageState.Destroyed)]
        public void UnavailablePassageFailsDomainAssessment(
            ModulePassageState state)
        {
            _passage.SetState(state);

            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(), _bastion);

            Assert.That(result.IsAllowed, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    BrigadeMoveFailureReason.PassageTraversalRejected));
        }

        [Test]
        public void WrongSourceReportsDomainFailure()
        {
            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(sourceId: _right.Id, targetId: _left.Id), _bastion);

            Assert.That(result.IsAllowed, Is.False);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.SourceMismatch));
        }

        [Test]
        public void DisbandedBrigadeReportsDomainFailure()
        {
            _brigade.ApplyCasualties(_brigade.CurrentPersonnel);

            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(), _bastion);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.BrigadeDisbanded));
        }

        [Test]
        public void MissingPassageReportsDomainFailure()
        {
            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(passageId: Guid.NewGuid()),
                    _bastion);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.PassageNotFound));
        }

        [Test]
        public void UnrelatedPassageReportsTraversalFailure()
        {
            ModuleInstance third = _bastion.TryInstallModule(
                _moduleDefinition, new GridPosition(2, 0)).Module;

            ModulePassage unrelated = InstallDoor(
                _right,
                third,
                ModulePassageTraversalMode.Bidirectional);

            BrigadeMoveAssessment result =
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(passageId: unrelated.Id),
                    _bastion);

            Assert.That(result.FailureReason,
                Is.EqualTo(
                    BrigadeMoveFailureReason.PassageTraversalRejected));

            Assert.That(result.TraversalFailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageDoesNotConnectModules));
        }

        [Test]
        public void AssessorRejectsNullIntent()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MoveBrigadeOrderAssessor.Assess(null, _bastion));
        }

        [Test]
        public void AssessorRejectsNullBastion()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MoveBrigadeOrderAssessor.Assess(
                    CreateIntent(), null));
        }

        [Test]
        public void AssessorRejectsDifferentOrderType()
        {
            var order = new TestOtherOrder(
                Guid.NewGuid(), _brigade.Id);

            var intent = new OrderPhaseIntent(
                order, 1, new[] { _brigade.Id });

            Assert.Throws<ArgumentException>(() =>
                MoveBrigadeOrderAssessor.Assess(intent, _bastion));
        }

        [Test]
        public void AssessorRejectsAnotherReservedBrigade()
        {
            var order = (MoveBrigadeOrder)CreateIntent().Order;

            var intent = new OrderPhaseIntent(
                order,
                1,
                new[] { Guid.NewGuid() });

            Assert.Throws<ArgumentException>(() =>
                MoveBrigadeOrderAssessor.Assess(intent, _bastion));
        }

        [Test]
        public void AssessorRejectsMultipleReservedBrigades()
        {
            var order = (MoveBrigadeOrder)CreateIntent().Order;

            var intent = new OrderPhaseIntent(
                order,
                1,
                new[] { _brigade.Id, Guid.NewGuid() });

            Assert.Throws<ArgumentException>(() =>
                MoveBrigadeOrderAssessor.Assess(intent, _bastion));
        }

        [Test]
        public void ResolverCannotCompleteMoveWithoutWorldMutation()
        {
            var participant = new TurnBrigadeParticipant(
                _brigade.Id, _brigade.Number);

            var cycle = new TurnCycle(
                1, 2, new[] { participant });

            var draft = new TurnPlanDraft(
                cycle.TurnNumber,
                cycle.ActionPhaseCount,
                cycle.ActiveBrigades);

            var order = (MoveBrigadeOrder)CreateIntent().Order;

            Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        order.OrderId, 1, _brigade.Id)).IsSuccess,
                Is.True);

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(draft);

            Assert.That(
                cycle.TryConfirmPlanning().IsSuccess,
                Is.True);

            var registry = new OrderExecutionRegistry();

            int activityCalls = 0;

            var resolver = new TurnPhaseResolver(
                cycle,
                plan,
                registry,
                id =>
                {
                    activityCalls++;
                    return null;
                });

            Assert.Throws<NotSupportedException>(() =>
                resolver.ResolvePhase(1));

            Assert.That(activityCalls, Is.Zero);
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
            Assert.That(registry.Count, Is.Zero);

            _bastion.TryGetBrigadeLocation(
                _brigade.Id, out ModuleInstance location);

            Assert.That(location.Id, Is.EqualTo(_left.Id));
        }

        private OrderPhaseIntent CreateIntent(
            Guid? brigadeId = null,
            Guid? sourceId = null,
            Guid? targetId = null,
            Guid? passageId = null)
        {
            var order = new MoveBrigadeOrder(
                Guid.NewGuid(),
                brigadeId ?? _brigade.Id,
                sourceId ?? _left.Id,
                targetId ?? _right.Id,
                passageId ?? _passage.Id);

            return new OrderPhaseIntent(
                order,
                actionPhase: 1,
                reservedBrigadeIds: new[] { _brigade.Id });
        }

        private ModulePassage InstallDoor(
            ModuleInstance source,
            ModuleInstance target,
            ModulePassageTraversalMode mode)
        {
            Assert.That(
                _bastion.TryGetModuleAdjacency(
                    source.Id,
                    target.Id,
                    out ModuleAdjacency adjacency),
                Is.True);

            ModulePassagePlacementResult result =
                _bastion.TryInstallPassage(
                    source.Id,
                    target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    mode);

            Assert.That(result.IsSuccess, Is.True);

            return result.Passage;
        }

        private sealed class TestOtherOrder : BrigadeTurnOrder
        {
            public TestOtherOrder(Guid orderId, Guid brigadeId)
                : base(orderId, 1, brigadeId)
            {
            }
        }
    }
}