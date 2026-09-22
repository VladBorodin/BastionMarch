using System;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Orders
{
    [TestFixture]
    public sealed class TurnOrderTests
    {
        [Test]
        public void OrderStoresIdentityAndDuration()
        {
            Guid orderId =
                Guid.NewGuid();

            var order =
                new TestTurnOrder(
                    orderId,
                    requiredPhases: 3);

            Assert.That(
                order.OrderId,
                Is.EqualTo(orderId));

            Assert.That(
                order.RequiredPhases,
                Is.EqualTo(3));
        }

        [Test]
        public void OrderRejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new TestTurnOrder(
                        Guid.Empty,
                        requiredPhases: 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void OrderRejectsInvalidRequiredPhases(
            int requiredPhases)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new TestTurnOrder(
                        Guid.NewGuid(),
                        requiredPhases));
        }

        [Test]
        public void OrderCanRequireMorePhasesThanStandardTurn()
        {
            var order =
                new TestTurnOrder(
                    Guid.NewGuid(),
                    requiredPhases: 5);

            Assert.That(
                order.RequiredPhases,
                Is.EqualTo(5));
        }

        [Test]
        public void InterfaceContainsOnlySharedOrderData()
        {
            string[] propertyNames =
                typeof(ITurnOrder)
                    .GetProperties()
                    .Select(property =>
                        property.Name)
                    .OrderBy(name =>
                        name)
                    .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(ITurnOrder.OrderId),
                    nameof(ITurnOrder.RequiredPhases)
                }
                .OrderBy(name => name)
                .ToArray(),
                propertyNames);
        }

        [Test]
        public void CommonOrderPropertiesAreReadOnly()
        {
            Assert.That(
                typeof(ITurnOrder)
                    .GetProperty(
                        nameof(ITurnOrder.OrderId))
                    .CanWrite,
                Is.False);

            Assert.That(
                typeof(ITurnOrder)
                    .GetProperty(
                        nameof(ITurnOrder.RequiredPhases))
                    .CanWrite,
                Is.False);
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