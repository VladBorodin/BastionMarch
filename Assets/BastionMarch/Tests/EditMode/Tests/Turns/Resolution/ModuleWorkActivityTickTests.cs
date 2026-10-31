using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class ModuleWorkActivityTickTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private ModuleInstance _source;
        private ModuleInstance _target;

        [SetUp]
        public void SetUp()
        {
            var definition = InitialModuleDefinitions.CreateCatalog()
                .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("activity-tick-test", 8, 2);

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
        }

        [Test]
        public void PrepareCapturesWorkingBrigade()
        {
            StartWork();

            ModuleWorkActivityTick tick = Prepare();

            Assert.That(tick.BrigadeId, Is.EqualTo(_brigade.Id));
            Assert.That(tick.ModuleId, Is.EqualTo(_source.Id));
            Assert.That(tick.ActionPhase, Is.EqualTo(1));
            Assert.That(tick.WorkingPersonnel, Is.EqualTo(6));
        }

        [Test]
        public void PreparationDoesNotModifyWorld()
        {
            StartWork();

            Prepare();
            Prepare();

            Assert.That(_source.WorkingBrigadeIds,
                Has.Member(_brigade.Id));

            Assert.That(_brigade.CurrentPersonnel, Is.EqualTo(6));

            _bastion.TryGetBrigadeOperationalState(
                _brigade.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.True);
        }

        [Test]
        public void StoppedWorkInvalidatesSelectedActivity()
        {
            StartWork();

            BrigadeActivityPhaseIntent intent = CreateIntent();

            Assert.That(
                _bastion.TryStopBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            Assert.Throws<InvalidOperationException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(intent, _bastion));
        }

        [Test]
        public void MoveInvalidatesActivityFromPreviousModule()
        {
            StartWork();

            BrigadeActivityPhaseIntent intent = CreateIntent();

            Assert.That(
                _bastion.TryGetModuleAdjacency(
                    _source.Id, _target.Id,
                    out ModuleAdjacency adjacency),
                Is.True);

            ModulePassagePlacementResult passage =
                _bastion.TryInstallPassage(
                    _source.Id,
                    _target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    ModulePassageTraversalMode.Bidirectional);

            Assert.That(passage.IsSuccess, Is.True);

            Assert.That(
                _bastion.TryMoveBrigade(
                    _brigade.Id,
                    _source.Id,
                    _target.Id,
                    passage.Passage.Id).IsSuccess,
                Is.True);

            Assert.Throws<InvalidOperationException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(intent, _bastion));
        }

        [Test]
        public void DisbandedBrigadeCannotTick()
        {
            StartWork();

            BrigadeActivityPhaseIntent intent = CreateIntent();

            _brigade.ApplyCasualties(_brigade.CurrentPersonnel);

            Assert.Throws<InvalidOperationException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(intent, _bastion));
        }

        [Test]
        public void PersonnelSnapshotReflectsCurrentCrew()
        {
            StartWork();

            ModuleWorkActivityTick before = Prepare();

            _brigade.ApplyCasualties(2);

            ModuleWorkActivityTick after = Prepare();

            Assert.That(before.WorkingPersonnel, Is.EqualTo(6));
            Assert.That(after.WorkingPersonnel, Is.EqualTo(4));
        }

        [Test]
        public void PrepareRejectsWrongActivityType()
        {
            var intent = new BrigadeActivityPhaseIntent(
                new TestActivity(_brigade.Id), 1);

            Assert.Throws<ArgumentException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(intent, _bastion));
        }

        [Test]
        public void PrepareRejectsNullIntent()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(null, _bastion));
        }

        [Test]
        public void PrepareRejectsNullBastion()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ModuleWorkActivityTickProcessor.Prepare(
                    new BrigadeActivityPhaseIntent(
                        new ModuleWorkActivity(_brigade.Id, _source.Id), 1),
                    null));
        }

        private void StartWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);
        }

        private BrigadeActivityPhaseIntent CreateIntent()
        {
            return new BrigadeActivityPhaseIntent(
                ModuleWorkActivityResolver.Resolve(_bastion, _brigade.Id),
                1);
        }

        private ModuleWorkActivityTick Prepare()
        {
            return ModuleWorkActivityTickProcessor.Prepare(
                CreateIntent(), _bastion);
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId) : base(brigadeId)
            {
            }
        }
    }
}