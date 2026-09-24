using System;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class OrderExecutionStateTests
    {
        [Test]
        public void StartCopiesOrderIdentityAndDuration()
        {
            Guid orderId =
                Guid.NewGuid();

            var order =
                new TestOrder(
                    orderId,
                    requiredPhases: 3);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            Assert.That(
                state.OrderId,
                Is.EqualTo(orderId));

            Assert.That(
                state.RequiredPhases,
                Is.EqualTo(3));
        }

        [Test]
        public void NewStateStartsActiveWithoutProgress()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 3);

            Assert.That(
                state.Status,
                Is.EqualTo(
                    OrderExecutionStatus.Active));

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(0));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(3));

            Assert.That(
                state.IsActive,
                Is.True);

            Assert.That(
                state.IsTerminal,
                Is.False);
        }

        [Test]
        public void ProgressTickAdvancesMultiPhaseOrder()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 3);

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(2));

            Assert.That(
                state.Status,
                Is.EqualTo(
                    OrderExecutionStatus.Active));

            Assert.That(
                outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Progressed));
        }

        [Test]
        public void FinalProgressTickCompletesOrder()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 2);

            state.ApplyProgressTick();

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(2));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(0));

            Assert.That(
                state.Status,
                Is.EqualTo(
                    OrderExecutionStatus.Completed));

            Assert.That(
                state.IsCompleted,
                Is.True);

            Assert.That(
                state.IsTerminal,
                Is.True);

            Assert.That(
                outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Completed));
        }

        [Test]
        public void SinglePhaseOrderCompletesOnFirstTick()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 1);

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            Assert.That(
                outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Completed));

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(0));

            Assert.That(
                state.IsCompleted,
                Is.True);
        }

        [Test]
        public void CompletedOrderCannotProgressAgain()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 1);

            state.ApplyProgressTick();

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    state.ApplyProgressTick());

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(1));
        }

        [Test]
        public void FailureDoesNotAddProgress()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 3);

            OrderExecutionOutcome outcome =
                state.Fail();

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(0));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(3));

            Assert.That(
                state.Status,
                Is.EqualTo(
                    OrderExecutionStatus.Failed));

            Assert.That(
                outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Failed));
        }

        [Test]
        public void PartiallyProgressedOrderCanFail()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 3);

            state.ApplyProgressTick();

            OrderExecutionOutcome outcome =
                state.Fail();

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                state.RemainingPhases,
                Is.EqualTo(2));

            Assert.That(
                state.IsFailed,
                Is.True);

            Assert.That(
                outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Failed));
        }

        [Test]
        public void FailedOrderCannotProgress()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 2);

            state.Fail();

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    state.ApplyProgressTick());
        }

        [Test]
        public void CompletedOrderCannotFail()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 1);

            state.ApplyProgressTick();

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    state.Fail());
        }

        [Test]
        public void ProgressedOutcomeCapturesProgressSnapshot()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 3);

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            Assert.That(
                outcome.OrderId,
                Is.EqualTo(
                    state.OrderId));

            Assert.That(
                outcome.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                outcome.RemainingPhases,
                Is.EqualTo(2));

            Assert.That(
                outcome.IsTerminal,
                Is.False);
        }

        [Test]
        public void CompletedOutcomeCapturesTerminalSnapshot()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 1);

            OrderExecutionOutcome outcome =
                state.ApplyProgressTick();

            Assert.That(
                outcome.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                outcome.RemainingPhases,
                Is.EqualTo(0));

            Assert.That(
                outcome.IsTerminal,
                Is.True);
        }

        [Test]
        public void FailedOutcomeCapturesTerminalSnapshot()
        {
            OrderExecutionState state =
                CreateState(
                    requiredPhases: 2);

            OrderExecutionOutcome outcome =
                state.Fail();

            Assert.That(
                outcome.CompletedPhases,
                Is.EqualTo(0));

            Assert.That(
                outcome.RemainingPhases,
                Is.EqualTo(2));

            Assert.That(
                outcome.IsTerminal,
                Is.True);
        }

        [Test]
        public void StartRejectsNullOrder()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    OrderExecutionState.Start(
                        null));
        }

        private static OrderExecutionState
            CreateState(
                int requiredPhases)
        {
            return OrderExecutionState.Start(
                new TestOrder(
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