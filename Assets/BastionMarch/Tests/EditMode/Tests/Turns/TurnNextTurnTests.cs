using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class TurnNextTurnTests
    {
        [Test]
        public void BeginNextTurnIncrementsTurnNumberAndStartsPlanning()
        {
            var cycle =
                CreateCompletedTurn(
                    turnNumber: 7,
                    actionPhaseCount: 2);

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(8));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.HasActiveActionPhase,
                Is.False);

            Assert.That(
                cycle.IsPlanningConfirmed,
                Is.False);
        }

        [Test]
        public void BeginNextTurnResultDescribesTurnBoundary()
        {
            var cycle =
                CreateCompletedTurn(
                    turnNumber: 17,
                    actionPhaseCount: 2);

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                result.PreviousTurnNumber,
                Is.EqualTo(17));

            Assert.That(
                result.CurrentTurnNumber,
                Is.EqualTo(18));

            Assert.That(
                result.TurnNumber,
                Is.EqualTo(18));

            Assert.That(
                result.PreviousStage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                result.CurrentStage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                result.PreviousActionPhase,
                Is.Null);

            Assert.That(
                result.CurrentActionPhase,
                Is.Null);
        }

        [Test]
        public void BeginNextTurnPreservesActionPhaseCount()
        {
            var cycle =
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 5);

            cycle.TryBeginNextTurn(
                Array.Empty<
                    TurnBrigadeParticipant>());

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(5));
        }

        [Test]
        public void BeginNextTurnReplacesParticipantSnapshot()
        {
            Guid oldId =
                Guid.NewGuid();

            Guid newId =
                Guid.NewGuid();

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        new[]
                        {
                            new TurnBrigadeParticipant(
                                oldId,
                                brigadeNumber: 1)
                        });

            CompleteCurrentTurn(
                cycle);

            IReadOnlyList<
                TurnBrigadeParticipant>
                    oldSnapshot =
                        cycle.ActiveBrigades;

            cycle.TryBeginNextTurn(
                new[]
                {
                    new TurnBrigadeParticipant(
                        newId,
                        brigadeNumber: 2)
                });

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0].BrigadeId,
                Is.EqualTo(newId));

            Assert.That(
                cycle.ActiveBrigades,
                Is.Not.SameAs(
                    oldSnapshot));
        }

        [Test]
        public void BeginNextTurnOrdersParticipantsByNumberThenId()
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

            var cycle =
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            cycle.TryBeginNextTurn(
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
                cycle.ActiveBrigades
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray());
        }

        [Test]
        public void BeginNextTurnCopiesParticipantCollection()
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
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            cycle.TryBeginNextTurn(
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
        public void BeginNextTurnAcceptsEmptyParticipantSnapshot()
        {
            var cycle =
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                cycle.ActiveBrigades,
                Is.Empty);
        }

        [Test]
        public void BeginNextTurnFailsBeforeTurnEnd()
        {
            var cycle =
                new TurnCycle();

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason
                        .TurnEndNotReached));

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(1));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));
        }

        [Test]
        public void BeginNextTurnFailsDuringActionResolution()
        {
            var cycle =
                new TurnCycle();

            cycle.TryConfirmPlanning();

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnTransitionFailureReason
                        .TurnEndNotReached));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));
        }

        [Test]
        public void FailedBeginNextTurnDoesNotReplaceParticipants()
        {
            Guid participantId =
                Guid.NewGuid();

            var cycle =
                new TurnCycle(
                    turnNumber: 4,
                    actionPhaseCount: 2,
                    activeBrigades:
                        new[]
                        {
                            new TurnBrigadeParticipant(
                                participantId,
                                brigadeNumber: 1)
                        });

            TurnTransitionResult result =
                cycle.TryBeginNextTurn(
                    new[]
                    {
                        new TurnBrigadeParticipant(
                            Guid.NewGuid(),
                            brigadeNumber: 2)
                    });

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(4));

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0].BrigadeId,
                Is.EqualTo(
                    participantId));
        }

        [Test]
        public void BeginNextTurnRejectsNullParticipantCollection()
        {
            var cycle =
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    cycle.TryBeginNextTurn(
                        null));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(1));
        }

        [Test]
        public void BeginNextTurnRejectsDuplicateParticipantIds()
        {
            Guid duplicateId =
                Guid.NewGuid();

            var cycle =
                CreateCompletedTurn(
                    turnNumber: 1,
                    actionPhaseCount: 2);

            Assert.Throws<
                ArgumentException>(
                () =>
                    cycle.TryBeginNextTurn(
                        new[]
                        {
                            new TurnBrigadeParticipant(
                                duplicateId,
                                brigadeNumber: 1),

                            new TurnBrigadeParticipant(
                                duplicateId,
                                brigadeNumber: 2)
                        }));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(1));
        }

        private static TurnCycle CreateCompletedTurn(
            int turnNumber,
            int actionPhaseCount)
        {
            var cycle =
                new TurnCycle(
                    turnNumber:
                        turnNumber,
                    actionPhaseCount:
                        actionPhaseCount);

            CompleteCurrentTurn(
                cycle);

            return cycle;
        }

        private static void CompleteCurrentTurn(
            TurnCycle cycle)
        {
            TurnTransitionResult confirm =
                cycle.TryConfirmPlanning();

            Assert.That(
                confirm.IsSuccess,
                Is.True);

            for (int phase = 0;
                 phase < cycle.ActionPhaseCount;
                 phase++)
            {
                TurnTransitionResult advance =
                    cycle.TryAdvanceActionPhase();

                Assert.That(
                    advance.IsSuccess,
                    Is.True);
            }

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));
        }
    }
}