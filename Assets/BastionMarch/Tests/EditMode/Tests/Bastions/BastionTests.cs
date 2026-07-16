using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BastionTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void ConstructorCreatesBastionWithProvidedIdentity()
        {
            Guid id = Guid.NewGuid();

            var bastion = new Bastion(
                id,
                name: "Испытательный бастион",
                width: 8,
                deckCount: 4);

            Assert.That(bastion.Id, Is.EqualTo(id));

            Assert.That(
                bastion.Name,
                Is.EqualTo("Испытательный бастион"));

            Assert.That(bastion.Width, Is.EqualTo(8));
            Assert.That(bastion.DeckCount, Is.EqualTo(4));
            Assert.That(bastion.ModuleCount, Is.Zero);
        }

        [Test]
        public void ConstructorRejectsEmptyIdentity()
        {
            Assert.Throws<ArgumentException>(
                () => new Bastion(
                    Guid.Empty,
                    name: "Некорректный бастион",
                    width: 8,
                    deckCount: 4));
        }

        [Test]
        public void ConstructorRejectsEmptyName()
        {
            Assert.Throws<ArgumentException>(
                () => new Bastion(
                    name: " ",
                    width: 8,
                    deckCount: 4));
        }

        [Test]
        public void InstallsAndRemovesModuleThroughAggregate()
        {
            var bastion = new Bastion(
                name: "Испытательный бастион",
                width: 8,
                deckCount: 4);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom);

            ModulePlacementResult placement =
                bastion.TryInstallModule(
                    definition,
                    new GridPosition(0, 0));

            Assert.That(placement.IsSuccess, Is.True);
            Assert.That(bastion.ModuleCount, Is.EqualTo(1));

            bool removed =
                bastion.TryRemoveModule(
                    placement.Module.Id,
                    out ModuleInstance removedModule);

            Assert.That(removed, Is.True);
            Assert.That(removedModule, Is.SameAs(placement.Module));
            Assert.That(bastion.ModuleCount, Is.Zero);
        }

        [Test]
        public void CalculatesCombinedDesignStatistics()
        {
            var bastion = new Bastion(
                name: "Расчётный образец",
                width: 8,
                deckCount: 4);

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.LargeMachineRoom),
                new GridPosition(1, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardRepairBay),
                new GridPosition(3, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardAmmoStorage),
                new GridPosition(5, 0));

            BastionDesignStatistics statistics =
                bastion.CalculateDesignStatistics();

            Assert.That(statistics.ModuleCount, Is.EqualTo(4));
            Assert.That(statistics.OccupiedCellCount, Is.EqualTo(9));

            Assert.That(
                statistics.TotalMassKg,
                Is.EqualTo(124_000));

            Assert.That(
                statistics.TotalCost,
                Is.EqualTo(8_200));

            Assert.That(
                statistics.TotalMaxDurability,
                Is.EqualTo(650));

            Assert.That(
                statistics.TotalIdlePowerConsumption,
                Is.EqualTo(7));

            Assert.That(
                statistics.TotalActivePowerConsumption,
                Is.EqualTo(27));

            Assert.That(
                statistics.TotalHeatGeneration,
                Is.EqualTo(60));

            Assert.That(
                statistics.MinimumPersonnel,
                Is.EqualTo(10));

            Assert.That(
                statistics.OptimalPersonnel,
                Is.EqualTo(20));

            Assert.That(
                statistics.MaximumPersonnel,
                Is.EqualTo(31));

            Assert.That(
                statistics.TotalHorsePower,
                Is.EqualTo(6_600));
        }

        [Test]
        public void DesignStatisticsDoNotDependOnCurrentDamage()
        {
            var bastion = new Bastion(
                name: "Повреждённый образец",
                width: 4,
                deckCount: 2);

            ModulePlacementResult placement =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.SmallMachineRoom),
                    new GridPosition(0, 0));

            placement.Module.ApplyDamage(1_000);

            BastionDesignStatistics statistics =
                bastion.CalculateDesignStatistics();

            Assert.That(
                placement.Module.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Destroyed));

            Assert.That(
                statistics.TotalMassKg,
                Is.EqualTo(18_000));

            Assert.That(
                statistics.TotalHorsePower,
                Is.EqualTo(1_200));
        }
    }
}