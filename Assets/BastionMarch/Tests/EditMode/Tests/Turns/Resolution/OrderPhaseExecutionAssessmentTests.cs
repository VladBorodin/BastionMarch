using System;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class
        OrderPhaseExecutionAssessmentTests
    {
        [Test]
        public void MissingExecutionStateAllowsStartingOrder()
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    requiredPhases: 3);

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    intent,
                    executionState: null);

            Assert.That(
                assessment.IsAllowed,
                Is.True);

            Assert.That(
                assessment.RequiresExecutionStart,
                Is.True);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .None));
        }

        [Test]
        public void MatchingActiveExecutionStateAllowsContinuation()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 3);

            OrderPhaseIntent intent =
                CreateIntent(
                    order);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    intent,
                    state);

            Assert.That(
                assessment.IsAllowed,
                Is.True);

            Assert.That(
                assessment.RequiresExecutionStart,
                Is.False);
        }

        [Test]
        public void AssessmentRejectsDifferentExecutionOrder()
        {
            var intentOrder =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            var otherOrder =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            OrderPhaseIntent intent =
                CreateIntent(
                    intentOrder);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    otherOrder);

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    intent,
                    state);

            Assert.That(
                assessment.IsAllowed,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .ExecutionOrderMismatch));
        }

        [Test]
        public void AssessmentRejectsDifferentExecutionDuration()
        {
            Guid orderId =
                Guid.NewGuid();

            var intentOrder =
                new TestOrder(
                    orderId,
                    requiredPhases: 3);

            var stateOrder =
                new TestOrder(
                    orderId,
                    requiredPhases: 2);

            OrderPhaseIntent intent =
                CreateIntent(
                    intentOrder);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    stateOrder);

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    intent,
                    state);

            Assert.That(
                assessment.IsAllowed,
                Is.False);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .ExecutionDurationMismatch));
        }

        [Test]
        public void AssessmentRejectsCompletedExecution()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 1);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    CreateIntent(order),
                    state);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyCompleted));
        }

        [Test]
        public void AssessmentRejectsFailedExecution()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.Fail();

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    CreateIntent(order),
                    state);

            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed));
        }

        [Test]
        public void AssessmentDoesNotChangeActiveExecutionProgress()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 3);

            OrderExecutionState state =
                OrderExecutionState.Start(
                    order);

            state.ApplyProgressTick();

            int completedBefore =
                state.CompletedPhases;

            OrderPhaseExecutionAssessor.Assess(
                CreateIntent(order),
                state);

            Assert.That(
                state.CompletedPhases,
                Is.EqualTo(
                    completedBefore));

            Assert.That(
                state.IsActive,
                Is.True);
        }

        [Test]
        public void AssessmentPreservesIntentIdentity()
        {
            var order =
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2);

            OrderPhaseIntent intent =
                CreateIntent(
                    order,
                    actionPhase: 2);

            OrderPhaseAssessment assessment =
                OrderPhaseExecutionAssessor.Assess(
                    intent,
                    executionState: null);

            Assert.That(
                assessment.OrderId,
                Is.EqualTo(
                    order.OrderId));

            Assert.That(
                assessment.ActionPhase,
                Is.EqualTo(2));
        }

        [Test]
        public void AssessorRejectsNullIntent()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    OrderPhaseExecutionAssessor.Assess(
                        null,
                        executionState: null));
        }

        [Test]
        public void RejectedAssessmentCannotUseNoneFailureReason()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    OrderPhaseAssessment.Rejected(
                        Guid.NewGuid(),
                        actionPhase: 1,
                        OrderPhaseAssessmentFailureReason
                            .None));
        }

        private static OrderPhaseIntent CreateIntent(
            int requiredPhases)
        {
            return CreateIntent(
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases));
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