using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class BastionCrewAssignmentTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void BrigadeCanBeAddedAndRetrieved()
        {
            var bastion = CreateBastion();

            Brigade brigade =
                CreateBrigade(
                    number: 1,
                    type: BrigadeType.Mechanic,
                    personnel: 6,
                    capacity: 6);

            bool added =
                bastion.TryAddBrigade(brigade);

            bool found =
                bastion.TryGetBrigade(
                    brigade.Id,
                    out Brigade storedBrigade);

            Assert.That(added, Is.True);
            Assert.That(found, Is.True);
            Assert.That(storedBrigade, Is.SameAs(brigade));
            Assert.That(bastion.BrigadeCount, Is.EqualTo(1));
        }

        [Test]
        public void SameBrigadeCannotBeAddedTwice()
        {
            var bastion = CreateBastion();

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bool first =
                bastion.TryAddBrigade(brigade);

            bool second =
                bastion.TryAddBrigade(brigade);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(bastion.BrigadeCount, Is.EqualTo(1));
        }

        [Test]
        public void DisbandedBrigadeCannotBeAdded()
        {
            var bastion = CreateBastion();

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Assault,
                    3,
                    12);

            brigade.ApplyCasualties(3);

            Assert.That(
                bastion.TryAddBrigade(brigade),
                Is.False);
        }

        [Test]
        public void BrigadeCanBeAssignedToModule()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bastion.TryAddBrigade(brigade);

            BrigadeOperationalResult result =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    repairBay.Id);

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(
                repairBay.OccupyingBrigadeIds,
                Does.Contain(brigade.Id));

            Assert.That(
                bastion.TryGetBrigadeLocation(
                    brigade.Id,
                    out ModuleInstance assignedModule),
                Is.True);

            Assert.That(
                assignedModule,
                Is.SameAs(repairBay));
        }

        [Test]
        public void BrigadeCannotOccupyTwoModulesAtOnce()
        {
            var bastion = CreateBastion();

            ModuleInstance first =
                InstallRepairBay(bastion, x: 0);

            ModuleInstance second =
                InstallRepairBay(bastion, x: 2);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                first.Id);

            BrigadeOperationalResult secondAssignment =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    second.Id);

            Assert.That(secondAssignment.IsSuccess, Is.False);

            Assert.That(
                secondAssignment.FailureReason,
                Is.EqualTo(
                    BrigadeOperationalFailureReason
                        .BrigadeAlreadyDeployed));
        }

        [Test]
        public void MultipleBrigadesCanOccupySameModule()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade first =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    3,
                    6);

            Brigade second =
                CreateBrigade(
                    2,
                    BrigadeType.Recruit,
                    6,
                    6);

            bastion.TryAddBrigade(first);
            bastion.TryAddBrigade(second);

            bastion.TryDeployBrigadeToModule(
                first.Id,
                repairBay.Id);

            bastion.TryDeployBrigadeToModule(
                second.Id,
                repairBay.Id);

            Assert.That(
                repairBay.OccupyingBrigadeIds.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void UndeployedBrigadeCanBeDeployedToAnotherModule()
        {
            var bastion = CreateBastion();

            ModuleInstance first =
                InstallRepairBay(bastion, x: 0);

            ModuleInstance second =
                InstallRepairBay(bastion, x: 2);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bastion.TryAddBrigade(brigade);

            BrigadeOperationalResult firstDeployment =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    first.Id);

            BrigadeOperationalResult undeployment =
                bastion.TryUndeployBrigade(
                    brigade.Id);

            BrigadeOperationalResult secondDeployment =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    second.Id);

            Assert.That(
                firstDeployment.IsSuccess,
                Is.True);

            Assert.That(
                undeployment.IsSuccess,
                Is.True);

            Assert.That(
                undeployment.Module,
                Is.SameAs(first));

            Assert.That(
                secondDeployment.IsSuccess,
                Is.True);

            Assert.That(
                bastion.TryGetBrigadeLocation(
                    brigade.Id,
                    out ModuleInstance currentModule),
                Is.True);

            Assert.That(
                currentModule,
                Is.SameAs(second));
        }

        [Test]
        public void RemovingModuleUnassignsItsBrigades()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            bastion.TryRemoveModule(
                repairBay.Id,
                out _);

            Assert.That(
                bastion.TryGetBrigadeLocation(
                    brigade.Id,
                    out _),
                Is.False);

            Assert.That(
                bastion.TryGetBrigade(
                    brigade.Id,
                    out _),
                Is.True);
        }

        [Test]
        public void RemovingBrigadeClearsModuleAssignment()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            bastion.TryRemoveBrigade(
                brigade.Id,
                out Brigade removedBrigade);

            Assert.That(removedBrigade, Is.SameAs(brigade));
            Assert.That(repairBay.OccupyingBrigadeIds, Is.Empty);
            Assert.That(bastion.BrigadeCount, Is.Zero);
        }

        [Test]
        public void DisbandedEnrolledBrigadeCannotBeAssigned()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade brigade =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    3,
                    6);

            bastion.TryAddBrigade(brigade);

            brigade.ApplyCasualties(3);

            BrigadeOperationalResult result =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    repairBay.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    BrigadeOperationalFailureReason
                        .BrigadeDisbanded));
        }

        [Test]
        public void StaffingAssessmentUsesPersonnelWeightedExperience()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade veterans =
                CreateBrigade(
                    number: 1,
                    type: BrigadeType.Mechanic,
                    personnel: 3,
                    capacity: 6,
                    experience: 90);

            Brigade recruits =
                CreateBrigade(
                    number: 2,
                    type: BrigadeType.Recruit,
                    personnel: 6,
                    capacity: 6,
                    experience: 0);

            bastion.TryAddBrigade(veterans);
            bastion.TryAddBrigade(recruits);

            DeployAndStartWork(
                bastion,
                repairBay,
                veterans);

            DeployAndStartWork(
                bastion,
                repairBay,
                recruits);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.WorkingBrigadeCount,
                Is.EqualTo(2));

            Assert.That(
                assessment.TotalWorkingPersonnel,
                Is.EqualTo(9));

            Assert.That(
                assessment.AverageExperience,
                Is.EqualTo(30));

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.AboveOptimal));
        }

        [Test]
        public void StaffingBelowMinimumIsReported()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade damagedBrigade =
                CreateBrigade(
                    number: 1,
                    type: BrigadeType.Mechanic,
                    personnel: 2,
                    capacity: 6);

            bastion.TryAddBrigade(damagedBrigade);

            DeployAndStartWork(
                bastion,
                repairBay,
                damagedBrigade);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.BelowMinimum));

            Assert.That(
                assessment.IsMinimumMet,
                Is.False);
        }

        [Test]
        public void StaffingAboveMaximumIsOvercrowded()
        {
            var bastion = CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion, x: 0);

            Brigade first =
                CreateBrigade(
                    1,
                    BrigadeType.Mechanic,
                    6,
                    6);

            Brigade second =
                CreateBrigade(
                    2,
                    BrigadeType.Recruit,
                    6,
                    6);

            bastion.TryAddBrigade(first);
            bastion.TryAddBrigade(second);

            DeployAndStartWork(
                bastion,
                repairBay,
                first);

            DeployAndStartWork(
                bastion,
                repairBay,
                second);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.TotalWorkingPersonnel,
                Is.EqualTo(12));

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.AboveOptimal));

            Assert.That(
                assessment.IsOvercrowded,
                Is.True);
        }

        private static Bastion CreateBastion()
        {
            return new Bastion(
                name: "Испытательный бастион",
                width: 12,
                deckCount: 3);
        }

        private ModuleInstance InstallRepairBay(
            Bastion bastion,
            int x)
        {
            return bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds.StandardRepairBay),
                    new GridPosition(x, 0))
                .Module;
        }

        private static Brigade CreateBrigade(
            int number,
            BrigadeType type,
            int personnel,
            int capacity,
            int experience = 0)
        {
            return new Brigade(
                number: number,
                type: type,
                currentPersonnel: personnel,
                maximumPersonnel: capacity,
                experience: experience);
        }

        private static void DeployAndStartWork(
            Bastion bastion,
            ModuleInstance module,
            Brigade brigade)
        {
            BrigadeOperationalResult deployment =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    module.Id);

            Assert.That(
                deployment.IsSuccess,
                Is.True);

            BrigadeOperationalResult work =
                bastion.TryStartBrigadeWork(
                    brigade.Id);

            Assert.That(
                work.IsSuccess,
                Is.True);
        }
    }
}