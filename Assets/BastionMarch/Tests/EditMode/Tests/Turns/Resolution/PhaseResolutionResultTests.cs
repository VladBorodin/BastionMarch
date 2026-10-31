
using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class PhaseResolutionResultTests
    {
        [Test]
        public void EmptyPhaseProducesEmptyJournal()
        {
            PhaseResolutionResult result = CreateResult();

            Assert.That(result.TurnNumber, Is.EqualTo(1));
            Assert.That(result.ActionPhase, Is.EqualTo(1));
            Assert.That(result.OrderEntryCount, Is.Zero);
            Assert.That(result.SelectedActivityCount, Is.Zero);
            Assert.That(result.ProcessedWorldProcessCount, Is.Zero);
        }

        [Test]
        public void ApprovedOrderRecordsCommittedProgress()
        {
            var resolution = CreateResolution(Guid.NewGuid());
            var commit = Commit(resolution);

            var result = CreateResult(
                resolutions: new[] { resolution },
                commits: new[] { commit });

            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(result.OrderEntries[0].WasCommitted, Is.True);
            Assert.That(result.OrderEntries[0].StartedExecution, Is.True);
            Assert.That(result.OrderEntries[0].CompletedPhases,
                Is.EqualTo(1));
            Assert.That(result.OrderEntries[0].OutcomeKind,
                Is.EqualTo(OrderExecutionOutcomeKind.Progressed));
        }

        [Test]
        public void RejectedOrderRecordsReasonWithoutProgress()
        {
            var resolution = CreateResolution(
                Guid.NewGuid(),
                approved: false);

            var result = CreateResult(
                resolutions: new[] { resolution });

            Assert.That(result.RejectedOrderCount, Is.EqualTo(1));
            Assert.That(result.OrderEntries[0].WasCommitted, Is.False);
            Assert.That(result.OrderEntries[0].CompletedPhases, Is.Null);
            Assert.That(result.OrderEntries[0].AssessmentFailureReason,
                Is.EqualTo(
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed));
        }

        [Test]
        public void OrderEntriesUseDeterministicOrder()
        {
            Guid firstId = Guid.Parse(
                "00000000-0000-0000-0000-000000000001");

            Guid secondId = Guid.Parse(
                "00000000-0000-0000-0000-000000000002");

            var first = CreateResolution(firstId);
            var second = CreateResolution(secondId);

            var result = CreateResult(
                resolutions: new[] { second, first },
                commits: new[]
                {
                    Commit(second),
                    Commit(first)
                });

            CollectionAssert.AreEqual(
                new[] { firstId, secondId },
                result.OrderEntries.Select(item => item.OrderId).ToArray());
        }

        [Test]
        public void SelectedActivityIsNotRecordedAsExecutedOrder()
        {
            Guid brigadeId = Guid.NewGuid();

            var activity = new BrigadeActivityPhaseIntent(
                new TestActivity(brigadeId), 1);

            var result = CreateResult(
                activities: new[] { activity });

            Assert.That(result.SelectedActivityCount, Is.EqualTo(1));
            Assert.That(result.SelectedActivityBrigadeIds[0],
                Is.EqualTo(brigadeId));
            Assert.That(result.CommittedOrderCount, Is.Zero);
        }

        [Test]
        public void PersistentProcessIdsArePreserved()
        {
            Guid processId = Guid.NewGuid();

            var context = CreateContext();

            var persistent = new PersistentWorldPhaseResult(
                context,
                new[] { processId });

            var result = CreateResult(
                context: context,
                persistentResult: persistent);

            Assert.That(result.ProcessedWorldProcessCount,
                Is.EqualTo(1));
            Assert.That(result.ProcessedWorldProcessIds[0],
                Is.EqualTo(processId));
        }

        [Test]
        public void JournalCopiesInputCollections()
        {
            var resolution = CreateResolution(Guid.NewGuid());
            var commits = new List<OrderPhaseCommitResult>
            {
                Commit(resolution)
            };

            var resolutions = new List<ResolvedOrderPhaseIntent>
            {
                resolution
            };

            var result = CreateResult(
                resolutions: resolutions,
                commits: commits);

            resolutions.Clear();
            commits.Clear();

            Assert.That(result.OrderEntryCount, Is.EqualTo(1));
            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
        }

        [Test]
        public void ApprovedOrderRequiresCommitResult()
        {
            var resolution = CreateResolution(Guid.NewGuid());

            Assert.Throws<ArgumentException>(() =>
                CreateResult(resolutions: new[] { resolution }));
        }

        [Test]
        public void CommitWithoutResolutionIsRejected()
        {
            var resolution = CreateResolution(Guid.NewGuid());

            Assert.Throws<ArgumentException>(() =>
                CreateResult(commits: new[] { Commit(resolution) }));
        }

        [Test]
        public void RejectedOrderCannotHaveCommitResult()
        {
            Guid orderId = Guid.NewGuid();

            var approved = CreateResolution(orderId);
            var rejected = CreateResolution(orderId, approved: false);

            Assert.Throws<ArgumentException>(() =>
                CreateResult(
                    resolutions: new[] { rejected },
                    commits: new[] { Commit(approved) }));
        }

        [Test]
        public void DuplicateOrderResolutionsAreRejected()
        {
            Guid id = Guid.NewGuid();

            var first = CreateResolution(id);
            var second = CreateResolution(id);

            Assert.Throws<ArgumentException>(() =>
                CreateResult(
                    resolutions: new[] { first, second }));
        }

        [Test]
        public void ReservedBrigadeCannotReceiveActivity()
        {
            Guid brigadeId = Guid.NewGuid();

            var resolution = CreateResolution(
                Guid.NewGuid(),
                approved: false,
                brigadeId: brigadeId);

            var activity = new BrigadeActivityPhaseIntent(
                new TestActivity(brigadeId), 1);

            Assert.Throws<ArgumentException>(() =>
                CreateResult(
                    resolutions: new[] { resolution },
                    activities: new[] { activity }));
        }

        [Test]
        public void ResultRejectsDifferentActionPhase()
        {
            var resolution = CreateResolution(
                Guid.NewGuid(),
                actionPhase: 2);

            Assert.Throws<ArgumentException>(() =>
                CreateResult(resolutions: new[] { resolution }));
        }

        [Test]
        public void ResultRejectsPersistentPhaseMismatch()
        {
            var persistent = new PersistentWorldPhaseResult(
                new PersistentWorldPhaseContext(1, 2, 2),
                Array.Empty<Guid>());

            Assert.Throws<ArgumentException>(() =>
                CreateResult(persistentResult: persistent));
        }

        [Test]
        public void ResultRejectsIncorrectTurnTransition()
        {
            var transition = TurnTransitionResult.Success(
                turnNumber: 1,
                previousStage: TurnStage.ActionResolution,
                currentStage: TurnStage.TurnEnd,
                previousActionPhase: 1,
                currentActionPhase: null);

            Assert.Throws<ArgumentException>(() =>
                CreateResult(transition: transition));
        }

        [Test]
        public void JournalCollectionsAreReadOnly()
        {
            var resolution = CreateResolution(Guid.NewGuid());

            var result = CreateResult(
                resolutions: new[] { resolution },
                commits: new[] { Commit(resolution) });

            Assert.That(
                ((IList<OrderPhaseJournalEntry>)result.OrderEntries)
                    .IsReadOnly,
                Is.True);

            Assert.That(
                ((IList<Guid>)result.SelectedActivityBrigadeIds)
                    .IsReadOnly,
                Is.True);

            Assert.That(
                ((IList<Guid>)result.ProcessedWorldProcessIds)
                    .IsReadOnly,
                Is.True);
        }

        private static PersistentWorldPhaseContext CreateContext()
        {
            return new PersistentWorldPhaseContext(1, 1, 2);
        }

        private static PhaseResolutionResult CreateResult(
            IEnumerable<ResolvedOrderPhaseIntent> resolutions = null,
            IEnumerable<OrderPhaseCommitResult> commits = null,
            IEnumerable<BrigadeActivityPhaseIntent> activities = null,
            PersistentWorldPhaseContext context = null,
            PersistentWorldPhaseResult persistentResult = null,
            TurnTransitionResult transition = null)
        {
            context = context ?? CreateContext();

            persistentResult = persistentResult ??
                new PersistentWorldPhaseResult(
                    context, Array.Empty<Guid>());

            transition = transition ??
                TurnTransitionResult.Success(
                    turnNumber: context.TurnNumber,
                    previousStage: TurnStage.ActionResolution,
                    currentStage: context.IsLastActionPhase
                        ? TurnStage.TurnEnd
                        : TurnStage.ActionResolution,
                    previousActionPhase: context.ActionPhase,
                    currentActionPhase: context.IsLastActionPhase
                        ? (int?)null
                        : context.ActionPhase + 1);

            return PhaseResolutionResult.Create(
                context,
                resolutions ?? Array.Empty<ResolvedOrderPhaseIntent>(),
                commits ?? Array.Empty<OrderPhaseCommitResult>(),
                activities ?? Array.Empty<BrigadeActivityPhaseIntent>(),
                persistentResult,
                transition);
        }

        private static ResolvedOrderPhaseIntent CreateResolution(
            Guid orderId,
            bool approved = true,
            int actionPhase = 1,
            Guid? brigadeId = null)
        {
            var order = new TestOrder(orderId, 2);

            var intent = new OrderPhaseIntent(
                order,
                actionPhase,
                new[] { brigadeId ?? Guid.NewGuid() });

            var assessment = approved
                ? OrderPhaseAssessment.Allowed(
                    orderId, actionPhase, true)
                : OrderPhaseAssessment.Rejected(
                    orderId,
                    actionPhase,
                    OrderPhaseAssessmentFailureReason
                        .ExecutionAlreadyFailed);

            var candidate = new AssessedOrderPhaseIntent(
                intent,
                assessment);

            return approved
                ? ResolvedOrderPhaseIntent.Approved(candidate)
                : ResolvedOrderPhaseIntent.RejectedByAssessment(candidate);
        }

        private static OrderPhaseCommitResult Commit(
            ResolvedOrderPhaseIntent resolution)
        {
            return OrderPhaseCommitter.Commit(
                resolution,
                new OrderExecutionRegistry());
        }

        private sealed class TestOrder : TurnOrder
        {
            public TestOrder(Guid orderId, int phases)
                : base(orderId, phases)
            {
            }
        }

        private sealed class TestActivity : BrigadeActivity
        {
            public TestActivity(Guid brigadeId)
                : base(brigadeId)
            {
            }
        }
    }
}
