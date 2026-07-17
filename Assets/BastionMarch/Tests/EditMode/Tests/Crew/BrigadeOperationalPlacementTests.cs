using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class BrigadeOperationalPlacementTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void DeploymentDoesNotAutomaticallyStartWork()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(
                brigade);

            BrigadeOperationalResult result =
                bastion.TryDeployBrigadeToModule(
                    brigade.Id,
                    repairBay.Id);

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(
                repairBay.OccupyingBrigadeIds,
                Does.Contain(brigade.Id));

            CollectionAssert.DoesNotContain(
                repairBay.WorkingBrigadeIds,
                brigade.Id);

            bastion.TryGetBrigadeOperationalState(
                brigade.Id,
                out BrigadeOperationalState state);

            Assert.That(state.IsDeployed, Is.True);
            Assert.That(state.IsWorking, Is.False);
        }

        [Test]
        public void DeployedBrigadeCanStartWork()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            BrigadeOperationalResult result =
                bastion.TryStartBrigadeWork(
                    brigade.Id);

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(
                repairBay.WorkingBrigadeIds,
                Does.Contain(brigade.Id));
        }

        [Test]
        public void UndeployedBrigadeCannotStartWork()
        {
            Bastion bastion =
                CreateBastion();

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(
                brigade);

            BrigadeOperationalResult result =
                bastion.TryStartBrigadeWork(
                    brigade.Id);

            Assert.That(result.IsSuccess, Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    BrigadeOperationalFailureReason
                        .BrigadeNotDeployed));
        }

        [Test]
        public void UndeployingBrigadeAlsoStopsWork()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            bastion.TryStartBrigadeWork(
                brigade.Id);

            bastion.TryUndeployBrigade(
                brigade.Id);

            Assert.That(
                repairBay.OccupyingBrigadeIds,
                Is.Empty);

            Assert.That(
                repairBay.WorkingBrigadeIds,
                Is.Empty);

            bastion.TryGetBrigadeOperationalState(
                brigade.Id,
                out BrigadeOperationalState state);

            Assert.That(state.IsDeployed, Is.False);
            Assert.That(state.IsWorking, Is.False);
        }

        [Test]
        public void OccupyingButIdleBrigadeDoesNotStaffModule()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.TotalOccupyingPersonnel,
                Is.EqualTo(6));

            Assert.That(
                assessment.TotalWorkingPersonnel,
                Is.Zero);

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.Unstaffed));
        }

        [Test]
        public void StartingWorkMakesPersonnelCountForStaffing()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            bastion.TryStartBrigadeWork(
                brigade.Id);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.TotalWorkingPersonnel,
                Is.EqualTo(6));

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.Optimal));
        }

        [Test]
        public void IdleOccupantsCanCauseOvercrowding()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade workers =
                CreateMechanics(
                    number: 1,
                    personnel: 6);

            Brigade idleOccupants =
                CreateMechanics(
                    number: 2,
                    personnel: 6);

            bastion.TryAddBrigade(workers);
            bastion.TryAddBrigade(idleOccupants);

            bastion.TryDeployBrigadeToModule(
                workers.Id,
                repairBay.Id);

            bastion.TryDeployBrigadeToModule(
                idleOccupants.Id,
                repairBay.Id);

            bastion.TryStartBrigadeWork(
                workers.Id);

            ModuleStaffingAssessment assessment =
                bastion.CalculateModuleStaffing(
                    repairBay.Id);

            Assert.That(
                assessment.TotalOccupyingPersonnel,
                Is.EqualTo(12));

            Assert.That(
                assessment.TotalWorkingPersonnel,
                Is.EqualTo(6));

            Assert.That(
                assessment.IsOvercrowded,
                Is.True);

            Assert.That(
                assessment.State,
                Is.EqualTo(
                    ModuleStaffingState.Optimal));
        }

        [Test]
        public void RemovingModuleClearsPresenceAndWork()
        {
            Bastion bastion =
                CreateBastion();

            ModuleInstance repairBay =
                InstallRepairBay(bastion);

            Brigade brigade =
                CreateMechanics(
                    personnel: 6);

            bastion.TryAddBrigade(brigade);

            bastion.TryDeployBrigadeToModule(
                brigade.Id,
                repairBay.Id);

            bastion.TryStartBrigadeWork(
                brigade.Id);

            bastion.TryRemoveModule(
                repairBay.Id,
                out _);

            bastion.TryGetBrigadeOperationalState(
                brigade.Id,
                out BrigadeOperationalState state);

            Assert.That(state.IsDeployed, Is.False);
            Assert.That(state.IsWorking, Is.False);
        }

        private static Bastion CreateBastion()
        {
            return new Bastion(
                name: "brigade-placement-test",
                width: 8,
                deckCount: 3);
        }

        private ModuleInstance InstallRepairBay(
            Bastion bastion)
        {
            return bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .StandardRepairBay),
                    new GridPosition(0, 0))
                .Module;
        }

        private static Brigade CreateMechanics(
            int personnel,
            int number = 1)
        {
            return new Brigade(
                number: number,
                type: BrigadeType.Mechanic,
                currentPersonnel: personnel,
                maximumPersonnel: personnel,
                experience: 50);
        }
    }
}