using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Planning
{
    [TestFixture]
    public sealed class BastionRouteSessionTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance[] _rooms;
        private ModulePassage[] _doors;

        [SetUp]
        public void SetUp()
        {
            _bastion = new Bastion("route-test", 4, 1);

            ModuleDefinition definition =
                InitialModuleDefinitions.CreateCatalog()
                    .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _rooms = new ModuleInstance[4];
            _doors = new ModulePassage[3];

            for (int i = 0; i < _rooms.Length; i++)
            {
                ModulePlacementResult placement =
                    _bastion.TryInstallModule(
                        definition, new GridPosition(i, 0));

                Assert.That(placement.IsSuccess, Is.True);
                _rooms[i] = placement.Module;
            }

            for (int i = 0; i < _doors.Length; i++)
            {
                Assert.That(
                    _bastion.TryGetModuleAdjacency(
                        _rooms[i].Id,
                        _rooms[i + 1].Id,
                        out ModuleAdjacency adjacency),
                    Is.True);

                ModulePassagePlacementResult placement =
                    _bastion.TryInstallPassage(
                        _rooms[i].Id,
                        _rooms[i + 1].Id,
                        adjacency.SharedBoundaries[0],
                        ModulePassageType.Door,
                        ModulePassageTraversalMode.Bidirectional);

                Assert.That(placement.IsSuccess, Is.True);
                _doors[i] = placement.Passage;
            }

            _brigade = new Brigade(
                1, BrigadeType.Mechanic, 6, 6);

            Assert.That(
                _bastion.TryAddBrigade(_brigade),
                Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _brigade.Id, _rooms[0].Id).IsSuccess,
                Is.True);
        }

        private BastionTurnSession CreateSession()
        {
            return new BastionTurnSession(
                _bastion,
                InitialBrigadeWorkProfiles.CreateCatalog());
        }

        private void AssertLocation(int roomIndex)
        {
            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    _brigade.Id, out ModuleInstance actual),
                Is.True);

            Assert.That(actual.Id, Is.EqualTo(_rooms[roomIndex].Id));
        }

        [Test]
        public void LongRouteContinuesAcrossTurnBoundary()
        {
            BastionTurnSession session = CreateSession();

            RoutePlanningResult result =
                session.TryPlanRoute(
                    _brigade.Id, _rooms[3].Id, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.RoutePlan.TotalStepCount, Is.EqualTo(3));

            // В первом ходу доступны только две фазы.
            Assert.That(session.Draft.OrderCount, Is.EqualTo(2));
            Assert.That(session.Draft.ReservationCount, Is.EqualTo(2));

            MoveBrigadeOrder[] moves =
                session.Draft.PhaseReservations
                    .OrderBy(r => r.ActionPhase)
                    .Select(r => (MoveBrigadeOrder)
                        session.Draft.Orders.Single(o =>
                            o.OrderId == r.OrderId))
                    .ToArray();

            Assert.That(moves[0].TargetModuleId,
                Is.EqualTo(_rooms[1].Id));

            Assert.That(moves[1].SourceModuleId,
                Is.EqualTo(_rooms[1].Id));

            Assert.That(moves[1].TargetModuleId,
                Is.EqualTo(_rooms[2].Id));

            AssertLocation(0);

            Assert.That(session.ConfirmPlanning().IsSuccess, Is.True);

            session.ResolveCurrentPhase();

            AssertLocation(1);
            Assert.That(result.RoutePlan.CompletedStepCount,
                Is.EqualTo(1));

            session.ResolveCurrentPhase();

            AssertLocation(2);
            Assert.That(result.RoutePlan.CompletedStepCount,
                Is.EqualTo(2));
            Assert.That(result.RoutePlan.RemainingStepCount,
                Is.EqualTo(1));

            Assert.That(session.BeginNextTurn().IsSuccess, Is.True);

            Assert.That(session.Cycle.TurnNumber, Is.EqualTo(2));
            Assert.That(session.Draft.OrderCount, Is.EqualTo(1));
            Assert.That(session.Draft.ReservationCount, Is.EqualTo(1));

            MoveBrigadeOrder remaining =
                (MoveBrigadeOrder)session.Draft.Orders.Single();

            Assert.That(remaining.SourceModuleId,
                Is.EqualTo(_rooms[2].Id));

            Assert.That(remaining.TargetModuleId,
                Is.EqualTo(_rooms[3].Id));

            Assert.That(session.ConfirmPlanning().IsSuccess, Is.True);

            session.ResolveCurrentPhase();

            AssertLocation(3);
            Assert.That(result.RoutePlan.IsCompleted, Is.True);
            Assert.That(result.RoutePlan.RemainingStepCount, Is.Zero);
        }

        [Test]
        public void UnreachableRouteDoesNotModifyDraft()
        {
            BastionTurnSession session = CreateSession();

            _doors[0].SetState(ModulePassageState.Locked);

            RoutePlanningResult result =
                session.TryPlanRoute(
                    _brigade.Id, _rooms[3].Id, 1);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.Draft.OrderCount, Is.Zero);
            Assert.That(session.Draft.ReservationCount, Is.Zero);

            Assert.That(
                session.TryGetRoutePlan(_brigade.Id, out _),
                Is.False);

            AssertLocation(0);
        }

        [Test]
        public void BlockedFirstStepNeverAdvancesRoute()
        {
            BastionTurnSession session = CreateSession();

            RoutePlanningResult result =
                session.TryPlanRoute(
                    _brigade.Id, _rooms[3].Id, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.ConfirmPlanning().IsSuccess, Is.True);

            // Проход закрыли уже после подтверждения плана.
            _doors[0].SetState(ModulePassageState.Blocked);

            session.ResolveCurrentPhase();

            Assert.That(result.RoutePlan.IsBlocked, Is.True);
            Assert.That(result.RoutePlan.CompletedStepCount, Is.Zero);
            AssertLocation(0);

            // Вторая фаза не должна переместить бригаду,
            // которая так и не добралась до комнаты B.
            session.ResolveCurrentPhase();
            AssertLocation(0);

            Assert.That(session.BeginNextTurn().IsSuccess, Is.True);
            Assert.That(session.Draft.OrderCount, Is.Zero);
        }

        [Test]
        public void BlockedSecondStepPreservesFirstMove()
        {
            BastionTurnSession session = CreateSession();

            RoutePlanningResult result =
                session.TryPlanRoute(
                    _brigade.Id, _rooms[3].Id, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.ConfirmPlanning().IsSuccess, Is.True);

            session.ResolveCurrentPhase();

            AssertLocation(1);
            Assert.That(result.RoutePlan.CompletedStepCount,
                Is.EqualTo(1));

            _doors[1].SetState(ModulePassageState.Blocked);

            session.ResolveCurrentPhase();

            AssertLocation(1);
            Assert.That(result.RoutePlan.IsBlocked, Is.True);
            Assert.That(result.RoutePlan.CompletedStepCount,
                Is.EqualTo(1));
            Assert.That(result.RoutePlan.RemainingStepCount,
                Is.EqualTo(2));

            Assert.That(session.BeginNextTurn().IsSuccess, Is.True);
            Assert.That(session.Draft.OrderCount, Is.Zero);
        }
    }
}
