using System;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class OrderPhaseCommitterTests
    {
        [Test]
        public void NewExecutionStartsAndReceivesFirstTick()
        {
            var registry =
                new OrderExecutionRegistry();

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    requiredPhases: 3,
                    requiresExecutionStart: true);

            OrderPhaseCommitResult result =
                OrderPhaseCommitter.Commit(
                    resolution,
                    registry);

            Assert.That(
                result.StartedExecution,
                Is.True);

            Assert.That(
                result.Outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Progressed));

            Assert.That(
                result.Outcome.CompletedPhases,
                Is.EqualTo(1));

            Assert.That(
                result.Outcome.RemainingPhases,
                Is.EqualTo(2));

            Assert.That(
                registry.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void SinglePhaseExecutionCompletesOnStart()
        {
            var registry =
                new OrderExecutionRegistry();

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    requiredPhases: 1,
                    requiresExecutionStart: true);

            OrderPhaseCommitResult result =
                OrderPhaseCommitter.Commit(
                    resolution,
                    registry);

            Assert.That(
                result.Outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Completed));

            Assert.That(
                result.IsTerminal,
                Is.True);

            Assert.That(
                registry.TryGet(
                    result.OrderId,
                    out OrderExecutionState state),
                Is.True);

            Assert.That(
                state.IsCompleted,
                Is.True);
        }

        [Test]
        public void ExistingExecutionReceivesNextTick()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 3);

            var registry =
                new OrderExecutionRegistry();

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            registry.Add(
                state);

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    order,
                    requiresExecutionStart: false);

            OrderPhaseCommitResult result =
                OrderPhaseCommitter.Commit(
                    resolution,
                    registry);

            Assert.That(
                result.StartedExecution,
                Is.False);

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(2));

            Assert.That(
                result.Outcome.CompletedPhases,
                Is.EqualTo(2));
        }

        [Test]
        public void ExistingExecutionCanComplete()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            var registry =
                new OrderExecutionRegistry();

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            registry.Add(
                state);

            OrderPhaseCommitResult result =
                OrderPhaseCommitter.Commit(
                    CreateApprovedResolution(
                        order,
                        requiresExecutionStart: false),
                    registry);

            Assert.That(
                result.Outcome.Kind,
                Is.EqualTo(
                    OrderExecutionOutcomeKind
                        .Completed));

            Assert.That(
                state.IsCompleted,
                Is.True);
        }

        [Test]
        public void MultipleReservedBrigadesStillProduceOneTick()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 3);

            OrderPhaseIntent intent =
                new OrderPhaseIntent(
                    order,
                    actionPhase: 1,
                    reservedBrigadeIds:
                        new[]
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            Guid.NewGuid()
                        });

            var candidate =
                new AssessedOrderPhaseIntent(
                    intent,
                    OrderPhaseAssessment.Allowed(
                        order.OrderId,
                        actionPhase: 1,
                        requiresExecutionStart:
                            true));

            ResolvedOrderPhaseIntent resolution =
                ResolvedOrderPhaseIntent.Approved(
                    candidate);

            var registry =
                new OrderExecutionRegistry();

            OrderPhaseCommitter.Commit(
                resolution,
                registry);

            registry.TryGet(
                order.OrderId,
                out OrderExecutionState state);

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(1));
        }

        [Test]
        public void NewExecutionCannotOverwriteExistingState()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 3);

            var registry =
                new OrderExecutionRegistry();

            OrderExecutionState existing =
                OrderExecutionState.Start(
                    order);

            registry.Add(
                existing);

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    order,
                    requiresExecutionStart: true);

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        resolution,
                        registry));

            Assert.That(
                existing.CompletedPhases,
                Is.EqualTo(0));
        }

        [Test]
        public void ExistingExecutionMustActuallyExist()
        {
            var registry =
                new OrderExecutionRegistry();

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    requiredPhases: 2,
                    requiresExecutionStart: false);

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        resolution,
                        registry));

            Assert.That(
                registry.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void ExistingExecutionDurationMustMatchOrder()
        {
            Guid orderId =
                Guid.NewGuid();

            var registryOrder =
                new TestOrder(
                    orderId,
                    requiredPhases: 2);

            var intentOrder =
                new TestOrder(
                    orderId,
                    requiredPhases: 3);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    registryOrder);

            var registry =
                new OrderExecutionRegistry();

            registry.Add(
                state);

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    intentOrder,
                    requiresExecutionStart: false);

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        resolution,
                        registry));

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(0));
        }

        [Test]
        public void TerminalExecutionCannotReceiveAnotherTick()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            var registry =
                new OrderExecutionRegistry();

            registry.Add(
                state);

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    order,
                    requiresExecutionStart: false);

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        resolution,
                        registry));
        }

        [Test]
        public void RejectedResolutionCannotBeCommitted()
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    new TestOrder(
                        Guid.NewGuid(),
                        requiredPhases: 2));

            var candidate =
                new AssessedOrderPhaseIntent(
                    intent,
                    OrderPhaseAssessment.Rejected(
                        intent.OrderId,
                        intent.ActionPhase,
                        OrderPhaseAssessmentFailureReason
                            .ExecutionAlreadyFailed));

            ResolvedOrderPhaseIntent resolution =
                ResolvedOrderPhaseIntent
                    .RejectedByAssessment(
                        candidate);

            var registry =
                new OrderExecutionRegistry();

            Assert.Throws<
                ArgumentException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        resolution,
                        registry));

            Assert.That(
                registry.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void CommitResultPreservesOrderAndPhaseIdentity()
        {
            var registry =
                new OrderExecutionRegistry();

            ResolvedOrderPhaseIntent resolution =
                CreateApprovedResolution(
                    requiredPhases: 2,
                    requiresExecutionStart: true,
                    actionPhase: 2);

            OrderPhaseCommitResult result =
                OrderPhaseCommitter.Commit(
                    resolution,
                    registry);

            Assert.That(
                result.OrderId,
                Is.EqualTo(
                    resolution.OrderId));

            Assert.That(
                result.ActionPhase,
                Is.EqualTo(2));

            Assert.That(
                result.Outcome.OrderId,
                Is.EqualTo(
                    resolution.OrderId));
        }

        [Test]
        public void CommitRejectsNullArguments()
        {
            var registry =
                new OrderExecutionRegistry();

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        null,
                        registry));

            Assert.Throws<
                ArgumentNullException>(
                () =>
                    OrderPhaseCommitter.Commit(
                        CreateApprovedResolution(
                            requiredPhases: 2,
                            requiresExecutionStart:
                                true),
                        null));
        }

        private static ResolvedOrderPhaseIntent
            CreateApprovedResolution(
                int requiredPhases,
                bool requiresExecutionStart,
                int actionPhase = 1)
        {
            return CreateApprovedResolution(
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases),
                requiresExecutionStart,
                actionPhase);
        }

        private static ResolvedOrderPhaseIntent
            CreateApprovedResolution(
                TestOrder order,
                bool requiresExecutionStart,
                int actionPhase = 1)
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    order,
                    actionPhase);

            var candidate =
                new AssessedOrderPhaseIntent(
                    intent,
                    OrderPhaseAssessment.Allowed(
                        order.OrderId,
                        actionPhase,
                        requiresExecutionStart));

            return ResolvedOrderPhaseIntent.Approved(
                candidate);
        }

        private static OrderPhaseIntent CreateIntent(
            TestOrder order,
            int actionPhase = 1)
        {
            return new OrderPhaseIntent(
                order,
                actionPhase,
                reservedBrigadeIds:
                    new[]
                    {
                        Guid.NewGuid()
                    });
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