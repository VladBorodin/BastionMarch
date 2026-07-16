using System;
using BastionMarch.Simulation.Crew;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Crew
{
    [TestFixture]
    public sealed class BrigadeTests
    {
        [Test]
        public void ConstructorCreatesBrigadeWithProvidedState()
        {
            Guid id = Guid.NewGuid();

            var brigade = new Brigade(
                id: id,
                number: 3,
                type: BrigadeType.Mechanic,
                currentPersonnel: 5,
                maximumPersonnel: 6,
                experience: 40,
                morale: 80,
                fatigue: 20,
                nickname: "Стальные руки");

            Assert.That(brigade.Id, Is.EqualTo(id));
            Assert.That(brigade.Number, Is.EqualTo(3));
            Assert.That(brigade.Type, Is.EqualTo(BrigadeType.Mechanic));

            Assert.That(brigade.CurrentPersonnel, Is.EqualTo(5));
            Assert.That(brigade.MaximumPersonnel, Is.EqualTo(6));
            Assert.That(brigade.VacantPersonnelSlots, Is.EqualTo(1));

            Assert.That(brigade.Experience, Is.EqualTo(40));
            Assert.That(brigade.PeakExperience, Is.EqualTo(40));
            Assert.That(brigade.Morale, Is.EqualTo(80));
            Assert.That(brigade.Fatigue, Is.EqualTo(20));

            Assert.That(brigade.Nickname, Is.EqualTo("Стальные руки"));
            Assert.That(brigade.IsDisbanded, Is.False);
        }

        [Test]
        public void ConstructorRejectsCapacityBelowMinimum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Brigade(
                    number: 1,
                    type: BrigadeType.Recruit,
                    currentPersonnel: 2,
                    maximumPersonnel: 2));
        }

        [Test]
        public void ConstructorRejectsCapacityAboveMaximum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Brigade(
                    number: 1,
                    type: BrigadeType.Assault,
                    currentPersonnel: 12,
                    maximumPersonnel: 13));
        }

        [Test]
        public void CasualtiesReduceCurrentPersonnel()
        {
            var brigade = new Brigade(
                number: 2,
                type: BrigadeType.Gunner,
                currentPersonnel: 6,
                maximumPersonnel: 6,
                experience: 50);

            int actualCasualties =
                brigade.ApplyCasualties(2);

            Assert.That(actualCasualties, Is.EqualTo(2));
            Assert.That(brigade.CurrentPersonnel, Is.EqualTo(4));
            Assert.That(brigade.Experience, Is.EqualTo(50));
            Assert.That(brigade.IsDisbanded, Is.False);
        }

        [Test]
        public void CasualtiesCannotReducePersonnelBelowZero()
        {
            var brigade = new Brigade(
                number: 4,
                type: BrigadeType.Signal,
                currentPersonnel: 3,
                maximumPersonnel: 4);

            int actualCasualties =
                brigade.ApplyCasualties(100);

            Assert.That(actualCasualties, Is.EqualTo(3));
            Assert.That(brigade.CurrentPersonnel, Is.Zero);
            Assert.That(brigade.IsDisbanded, Is.True);
        }

        [Test]
        public void DisbandedBrigadeCannotBeReinforced()
        {
            var brigade = new Brigade(
                number: 5,
                type: BrigadeType.Mechanic,
                currentPersonnel: 3,
                maximumPersonnel: 6);

            brigade.ApplyCasualties(3);

            Assert.Throws<InvalidOperationException>(
                () => brigade.Reinforce(3));
        }

        [Test]
        public void ReinforcementIsLimitedByAvailableCapacity()
        {
            var brigade = new Brigade(
                number: 6,
                type: BrigadeType.Logistics,
                currentPersonnel: 4,
                maximumPersonnel: 6,
                experience: 30);

            int added =
                brigade.Reinforce(
                    requestedPersonnel: 10,
                    reinforcementExperience: 10);

            Assert.That(added, Is.EqualTo(2));
            Assert.That(brigade.CurrentPersonnel, Is.EqualTo(6));
            Assert.That(brigade.VacantPersonnelSlots, Is.Zero);
        }

        [Test]
        public void RecruitReinforcementDilutesCurrentExperience()
        {
            var brigade = new Brigade(
                number: 7,
                type: BrigadeType.Mechanic,
                currentPersonnel: 3,
                maximumPersonnel: 6,
                experience: 90);

            brigade.Reinforce(
                requestedPersonnel: 3,
                reinforcementExperience: 0);

            Assert.That(brigade.CurrentPersonnel, Is.EqualTo(6));
            Assert.That(brigade.Experience, Is.EqualTo(45));
            Assert.That(brigade.PeakExperience, Is.EqualTo(90));
        }

        [Test]
        public void VeteranTraditionSurvivesRecruitReinforcement()
        {
            var brigade = new Brigade(
                number: 8,
                type: BrigadeType.Gunner,
                currentPersonnel: 3,
                maximumPersonnel: 6,
                experience: 90,
                nickname: "Громовержцы");

            brigade.Reinforce(
                requestedPersonnel: 3,
                reinforcementExperience: 0);

            Assert.That(brigade.Experience, Is.EqualTo(45));
            Assert.That(brigade.PeakExperience, Is.EqualTo(90));

            Assert.That(
                brigade.HasVeteranTradition,
                Is.True);

            Assert.That(
                brigade.Nickname,
                Is.EqualTo("Громовержцы"));
        }

        [Test]
        public void VeteranTraditionEndsWhenBrigadeIsDisbanded()
        {
            var brigade = new Brigade(
                number: 9,
                type: BrigadeType.Assault,
                currentPersonnel: 4,
                maximumPersonnel: 12,
                experience: 85);

            brigade.ApplyCasualties(4);

            Assert.That(brigade.IsDisbanded, Is.True);

            Assert.That(
                brigade.HasVeteranTradition,
                Is.False);
        }

        [Test]
        public void ExperienceGainCanCreateVeteranTradition()
        {
            var brigade = new Brigade(
                number: 10,
                type: BrigadeType.Recruit,
                currentPersonnel: 6,
                maximumPersonnel: 6,
                experience: 70);

            brigade.GainExperience(15);

            Assert.That(brigade.Experience, Is.EqualTo(85));
            Assert.That(brigade.PeakExperience, Is.EqualTo(85));

            Assert.That(
                brigade.HasVeteranTradition,
                Is.True);
        }

        [Test]
        public void MoraleAndFatigueAreClampedToValidRange()
        {
            var brigade = new Brigade(
                number: 11,
                type: BrigadeType.Officer,
                currentPersonnel: 3,
                maximumPersonnel: 3,
                morale: 90,
                fatigue: 10);

            brigade.ChangeMorale(50);
            brigade.ChangeFatigue(-50);

            Assert.That(brigade.Morale, Is.EqualTo(100));
            Assert.That(brigade.Fatigue, Is.Zero);

            brigade.ChangeMorale(-200);
            brigade.ChangeFatigue(200);

            Assert.That(brigade.Morale, Is.Zero);
            Assert.That(brigade.Fatigue, Is.EqualTo(100));
        }

        [Test]
        public void NicknameIsTrimmed()
        {
            var brigade = new Brigade(
                number: 12,
                type: BrigadeType.Medical,
                currentPersonnel: 4,
                maximumPersonnel: 6);

            brigade.SetNickname("  Белые каски  ");

            Assert.That(
                brigade.Nickname,
                Is.EqualTo("Белые каски"));
        }
    }
}