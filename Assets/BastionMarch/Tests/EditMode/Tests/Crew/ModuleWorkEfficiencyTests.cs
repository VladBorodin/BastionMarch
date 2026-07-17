using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class ModuleWorkEfficiencyTests
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
        public void MechanicOutperformsRecruitInMechanicalWork()
        {
            ModuleWorkTypeAssessment mechanicResult =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6);

            ModuleWorkTypeAssessment recruitResult =
                CalculateMechanicalAssessment(
                    BrigadeType.Recruit,
                    personnel: 6);

            Assert.That(
                mechanicResult.EffectivePersonnel,
                Is.GreaterThan(
                    recruitResult.EffectivePersonnel));

            Assert.That(
                mechanicResult.IsMinimumMet,
                Is.True);

            Assert.That(
                recruitResult.IsMinimumMet,
                Is.False);
        }

        [Test]
        public void ExperienceRaisesEffectivePersonnel()
        {
            ModuleWorkTypeAssessment inexperienced =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    experience: 0);

            ModuleWorkTypeAssessment veteran =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    experience: 100);

            Assert.That(
                veteran.EffectivePersonnel,
                Is.GreaterThan(
                    inexperienced.EffectivePersonnel));
        }

        [Test]
        public void FatigueReducesEffectivePersonnel()
        {
            ModuleWorkTypeAssessment rested =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    fatigue: 0);

            ModuleWorkTypeAssessment exhausted =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    fatigue: 100);

            Assert.That(
                exhausted.EffectivePersonnel,
                Is.LessThan(
                    rested.EffectivePersonnel));
        }

        [Test]
        public void LowMoraleReducesEffectivePersonnel()
        {
            ModuleWorkTypeAssessment confident =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    morale: 100);

            ModuleWorkTypeAssessment broken =
                CalculateMechanicalAssessment(
                    BrigadeType.Mechanic,
                    personnel: 6,
                    morale: 0);

            Assert.That(
                broken.EffectivePersonnel,
                Is.LessThan(
                    confident.EffectivePersonnel));
        }

        [Test]
        public void MultipleBrigadesCombineTheirContribution()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade mechanics =
                CreateBrigade(
                    number: 1,
                    BrigadeType.Mechanic,
                    personnel: 3);

            Brigade recruits =
                CreateBrigade(
                    number: 2,
                    BrigadeType.Recruit,
                    personnel: 6);

            AddDeployAndStartWork(
                bastion,
                repairBay,
                mechanics);

            AddDeployAndStartWork(
                bastion,
                repairBay,
                recruits);

            ModuleWorkEfficiencyAssessment result =
                bastion.CalculateModuleWorkEfficiency(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(
                result.WorkingBrigadeCount,
                Is.EqualTo(2));

            Assert.That(
                result.TotalWorkingPersonnel,
                Is.EqualTo(9));

            Assert.That(
                result.Get(WorkType.Mechanical)
                    .EffectivePersonnel,
                Is.GreaterThan(3));
        }

        [Test]
        public void InsufficientQualifiedPersonnelPreventsOperation()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade recruits =
                CreateBrigade(
                    number: 1,
                    BrigadeType.Recruit,
                    personnel: 3);

            AddDeployAndStartWork(
                bastion,
                repairBay,
                recruits);

            ModuleWorkEfficiencyAssessment result =
                bastion.CalculateModuleWorkEfficiency(
                    repairBay.Id,
                    _profileCatalog);

            Assert.That(result.CanOperate, Is.False);

            Assert.That(
                result.Get(WorkType.Mechanical)
                    .IsMinimumMet,
                Is.False);
        }

        [Test]
        public void OvercrowdingReducesOverallEfficiency()
        {
            ModuleWorkEfficiencyAssessment normal =
                CalculateRepairBayEfficiency(
                    personnel: 9);

            ModuleWorkEfficiencyAssessment overcrowded =
                CalculateRepairBayEfficiency(
                    personnel: 12);

            Assert.That(
                normal.OvercrowdingMultiplier,
                Is.EqualTo(1.0));

            Assert.That(
                overcrowded.OvercrowdingMultiplier,
                Is.LessThan(1.0));

            Assert.That(
                overcrowded.OverallEfficiencyRatio,
                Is.LessThan(
                    normal.OverallEfficiencyRatio));
        }

        [Test]
        public void ModuleWithoutWorkRequirementsNeedsNoBrigade()
        {
            var bastion = CreateBastion();

            ModuleInstance crewQuarters =
                bastion.TryInstallModule(
                        _moduleCatalog.GetRequired(
                            ModuleDefinitionIds
                                .StandardCrewQuarters),
                        new GridPosition(0, 0))
                    .Module;

            ModuleWorkEfficiencyAssessment result =
                bastion.CalculateModuleWorkEfficiency(
                    crewQuarters.Id,
                    _profileCatalog);

            Assert.That(result.CanOperate, Is.True);

            Assert.That(
                result.OverallEfficiencyPercent,
                Is.EqualTo(100));
        }

        private ModuleWorkTypeAssessment
            CalculateMechanicalAssessment(
                BrigadeType brigadeType,
                int personnel,
                int experience = 0,
                int morale = 100,
                int fatigue = 0)
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                new Brigade(
                    number: 1,
                    type: brigadeType,
                    currentPersonnel: personnel,
                    maximumPersonnel: personnel,
                    experience: experience,
                    morale: morale,
                    fatigue: fatigue);

            AddDeployAndStartWork(
                bastion,
                repairBay,
                brigade);

            return bastion
                .CalculateModuleWorkEfficiency(
                    repairBay.Id,
                    _profileCatalog)
                .Get(WorkType.Mechanical);
        }

        private ModuleWorkEfficiencyAssessment
            CalculateRepairBayEfficiency(
                int personnel)
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateBrigade(
                    number: 1,
                    BrigadeType.Mechanic,
                    personnel);

            AddDeployAndStartWork(
                bastion,
                repairBay,
                brigade);

            return bastion.CalculateModuleWorkEfficiency(
                repairBay.Id,
                _profileCatalog);
        }

        private static Bastion CreateBastion()
        {
            return new Bastion(
                name: "work-efficiency-test",
                width: 8,
                deckCount: 3);
        }

        private ModuleInstance InstallRepairBay(
            Bastion bastion)
        {
            return bastion.TryInstallModule(
                    _moduleCatalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(0, 0))
                .Module;
        }

        private static Brigade CreateBrigade(
            int number,
            BrigadeType type,
            int personnel)
        {
            return new Brigade(
                number: number,
                type: type,
                currentPersonnel: personnel,
                maximumPersonnel: personnel);
        }

        private static void AddDeployAndStartWork(
            Bastion bastion,
            ModuleInstance module,
            Brigade brigade)
        {
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