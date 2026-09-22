using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanDraftTests
    {
        [Test]
        public void NewDraftStoresTurnIdentityAndPhaseCount()
        {
            var draft =
                new TurnPlanDraft(
                    turnNumber: 7,
                    actionPhaseCount: 3,
                    participants:
                        Array.Empty<
                            TurnBrigadeParticipant>());

            Assert.That(
                draft.TurnNumber,
                Is.EqualTo(7));

            Assert.That(
                draft.ActionPhaseCount,
                Is.EqualTo(3));
        }

        [Test]
        public void NewDraftAllowsEmptyParticipantSnapshot()
        {
            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        Array.Empty<
                            TurnBrigadeParticipant>());

            Assert.That(
                draft.ParticipantCount,
                Is.EqualTo(0));

            Assert.That(
                draft.Participants,
                Is.Empty);
        }

        [Test]
        public void DraftCopiesParticipantCollection()
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

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
                        source);

            source.Clear();

            Assert.That(
                draft.ParticipantCount,
                Is.EqualTo(1));

            Assert.That(
                draft.Participants[0],
                Is.SameAs(
                    participant));
        }

        [Test]
        public void DraftOrdersParticipantsByNumberThenId()
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

            var draft =
                new TurnPlanDraft(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    participants:
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
                        });

            CollectionAssert.AreEqual(
                new[]
                {
                    firstId,
                    secondId,
                    thirdId
                },
                draft.Participants
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray());
        }

        [Test]
        public void DraftRejectsDuplicateParticipantIds()
        {
            Guid duplicateId =
                Guid.NewGuid();

            Assert.Throws<
                ArgumentException>(
                () =>
                    new TurnPlanDraft(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        participants:
                            new[]
                            {
                                new TurnBrigadeParticipant(
                                    duplicateId,
                                    brigadeNumber: 1),

                                new TurnBrigadeParticipant(
                                    duplicateId,
                                    brigadeNumber: 2)
                            }));
        }

        [Test]
        public void DraftRejectsNullParticipantCollection()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new TurnPlanDraft(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        participants:
                            null));
        }

        [Test]
        public void DraftRejectsNullParticipant()
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
                    new TurnPlanDraft(
                        turnNumber: 1,
                        actionPhaseCount: 2,
                        participants:
                            participants));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DraftRejectsInvalidTurnNumber(
            int invalidTurnNumber)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new TurnPlanDraft(
                        turnNumber:
                            invalidTurnNumber,
                        actionPhaseCount: 2,
                        participants:
                            Array.Empty<
                                TurnBrigadeParticipant>()));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DraftRejectsInvalidActionPhaseCount(
            int invalidActionPhaseCount)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new TurnPlanDraft(
                        turnNumber: 1,
                        actionPhaseCount:
                            invalidActionPhaseCount,
                        participants:
                            Array.Empty<
                                TurnBrigadeParticipant>()));
        }
    }
}