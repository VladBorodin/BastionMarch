using System;
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

namespace BastionMarch.Simulation.EditModeTests.Turns.Activities
{
    [TestFixture]
    public sealed class ModuleWorkActivityTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance _source;
        private ModuleInstance _target;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();
            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("activity-test", 8, 2);

            _source = _bastion.TryInstallModule(
                definition, new GridPosition(0, 0)).Module;

            _target = _bastion.TryInstallModule(
                definition, new GridPosition(1, 0)).Module;

            _brigade = CreateBrigade(1);

            Assert.That(_bastion.TryAddBrigade(_brigade), Is.True);
            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _brigade.Id, _source.Id).IsSuccess,
                Is.True);
        }

        [Test]
        public void ActivityStoresBrigadeAndModuleIdentity()
        {
            var activity = new ModuleWorkActivity(
                _brigade.Id, _source.Id);

            Assert.That(activity.BrigadeId, Is.EqualTo(_brigade.Id));
            Assert.That(activity.ModuleId, Is.EqualTo(_source.Id));
        }

        [Test]
        public void ActivityRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(() =>
                new ModuleWorkActivity(Guid.Empty, _source.Id));
        }

        [Test]
        public void ActivityRejectsEmptyModuleId()
        {
            Assert.Throws<ArgumentException>(() =>
                new ModuleWorkActivity(_brigade.Id, Guid.Empty));
        }

        [Test]
        public void ActivityIsNotTurnOrder()
        {
            var activity = new ModuleWorkActivity(
                _brigade.Id, _source.Id);

            Assert.That(activity, Is.InstanceOf<IBrigadeActivity>());
            Assert.That(activity, Is.Not.InstanceOf<ITurnOrder>());

            Assert.That(
                typeof(ModuleWorkActivity).GetProperty("RequiredPhases"),
                Is.Null);
        }

        [Test]
        public void ActivityIdentityPropertiesAreReadOnly()
        {
            Assert.That(
                typeof(ModuleWorkActivity)
                    .GetProperty(nameof(ModuleWorkActivity.BrigadeId))
                    .CanWrite,
                Is.False);

            Assert.That(
                typeof(ModuleWorkActivity)
                    .GetProperty(nameof(ModuleWorkActivity.ModuleId))
                    .CanWrite,
                Is.False);
        }

        [Test]
        public void ResolverFindsWorkingBrigade()
        {
            StartWork();

            ModuleWorkActivity activity = Resolve();

            Assert.That(activity, Is.Not.Null);
            Assert.That(activity.BrigadeId, Is.EqualTo(_brigade.Id));
            Assert.That(activity.ModuleId, Is.EqualTo(_source.Id));
        }

        [Test]
        public void ResolverReturnsNullForDeployedIdleBrigade()
        {
            Assert.That(Resolve(), Is.Null);
        }

        [Test]
        public void ResolverReturnsNullForUnknownBrigade()
        {
            Assert.That(
                ModuleWorkActivityResolver.Resolve(
                    _bastion, Guid.NewGuid()),
                Is.Null);
        }

        [Test]
        public void ResolverReturnsNullAfterWorkStops()
        {
            StartWork();

            Assert.That(
                _bastion.TryStopBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(Resolve(), Is.Null);
        }

        [Test]
        public void ResolverReturnsNullAfterUndeploy()
        {
            StartWork();

            Assert.That(
                _bastion.TryUndeployBrigade(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(Resolve(), Is.Null);
        }

        [Test]
        public void ResolverReturnsNullForDisbandedWorkingBrigade()
        {
            StartWork();

            _brigade.ApplyCasualties(_brigade.CurrentPersonnel);

            Assert.That(Resolve(), Is.Null);
        }

        [Test]
        public void MovingBrigadeEndsOldModuleActivity()
        {
            StartWork();

            Assert.That(Resolve().ModuleId, Is.EqualTo(_source.Id));

            MoveToTarget();

            Assert.That(Resolve(), Is.Null);
            Assert.That(_source.WorkingBrigadeIds,
                Has.No.Member(_brigade.Id));
            Assert.That(_target.WorkingBrigadeIds,
                Has.No.Member(_brigade.Id));
        }

        [Test]
        public void WorkInTargetRequiresExplicitStart()
        {
            StartWork();
            MoveToTarget();

            Assert.That(Resolve(), Is.Null);

            StartWork();

            ModuleWorkActivity activity = Resolve();

            Assert.That(activity, Is.Not.Null);
            Assert.That(activity.ModuleId, Is.EqualTo(_target.Id));
        }

        [Test]
        public void ResolverDoesNotChangeWorldState()
        {
            StartWork();

            ModuleWorkActivity first = Resolve();
            ModuleWorkActivity second = Resolve();

            Assert.That(first.ModuleId, Is.EqualTo(second.ModuleId));
            Assert.That(_source.OccupyingBrigadeIds,
                Has.Member(_brigade.Id));
            Assert.That(_source.WorkingBrigadeIds,
                Has.Member(_brigade.Id));

            Assert.That(
                _bastion.TryGetBrigadeOperationalState(
                    _brigade.Id, out BrigadeOperationalState state),
                Is.True);

            Assert.That(state.IsWorking, Is.True);
            Assert.That(state.CurrentModuleId, Is.EqualTo(_source.Id));
        }

        [Test]
        public void FreePhaseSelectsModuleWorkActivity()
        {
            StartWork();

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                CreatePlan(reserveBrigade: false),
                1,
                id => ModuleWorkActivityResolver.Resolve(_bastion, id));

            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(
                intents[0].Activity,
                Is.TypeOf<ModuleWorkActivity>());

            var activity = (ModuleWorkActivity)intents[0].Activity;

            Assert.That(activity.ModuleId, Is.EqualTo(_source.Id));
        }

        [Test]
        public void BlockingReservationSuppressesModuleWorkActivity()
        {
            StartWork();

            int calls = 0;

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                CreatePlan(reserveBrigade: true),
                1,
                id =>
                {
                    calls++;
                    return ModuleWorkActivityResolver.Resolve(_bastion, id);
                });

            Assert.That(intents, Is.Empty);
            Assert.That(calls, Is.Zero);

            // Наличие приказа само по себе не останавливает
            // operational work до фактического исполнения.
            Assert.That(Resolve(), Is.Not.Null);
        }

        [Test]
        public void ResolverRejectsNullBastion()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ModuleWorkActivityResolver.Resolve(null, _brigade.Id));
        }

        [Test]
        public void ResolverRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(() =>
                ModuleWorkActivityResolver.Resolve(_bastion, Guid.Empty));
        }

        [Test]
        public void TwoWorkingBrigadesResolveIndependently()
        {
            Brigade second = CreateBrigade(2);

            Assert.That(_bastion.TryAddBrigade(second), Is.True);
            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    second.Id, _target.Id).IsSuccess,
                Is.True);

            StartWork();

            Assert.That(
                _bastion.TryStartBrigadeWork(second.Id).IsSuccess,
                Is.True);

            ModuleWorkActivity firstActivity = Resolve();
            ModuleWorkActivity secondActivity =
                ModuleWorkActivityResolver.Resolve(_bastion, second.Id);

            Assert.That(firstActivity.ModuleId, Is.EqualTo(_source.Id));
            Assert.That(secondActivity.ModuleId, Is.EqualTo(_target.Id));
        }

        private ModuleWorkActivity Resolve()
        {
            return ModuleWorkActivityResolver.Resolve(
                _bastion, _brigade.Id);
        }

        private void StartWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);
        }

        private void MoveToTarget()
        {
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

            BrigadeMoveResult move = _bastion.TryMoveBrigade(
                _brigade.Id,
                _source.Id,
                _target.Id,
                placement.Passage.Id);

            Assert.That(move.IsSuccess, Is.True);
        }

        private ConfirmedTurnPlan CreatePlan(bool reserveBrigade)
        {
            var participant = new TurnBrigadeParticipant(
                _brigade.Id, _brigade.Number);

            var draft = new TurnPlanDraft(
                turnNumber: 1,
                actionPhaseCount: 2,
                participants: new[] { participant });

            if (reserveBrigade)
            {
                var order = new MoveBrigadeOrder(
                    Guid.NewGuid(),
                    _brigade.Id,
                    _source.Id,
                    _target.Id,
                    Guid.NewGuid());

                Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

                Assert.That(
                    draft.TryAddReservation(
                        new PhaseReservation(
                            order.OrderId, 1, _brigade.Id)).IsSuccess,
                    Is.True);
            }

            return ConfirmedTurnPlan.CreateSnapshot(draft);
        }

        private static Brigade CreateBrigade(int number)
        {
            return new Brigade(
                number: number,
                type: BrigadeType.Mechanic,
                currentPersonnel: 6,
                maximumPersonnel: 6);
        }
    }
}