
using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class PersistentWorldPhaseProcessorTests
    {
        [Test]
        public void NoOpProcessorReturnsEmptyResult()
        {
            var context = CreateContext();

            var result =
                NoOpPersistentWorldPhaseProcessor.Instance
                    .PreparePhase(context);

            Assert.That(result.TurnNumber, Is.EqualTo(1));
            Assert.That(result.ActionPhase, Is.EqualTo(1));
            Assert.That(result.ProcessedProcessCount, Is.Zero);
            Assert.That(result.ProcessedProcessIds, Is.Empty);
        }

        [Test]
        public void NoOpProcessorSupportsConsecutiveTurns()
        {
            var processor =
                NoOpPersistentWorldPhaseProcessor.Instance;

            var first = processor.PreparePhase(
                new PersistentWorldPhaseContext(1, 2, 2));

            var second = processor.PreparePhase(
                new PersistentWorldPhaseContext(2, 1, 2));

            Assert.That(first.TurnNumber, Is.EqualTo(1));
            Assert.That(second.TurnNumber, Is.EqualTo(2));
            Assert.That(second.ActionPhase, Is.EqualTo(1));
        }

        [Test]
        public void NoOpProcessorRejectsNullContext()
        {
            Assert.Throws<ArgumentNullException>(() =>
                NoOpPersistentWorldPhaseProcessor.Instance
                    .PreparePhase(null));
        }

        [Test]
        public void ResultCopiesSourceCollection()
        {
            Guid id = Guid.NewGuid();
            var source = new List<Guid> { id };

            var result = new PersistentWorldPhaseResult(
                CreateContext(),
                source);

            source.Clear();

            Assert.That(result.ProcessedProcessCount, Is.EqualTo(1));
            Assert.That(result.ProcessedProcessIds[0],
                Is.EqualTo(id));
        }

        [Test]
        public void ResultUsesDeterministicProcessIdOrder()
        {
            Guid first = Guid.Parse(
                "00000000-0000-0000-0000-000000000001");

            Guid second = Guid.Parse(
                "00000000-0000-0000-0000-000000000002");

            var result = new PersistentWorldPhaseResult(
                CreateContext(),
                new[] { second, first });

            CollectionAssert.AreEqual(
                new[] { first, second },
                result.ProcessedProcessIds);
        }

        [Test]
        public void ResultRejectsDuplicateProcessIds()
        {
            Guid id = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() =>
                new PersistentWorldPhaseResult(
                    CreateContext(),
                    new[] { id, id }));
        }

        [Test]
        public void ResultRejectsEmptyProcessId()
        {
            Assert.Throws<ArgumentException>(() =>
                new PersistentWorldPhaseResult(
                    CreateContext(),
                    new[] { Guid.Empty }));
        }

        [Test]
        public void ResultRejectsNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PersistentWorldPhaseResult(
                    null,
                    Array.Empty<Guid>()));

            Assert.Throws<ArgumentNullException>(() =>
                new PersistentWorldPhaseResult(
                    CreateContext(),
                    null));
        }

        [Test]
        public void ResultCollectionIsReadOnly()
        {
            var result = new PersistentWorldPhaseResult(
                CreateContext(),
                new[] { Guid.NewGuid() });

            Assert.That(
                ((IList<Guid>)result.ProcessedProcessIds).IsReadOnly,
                Is.True);
        }

        [Test]
        public void CustomProcessorReceivesPhaseContext()
        {
            var processor = new TestProcessor();
            var context = new PersistentWorldPhaseContext(
                turnNumber: 12,
                actionPhase: 2,
                actionPhaseCount: 3);

            var result = processor.PreparePhase(context);

            Assert.That(processor.CallCount, Is.EqualTo(1));
            Assert.That(processor.LastContext, Is.SameAs(context));
            Assert.That(result.TurnNumber, Is.EqualTo(12));
            Assert.That(result.ActionPhase, Is.EqualTo(2));
            Assert.That(result.ProcessedProcessCount, Is.EqualTo(1));
        }

        private static PersistentWorldPhaseContext CreateContext()
        {
            return new PersistentWorldPhaseContext(turnNumber: 1, actionPhase: 1, actionPhaseCount: 2);
        }

        private sealed class TestProcessor :
            IPersistentWorldPhaseProcessor
        {
            private readonly Guid _processId = Guid.NewGuid();

            public int CallCount { get; private set; }

            public PersistentWorldPhaseContext LastContext
            {
                get;
                private set;
            }

            public PersistentWorldPhaseResult PreparePhase(PersistentWorldPhaseContext context)
            {
                CallCount++;
                LastContext = context;

                return new PersistentWorldPhaseResult(context, new[] { _processId });
            }
        }
    }
}
