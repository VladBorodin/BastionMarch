using System.Linq;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BastionPresentationStateTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void CapturesBastionAndModuleState()
        {
            var bastion =
                new Bastion(
                    name: "snapshot-test",
                    width: 6,
                    deckCount: 3);

            ModuleInstance module =
                bastion.TryInstallModule(
                        _catalog.GetRequired(
                            ModuleDefinitionIds
                                .StandardGeneratorRoom),
                        new GridPosition(1, 0))
                    .Module;

            module.SetPowerMode(
                ModulePowerMode.Active);

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            Assert.That(
                state.BastionId,
                Is.EqualTo(bastion.Id));

            Assert.That(
                state.Name,
                Is.EqualTo(bastion.Name));

            Assert.That(
                state.Width,
                Is.EqualTo(6));

            Assert.That(
                state.DeckCount,
                Is.EqualTo(3));

            Assert.That(
                state.ModuleCount,
                Is.EqualTo(1));

            ModulePresentationState moduleState =
                state.Modules[0];

            Assert.That(
                moduleState.ModuleId,
                Is.EqualTo(module.Id));

            Assert.That(
                moduleState.DefinitionId,
                Is.EqualTo(
                    module.Definition.Id));

            Assert.That(
                moduleState.Position,
                Is.EqualTo(
                    module.Position));

            Assert.That(
                moduleState.CurrentDurability,
                Is.EqualTo(
                    module.CurrentDurability));

            Assert.That(
                moduleState.RequestedPowerMode,
                Is.EqualTo(
                    ModulePowerMode.Active));
        }

        [Test]
        public void CapturedStateDoesNotChangeWithSimulation()
        {
            var bastion =
                new Bastion(
                    name: "immutable-snapshot-test",
                    width: 6,
                    deckCount: 3);

            ModuleInstance module =
                bastion.TryInstallModule(
                        _catalog.GetRequired(
                            ModuleDefinitionIds
                                .SmallMachineRoom),
                        new GridPosition(0, 0))
                    .Module;

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            ModulePresentationState captured =
                state.Modules[0];

            int capturedDurability =
                captured.CurrentDurability;

            ModulePowerMode capturedPowerMode =
                captured.RequestedPowerMode;

            module.ApplyDamage(
                module.Definition.MaxDurability);

            module.SetPowerMode(
                ModulePowerMode.Offline);

            Assert.That(
                captured.CurrentDurability,
                Is.EqualTo(
                    capturedDurability));

            Assert.That(
                captured.RequestedPowerMode,
                Is.EqualTo(
                    capturedPowerMode));

            Assert.That(
                module.TechnicalState,
                Is.EqualTo(
                    ModuleTechnicalState.Destroyed));

            Assert.That(
                module.RequestedPowerMode,
                Is.EqualTo(
                    ModulePowerMode.Offline));
        }

        [Test]
        public void ModulesAreCapturedInDeterministicGridOrder()
        {
            var bastion =
                new Bastion(
                    name: "ordering-test",
                    width: 6,
                    deckCount: 3);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds
                        .SmallMachineRoom);

            ModuleInstance upperRight =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(3, 1))
                    .Module;

            ModuleInstance lower =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(1, 0))
                    .Module;

            ModuleInstance upperLeft =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(0, 1))
                    .Module;

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            CollectionAssert.AreEqual(
                new[]
                {
                    lower.Id,
                    upperLeft.Id,
                    upperRight.Id
                },
                state.Modules
                    .Select(module =>
                        module.ModuleId)
                    .ToArray());
        }
    }
}