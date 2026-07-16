using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Power
{
    [TestFixture]
    public sealed class ModulePowerStateTests
    {
        private ModuleInstance _module;

        [SetUp]
        public void SetUp()
        {
            ModuleDefinition definition =
                InitialModuleDefinitions
                    .CreateCatalog()
                    .GetRequired(
                        ModuleDefinitionIds.SmallMachineRoom);

            _module = new ModuleInstance(
                definition,
                new GridPosition(0, 0));
        }

        [Test]
        public void NewModuleStartsInStandbyWithNormalPriority()
        {
            Assert.That(
                _module.PowerMode,
                Is.EqualTo(ModulePowerMode.Standby));

            Assert.That(
                _module.PowerPriority,
                Is.EqualTo(PowerPriority.Normal));

            Assert.That(
                _module.CurrentContinuousPowerDemand,
                Is.EqualTo(1));
        }

        [Test]
        public void OfflineModuleConsumesNoContinuousPower()
        {
            _module.SetPowerMode(
                ModulePowerMode.Offline);

            Assert.That(
                _module.CurrentContinuousPowerDemand,
                Is.Zero);
        }

        [Test]
        public void ActiveModuleUsesActivePowerDemand()
        {
            _module.SetPowerMode(
                ModulePowerMode.Active);

            Assert.That(
                _module.CurrentContinuousPowerDemand,
                Is.EqualTo(4));
        }

        [Test]
        public void ModulePowerPriorityCanBeChanged()
        {
            _module.SetPowerPriority(
                PowerPriority.Critical);

            Assert.That(
                _module.PowerPriority,
                Is.EqualTo(PowerPriority.Critical));
        }
    }
}