
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

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class BastionTurnSessionTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance _source;
        private ModuleInstance _target;
        private ModulePassage _passage;

        [SetUp]
        public void SetUp()
        {
            _bastion = new Bastion("turn-session-test", 4, 2);

            ModuleDefinition definition =
                InitialModuleDefinitions.CreateCatalog()
                    .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _source = _bastion.TryInstallModule(
                definition, new GridPosition(0, 0)).Module;

            _target = _bastion.TryInstallModule(
                definition, new GridPosition(1, 0)).Module;

            _brigade = new Brigade(
                1, BrigadeType.Mechanic, 6, 6);

            Assert.That(
                _bastion.TryAddBrigade(_brigade), Is.True);

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
        public void SessionStartsInPlanning()
        {
            BastionTurnSession session = CreateSession();

            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            Assert.That(session.Cycle.TurnNumber, Is.EqualTo(1));
            Assert.That(session.Cycle.ActionPhaseCount, Is.EqualTo(2));

            Assert.That(session.Draft.ParticipantCount, Is.EqualTo(1));
            Assert.That(session.Draft.Participants[0].BrigadeId,
                Is.EqualTo(_brigade.Id));

            Assert.That(session.ConfirmedPlan, Is.Null);
            Assert.That(session.LastPhaseResult, Is.Null);
            Assert.That(session.ExecutionRegistry.Count, Is.Zero);
        }

        [Test]
        public void CannotResolveBeforePlanningConfirmation()
        {
            BastionTurnSession session = CreateSession();

            Assert.Throws<InvalidOperationException>(() =>
                session.ResolveCurrentPhase());

            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            AssertLocation(_source);
        }

        [Test]
        public void InvalidPlanDoesNotAdvanceTime()
        {
            BastionTurnSession session = CreateSession();

            // Приказ без PhaseReservation:
            // существующий validator обязан его отклонить.
            MoveBrigadeOrder order = CreateMove();

            Assert.That(
                session.Draft.TryAddOrder(order).IsSuccess,
                Is.True);

            TurnPlanningConfirmationResult result =
                session.ConfirmPlanning();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assessment.FailureReason,
                Is.EqualTo(TurnPlanFailureReason.OrderHasNoReservations));

            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            Assert.That(session.ConfirmedPlan, Is.Null);
            AssertLocation(_source);
        }

        [Test]
        public void EmptyTurnReachesTurnEndAndStartsNextTurn()
        {
            BastionTurnSession session = CreateSession();

            Assert.That(
                session.ConfirmPlanning().IsSuccess, Is.True);

            session.ResolveCurrentPhase();
            session.ResolveCurrentPhase();

            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.TurnEnd));

            Assert.That(
                session.BeginNextTurn().IsSuccess, Is.True);

            Assert.That(session.Cycle.TurnNumber, Is.EqualTo(2));
            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            Assert.That(session.Draft.TurnNumber, Is.EqualTo(2));
            Assert.That(session.ConfirmedPlan, Is.Null);
            Assert.That(session.LastPhaseResult, Is.Null);
        }

        [Test]
        public void IdleBrigadeStartsDefaultWorkDuringFreePhase()
        {
            BastionTurnSession session = CreateSession();

            Assert.That(
                session.ConfirmPlanning().IsSuccess, Is.True);

            PhaseResolutionResult result =
                session.ResolveCurrentPhase();

            Assert.That(result.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(result.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_source.Id));

            Assert.That(
                _bastion.TryGetBrigadeOperationalState(
                    _brigade.Id, out BrigadeOperationalState state),
                Is.True);

            Assert.That(state.IsWorking, Is.True);
            Assert.That(session.Cycle.CurrentActionPhase,
                Is.EqualTo(2));

            Assert.That(session.LastPhaseResult, Is.SameAs(result));
        }

        [Test]
        public void MoveThenWorkIsExecutedThroughSession()
        {
            BastionTurnSession session = CreateSession();

            MoveBrigadeOrder move = CreateMove();

            Assert.That(
                session.Draft.TryAddOrder(move).IsSuccess,
                Is.True);

            Assert.That(
                session.Draft.TryAddReservation(
                    new PhaseReservation(
                        move.OrderId, 1, _brigade.Id)).IsSuccess,
                Is.True);

            Assert.That(
                session.ConfirmPlanning().IsSuccess,
                Is.True);

            PhaseResolutionResult first =
                session.ResolveCurrentPhase();

            Assert.That(first.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(first.ModuleWorkTickCount, Is.Zero);
            AssertLocation(_target);

            PhaseResolutionResult second =
                session.ResolveCurrentPhase();

            Assert.That(second.ModuleWorkTickCount, Is.EqualTo(1));
            Assert.That(second.ModuleWorkTicks[0].ModuleId,
                Is.EqualTo(_target.Id));

            Assert.That(session.Cycle.Stage,
                Is.EqualTo(TurnStage.TurnEnd));

            Assert.That(
                session.ExecutionRegistry.TryGet(
                    move.OrderId, out OrderExecutionState state),
                Is.True);

            Assert.That(state.IsCompleted, Is.True);
        }

        [Test]
        public void NextTurnRefreshesParticipantsAndKeepsRegistry()
        {
            BastionTurnSession session = CreateSession();

            OrderExecutionRegistry registry =
                session.ExecutionRegistry;

            Assert.That(
                session.ConfirmPlanning().IsSuccess,
                Is.True);

            session.ResolveCurrentPhase();
            session.ResolveCurrentPhase();

            _brigade.ApplyCasualties(
                _brigade.CurrentPersonnel);

            Assert.That(_brigade.IsDisbanded, Is.True);

            Assert.That(
                session.BeginNextTurn().IsSuccess,
                Is.True);

            Assert.That(session.Cycle.ActiveBrigadeCount,
                Is.Zero);

            Assert.That(session.ExecutionRegistry,
                Is.SameAs(registry));

            Assert.That(session.Draft.ParticipantCount,
                Is.Zero);
        }

        private BastionTurnSession CreateSession()
        {
            return new BastionTurnSession(
                _bastion,
                InitialBrigadeWorkProfiles.CreateCatalog());
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

        private void AssertLocation(ModuleInstance expected)
        {
            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    _brigade.Id, out ModuleInstance actual),
                Is.True);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
        }
    }
}
