using System;
using System.Collections.Generic;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Modules.Features;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Modules
{
    [TestFixture]
    public sealed class ModuleDefinitionCatalogTests
    {
        [Test]
        public void InitialCatalogContainsExpectedDefinitions()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            Assert.That(catalog.All.Count, Is.EqualTo(5));

            Assert.That(
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom),
                Is.Not.Null);

            Assert.That(
                catalog.GetRequired(ModuleDefinitionIds.LargeMachineRoom),
                Is.Not.Null);

            Assert.That(
                catalog.GetRequired(ModuleDefinitionIds.StandardRepairBay),
                Is.Not.Null);

            Assert.That(
                catalog.GetRequired(ModuleDefinitionIds.StandardAmmoStorage),
                Is.Not.Null);

            Assert.That(
                catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom),
                Is.Not.Null);
        }

        [Test]
        public void CatalogRejectsDuplicateDefinitionIds()
        {
            ModuleDefinition definition =
                InitialModuleDefinitions
                    .CreateCatalog()
                    .GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            var duplicatedDefinitions = new[]
            {
                definition,
                definition
            };

            Assert.Throws<ArgumentException>(
                () => new ModuleDefinitionCatalog(duplicatedDefinitions));
        }

        [Test]
        public void GetRequiredThrowsWhenDefinitionDoesNotExist()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            Assert.Throws<KeyNotFoundException>(
                () => catalog.GetRequired("module.does-not-exist"));
        }

        [Test]
        public void MachineRoomProvidesPropulsionFeature()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.SmallMachineRoom);

            bool found = definition.TryGetFeature(
                out PropulsionFeatureDefinition feature);

            Assert.That(found, Is.True);
            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.HorsePower, Is.EqualTo(1_200));
            Assert.That(feature.FuelConsumptionPerTurn, Is.EqualTo(20));
        }

        [Test]
        public void RepairBayProvidesRepairSupportFeature()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.StandardRepairBay);

            bool found = definition.TryGetFeature(
                out RepairSupportFeatureDefinition feature);

            Assert.That(found, Is.True);
            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.RepairPointsPerTurn, Is.EqualTo(20));
            Assert.That(feature.MaximumConcurrentJobs, Is.EqualTo(2));
        }

        [Test]
        public void AmmoStorageDoesNotProvidePropulsionFeature()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            ModuleDefinition definition =
                catalog.GetRequired(ModuleDefinitionIds.StandardAmmoStorage);

            bool found = definition.TryGetFeature(
                out PropulsionFeatureDefinition feature);

            Assert.That(found, Is.False);
            Assert.That(feature, Is.Null);
        }

        [Test]
        public void GeneratorRoomProvidesPowerGenerationFeature()
        {
            ModuleDefinitionCatalog catalog =
                InitialModuleDefinitions.CreateCatalog();

            ModuleDefinition definition =
                catalog.GetRequired(
                    ModuleDefinitionIds.StandardGeneratorRoom);

            bool found = definition.TryGetFeature(
                out PowerGenerationFeatureDefinition feature);

            Assert.That(found, Is.True);
            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.MaximumPowerOutput, Is.EqualTo(40));
            Assert.That(feature.FuelConsumptionPerTurn, Is.EqualTo(18));
        }
    }
}