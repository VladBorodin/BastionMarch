using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Power
{
    [TestFixture]
    public sealed class BastionPowerDistributionTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void ManualOfflineModuleRemainsOffline()
        {
            var bastion = new Bastion(
                name: "Ручное управление",
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
                ModulePowerMode.Offline);

            BastionPowerDistributionResult result =
                bastion.ResolvePowerDistribution();

            Assert.That(
                repairBay.Module.RequestedPowerMode,
                Is.EqualTo(ModulePowerMode.Offline));

            Assert.That(
                repairBay.Module.EffectivePowerMode,
                Is.EqualTo(ModulePowerMode.Offline));

            Assert.That(
                result.TotalGrantedDemand,
                Is.EqualTo(2));
        }

        [Test]
        public void CriticalConsumerIsPoweredBeforeLowPriorityConsumer()
        {
            var bastion = new Bastion(
                name: "Приоритетная сеть",
                width: 12,
                deckCount: 3);

            ModulePlacementResult generator =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardGeneratorRoom),
                    new GridPosition(0, 0));

            ModulePlacementResult criticalRepairBay =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(2, 0));

            ModulePlacementResult normalRepairBayOne =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(4, 0));

            ModulePlacementResult normalRepairBayTwo =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(6, 0));

            ModulePlacementResult lowPriorityRepairBay =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(8, 0));

            generator.Module.SetPowerMode(ModulePowerMode.Active);

            criticalRepairBay.Module.SetPowerMode(
                ModulePowerMode.Active);

            normalRepairBayOne.Module.SetPowerMode(
                ModulePowerMode.Active);

            normalRepairBayTwo.Module.SetPowerMode(
                ModulePowerMode.Active);

            lowPriorityRepairBay.Module.SetPowerMode(
                ModulePowerMode.Active);

            criticalRepairBay.Module.SetPowerPriority(
                PowerPriority.Critical);

            lowPriorityRepairBay.Module.SetPowerPriority(
                PowerPriority.Low);

            BastionPowerDistributionResult result =
                bastion.ResolvePowerDistribution();

            Assert.That(
                criticalRepairBay.Module.EffectivePowerMode,
                Is.EqualTo(ModulePowerMode.Active));

            Assert.That(
                lowPriorityRepairBay.Module.EffectivePowerMode,
                Is.EqualTo(ModulePowerMode.Offline));

            Assert.That(result.HasLoadShedding, Is.True);
        }

        [Test]
        public void RestoredManualRequestAllowsModuleToReceivePower()
        {
            var bastion = new Bastion(
                name: "Повторный запуск",
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
                ModulePowerMode.Offline);

            bastion.ResolvePowerDistribution();

            repairBay.Module.SetPowerMode(
                ModulePowerMode.Active);

            bastion.ResolvePowerDistribution();

            Assert.That(
                repairBay.Module.RequestedPowerMode,
                Is.EqualTo(ModulePowerMode.Active));

            Assert.That(
                repairBay.Module.EffectivePowerMode,
                Is.EqualTo(ModulePowerMode.Active));
        }
    }
}