
using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BrigadeMoveTests
    {
        private Bastion _bastion;
        private ModuleInstance _left;
        private ModuleInstance _right;
        private ModuleInstance _third;
        private ModulePassage _passage;
        private Brigade _brigade;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();

            _bastion = new Bastion("move-test", 8, 2);

            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _left = _bastion.TryInstallModule(
                definition, new GridPosition(0, 0)).Module;

            _right = _bastion.TryInstallModule(
                definition, new GridPosition(1, 0)).Module;

            _third = _bastion.TryInstallModule(
                definition, new GridPosition(2, 0)).Module;

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
        public void ValidMoveUpdatesLocation()
        {
            BrigadeMoveResult result = Move();

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    _brigade.Id, out ModuleInstance location),
                Is.True);

            Assert.That(location.Id, Is.EqualTo(_right.Id));
        }

        [Test]
        public void ValidMoveTransfersOccupancy()
        {
            Move();

            CollectionAssert.DoesNotContain(_left.OccupyingBrigadeIds, _brigade.Id);
            CollectionAssert.Contains(_right.OccupyingBrigadeIds, _brigade.Id);
        }

        [Test]
        public void MoveStopsPreviousWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            BrigadeMoveResult result = Move();

            Assert.That(result.IsSuccess, Is.True);

            CollectionAssert.DoesNotContain(_left.WorkingBrigadeIds, _brigade.Id);
            CollectionAssert.DoesNotContain(_right.WorkingBrigadeIds, _brigade.Id);

            _bastion.TryGetBrigadeOperationalState(
                _brigade.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.False);
        }

        [Test]
        public void AssessmentDoesNotChangeWorld()
        {
            _bastion.TryStartBrigadeWork(_brigade.Id);

            BrigadeMoveAssessment assessment =
                _bastion.AssessBrigadeMove(
                    _brigade.Id, _left.Id, _right.Id, _passage.Id);

            Assert.That(assessment.IsAllowed, Is.True);

            Assert.That(
                _left.OccupyingBrigadeIds,
                Does.Contain(_brigade.Id));

            Assert.That(
                _left.WorkingBrigadeIds,
                Does.Contain(_brigade.Id));

            Assert.That(_right.OccupyingBrigadeIds, Is.Empty);
        }

        [Test]
        public void UnknownBrigadeIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                Guid.NewGuid(), _left.Id, _right.Id, _passage.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.BrigadeNotFound));
        }

        [Test]
        public void DisbandedBrigadeIsRejected()
        {
            _brigade.ApplyCasualties(_brigade.CurrentPersonnel);

            BrigadeMoveResult result = Move();

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.BrigadeDisbanded));
        }

        [Test]
        public void UndeployedBrigadeIsRejected()
        {
            _bastion.TryUndeployBrigade(_brigade.Id);

            BrigadeMoveResult result = Move();

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.BrigadeNotDeployed));
        }

        [Test]
        public void WrongSourceIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _right.Id, _left.Id, _passage.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.SourceMismatch));
        }

        [Test]
        public void MissingTargetIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _left.Id, Guid.NewGuid(), _passage.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.TargetModuleNotFound));
        }

        [Test]
        public void SameModuleIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _left.Id, _left.Id, _passage.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.SameModule));
        }

        [Test]
        public void NonAdjacentTargetIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _left.Id, _third.Id, _passage.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.ModulesNotAdjacent));
        }

        [Test]
        public void MissingPassageIsRejected()
        {
            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _left.Id, _right.Id, Guid.NewGuid());

            Assert.That(result.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.PassageNotFound));
        }

        [Test]
        public void UnrelatedPassageIsRejected()
        {
            ModulePassage unrelated = InstallDoor(
                _right,
                _third,
                ModulePassageTraversalMode.Bidirectional);

            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _left.Id, _right.Id, unrelated.Id);

            Assert.That(result.FailureReason,
                Is.EqualTo(
                    BrigadeMoveFailureReason.PassageTraversalRejected));

            Assert.That(
                result.Assessment.TraversalFailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason
                        .PassageDoesNotConnectModules));
        }

        [TestCase(ModulePassageState.Closed)]
        [TestCase(ModulePassageState.Locked)]
        [TestCase(ModulePassageState.Blocked)]
        [TestCase(ModulePassageState.Destroyed)]
        public void UnavailablePassageRejectsMove(
            ModulePassageState state)
        {
            _passage.SetState(state);

            BrigadeMoveResult result = Move();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason,
                Is.EqualTo(
                    BrigadeMoveFailureReason.PassageTraversalRejected));
        }

        [Test]
        public void OneWayPassageRejectsReverseMovement()
        {
            _bastion.TryRemovePassage(_passage.Id, out _);

            _passage = InstallDoor(
                _left,
                _right,
                ModulePassageTraversalMode.SourceToTargetOnly);

            _bastion.TryUndeployBrigade(_brigade.Id);
            _bastion.TryDeployBrigadeToModule(_brigade.Id, _right.Id);

            BrigadeMoveResult result = _bastion.TryMoveBrigade(
                _brigade.Id, _right.Id, _left.Id, _passage.Id);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Assessment.TraversalFailureReason,
                Is.EqualTo(
                    ModulePassageTraversalFailureReason.DirectionNotAllowed));
        }

        [Test]
        public void FailedMovePreservesLocationAndWork()
        {
            _bastion.TryStartBrigadeWork(_brigade.Id);
            _passage.SetState(ModulePassageState.Locked);

            BrigadeMoveResult result = Move();

            Assert.That(result.IsSuccess, Is.False);

            _bastion.TryGetBrigadeOperationalState(
                _brigade.Id, out BrigadeOperationalState state);

            Assert.That(state.CurrentModuleId, Is.EqualTo(_left.Id));
            Assert.That(state.IsWorking, Is.True);

            Assert.That(
                _left.OccupyingBrigadeIds,
                Does.Contain(_brigade.Id));

            Assert.That(_right.OccupyingBrigadeIds, Is.Empty);
        }

        [Test]
        public void CompletedMoveCannotRepeatFromOldSource()
        {
            Assert.That(Move().IsSuccess, Is.True);

            BrigadeMoveResult repeated = Move();

            Assert.That(repeated.IsSuccess, Is.False);
            Assert.That(repeated.FailureReason,
                Is.EqualTo(BrigadeMoveFailureReason.SourceMismatch));
        }

        [Test]
        public void MovedBrigadeCanStartWorkingInTarget()
        {
            Assert.That(Move().IsSuccess, Is.True);

            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(
                _right.WorkingBrigadeIds,
                Does.Contain(_brigade.Id));
        }

        private BrigadeMoveResult Move()
        {
            return _bastion.TryMoveBrigade(
                _brigade.Id,
                _left.Id,
                _right.Id,
                _passage.Id);
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
    }
}
