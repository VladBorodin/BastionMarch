
using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns.Activities;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Activities
{
    [TestFixture]
    public sealed class ModuleWorkDefaultPolicyTests
    {
        private Bastion _bastion;
        private Brigade _brigade;
        private BrigadeWorkProfileCatalog _profiles;

        private ModuleInstance _machine;
        private ModuleInstance _repairBay;
        private ModuleInstance _quarters;
        private ModuleInstance _ammoStorage;

        [SetUp]
        public void SetUp()
        {
            ModuleDefinitionCatalog definitions =
                InitialModuleDefinitions.CreateCatalog();

            _profiles = InitialBrigadeWorkProfiles.CreateCatalog();
            _bastion = new Bastion("default-work-test", 10, 1);

            _machine = Install(
                definitions.GetRequired(ModuleDefinitionIds.SmallMachineRoom),
                0);

            _repairBay = Install(
                definitions.GetRequired(ModuleDefinitionIds.StandardRepairBay),
                2);

            _quarters = Install(
                definitions.GetRequired(ModuleDefinitionIds.StandardCrewQuarters),
                4);

            _ammoStorage = Install(
                definitions.GetRequired(ModuleDefinitionIds.StandardAmmoStorage),
                6);

            _brigade = CreateBrigade(1, BrigadeType.Mechanic, _machine);
        }

        [Test]
        public void MechanicGetsMechanicalDefaultInMachineRoom()
        {
            ModuleWorkDefaultAssessment result = Assess();

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.BrigadeId, Is.EqualTo(_brigade.Id));
            Assert.That(result.ModuleId, Is.EqualTo(_machine.Id));
            Assert.That(result.WorkType, Is.EqualTo(WorkType.Mechanical));
        }

        [Test]
        public void AssessmentDoesNotStartWork()
        {
            Assert.That(Assess().IsAllowed, Is.True);

            Assert.That(
                _bastion.TryGetBrigadeOperationalState(
                    _brigade.Id, out BrigadeOperationalState state),
                Is.True);

            Assert.That(state.IsWorking, Is.False);
            Assert.That(_machine.WorkingBrigadeIds, Is.Empty);
            Assert.That(
                _machine.OccupyingBrigadeIds,
                Has.Member(_brigade.Id));
        }

        [Test]
        public void AlreadyWorkingBrigadeDoesNotNeedDefault()
        {
            Assert.That(
                _bastion.TryStartBrigadeWork(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(
                Assess().FailureReason,
                Is.EqualTo(
                    ModuleWorkDefaultFailureReason.BrigadeAlreadyWorking));
        }

        [Test]
        public void CrewQuartersHaveNoDefaultWork()
        {
            ReassignForSetup(_brigade, _quarters);

            Assert.That(
                Assess().FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.NoMatchingWork));
        }

        [Test]
        public void NonPrimarySpecialistIsNotAssignedAutomatically()
        {
            Brigade engineer =
                CreateBrigade(2, BrigadeType.Engineer, _machine);

            Assert.That(
                Assess(engineer.Id).FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.NoMatchingWork));
        }

        [Test]
        public void MechanicGetsUniqueMechanicalWorkInRepairBay()
        {
            ReassignForSetup(_brigade, _repairBay);

            ModuleWorkDefaultAssessment result = Assess();

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.ModuleId, Is.EqualTo(_repairBay.Id));
            Assert.That(result.WorkType, Is.EqualTo(WorkType.Mechanical));
        }

        [Test]
        public void TwoEquallyPrimaryWorkTypesAreAmbiguous()
        {
            ReassignForSetup(_brigade, _repairBay);

            var customProfile = new BrigadeWorkProfile(
                BrigadeType.Mechanic,
                defaultEfficiencyPercent: 25,
                new[]
                {
                    new WorkAffinityDefinition(WorkType.Mechanical, 100),
                    new WorkAffinityDefinition(WorkType.General, 100)
                });

            var customCatalog = new BrigadeWorkProfileCatalog(
                new[] { customProfile });

            ModuleWorkDefaultAssessment result =
                ModuleWorkDefaultPolicy.Assess(
                    _bastion, _brigade.Id, customCatalog);

            Assert.That(result.FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.AmbiguousWork));
        }

        [Test]
        public void LogisticBrigadeGetsLogisticsInAmmoStorage()
        {
            Brigade logistics =
                CreateBrigade(2, BrigadeType.Logistics, _ammoStorage);

            ModuleWorkDefaultAssessment result = Assess(logistics.Id);

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.ModuleId, Is.EqualTo(_ammoStorage.Id));
            Assert.That(result.WorkType, Is.EqualTo(WorkType.Logistics));
        }

        [Test]
        public void DisbandedBrigadeHasNoDefault()
        {
            _brigade.ApplyCasualties(_brigade.CurrentPersonnel);

            Assert.That(
                Assess().FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.BrigadeDisbanded));
        }

        [Test]
        public void UndeployedBrigadeHasNoDefault()
        {
            Assert.That(
                _bastion.TryUndeployBrigade(_brigade.Id).IsSuccess,
                Is.True);

            Assert.That(
                Assess().FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.BrigadeNotDeployed));
        }

        [Test]
        public void UnknownBrigadeHasNoDefault()
        {
            Assert.That(
                Assess(Guid.NewGuid()).FailureReason,
                Is.EqualTo(ModuleWorkDefaultFailureReason.BrigadeNotFound));
        }

        [Test]
        public void InvalidArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ModuleWorkDefaultPolicy.Assess(null, _brigade.Id, _profiles));

            Assert.Throws<ArgumentNullException>(() =>
                ModuleWorkDefaultPolicy.Assess(_bastion, _brigade.Id, null));

            Assert.Throws<ArgumentException>(() =>
                ModuleWorkDefaultPolicy.Assess(
                    _bastion, Guid.Empty, _profiles));
        }

        private ModuleWorkDefaultAssessment Assess(Guid? brigadeId = null)
        {
            return ModuleWorkDefaultPolicy.Assess(
                _bastion,
                brigadeId ?? _brigade.Id,
                _profiles);
        }

        private ModuleInstance Install(ModuleDefinition definition, int x)
        {
            ModulePlacementResult result = _bastion.TryInstallModule(
                definition, new GridPosition(x, 0));

            Assert.That(result.IsSuccess, Is.True);
            return result.Module;
        }

        private Brigade CreateBrigade(
            int number,
            BrigadeType type,
            ModuleInstance module)
        {
            var brigade = new Brigade(
                number,
                type,
                currentPersonnel: 6,
                maximumPersonnel: 6);

            Assert.That(_bastion.TryAddBrigade(brigade), Is.True);
            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    brigade.Id, module.Id).IsSuccess,
                Is.True);

            return brigade;
        }

        private void ReassignForSetup(Brigade brigade, ModuleInstance target)
        {
            Assert.That(
                _bastion.TryUndeployBrigade(brigade.Id).IsSuccess,
                Is.True);

            Assert.That(
                _bastion.TryDeployBrigadeToModule(
                    brigade.Id, target.Id).IsSuccess,
                Is.True);
        }
    }
}
