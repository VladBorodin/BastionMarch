using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BastionGridTests
    {
        private ModuleDefinitionCatalog _catalog;
        private BastionGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();

            _grid = new BastionGrid(
                width: 6,
                deckCount: 3);
        }

        [Test]
        public void ConstructorRejectsNonPositiveWidth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BastionGrid(
                    width: 0,
                    deckCount: 3));
        }

        [Test]
        public void ConstructorRejectsNonPositiveDeckCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BastionGrid(
                    width: 6,
                    deckCount: 0));
        }

        [Test]
        public void PlacesModuleAndReturnsItByCell()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom);

            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 1, deck: 1));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Module, Is.Not.Null);
            Assert.That(_grid.ModuleCount, Is.EqualTo(1));

            bool found = _grid.TryGetModuleAt(
                new GridPosition(x: 1, deck: 1),
                out ModuleInstance module);

            Assert.That(found, Is.True);
            Assert.That(module, Is.SameAs(result.Module));
        }

        [Test]
        public void LargeModuleOccupiesEveryCoveredCell()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom);

            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 2, deck: 0));

            Assert.That(result.IsSuccess, Is.True);

            GridPosition[] expectedCells =
            {
                new(2, 0),
                new(3, 0),
                new(2, 1),
                new(3, 1)
            };

            foreach (GridPosition cell in expectedCells)
            {
                bool found = _grid.TryGetModuleAt(
                    cell,
                    out ModuleInstance module);

                Assert.That(found, Is.True);
                Assert.That(module, Is.SameAs(result.Module));
            }
        }

        [Test]
        public void RejectsModuleOutsideHorizontalBounds()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom);

            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 5, deck: 0));

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePlacementFailureReason.OutOfBounds));

            Assert.That(result.Module, Is.Null);
            Assert.That(_grid.ModuleCount, Is.Zero);
        }

        [Test]
        public void RejectsModuleOutsideVerticalBounds()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom);

            ModulePlacementResult result =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 0, deck: 2));

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ModulePlacementFailureReason.OutOfBounds));

            Assert.That(_grid.ModuleCount, Is.Zero);
        }

        [Test]
        public void RejectsOverlapWithExistingModule()
        {
            ModuleDefinition smallDefinition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom);

            ModuleDefinition largeDefinition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom);

            ModulePlacementResult firstPlacement =
                _grid.TryPlaceModule(
                    smallDefinition,
                    new GridPosition(x: 1, deck: 0));

            ModulePlacementResult overlappingPlacement =
                _grid.TryPlaceModule(
                    largeDefinition,
                    new GridPosition(x: 0, deck: 0));

            Assert.That(firstPlacement.IsSuccess, Is.True);
            Assert.That(overlappingPlacement.IsSuccess, Is.False);

            Assert.That(
                overlappingPlacement.FailureReason,
                Is.EqualTo(
                    ModulePlacementFailureReason.Occupied));

            Assert.That(_grid.ModuleCount, Is.EqualTo(1));
        }

        [Test]
        public void AllowsModulesInAdjacentCells()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom);

            ModulePlacementResult first =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 0, deck: 0));

            ModulePlacementResult second =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 1, deck: 0));

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(_grid.ModuleCount, Is.EqualTo(2));
        }

        [Test]
        public void RemovingModuleFreesEveryOccupiedCell()
        {
            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom);

            ModulePlacementResult placement =
                _grid.TryPlaceModule(
                    definition,
                    new GridPosition(x: 2, deck: 0));

            bool removed = _grid.TryRemoveModule(
                placement.Module.Id,
                out ModuleInstance removedModule);

            Assert.That(removed, Is.True);
            Assert.That(removedModule, Is.SameAs(placement.Module));
            Assert.That(_grid.ModuleCount, Is.Zero);

            Assert.That(
                _grid.TryGetModuleAt(
                    new GridPosition(2, 0),
                    out _),
                Is.False);

            Assert.That(
                _grid.TryGetModuleAt(
                    new GridPosition(3, 1),
                    out _),
                Is.False);
        }
    }
}