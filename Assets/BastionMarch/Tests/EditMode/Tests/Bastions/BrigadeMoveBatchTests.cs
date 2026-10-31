
using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BrigadeMoveBatchTests
    {
        private Bastion _bastion;
        private ModuleInstance _left;
        private ModuleInstance _middle;
        private ModuleInstance _right;
        private ModulePassage _leftMiddle;
        private ModulePassage _middleRight;
        private Brigade _first;
        private Brigade _second;

        [SetUp]
        public void SetUp()
        {
            var catalog = InitialModuleDefinitions.CreateCatalog();
            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _bastion = new Bastion("batch-move-test", 8, 2);

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
                    _first.Id, _left.Id).IsSuccess,
                Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    _second.Id, _middle.Id).IsSuccess,
                Is.True);
        }

        [Test]
        public void MultipleMovesAreAppliedFromSharedStartingState()
        {
            BrigadeMoveAssessment first = FirstMove();
            BrigadeMoveAssessment second = SecondMove();

            Assert.That(first.IsAllowed, Is.True);
            Assert.That(second.IsAllowed, Is.True);

            IReadOnlyList<BrigadeMoveResult> results =
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { first, second });

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(item => item.IsSuccess), Is.True);

            AssertLocation(_first, _middle);
            AssertLocation(_second, _right);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InputOrderDoesNotDetermineMoveSuccess(bool reverse)
        {
            BrigadeMoveAssessment first = FirstMove();

            BrigadeMoveAssessment second =
                _bastion.AssessBrigadeMove(
                    _second.Id,
                    _middle.Id,
                    _left.Id,
                    _leftMiddle.Id);

            var moves = reverse
                ? new[] { second, first }
                : new[] { first, second };

            IReadOnlyList<BrigadeMoveResult> results =
                _bastion.CommitAssessedBrigadeMoves(moves);

            Assert.That(results.All(item => item.IsSuccess), Is.True);

            AssertLocation(_first, _middle);
            AssertLocation(_second, _left);
        }

        [Test]
        public void RejectedMoveAbortsBatchWithoutMutation()
        {
            BrigadeMoveAssessment first = FirstMove();

            _middleRight.SetState(ModulePassageState.Locked);
            BrigadeMoveAssessment second = SecondMove();

            Assert.That(first.IsAllowed, Is.True);
            Assert.That(second.IsAllowed, Is.False);

            Assert.Throws<ArgumentException>(() =>
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { first, second }));

            AssertInitialLocations();
        }

        [Test]
        public void StaleAssessmentAbortsBatchWithoutMutation()
        {
            BrigadeMoveAssessment first = FirstMove();
            BrigadeMoveAssessment second = SecondMove();

            Assert.That(first.IsAllowed, Is.True);
            Assert.That(second.IsAllowed, Is.True);

            // Мир изменился после read-only assessment.
            _middleRight.SetState(ModulePassageState.Locked);

            Assert.Throws<InvalidOperationException>(() =>
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { first, second }));

            AssertInitialLocations();
        }

        [Test]
        public void FailedBatchPreservesWorkingState()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_first.Id).IsSuccess,
                Is.True);

            BrigadeMoveAssessment first = FirstMove();
            BrigadeMoveAssessment second = SecondMove();

            _middleRight.SetState(ModulePassageState.Blocked);

            Assert.Throws<InvalidOperationException>(() =>
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { first, second }));

            AssertInitialLocations();

            Assert.That(
                _left.WorkingBrigadeIds,
                Has.Member(_first.Id));

            _bastion.TryGetBrigadeOperationalState(
                _first.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.True);
        }

        [Test]
        public void SuccessfulBatchStopsPreviousWork()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_first.Id).IsSuccess,
                Is.True);

            _bastion.CommitAssessedBrigadeMoves(
                new[] { FirstMove(), SecondMove() });

            Assert.That(
                _left.WorkingBrigadeIds,
                Has.No.Member(_first.Id));

            Assert.That(
                _middle.WorkingBrigadeIds,
                Has.No.Member(_first.Id));

            _bastion.TryGetBrigadeOperationalState(
                _first.Id, out BrigadeOperationalState state);

            Assert.That(state.IsWorking, Is.False);
        }

        [Test]
        public void DuplicateBrigadeIsRejected()
        {
            BrigadeMoveAssessment move = FirstMove();

            Assert.Throws<ArgumentException>(() =>
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { move, move }));

            AssertInitialLocations();
        }

        [Test]
        public void NullCollectionIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _bastion.CommitAssessedBrigadeMoves(null));
        }

        [Test]
        public void NullAssessmentIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                _bastion.CommitAssessedBrigadeMoves(
                    new BrigadeMoveAssessment[]
                    {
                        FirstMove(),
                        null
                    }));

            AssertInitialLocations();
        }

        [Test]
        public void EmptyBatchIsValid()
        {
            IReadOnlyList<BrigadeMoveResult> results =
                _bastion.CommitAssessedBrigadeMoves(
                    Array.Empty<BrigadeMoveAssessment>());

            Assert.That(results, Is.Empty);
            AssertInitialLocations();
        }

        [Test]
        public void ResultIsReadOnly()
        {
            IReadOnlyList<BrigadeMoveResult> results =
                _bastion.CommitAssessedBrigadeMoves(
                    new[] { FirstMove(), SecondMove() });

            Assert.That(
                ((IList<BrigadeMoveResult>)results).IsReadOnly,
                Is.True);

            CollectionAssert.AreEqual(
                new[] { _first.Id, _second.Id }.OrderBy(id => id),
                results.Select(item => item.BrigadeId));
        }

        private BrigadeMoveAssessment FirstMove()
        {
            return _bastion.AssessBrigadeMove(
                _first.Id,
                _left.Id,
                _middle.Id,
                _leftMiddle.Id);
        }

        private BrigadeMoveAssessment SecondMove()
        {
            return _bastion.AssessBrigadeMove(
                _second.Id,
                _middle.Id,
                _right.Id,
                _middleRight.Id);
        }

        private void AssertInitialLocations()
        {
            AssertLocation(_first, _left);
            AssertLocation(_second, _middle);
        }

        private void AssertLocation(
            Brigade brigade,
            ModuleInstance expected)
        {
            Assert.That(
                _bastion.TryGetBrigadeLocation(
                    brigade.Id, out ModuleInstance actual),
                Is.True);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
        }

        private Brigade CreateBrigade(int number)
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
                    source.Id, target.Id,
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
    }
}
