using System;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class OrderExecutionRegistryTests
    {
        [Test]
        public void NewRegistryStartsEmpty()
        {
            var registry =
                new OrderExecutionRegistry();

            Assert.That(
                registry.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void AddedStateCanBeRetrieved()
        {
            var registry =
                new OrderExecutionRegistry();

            OrderExecutionState state =
                CreateState();

            registry.Add(
                state);

            bool found =
                registry.TryGet(
                    state.OrderId,
                    out OrderExecutionState actual);

            Assert.That(
                found,
                Is.True);

            Assert.That(
                actual,
                Is.SameAs(state));

            Assert.That(
                registry.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void UnknownOrderIsNotFound()
        {
            var registry =
                new OrderExecutionRegistry();

            bool found =
                registry.TryGet(
                    Guid.NewGuid(),
                    out OrderExecutionState state);

            Assert.That(
                found,
                Is.False);

            Assert.That(
                state,
                Is.Null);
        }

        [Test]
        public void RegistryRejectsDuplicateOrderId()
        {
            Guid orderId =
                Guid.NewGuid();

            var registry =
                new OrderExecutionRegistry();

            registry.Add(
                CreateState(
                    orderId));

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    registry.Add(
                        CreateState(
                            orderId)));

            Assert.That(
                registry.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void RegistryRejectsNullState()
        {
            var registry =
                new OrderExecutionRegistry();

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    registry.Add(
                        null));
        }

        [Test]
        public void RegistryMayKeepTerminalState()
        {
            var registry =
                new OrderExecutionRegistry();

            OrderExecutionState state =
                CreateState(
                    requiredPhases: 1);

            state.ApplyProgressTick();

            registry.Add(
                state);

            Assert.That(
                registry.TryGet(
                    state.OrderId,
                    out OrderExecutionState actual),
                Is.True);

            Assert.That(
                actual.IsCompleted,
                Is.True);
        }

        private static OrderExecutionState
            CreateState(
                Guid? orderId = null,
                int requiredPhases = 2)
        {
            return OrderExecutionState.Start(
                new TestOrder(
                    orderId ??
                        Guid.NewGuid(),
                    requiredPhases));
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
    }
}