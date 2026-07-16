using System;
using BastionMarch.Simulation.Crew;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class BrigadeWorkProfileTests
    {
        [Test]
        public void InitialCatalogContainsEveryBrigadeType()
        {
            BrigadeWorkProfileCatalog catalog =
                InitialBrigadeWorkProfiles.CreateCatalog();

            Assert.That(
                catalog.All.Count,
                Is.EqualTo(
                    Enum.GetValues(
                        typeof(BrigadeType)).Length));
        }

        [Test]
        public void MechanicHasFullMechanicalAffinity()
        {
            BrigadeWorkProfile profile =
                InitialBrigadeWorkProfiles
                    .CreateCatalog()
                    .GetRequired(
                        BrigadeType.Mechanic);

            Assert.That(
                profile.GetEfficiencyPercent(
                    WorkType.Mechanical),
                Is.EqualTo(100));
        }

        [Test]
        public void UnspecifiedWorkUsesDefaultAffinity()
        {
            BrigadeWorkProfile profile =
                InitialBrigadeWorkProfiles
                    .CreateCatalog()
                    .GetRequired(
                        BrigadeType.Medical);

            Assert.That(
                profile.GetEfficiencyPercent(
                    WorkType.Driving),
                Is.EqualTo(20));
        }
    }
}