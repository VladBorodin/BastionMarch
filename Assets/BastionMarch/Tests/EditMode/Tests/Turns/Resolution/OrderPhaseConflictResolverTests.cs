using System;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class AssessedOrderPhaseIntentTests
    {
        [Test]
        public void CandidateStoresIntentAndAssessment()
        {
            OrderPhaseIntent intent =
                CreateIntent();

            OrderPhaseAssessment assessment =
                OrderPhaseAssessment.Allowed(
                    intent.OrderId,
                    intent.ActionPhase,
                    requiresExecutionStart: true);

            var candidate =
                new AssessedOrderPhaseIntent(
                    intent,
                    assessment);

            Assert.That(
                candidate.Intent,
                Is.SameAs(intent));

            Assert.That(
                candidate.Assessment,
                Is.SameAs(assessment));

            Assert.That(
                candidate.OrderId,
                Is.EqualTo(intent.OrderId));

            Assert.That(
                candidate.ActionPhase,
                Is.EqualTo(intent.ActionPhase));
        }

        [Test]
        public void CandidateRejectsNullIntent()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new AssessedOrderPhaseIntent(
                        null,
                        OrderPhaseAssessment.Allowed(
                            Guid.NewGuid(),
                            actionPhase: 1,
                            requiresExecutionStart:
                                true)));
        }

        [Test]
        public void CandidateRejectsNullAssessment()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new AssessedOrderPhaseIntent(
                        CreateIntent(),
                        null));
        }

        [Test]
        public void CandidateRejectsDifferentOrderIdentity()
        {
            OrderPhaseIntent intent =
                CreateIntent();

            OrderPhaseAssessment assessment =
                OrderPhaseAssessment.Allowed(
                    Guid.NewGuid(),
                    intent.ActionPhase,
                    requiresExecutionStart: true);

            Assert.Throws<
                ArgumentException>(
                () =>
                    new AssessedOrderPhaseIntent(
                        intent,
                        assessment));
        }

        [Test]
        public void CandidateRejectsDifferentActionPhase()
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    actionPhase: 1);

            OrderPhaseAssessment assessment =
                OrderPhaseAssessment.Allowed(
                    intent.OrderId,
                    actionPhase: 2,
                    requiresExecutionStart: true);

            Assert.Throws<
                ArgumentException>(
                () =>
                    new AssessedOrderPhaseIntent(
                        intent,
                        assessment));
        }

        [Test]
        public void CandidateMayContainRejectedAssessment()
        {
            OrderPhaseIntent intent =
                CreateIntent();

            OrderPhaseAssessment assessment =
                OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed);

            var candidate =
                new AssessedOrderPhaseIntent(
                    intent,
                    assessment);

            Assert.That(
                candidate.PassedAssessment,
                Is.False);
        }

        private static OrderPhaseIntent CreateIntent(
            int actionPhase = 1)
        {
            return new OrderPhaseIntent(
                new TestOrder(
                    Guid.NewGuid(),
                    requiredPhases: 2),
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