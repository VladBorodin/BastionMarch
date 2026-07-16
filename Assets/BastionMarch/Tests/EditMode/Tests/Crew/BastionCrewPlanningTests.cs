using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class BastionCrewPlanningTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void RequirementsAreAggregatedByWorkType()
        {
            var bastion = CreateBastion();

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.SmallMachineRoom),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardRepairBay),
                new GridPosition(1, 0));

            BastionCrewRequirements requirements =
                bastion.CalculateCrewRequirements();

            WorkRequirementSummary mechanical =
                requirements.Get(WorkType.Mechanical);

            WorkRequirementSummary general =
                requirements.Get(WorkType.General);

            Assert.That(
                mechanical.MinimumPersonnel,
                Is.EqualTo(4));

            Assert.That(
                mechanical.OptimalPersonnel,
                Is.EqualTo(7));

            Assert.That(
                mechanical.MaximumUsefulPersonnel,
                Is.EqualTo(10));

            Assert.That(
                general.MinimumPersonnel,
                Is.EqualTo(1));

            Assert.That(
                general.OptimalPersonnel,
                Is.EqualTo(2));

            Assert.That(
                general.MaximumUsefulPersonnel,
                Is.EqualTo(3));

            Assert.That(
                requirements.MinimumPersonnel,
                Is.EqualTo(5));

            Assert.That(
                requirements.OptimalPersonnel,
                Is.EqualTo(9));

            Assert.That(
                requirements.MaximumUsefulPersonnel,
                Is.EqualTo(13));
        }

        [Test]
        public void CrewQuartersProvideBerthsAndAccommodation()
        {
            var bastion = CreateBastion();

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardCrewQuarters),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardVentilation),
                new GridPosition(2, 0));

            BastionCrewCapacity capacity =
                bastion.CalculateCrewCapacity();

            Assert.That(capacity.TotalBerths, Is.EqualTo(8));

            Assert.That(
                capacity.NominalAccommodationCapacity,
                Is.EqualTo(12));

            Assert.That(
                capacity.EmergencyAccommodationCapacity,
                Is.EqualTo(18));

            Assert.That(
                capacity.VentilationPersonnelCapacity,
                Is.EqualTo(24));

            Assert.That(
                capacity.NominalSupportedPersonnel,
                Is.EqualTo(12));

            Assert.That(
                capacity.EmergencySupportedPersonnel,
                Is.EqualTo(18));
        }

        [Test]
        public void VentilationCanLimitMultipleCrewQuarters()
        {
            var bastion = CreateBastion();

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardCrewQuarters),
                new GridPosition(0, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardCrewQuarters),
                new GridPosition(2, 0));

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardVentilation),
                new GridPosition(4, 0));

            BastionCrewCapacity capacity =
                bastion.CalculateCrewCapacity();

            Assert.That(
                capacity.NominalAccommodationCapacity,
                Is.EqualTo(24));

            Assert.That(
                capacity.EmergencyAccommodationCapacity,
                Is.EqualTo(36));

            Assert.That(
                capacity.VentilationPersonnelCapacity,
                Is.EqualTo(24));

            Assert.That(
                capacity.NominalSupportedPersonnel,
                Is.EqualTo(24));

            Assert.That(
                capacity.EmergencySupportedPersonnel,
                Is.EqualTo(24));
        }

        [Test]
        public void AccommodationWithoutVentilationSupportsNoCrew()
        {
            var bastion = CreateBastion();

            bastion.TryInstallModule(
                _catalog.GetRequired(
                    ModuleDefinitionIds.StandardCrewQuarters),
                new GridPosition(0, 0));

            BastionCrewCapacity capacity =
                bastion.CalculateCrewCapacity();

            Assert.That(
                capacity.NominalAccommodationCapacity,
                Is.EqualTo(12));

            Assert.That(
                capacity.VentilationPersonnelCapacity,
                Is.Zero);

            Assert.That(
                capacity.NominalSupportedPersonnel,
                Is.Zero);
        }

        [Test]
        public void RosterSummaryCountsPeopleByBrigadeType()
        {
            var bastion = CreateBastion();

            bastion.TryAddBrigade(
                new Brigade(
                    number: 1,
                    type: BrigadeType.Mechanic,
                    currentPersonnel: 6,
                    maximumPersonnel: 6));

            bastion.TryAddBrigade(
                new Brigade(
                    number: 2,
                    type: BrigadeType.Mechanic,
                    currentPersonnel: 4,
                    maximumPersonnel: 6));

            bastion.TryAddBrigade(
                new Brigade(
                    number: 1,
                    type: BrigadeType.Recruit,
                    currentPersonnel: 8,
                    maximumPersonnel: 8));

            BastionCrewRosterSummary summary =
                bastion.CalculateCrewRosterSummary();

            Assert.That(
                summary.ActiveBrigadeCount,
                Is.EqualTo(3));

            Assert.That(
                summary.TotalPersonnel,
                Is.EqualTo(18));

            BrigadeTypePersonnelSummary mechanics =
                System.Linq.Enumerable.Single(
                    summary.ByType,
                    item =>
                        item.BrigadeType ==
                        BrigadeType.Mechanic);

            Assert.That(
                mechanics.BrigadeCount,
                Is.EqualTo(2));

            Assert.That(
                mechanics.Personnel,
                Is.EqualTo(10));
        }

        private static Bastion CreateBastion()
        {
            return new Bastion(
                name: "Экипажный образец",
                width: 12,
                deckCount: 3);
        }
    }
}