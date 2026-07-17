using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Modules
{
    [TestFixture]
    public sealed class ModuleOperationalEfficiencyTests
    {
        private ModuleDefinitionCatalog _moduleCatalog;
        private BrigadeWorkProfileCatalog _profileCatalog;

        [SetUp]
        public void SetUp()
        {
            _moduleCatalog =
                InitialModuleDefinitions.CreateCatalog();

            _profileCatalog =
                InitialBrigadeWorkProfiles.CreateCatalog();
        }

        [Test]
        public void PoweredStaffedOperationalModuleCanWork()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.True);

            Assert.That(
                result.TechnicalEfficiencyMultiplier,
                Is.EqualTo(1.0));

            Assert.That(
                result.OverallEfficiencyRatio,
                Is.GreaterThan(0));

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue.Destroyed),
                Is.False);
        }

        [Test]
        public void StandbyModuleCannotPerformActiveWork()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.SetPowerMode(
                ModulePowerMode.Standby);

            bastion.ResolvePowerDistribution();

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue
                        .InactivePowerMode),
                Is.True);

            Assert.That(
                result.OverallEfficiencyRatio,
                Is.Zero);
        }

        [Test]
        public void ManualShutdownPreventsActiveWork()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.SetPowerMode(
                ModulePowerMode.Offline);

            bastion.ResolvePowerDistribution();

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                repairBay.IsManuallyPoweredOff,
                Is.True);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue
                        .InactivePowerMode),
                Is.True);
        }

        [Test]
        public void DamagedModuleRetainsReducedEfficiency()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.ApplyDamage(60);

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                repairBay.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Damaged));

            Assert.That(
                result.CanPerformActiveWork,
                Is.True);

            Assert.That(
                result.TechnicalEfficiencyMultiplier,
                Is.EqualTo(0.75));

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue.Damaged),
                Is.True);
        }

        [Test]
        public void CriticalModuleRetainsSeverelyReducedEfficiency()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.ApplyDamage(110);

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                repairBay.TechnicalState,
                Is.EqualTo(ModuleTechnicalState.Critical));

            Assert.That(
                result.CanPerformActiveWork,
                Is.True);

            Assert.That(
                result.TechnicalEfficiencyMultiplier,
                Is.EqualTo(0.40));

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue.CriticalDamage),
                Is.True);
        }

        [Test]
        public void DestroyedModuleCannotWork()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.ApplyDamage(1_000);

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                result.OverallEfficiencyRatio,
                Is.Zero);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue.Destroyed),
                Is.True);
        }

        [Test]
        public void OccupiedModuleCannotServePlayer()
        {
            Bastion bastion =
                CreatePoweredRepairBastion(
                    out ModuleInstance repairBay);

            repairBay.SetControlState(
                ModuleControlState.Occupied);

            bastion.ResolvePowerDistribution();

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue
                        .NotFriendlyControlled),
                Is.True);
        }

        [Test]
        public void InsufficientQualifiedPersonnelBlocksOperation()
        {
            var bastion = new Bastion(
                name: "understaffed-test",
                width: 8,
                deckCount: 3);

            ModuleInstance generator =
                Install(
                    bastion,
                    ModuleDefinitionIds
                        .StandardGeneratorRoom,
                    x: 0);

            ModuleInstance repairBay =
                Install(
                    bastion,
                    ModuleDefinitionIds
                        .StandardRepairBay,
                    x: 2);

            var recruits = new Brigade(
                number: 1,
                type: BrigadeType.Recruit,
                currentPersonnel: 3,
                maximumPersonnel: 3);

            bastion.TryAddBrigade(recruits);

            bastion.TryDeployBrigadeToModule(
                recruits.Id,
                repairBay.Id);

            bastion.TryStartBrigadeWork(
                recruits.Id);

            generator.SetPowerMode(ModulePowerMode.Active);
            repairBay.SetPowerMode(ModulePowerMode.Active);

            bastion.ResolvePowerDistribution();

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue
                        .InsufficientQualifiedPersonnel),
                Is.True);
        }

        [Test]
        public void PowerDeficitCanDisableOtherwiseValidModule()
        {
            var bastion = new Bastion(
                name: "power-deficit-test",
                width: 8,
                deckCount: 3);

            ModuleInstance repairBay =
                Install(
                    bastion,
                    ModuleDefinitionIds
                        .StandardRepairBay,
                    x: 0);

            AddMechanics(
                bastion,
                repairBay,
                personnel: 6);

            repairBay.SetPowerMode(
                ModulePowerMode.Active);

            bastion.ResolvePowerDistribution();

            ModuleOperationalAssessment result =
                bastion.CalculateModuleOperationalAssessment(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                repairBay.EffectivePowerMode,
                Is.EqualTo(ModulePowerMode.Offline));

            Assert.That(
                result.CanPerformActiveWork,
                Is.False);

            Assert.That(
                result.HasIssue(
                    ModuleOperationalIssue
                        .InactivePowerMode),
                Is.True);
        }

        private Bastion CreatePoweredRepairBastion(
            out ModuleInstance repairBay)
        {
            var bastion = new Bastion(
                name: "operational-test",
                width: 8,
                deckCount: 3);

            ModuleInstance generator =
                Install(
                    bastion,
                    ModuleDefinitionIds
                        .StandardGeneratorRoom,
                    x: 0);

            repairBay =
                Install(
                    bastion,
                    ModuleDefinitionIds
                        .StandardRepairBay,
                    x: 2);

            AddMechanics(
                bastion,
                repairBay,
                personnel: 6);

            generator.SetPowerMode(
                ModulePowerMode.Active);

            repairBay.SetPowerMode(
                ModulePowerMode.Active);

            bastion.ResolvePowerDistribution();

            return bastion;
        }

        private ModuleInstance Install(
            Bastion bastion,
            string definitionId,
            int x)
        {
            return bastion.TryInstallModule(
                    _moduleCatalog.GetRequired(definitionId),
                    new GridPosition(x, 0))
                .Module;
        }

        private static void AddMechanics(
            Bastion bastion,
            ModuleInstance module,
            int personnel)
        {
            var brigade = new Brigade(
                number: 1,
                type: BrigadeType.Mechanic,
                currentPersonnel: personnel,
                maximumPersonnel: personnel,
                experience: 50);

            bastion.TryAddBrigade(
                brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                module.Id);

            bastion.TryStartBrigadeWork(
                brigade.Id);
        }
    }
}