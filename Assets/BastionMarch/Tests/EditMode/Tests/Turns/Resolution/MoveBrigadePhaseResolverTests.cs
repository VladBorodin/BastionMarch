
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
    public sealed class MoveBrigadePhaseResolverTests
    {
        private Bastion _bastion;
        private Brigade _first;
        private Brigade _second;
        private ModuleInstance _left;
        private ModuleInstance _middle;
        private ModuleInstance _right;
        private ModulePassage _leftMiddle;
        private ModulePassage _middleRight;
        private TurnCycle _cycle;
        private OrderExecutionRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();
            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("turn-move-test", 8, 2);

            _left = _bastion.TryInstallModule(
                definition, new GridPosition(0, 0)).Module;

            _middle = _bastion.TryInstallModule(
                definition, new GridPosition(1, 0)).Module;

            _right = _bastion.TryInstallModule(
                definition, new GridPosition(2, 0)).Module;

            _leftMiddle = InstallDoor(_left, _middle);
            _middleRight = InstallDoor(_middle, _right);

            _first = CreateBrigade(1);
            _second = CreateBrigade(2);

            Assert.That(_bastion.TryAddBrigade(_first), Is.True);
            Assert.That(_bastion.TryAddBrigade(_second), Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _first.Id, _left.Id).IsSuccess, Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _second.Id, _middle.Id).IsSuccess, Is.True);
        }

        [Test]
        public void SuccessfulMoveCompletesOrderAndAdvancesPhase()
        {
            MoveBrigadeOrder order = FirstMove();
            TurnPhaseResolver resolver = CreateResolver(order);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _middle);
            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(result.OrderEntries[0].OutcomeKind,
                Is.EqualTo(OrderExecutionOutcomeKind.Completed));
            Assert.That(_cycle.CurrentActionPhase, Is.EqualTo(2));

            Assert.That(_registry.TryGet(
                order.OrderId, out OrderExecutionState state), Is.True);
            Assert.That(state.IsCompleted, Is.True);
            Assert.That(state.CompletedPhases, Is.EqualTo(1));
        }

        [Test]
        public void LockedPassageFailsOrderWithoutMoving()
        {
            _leftMiddle.SetState(ModulePassageState.Locked);

            MoveBrigadeOrder order = FirstMove();
            TurnPhaseResolver resolver = CreateResolver(order);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _left);
            Assert.That(result.CommittedOrderCount, Is.Zero);
            Assert.That(result.RejectedOrderCount, Is.EqualTo(1));

            OrderPhaseJournalEntry entry = result.OrderEntries.Single();

            Assert.That(entry.WasCommitted, Is.False);
            Assert.That(entry.OutcomeKind,
                Is.EqualTo(OrderExecutionOutcomeKind.Failed));
            Assert.That(entry.MoveFailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.PassageTraversalRejected));
            Assert.That(entry.MoveTraversalFailureReason,
                Is.EqualTo(ModulePassageTraversalFailureReason.PassageLocked));

            Assert.That(_registry.TryGet(
                order.OrderId, out OrderExecutionState state), Is.True);
            Assert.That(state.IsFailed, Is.True);
            Assert.That(state.CompletedPhases, Is.Zero);
            Assert.That(_cycle.CurrentActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void WrongSourceFailsWithoutChangingLocation()
        {
            var order = new MoveBrigadeOrder(
                Guid.NewGuid(),
                _first.Id,
                _middle.Id,
                _left.Id,
                _leftMiddle.Id);

            TurnPhaseResolver resolver = CreateResolver(order);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _left);
            Assert.That(result.OrderEntries[0].MoveFailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.SourceMismatch));

            _registry.TryGet(order.OrderId, out OrderExecutionState state);
            Assert.That(state.IsFailed, Is.True);
        }

        [Test]
        public void FailedMoveDoesNotTriggerActivityForReservedBrigade()
        {
            _leftMiddle.SetState(ModulePassageState.Closed);

            var queriedBrigades = new List<Guid>();

            TurnPhaseResolver resolver = CreateResolver(
                id =>
                {
                    queriedBrigades.Add(id);
                    return new TestActivity(id);
                },
                FirstMove());

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(queriedBrigades, Has.No.Member(_first.Id));
            Assert.That(queriedBrigades, Has.Member(_second.Id));

            Assert.That(result.SelectedActivityBrigadeIds,
                Has.No.Member(_first.Id));
            Assert.That(result.SelectedActivityBrigadeIds,
                Has.Member(_second.Id));

            AssertLocation(_first, _left);
        }

        [Test]
        public void TwoMovesUseSharedPhaseStartState()
        {
            MoveBrigadeOrder firstMove = FirstMove();

            var secondMove = new MoveBrigadeOrder(
                Guid.NewGuid(),
                _second.Id,
                _middle.Id,
                _left.Id,
                _leftMiddle.Id);

            TurnPhaseResolver resolver =
                CreateResolver(firstMove, secondMove);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _middle);
            AssertLocation(_second, _left);
            Assert.That(result.CommittedOrderCount, Is.EqualTo(2));
            Assert.That(_registry.Count, Is.EqualTo(2));
        }

        [Test]
        public void InvalidMoveDoesNotBlockIndependentValidMove()
        {
            _middleRight.SetState(ModulePassageState.Locked);

            MoveBrigadeOrder firstMove = FirstMove();
            MoveBrigadeOrder secondMove = SecondMove();

            TurnPhaseResolver resolver =
                CreateResolver(firstMove, secondMove);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _middle);
            AssertLocation(_second, _middle);

            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(result.RejectedOrderCount, Is.EqualTo(1));

            _registry.TryGet(firstMove.OrderId,
                out OrderExecutionState firstState);
            _registry.TryGet(secondMove.OrderId,
                out OrderExecutionState secondState);

            Assert.That(firstState.IsCompleted, Is.True);
            Assert.That(secondState.IsFailed, Is.True);
        }

        [Test]
        public void MoveStopsWorkWithoutStartingWorkInTarget()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_first.Id).IsSuccess,
                Is.True);

            TurnPhaseResolver resolver = CreateResolver(FirstMove());

            resolver.ResolvePhase(1);

            AssertLocation(_first, _middle);
            Assert.That(_left.WorkingBrigadeIds,
                Has.No.Member(_first.Id));
            Assert.That(_middle.WorkingBrigadeIds,
                Has.No.Member(_first.Id));

            _bastion.TryGetBrigadeOperationalState(
                _first.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.False);
        }

        [Test]
        public void ResolvedPhaseCannotMoveBrigadeAgain()
        {
            MoveBrigadeOrder order = FirstMove();
            TurnPhaseResolver resolver = CreateResolver(order);

            resolver.ResolvePhase(1);

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(1));

            AssertLocation(_first, _middle);
            Assert.That(_registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void AlreadyFailedExecutionIsNotRestarted()
        {
            MoveBrigadeOrder order = FirstMove();
            TurnPhaseResolver resolver = CreateResolver(order);

            OrderExecutionState existing =
                OrderExecutionState.Start(order);
            existing.Fail();
            _registry.Add(existing);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            AssertLocation(_first, _left);
            Assert.That(result.CommittedOrderCount, Is.Zero);
            Assert.That(result.RejectedOrderCount, Is.EqualTo(1));
            Assert.That(result.OrderEntries[0].AssessmentFailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason.ExecutionAlreadyFailed));

            _registry.TryGet(order.OrderId, out OrderExecutionState state);
            Assert.That(state.IsFailed, Is.True);
            Assert.That(_registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void NextEmptyPhaseDoesNotRepeatCompletedMove()
        {
            TurnPhaseResolver resolver = CreateResolver(FirstMove());

            resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(second.OrderEntryCount, Is.Zero);
            Assert.That(_cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
            AssertLocation(_first, _middle);
            Assert.That(_registry.Count, Is.EqualTo(1));
        }

        private TurnPhaseResolver CreateResolver(
            params MoveBrigadeOrder[] orders)
        {
            return CreateResolver(id => null, orders);
        }

        private TurnPhaseResolver CreateResolver(
            Func<Guid, IBrigadeActivity> activityResolver,
            params MoveBrigadeOrder[] orders)
        {
            var participants = new[]
            {
                new TurnBrigadeParticipant(_first.Id, _first.Number),
                new TurnBrigadeParticipant(_second.Id, _second.Number)
            };

            _cycle = new TurnCycle(1, 2, participants);
            _registry = new OrderExecutionRegistry();

            var draft = new TurnPlanDraft(
                _cycle.TurnNumber,
                _cycle.ActionPhaseCount,
                _cycle.ActiveBrigades);

            foreach (MoveBrigadeOrder order in orders)
            {
                Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

                Assert.That(
                    draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId,
                            1,
                            order.BrigadeId)).IsSuccess,
                    Is.True);
            }

            ConfirmedTurnPlan plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            Assert.That(_cycle.TryConfirmPlanning().IsSuccess, Is.True);

            return new TurnPhaseResolver(
                _cycle,
                plan,
                _registry,
                activityResolver,
                _bastion);
        }

        private MoveBrigadeOrder FirstMove()
        {
            return new MoveBrigadeOrder(
                Guid.NewGuid(),
                _first.Id,
                _left.Id,
                _middle.Id,
                _leftMiddle.Id);
        }

        private MoveBrigadeOrder SecondMove()
        {
            return new MoveBrigadeOrder(
                Guid.NewGuid(),
                _second.Id,
                _middle.Id,
                _right.Id,
                _middleRight.Id);
        }

        private void AssertLocation(Brigade brigade, ModuleInstance expected)
        {
            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    brigade.Id, out ModuleInstance actual),
                Is.True);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
        }

        private static Brigade CreateBrigade(int number)
        {
            return new Brigade(
                number: number,
                type: BrigadeType.Mechanic,
                currentPersonnel: 6,
                maximumPersonnel: 6);
        }

        private ModulePassage InstallDoor(
            ModuleInstance source,
            ModuleInstance target)
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
                    ModulePassageTraversalMode.Bidirectional);

            Assert.That(result.IsSuccess, Is.True);

            return result.Passage;
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId) : base(brigadeId)
            {
            }
        }
    }
}
