using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class TurnBrigadeParticipantTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void CycleWithoutParticipantsHasEmptySnapshot()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(0));

            Assert.That(
                cycle.ActiveBrigades,
                Is.Empty);
        }

        [Test]
        public void CycleOrdersParticipantsByNumberThenId()
        {
            Guid firstId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            Guid thirdId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000003");

            var participants =
                new[]
                {
                    new TurnBrigadeParticipant(
                        thirdId,
                        brigadeNumber: 3),

                    new TurnBrigadeParticipant(
                        secondId,
                        brigadeNumber: 2),

                    new TurnBrigadeParticipant(
                        firstId,
                        brigadeNumber: 2)
                };

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        participants);

            CollectionAssert.AreEqual(
                new[]
                {
                    firstId,
                    secondId,
                    thirdId
                },
                cycle.ActiveBrigades
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray());
        }

        [Test]
        public void CycleCopiesParticipantCollection()
        {
            var participant =
                new TurnBrigadeParticipant(
                    Guid.NewGuid(),
                    brigadeNumber: 1);

            var source =
                new List<
                    TurnBrigadeParticipant>
                {
                    participant
                };

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        source);

            source.Clear();

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0],
                Is.SameAs(
                    participant));
        }

        [Test]
        public void CycleRejectsDuplicateParticipantIds()
        {
            Guid duplicateId =
                Guid.NewGuid();

            var participants =
                new[]
                {
                    new TurnBrigadeParticipant(
                        duplicateId,
                        brigadeNumber: 1),

                    new TurnBrigadeParticipant(
                        duplicateId,
                        brigadeNumber: 2)
                };

            Assert.Throws<
                ArgumentException>(
                () =>
                    new TurnCycle(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        activeBrigades:
                            participants));
        }

        [Test]
        public void FactoryCapturesOnlyDeployedNonDisbandedBrigades()
        {
            var bastion =
                new Bastion(
                    name:
                        "turn-active-brigades",
                    width: 4,
                    deckCount: 1);

            ModuleInstance module =
                InstallSmallModule(
                    bastion,
                    x: 0);

            var deployed =
                CreateBrigade(
                    number: 1,
                    personnel: 4);

            var undeployed =
                CreateBrigade(
                    number: 2,
                    personnel: 4);

            var disbandedAfterDeployment =
                CreateBrigade(
                    number: 3,
                    personnel: 4);

            Assert.That(
                bastion.TryAddBrigade(
                    deployed),
                Is.True);

            Assert.That(
                bastion.TryAddBrigade(
                    undeployed),
                Is.True);

            Assert.That(
                bastion.TryAddBrigade(
                    disbandedAfterDeployment),
                Is.True);

            Assert.That(
                bastion.TryDeployBrigadeToModule(
                        deployed.Id,
                        module.Id)
                    .IsSuccess,
                Is.True);

            Assert.That(
                bastion.TryDeployBrigadeToModule(
                        disbandedAfterDeployment.Id,
                        module.Id)
                    .IsSuccess,
                Is.True);

            disbandedAfterDeployment
                .ApplyCasualties(
                    disbandedAfterDeployment
                        .CurrentPersonnel);

            Assert.That(
                disbandedAfterDeployment
                    .IsDisbanded,
                Is.True);

            IReadOnlyList<
                TurnBrigadeParticipant>
                    participants =
                        TurnBrigadeParticipantFactory
                            .CaptureActive(
                                bastion);

            Assert.That(
                participants.Count,
                Is.EqualTo(1));

            Assert.That(
                participants[0].BrigadeId,
                Is.EqualTo(
                    deployed.Id));
        }

        [Test]
        public void FactoryUsesDeterministicNumberThenIdOrder()
        {
            var bastion =
                new Bastion(
                    name:
                        "turn-brigade-order",
                    width: 4,
                    deckCount: 1);

            ModuleInstance module =
                InstallSmallModule(
                    bastion,
                    x: 0);

            Guid firstId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            Guid thirdId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000003");

            Brigade laterNumber =
                CreateBrigade(
                    thirdId,
                    number: 3,
                    personnel: 4);

            Brigade sameNumberSecond =
                CreateBrigade(
                    secondId,
                    number: 2,
                    personnel: 4);

            Brigade sameNumberFirst =
                CreateBrigade(
                    firstId,
                    number: 2,
                    personnel: 4);

            AddAndDeploy(
                bastion,
                module,
                laterNumber);

            AddAndDeploy(
                bastion,
                module,
                sameNumberSecond);

            AddAndDeploy(
                bastion,
                module,
                sameNumberFirst);

            IReadOnlyList<
                TurnBrigadeParticipant>
                    participants =
                        TurnBrigadeParticipantFactory
                            .CaptureActive(
                                bastion);

            CollectionAssert.AreEqual(
                new[]
                {
                    firstId,
                    secondId,
                    thirdId
                },
                participants
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray());
        }

        private ModuleInstance InstallSmallModule(
            Bastion bastion,
            int x)
        {
            ModulePlacementResult result =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom),
                    new GridPosition(
                        x,
                        0));

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Module;
        }

        private static Brigade CreateBrigade(
            int number,
            int personnel)
        {
            return new Brigade(
                number:
                    number,
                type:
                    BrigadeType.Recruit,
                currentPersonnel:
                    personnel,
                maximumPersonnel:
                    personnel);
        }

        private static Brigade CreateBrigade(
            Guid id,
            int number,
            int personnel)
        {
            return new Brigade(
                id:
                    id,
                number:
                    number,
                type:
                    BrigadeType.Recruit,
                currentPersonnel:
                    personnel,
                maximumPersonnel:
                    personnel);
        }

        private static void AddAndDeploy(
            Bastion bastion,
            ModuleInstance module,
            Brigade brigade)
        {
            Assert.That(
                bastion.TryAddBrigade(
                    brigade),
                Is.True);

            Assert.That(
                bastion.TryDeployBrigadeToModule(
                        brigade.Id,
                        module.Id)
                    .IsSuccess,
                Is.True);
        }

        [Test]
        public void CycleRejectsNullParticipantCollection()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new TurnCycle(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        activeBrigades: null));
        }

        [Test]
        public void CycleRejectsNullParticipantItem()
        {
            var participants =
                new TurnBrigadeParticipant[]
                {
                    new TurnBrigadeParticipant(
                        Guid.NewGuid(),
                        brigadeNumber: 1),
                    null
                };

            Assert.Throws<
                ArgumentException>(
                () =>
                    new TurnCycle(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        activeBrigades:
                            participants));
        }
    }
}