using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class TurnPlanOrderTests
    {
        [Test]
        public void NewDraftStartsWithoutOrders()
        {
            TurnPlanDraft draft =
                CreateDraft();

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(0));

            Assert.That(
                draft.Orders,
                Is.Empty);
        }

        [Test]
        public void DraftAddsOrder()
        {
            TurnPlanDraft draft =
                CreateDraft();

            var order =
                new TestTurnOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1);

            TurnPlanOrderResult result =
                draft.TryAddOrder(
                    order);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnPlanOrderFailureReason.None));

            Assert.That(
                result.Order,
                Is.SameAs(order));

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                draft.Orders[0],
                Is.SameAs(order));
        }

        [Test]
        public void DraftRejectsDuplicateOrderId()
        {
            Guid orderId =
                Guid.NewGuid();

            TurnPlanDraft draft =
                CreateDraft();

            Assert.That(
                draft.TryAddOrder(
                        new TestTurnOrder(
                            orderId,
                            requiredPhases: 1))
                    .IsSuccess,
                Is.True);

            TurnPlanOrderResult result =
                draft.TryAddOrder(
                    new TestTurnOrder(
                        orderId,
                        requiredPhases: 3));

            Assert.That(
                result.IsSuccess,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    TurnPlanOrderFailureReason
                        .DuplicateOrderId));

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(1));
        }

        [Test]
        public void DraftAcceptsDifferentOrderIds()
        {
            TurnPlanDraft draft =
                CreateDraft();

            TurnPlanOrderResult first =
                draft.TryAddOrder(
                    new TestTurnOrder(
                        Guid.NewGuid(),
                        requiredPhases: 1));

            TurnPlanOrderResult second =
                draft.TryAddOrder(
                    new TestTurnOrder(
                        Guid.NewGuid(),
                        requiredPhases: 2));

            Assert.That(
                first.IsSuccess,
                Is.True);

            Assert.That(
                second.IsSuccess,
                Is.True);

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(2));
        }

        [Test]
        public void OrdersUseDeterministicIdOrder()
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

            TurnPlanDraft draft =
                CreateDraft();

            draft.TryAddOrder(
                new TestTurnOrder(
                    thirdId,
                    requiredPhases: 1));

            draft.TryAddOrder(
                new TestTurnOrder(
                    firstId,
                    requiredPhases: 1));

            draft.TryAddOrder(
                new TestTurnOrder(
                    secondId,
                    requiredPhases: 1));

            CollectionAssert.AreEqual(
                new[]
                {
                    firstId,
                    secondId,
                    thirdId
                },
                draft.Orders
                    .Select(order =>
                        order.OrderId)
                    .ToArray());
        }

        [Test]
        public void DraftRejectsNullOrder()
        {
            TurnPlanDraft draft =
                CreateDraft();

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    draft.TryAddOrder(
                        null));

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(0));
        }

        private static TurnPlanDraft CreateDraft()
        {
            return new TurnPlanDraft(
                turnNumber: 1,
                actionPhaseCount: 2,
                participants:
                    Array.Empty<
                        TurnBrigadeParticipant>());
        }

        private sealed class TestTurnOrder :
            TurnOrder
        {
            public TestTurnOrder(
                Guid orderId,
                int requiredPhases)
                : base(
                    orderId,
                    requiredPhases)
            {
            }
        }
    }
}