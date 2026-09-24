using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Orders
{
    [TestFixture]
    public sealed class OrderScopeTests
    {
        [Test]
        public void BrigadeScopedOrderStoresCommonAndScopeData()
        {
            Guid orderId =
                Guid.NewGuid();

            Guid brigadeId =
                Guid.NewGuid();

            var order =
                new TestBrigadeOrder(
                    orderId,
                    requiredPhases: 2,
                    brigadeId);

            Assert.That(
                order.OrderId,
                Is.EqualTo(orderId));

            Assert.That(
                order.RequiredPhases,
                Is.EqualTo(2));

            Assert.That(
                order.BrigadeId,
                Is.EqualTo(brigadeId));
        }

        [Test]
        public void BrigadeScopedOrderRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new TestBrigadeOrder(
                        Guid.NewGuid(),
                        requiredPhases: 1,
                        brigadeId:
                            Guid.Empty));
        }

        [Test]
        public void BastionScopedOrderStoresCommonAndScopeData()
        {
            Guid orderId =
                Guid.NewGuid();

            Guid bastionId =
                Guid.NewGuid();

            var order =
                new TestBastionOrder(
                    orderId,
                    requiredPhases: 2,
                    bastionId);

            Assert.That(
                order.OrderId,
                Is.EqualTo(orderId));

            Assert.That(
                order.RequiredPhases,
                Is.EqualTo(2));

            Assert.That(
                order.BastionId,
                Is.EqualTo(bastionId));
        }

        [Test]
        public void BastionScopedOrderRejectsEmptyBastionId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new TestBastionOrder(
                        Guid.NewGuid(),
                        requiredPhases: 1,
                        bastionId:
                            Guid.Empty));
        }

        [Test]
        public void BrigadeScopedContractAddsOnlyBrigadeId()
        {
            string[] propertyNames =
                typeof(IBrigadeScopedOrder)
                    .GetProperties()
                    .Select(property =>
                        property.Name)
                    .OrderBy(name =>
                        name)
                    .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(
                        IBrigadeScopedOrder
                            .BrigadeId)
                },
                propertyNames);
        }

        [Test]
        public void BastionScopedContractAddsOnlyBastionId()
        {
            string[] propertyNames =
                typeof(IBastionScopedOrder)
                    .GetProperties()
                    .Select(property =>
                        property.Name)
                    .OrderBy(name =>
                        name)
                    .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(
                        IBastionScopedOrder
                            .BastionId)
                },
                propertyNames);
        }

        [Test]
        public void BrigadeScopedOrderIsTurnOrder()
        {
            ITurnOrder order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    brigadeId:
                        Guid.NewGuid());

            Assert.That(
                order,
                Is.InstanceOf<
                    IBrigadeScopedOrder>());
        }

        [Test]
        public void BastionScopedOrderIsTurnOrder()
        {
            ITurnOrder order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            Assert.That(
                order,
                Is.InstanceOf<
                    IBastionScopedOrder>());
        }

        [Test]
        public void DraftAcceptsBrigadeScopedOrder()
        {
            TurnPlanDraft draft =
                CreateDraft();

            var order =
                new TestBrigadeOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    brigadeId:
                        Guid.NewGuid());

            TurnPlanOrderResult result =
                draft.TryAddOrder(
                    order);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                draft.Orders,
                Does.Contain(order));
        }

        [Test]
        public void DraftAcceptsBastionScopedOrder()
        {
            TurnPlanDraft draft =
                CreateDraft();

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            TurnPlanOrderResult result =
                draft.TryAddOrder(
                    order);

            Assert.That(
                result.IsSuccess,
                Is.True);

            Assert.That(
                draft.Orders,
                Does.Contain(order));
        }

        [Test]
        public void AddingScopedOrderDoesNotCreateReservations()
        {
            TurnPlanDraft draft =
                CreateDraft();

            var order =
                new TestBastionOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1,
                    bastionId:
                        Guid.NewGuid());

            Assert.That(
                draft.TryAddOrder(order)
                    .IsSuccess,
                Is.True);

            Assert.That(
                draft.OrderCount,
                Is.EqualTo(1));

            Assert.That(
                draft.ReservationCount,
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