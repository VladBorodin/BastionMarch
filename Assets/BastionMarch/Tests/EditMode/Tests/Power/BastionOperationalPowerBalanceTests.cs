using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Power
{
    [TestFixture]
    public sealed class BastionOperationalPowerBalanceTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void GeneratorInStandbyConsumesPowerButDoesNotProduceIt()
        {
            var bastion = new Bastion(
                name: "Резервный генератор",
                width: 4,
                deckCount: 2);

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom),
                new GridPosition(0, 0));

            BastionOperationalPowerBalance balance =
                bastion.CalculateOperationalPowerBalance();

            Assert.That(
                balance.AvailablePowerGeneration,
                Is.Zero);

            Assert.That(
                balance.CurrentPowerDemand,
                Is.EqualTo(1));

            Assert.That(balance.PowerReserve, Is.EqualTo(-1));
            Assert.That(balance.IsBalanced, Is.False);
        }

        [Test]
        public void ActiveGeneratorProducesPowerAndUsesActiveDemand()
        {
            var bastion = new Bastion(
                name: "Работающий генератор",
                width: 4,
                deckCount: 2);

            ModulePlacementResult placement =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            placement.Module.SetPowerMode(
                ModulePowerMode.Active);

            BastionOperationalPowerBalance balance =
                bastion.CalculateOperationalPowerBalance();

            Assert.That(
                balance.AvailablePowerGeneration,
                Is.EqualTo(40));

            Assert.That(
                balance.CurrentPowerDemand,
                Is.EqualTo(2));

            Assert.That(
                balance.PowerReserve,
                Is.EqualTo(38));

            Assert.That(balance.IsBalanced, Is.True);
        }

        [Test]
        public void ActiveConsumersUseTheirFullContinuousDemand()
        {
            var bastion = new Bastion(
                name: "Нагруженная энергосистема",
                width: 8,
                deckCount: 3);

            ModulePlacementResult generator =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            ModulePlacementResult repairBay =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(2, 0));

            generator.Module.SetPowerMode(
                ModulePowerMode.Active);

            repairBay.Module.SetPowerMode(
                ModulePowerMode.Active);

            BastionOperationalPowerBalance balance =
                bastion.CalculateOperationalPowerBalance();

            Assert.That(
                balance.AvailablePowerGeneration,
                Is.EqualTo(40));

            Assert.That(
                balance.CurrentPowerDemand,
                Is.EqualTo(14));

            Assert.That(
                balance.PowerReserve,
                Is.EqualTo(26));
        }

        [Test]
        public void DestroyedGeneratorDoesNotProduceOrConsumePower()
        {
            var bastion = new Bastion(
                name: "Уничтоженный генератор",
                width: 4,
                deckCount: 2);

            ModulePlacementResult generator =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            generator.Module.SetPowerMode(
                ModulePowerMode.Active);

            generator.Module.ApplyDamage(1_000);

            BastionOperationalPowerBalance balance =
                bastion.CalculateOperationalPowerBalance();

            Assert.That(
                balance.AvailablePowerGeneration,
                Is.Zero);

            Assert.That(
                balance.CurrentPowerDemand,
                Is.Zero);

            Assert.That(balance.IsBalanced, Is.True);
        }

        [Test]
        public void OccupiedGeneratorDoesNotServePlayerPowerNetwork()
        {
            var bastion = new Bastion(
                name: "Захваченный генератор",
                width: 4,
                deckCount: 2);

            ModulePlacementResult generator =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            generator.Module.SetPowerMode(
                ModulePowerMode.Active);

            generator.Module.SetControlState(
                ModuleControlState.Occupied);

            BastionOperationalPowerBalance balance =
                bastion.CalculateOperationalPowerBalance();

            Assert.That(
                balance.AvailablePowerGeneration,
                Is.Zero);

            Assert.That(
                balance.CurrentPowerDemand,
                Is.Zero);
        }
    }
}