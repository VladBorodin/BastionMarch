using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class OrderPhaseIntentTests
    {
        [Test]
        public void IntentStoresOrderPhaseAndReservedBrigades()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            Guid firstBrigadeId =
                Guid.NewGuid();

            Guid secondBrigadeId =
                Guid.NewGuid();

            var intent =
                new OrderPhaseIntent(
                    order,
                    actionPhase: 2,
                    reservedBrigadeIds:
                        new[]
                        {
                            firstBrigadeId,
                            secondBrigadeId
                        });

            Assert.That(
                intent.Order,
                Is.SameAs(order));

            Assert.That(
                intent.OrderId,
                Is.EqualTo(
                    order.OrderId));

            Assert.That(
                intent.ActionPhase,
                Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[]
                {
                    firstBrigadeId,
                    secondBrigadeId
                },
                intent.ReservedBrigadeIds);
        }

        [Test]
        public void IntentRejectsNullOrder()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new OrderPhaseIntent(
                        null,
                        actionPhase: 1,
                        reservedBrigadeIds:
                            new[]
                            {
                                Guid.NewGuid()
                            }));
        }

        [Test]
        public void IntentRejectsEmptyReservedBrigadeCollection()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    new OrderPhaseIntent(
                        new TestOrder(
                            Guid.NewGuid(),
                            requiredPhases: 1),
                        actionPhase: 1,
                        reservedBrigadeIds:
                            Array.Empty<Guid>()));
        }

        [Test]
        public void IntentRejectsDuplicateReservedBrigades()
        {
            Guid brigadeId =
                Guid.NewGuid();

            Assert.Throws<
                ArgumentException>(
                () =>
                    new OrderPhaseIntent(
                        new TestOrder(
                            Guid.NewGuid(),
                            requiredPhases: 1),
                        actionPhase: 1,
                        reservedBrigadeIds:
                            new[]
                            {
                                brigadeId,
                                brigadeId
                            }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void IntentRejectsInvalidActionPhase(
            int actionPhase)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new OrderPhaseIntent(
                        new TestOrder(
                            Guid.NewGuid(),
                            requiredPhases: 1),
                        actionPhase,
                        reservedBrigadeIds:
                            new[]
                            {
                                Guid.NewGuid()
                            }));
        }

        [Test]
        public void EmptyPhaseProducesNoOrderIntents()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            ConfirmedTurnPlan plan =
                CreateConfirmedPlan(
                    brigade);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            Assert.That(
                intents,
                Is.Empty);
        }

        [Test]
        public void ReservationProducesOrderIntent()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    brigade);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    brigade.BrigadeId);

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    brigade.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            Assert.That(
                intents.Count,
                Is.EqualTo(1));

            Assert.That(
                intents[0].Order,
                Is.SameAs(order));
        }

        [Test]
        public void MultipleBrigadesForSameOrderProduceOneIntent()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            TurnPlanDraft draft =
                CreateDraft(
                    first,
                    second);

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    first.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    second.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            Assert.That(
                intents.Count,
                Is.EqualTo(1));

            Assert.That(
                intents[0].ReservedBrigadeCount,
                Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[]
                {
                    first.BrigadeId,
                    second.BrigadeId
                },
                intents[0]
                    .ReservedBrigadeIds);
        }

        [Test]
        public void FactoryUsesOnlyRequestedActionPhase()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    brigade);

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2,
                    brigade.BrigadeId);

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    brigade.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 2,
                    brigade.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var phaseTwo =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 2);

            Assert.That(
                phaseTwo.Count,
                Is.EqualTo(1));

            Assert.That(
                phaseTwo[0].ActionPhase,
                Is.EqualTo(2));
        }

        [Test]
        public void DifferentOrdersProduceSeparateIntents()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            TurnPlanDraft draft =
                CreateDraft(
                    first,
                    second);

            var firstOrder =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    first.BrigadeId);

            var secondOrder =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    second.BrigadeId);

            draft.TryAddOrder(
                firstOrder);

            draft.TryAddOrder(
                secondOrder);

            draft.TryAddReservation(
                new PhaseReservation(
                    firstOrder.OrderId,
                    actionPhase: 1,
                    first.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    secondOrder.OrderId,
                    actionPhase: 1,
                    second.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            Assert.That(
                intents.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void IntentsUseDeterministicOrderIdOrder()
        {
            TurnBrigadeParticipant first =
                CreateParticipant(
                    number: 1);

            TurnBrigadeParticipant second =
                CreateParticipant(
                    number: 2);

            Guid firstOrderId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondOrderId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            TurnPlanDraft draft =
                CreateDraft(
                    first,
                    second);

            var secondOrder =
                new TestBrigadeOrder(
                    secondOrderId,
                    requiredPhases: 1,
                    second.BrigadeId);

            var firstOrder =
                new TestBrigadeOrder(
                    firstOrderId,
                    requiredPhases: 1,
                    first.BrigadeId);

            draft.TryAddOrder(
                secondOrder);

            draft.TryAddOrder(
                firstOrder);

            draft.TryAddReservation(
                new PhaseReservation(
                    secondOrder.OrderId,
                    actionPhase: 1,
                    second.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    firstOrder.OrderId,
                    actionPhase: 1,
                    first.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            CollectionAssert.AreEqual(
                new[]
                {
                    firstOrderId,
                    secondOrderId
                },
                intents
                    .Select(intent =>
                        intent.OrderId)
                    .ToArray());
        }

        [Test]
        public void ReservedBrigadesUseParticipantOrder()
        {
            Guid firstBrigadeId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondBrigadeId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            var brigadeTwo =
                new TurnBrigadeParticipant(
                    secondBrigadeId,
                    brigadeNumber: 2);

            var brigadeOne =
                new TurnBrigadeParticipant(
                    firstBrigadeId,
                    brigadeNumber: 1);

            TurnPlanDraft draft =
                CreateDraft(
                    brigadeTwo,
                    brigadeOne);

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            draft.TryAddOrder(order);

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    brigadeTwo.BrigadeId));

            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId,
                    actionPhase: 1,
                    brigadeOne.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            var intents =
                OrderPhaseIntentFactory
                    .CreateForPhase(
                        plan,
                        actionPhase: 1);

            CollectionAssert.AreEqual(
                new[]
                {
                    brigadeOne.BrigadeId,
                    brigadeTwo.BrigadeId
                },
                intents[0]
                    .ReservedBrigadeIds);
        }

        [TestCase(0)]
        [TestCase(3)]
        public void FactoryRejectsPhaseOutsidePlan(
            int actionPhase)
        {
            ConfirmedTurnPlan plan =
                CreateConfirmedPlan();

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    OrderPhaseIntentFactory
                        .CreateForPhase(
                            plan,
                            actionPhase));
        }

        [Test]
        public void FactoryRejectsReservationForMissingOrder()
        {
            TurnBrigadeParticipant brigade =
                CreateParticipant();

            TurnPlanDraft draft =
                CreateDraft(
                    brigade);

            draft.TryAddReservation(
                new PhaseReservation(
                    Guid.NewGuid(),
                    actionPhase: 1,
                    brigade.BrigadeId));

            ConfirmedTurnPlan plan =
                ConfirmedTurnPlan.CreateSnapshot(
                    draft);

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    OrderPhaseIntentFactory
                        .CreateForPhase(
                            plan,
                            actionPhase: 1));
        }

        private static ConfirmedTurnPlan
            CreateConfirmedPlan(
                params TurnBrigadeParticipant[]
                    participants)
        {
            return ConfirmedTurnPlan.CreateSnapshot(
                CreateDraft(
                    participants));
        }

        private static TurnPlanDraft CreateDraft(
            params TurnBrigadeParticipant[]
                participants)
        {
            return new TurnPlanDraft(
                turnNumber: 1,
                actionPhaseCount: 2,
                participants:
                    participants);
        }

        private static TurnBrigadeParticipant
            CreateParticipant(
                int number = 1)
        {
            return new TurnBrigadeParticipant(
                Guid.NewGuid(),
                number);
        }

        private sealed class TestOrder :
            TurnOrder
        {
            public TestOrder(
                Guid orderId,
                int requiredPhases)
                : base(
                    orderId,
                    requiredPhases)
            {
            }
        }

        private sealed class TestBrigadeOrder :
            BrigadeTurnOrder
        {
            public TestBrigadeOrder(
                Guid orderId,
                int requiredPhases,
                Guid brigadeId)
                : base(
                    orderId,
                    requiredPhases,
                    brigadeId)
            {
            }
        }

        private sealed class TestBastionOrder :
            BastionTurnOrder
        {
            public TestBastionOrder(
                Guid orderId,
                int requiredPhases,
                Guid bastionId)
                : base(
                    orderId,
                    requiredPhases,
                    bastionId)
            {
            }
        }
    }
}