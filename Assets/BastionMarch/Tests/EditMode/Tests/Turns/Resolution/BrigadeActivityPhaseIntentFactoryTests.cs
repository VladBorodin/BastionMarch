
using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class BrigadeActivityPhaseIntentFactoryTests
    {
        [Test]
        public void FreeBrigadeCreatesActivityIntent()
        {
            var brigade = CreateParticipant();
            var activity = new TestActivity(brigade.BrigadeId);
            var plan = CreatePlan(brigade);

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => activity);

            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Activity, Is.SameAs(activity));
            Assert.That(intents[0].BrigadeId, Is.EqualTo(brigade.BrigadeId));
            Assert.That(intents[0].ActionPhase, Is.EqualTo(1));
        }

        [Test]
        public void MissingActivityLeavesBrigadeInStandby()
        {
            var plan = CreatePlan(CreateParticipant());

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => null);

            Assert.That(intents, Is.Empty);
        }

        [Test]
        public void ReservedBrigadeDoesNotQueryActivity()
        {
            var brigade = CreateParticipant();
            var plan = CreateReservedPlan(1, brigade);

            int resolverCalls = 0;

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan,
                1,
                id =>
                {
                    resolverCalls++;
                    return new TestActivity(id);
                });

            Assert.That(intents, Is.Empty);
            Assert.That(resolverCalls, Is.EqualTo(0));
        }

        [Test]
        public void FreeBrigadeGetsActivityWhileAnotherHasOrder()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);

            var plan = CreateReservedPlan(1, first, second);

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => new TestActivity(id));

            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].BrigadeId,
                Is.EqualTo(second.BrigadeId));
        }

        [Test]
        public void ReservationInPreviousPhaseDoesNotBlockActivity()
        {
            var brigade = CreateParticipant();
            var plan = CreateReservedPlan(1, brigade);

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 2, id => new TestActivity(id));

            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].ActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void BastionOrderMayReserveSeveralBrigades()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);

            var draft = CreateDraft(first, second);
            var order = CreateOrder();

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(order.OrderId, 1, first.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(order.OrderId, 1, second.BrigadeId));

            var plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            int calls = 0;

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan,
                1,
                id =>
                {
                    calls++;
                    return new TestActivity(id);
                });

            Assert.That(intents, Is.Empty);
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void IntentsUseParticipantSnapshotOrder()
        {
            var third = CreateParticipant(3);
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);

            var plan = CreatePlan(third, first, second);

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => new TestActivity(id));

            CollectionAssert.AreEqual(
                new[]
                {
                    first.BrigadeId,
                    second.BrigadeId,
                    third.BrigadeId
                },
                intents.Select(intent => intent.BrigadeId).ToArray());
        }

        [Test]
        public void FactoryRejectsActivityForDifferentBrigade()
        {
            var brigade = CreateParticipant();
            var plan = CreatePlan(brigade);

            Assert.Throws<InvalidOperationException>(() =>
                BrigadeActivityPhaseIntentFactory.CreateForPhase(
                    plan,
                    1,
                    id => new TestActivity(Guid.NewGuid())));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void FactoryRejectsPhaseOutsidePlan(int phase)
        {
            var plan = CreatePlan();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BrigadeActivityPhaseIntentFactory.CreateForPhase(
                    plan,
                    phase,
                    id => null));
        }

        [Test]
        public void FactoryRejectsNullDependencies()
        {
            var plan = CreatePlan();

            Assert.Throws<ArgumentNullException>(() =>
                BrigadeActivityPhaseIntentFactory.CreateForPhase(
                    null, 1, id => null));

            Assert.Throws<ArgumentNullException>(() =>
                BrigadeActivityPhaseIntentFactory.CreateForPhase(
                    plan, 1, null));
        }

        [Test]
        public void FactoryReevaluatesActivityForNextPhase()
        {
            var brigade = CreateParticipant();
            var plan = CreatePlan(brigade);

            IBrigadeActivity currentActivity =
                new TestActivity(brigade.BrigadeId);

            var firstPhase = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => currentActivity);

            IBrigadeActivity laterActivity =
                new TestActivity(brigade.BrigadeId);

            currentActivity = laterActivity;

            var secondPhase = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 2, id => currentActivity);

            Assert.That(firstPhase[0].Activity,
                Is.Not.SameAs(secondPhase[0].Activity));

            Assert.That(secondPhase[0].Activity,
                Is.SameAs(laterActivity));
        }

        [Test]
        public void EmptyPlanDoesNotQueryActivityResolver()
        {
            var plan = CreatePlan();
            int calls = 0;

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan,
                1,
                id =>
                {
                    calls++;
                    return null;
                });

            Assert.That(intents, Is.Empty);
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void FactoryReturnsReadOnlyCollection()
        {
            var plan = CreatePlan(CreateParticipant());

            var intents = BrigadeActivityPhaseIntentFactory.CreateForPhase(
                plan, 1, id => new TestActivity(id));

            Assert.That(
                ((IList<BrigadeActivityPhaseIntent>)intents).IsReadOnly,
                Is.True);
        }

        [Test]
        public void OrphanReservationCannotSilentlyBlockActivity()
        {
            var brigade = CreateParticipant();
            var draft = CreateDraft(brigade);

            draft.TryAddReservation(
                new PhaseReservation(
                    Guid.NewGuid(),
                    actionPhase: 1,
                    brigade.BrigadeId));

            var plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            Assert.Throws<InvalidOperationException>(() =>
                BrigadeActivityPhaseIntentFactory.CreateForPhase(
                    plan, 1, id => new TestActivity(id)));
        }

        private static TurnBrigadeParticipant CreateParticipant(
            int number = 1)
        {
            return new TurnBrigadeParticipant(Guid.NewGuid(), number);
        }

        private static TurnPlanDraft CreateDraft(
            params TurnBrigadeParticipant[] participants)
        {
            return new TurnPlanDraft(
                turnNumber: 1,
                actionPhaseCount: 2,
                participants: participants);
        }

        private static ConfirmedTurnPlan CreatePlan(
            params TurnBrigadeParticipant[] participants)
        {
            return ConfirmedTurnPlan.CreateSnapshot(
                CreateDraft(participants));
        }

        private static ConfirmedTurnPlan CreateReservedPlan(
            int reservedPhase,
            TurnBrigadeParticipant reservedBrigade,
            params TurnBrigadeParticipant[] otherParticipants)
        {
            var participants = new[] { reservedBrigade }
                .Concat(otherParticipants)
                .ToArray();

            var draft = CreateDraft(participants);
            var order = CreateOrder();

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    reservedPhase,
                    reservedBrigade.BrigadeId));

            return ConfirmedTurnPlan.CreateSnapshot(draft);
        }

        private static TestBastionOrder CreateOrder()
        {
            return new TestBastionOrder(
                Guid.NewGuid(),
                requiredPhases: 2,
                bastionId: Guid.NewGuid());
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId)
                : base(brigadeId)
            {
            }
        }

        private sealed class TestBastionOrder : BastionTurnOrder
        {
            public TestBastionOrder(
                Guid orderId,
                int requiredPhases,
                Guid bastionId)
                : base(orderId, requiredPhases, bastionId)
            {
            }
        }
    }
}
