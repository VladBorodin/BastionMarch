using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Bastions
{
    [TestFixture]
    public sealed class BastionPowerBalanceTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void EmptyBastionHasZeroBalancedPower()
        {
            var bastion = new Bastion(
                name: "Пустой бастион",
                width: 6,
                deckCount: 3);

            BastionPowerBalance balance =
                bastion.CalculateDesignPowerBalance();

            Assert.That(
                balance.TotalPowerGeneration,
                Is.Zero);

            Assert.That(
                balance.TotalIdlePowerDemand,
                Is.Zero);

            Assert.That(
                balance.TotalActivePowerDemand,
                Is.Zero);

            Assert.That(balance.IdlePowerReserve, Is.Zero);
            Assert.That(balance.ActivePowerReserve, Is.Zero);

            Assert.That(
                balance.CanSustainIdleLoad,
                Is.True);

            Assert.That(
                balance.CanSustainFullLoad,
                Is.True);
        }

        [Test]
        public void ConsumersWithoutGeneratorCreatePowerDeficit()
        {
            var bastion = new Bastion(
                name: "Обесточенный бастион",
                width: 6,
                deckCount: 3);

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardRepairBay),
                new GridPosition(1, 0));

            BastionPowerBalance balance =
                bastion.CalculateDesignPowerBalance();

            Assert.That(
                balance.TotalPowerGeneration,
                Is.Zero);

            Assert.That(
                balance.TotalIdlePowerDemand,
                Is.EqualTo(4));

            Assert.That(
                balance.TotalActivePowerDemand,
                Is.EqualTo(16));

            Assert.That(
                balance.IdlePowerReserve,
                Is.EqualTo(-4));

            Assert.That(
                balance.ActivePowerReserve,
                Is.EqualTo(-16));

            Assert.That(
                balance.CanSustainIdleLoad,
                Is.False);

            Assert.That(
                balance.CanSustainFullLoad,
                Is.False);
        }

        [Test]
        public void GeneratorCanSupplyInstalledConsumers()
        {
            var bastion = new Bastion(
                name: "Энергетический образец",
                width: 8,
                deckCount: 3);

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom),
                new GridPosition(2, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardRepairBay),
                new GridPosition(3, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardAmmoStorage),
                new GridPosition(5, 0));

            BastionPowerBalance balance =
                bastion.CalculateDesignPowerBalance();

            Assert.That(
                balance.TotalPowerGeneration,
                Is.EqualTo(40));

            Assert.That(
                balance.TotalIdlePowerDemand,
                Is.EqualTo(5));

            Assert.That(
                balance.TotalActivePowerDemand,
                Is.EqualTo(19));

            Assert.That(
                balance.IdlePowerReserve,
                Is.EqualTo(35));

            Assert.That(
                balance.ActivePowerReserve,
                Is.EqualTo(21));

            Assert.That(
                balance.CanSustainIdleLoad,
                Is.True);

            Assert.That(
                balance.CanSustainFullLoad,
                Is.True);
        }

        [Test]
        public void MultipleGeneratorsAddTheirPowerOutput()
        {
            var bastion = new Bastion(
                name: "Резервированная энергосистема",
                width: 8,
                deckCount: 3);

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom),
                new GridPosition(2, 0));

            BastionPowerBalance balance =
                bastion.CalculateDesignPowerBalance();

            Assert.That(
                balance.TotalPowerGeneration,
                Is.EqualTo(80));

            Assert.That(
                balance.TotalIdlePowerDemand,
                Is.EqualTo(2));

            Assert.That(
                balance.TotalActivePowerDemand,
                Is.EqualTo(4));
        }

        [Test]
        public void DesignBalanceStillIncludesDestroyedGenerator()
        {
            var bastion = new Bastion(
                name: "Повреждённый генератор",
                width: 4,
                deckCount: 2);

            ModulePlacementResult placement =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            placement.Module.ApplyDamage(1_000);

            BastionPowerBalance balance =
                bastion.CalculateDesignPowerBalance();

            Assert.That(
                placement.Module.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Destroyed));

            Assert.That(
                balance.TotalPowerGeneration,
                Is.EqualTo(40));
        }
    }
}