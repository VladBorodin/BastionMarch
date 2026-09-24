using System;
using System.Linq;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Resolution
{
    [TestFixture]
    public sealed class
        OrderPhaseConflictResolverTests
    {
        [Test]
        public void AllowedCandidateIsApproved()
        {
            AssessedOrderPhaseIntent candidate =
                CreateAllowedCandidate();

            var resolutions =
                OrderPhaseConflictResolver.Resolve(
                    new[]
                    {
                        candidate
                    });

            Assert.That(
                resolutions.Count,
                Is.EqualTo(1));

            Assert.That(
                resolutions[0].IsApproved,
                Is.True);

            Assert.That(
                resolutions[0].Decision,
                Is.EqualTo(
                    OrderPhaseResolutionDecision
                        .Approved));
        }

        [Test]
        public void RejectedAssessmentIsNotApproved()
        {
            AssessedOrderPhaseIntent candidate =
                CreateRejectedCandidate();

            var resolutions =
                OrderPhaseConflictResolver.Resolve(
                    new[]
                    {
                        candidate
                    });

            Assert.That(
                resolutions[0].IsApproved,
                Is.False);

            Assert.That(
                resolutions[0].Decision,
                Is.EqualTo(
                    OrderPhaseResolutionDecision
                        .RejectedByAssessment));
        }

        [Test]
        public void MultipleAllowedCandidatesAreAllApproved()
        {
            AssessedOrderPhaseIntent first =
                CreateAllowedCandidate();

            AssessedOrderPhaseIntent second =
                CreateAllowedCandidate();

            var resolutions =
                OrderPhaseConflictResolver.Resolve(
                    new[]
                    {
                        first,
                        second
                    });

            Assert.That(
                resolutions.Count,
                Is.EqualTo(2));

            Assert.That(
                resolutions.All(
                    resolution =>
                        resolution.IsApproved),
                Is.True);
        }

        [Test]
        public void ResultsUseDeterministicOrder()
        {
            Guid firstOrderId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondOrderId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            AssessedOrderPhaseIntent second =
                CreateAllowedCandidate(
                    secondOrderId);

            AssessedOrderPhaseIntent first =
                CreateAllowedCandidate(
                    firstOrderId);

            var resolutions =
                OrderPhaseConflictResolver.Resolve(
                    new[]
                    {
                        second,
                        first
                    });

            CollectionAssert.AreEqual(
                new[]
                {
                    firstOrderId,
                    secondOrderId
                },
                resolutions
                    .Select(resolution =>
                        resolution.OrderId)
                    .ToArray());
        }

        [Test]
        public void ResolutionPreservesCandidateReference()
        {
            AssessedOrderPhaseIntent candidate =
                CreateAllowedCandidate();

            var resolution =
                OrderPhaseConflictResolver.Resolve(
                    new[]
                    {
                        candidate
                    })
                .Single();

            Assert.That(
                resolution.Candidate,
                Is.SameAs(candidate));

            Assert.That(
                resolution.Intent,
                Is.SameAs(candidate.Intent));

            Assert.That(
                resolution.Assessment,
                Is.SameAs(
                    candidate.Assessment));
        }

        [Test]
        public void ResolverRejectsNullCollection()
        {
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    OrderPhaseConflictResolver.Resolve(
                        null));
        }

        [Test]
        public void ResolverRejectsNullCandidate()
        {
            var candidates =
                new AssessedOrderPhaseIntent[]
                {
                    CreateAllowedCandidate(),
                    null
                };

            Assert.Throws<
                ArgumentException>(
                () =>
                    OrderPhaseConflictResolver.Resolve(
                        candidates));
        }

        [Test]
        public void ResolverRejectsDuplicateOrderPhaseCandidates()
        {
            Guid orderId =
                Guid.NewGuid();

            AssessedOrderPhaseIntent first =
                CreateAllowedCandidate(
                    orderId,
                    actionPhase: 1);

            AssessedOrderPhaseIntent duplicate =
                CreateAllowedCandidate(
                    orderId,
                    actionPhase: 1);

            Assert.Throws<
                ArgumentException>(
                () =>
                    OrderPhaseConflictResolver.Resolve(
                        new[]
                        {
                            first,
                            duplicate
                        }));
        }

        private static AssessedOrderPhaseIntent
            CreateAllowedCandidate(
                Guid? orderId = null,
                int actionPhase = 1)
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    orderId ??
                        Guid.NewGuid(),
                    actionPhase);

            return new AssessedOrderPhaseIntent(
                intent,
                OrderPhaseAssessment.Allowed(
                    intent.OrderId,
                    intent.ActionPhase,
                    requiresExecutionStart:
                        true));
        }

        private static AssessedOrderPhaseIntent
            CreateRejectedCandidate()
        {
            OrderPhaseIntent intent =
                CreateIntent(
                    Guid.NewGuid(),
                    actionPhase: 1);

            return new AssessedOrderPhaseIntent(
                intent,
                OrderPhaseAssessment.Rejected(
                    intent.OrderId,
                    intent.ActionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed));
        }

        private static OrderPhaseIntent CreateIntent(
            Guid orderId,
            int actionPhase)
        {
            return new OrderPhaseIntent(
                new TestOrder(
                    orderId,
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