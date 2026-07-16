using System;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Modules
{
    [TestFixture]
    public sealed class ModuleInstanceTests
    {
        private ModuleDefinition _definition;
        private ModuleInstance _instance;

        [SetUp]
        public void SetUp()
        {
            _definition = InitialModuleDefinitions
                .CreateCatalog()
                .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            _instance = new ModuleInstance(
                _definition,
                new GridPosition(x: 0, deck: 0));
        }

        [Test]
        public void NewModuleStartsFullyOperational()
        {
            Assert.That(
                _instance.CurrentDurability,
                Is.EqualTo(_definition.MaxDurability));

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Operational));

            Assert.That(
                _instance.ControlState,
                Is.EqualTo(ModuleControlState.Friendly));
        }

        [Test]
        public void DamageChangesTechnicalStateAtDefinitionThresholds()
        {
            _instance.ApplyDamage(40);

            Assert.That(_instance.CurrentDurability, Is.EqualTo(60));

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Damaged));

            _instance.ApplyDamage(35);

            Assert.That(_instance.CurrentDurability, Is.EqualTo(25));

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Critical));

            _instance.ApplyDamage(25);

            Assert.That(_instance.CurrentDurability, Is.Zero);

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Destroyed));
        }

        [Test]
        public void DamageCannotReduceDurabilityBelowZero()
        {
            _instance.ApplyDamage(1_000);

            Assert.That(_instance.CurrentDurability, Is.Zero);

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Destroyed));
        }

        [Test]
        public void RepairCannotExceedMaximumDurability()
        {
            _instance.ApplyDamage(70);
            _instance.RestoreDurability(1_000);

            Assert.That(
                _instance.CurrentDurability,
                Is.EqualTo(_definition.MaxDurability));

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Operational));
        }

        [Test]
        public void ControlStateIsIndependentFromTechnicalState()
        {
            _instance.SetControlState(ModuleControlState.Occupied);
            _instance.ApplyDamage(1_000);

            Assert.That(
                _instance.ControlState,
                Is.EqualTo(ModuleControlState.Occupied));

            Assert.That(
                _instance.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Destroyed));
        }

        [Test]
        public void NewModuleUsesProvidedPosition()
        {
            Assert.That(_instance.Position.X, Is.Zero);
            Assert.That(_instance.Position.Deck, Is.Zero);
        }
    }
}