
using System;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Resolution
{
    [TestFixture]
    public sealed class TurnPhaseResolverTests
    {
        [Test]
        public void EmptyPhaseAdvancesToNextPhase()
        {
            var cycle = CreateCycle();
            var plan = Confirm(cycle);
            var registry = new OrderExecutionRegistry();

            var resolver = CreateResolver(cycle, plan, registry);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.OrderEntryCount, Is.Zero);
            Assert.That(result.ProcessedWorldProcessCount, Is.Zero);
            Assert.That(cycle.Stage,
                Is.EqualTo(TurnStage.ActionResolution));
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void FinalPhaseTransitionsToTurnEnd()
        {
            var cycle = CreateCycle();
            var plan = Confirm(cycle);
            var resolver = CreateResolver(
                cycle, plan, new OrderExecutionRegistry());

            resolver.ResolvePhase(1);
            PhaseResolutionResult last = resolver.ResolvePhase(2);

            Assert.That(last.ActionPhase, Is.EqualTo(2));
            Assert.That(cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
            Assert.That(cycle.CurrentActionPhase, Is.Null);
        }

        [Test]
        public void OrderProgressesAcrossTwoActionPhases()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var order = new TestOrder(Guid.NewGuid(), 3);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, brigade, 1);
            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId, 2, brigade.BrigadeId));

            var plan = Confirm(cycle, draft);
            var registry = new OrderExecutionRegistry();
            var resolver = CreateResolver(cycle, plan, registry);

            resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(second.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(registry.TryGet(
                order.OrderId, out OrderExecutionState state), Is.True);
            Assert.That(state.CompletedPhases, Is.EqualTo(2));
            Assert.That(state.RemainingPhases, Is.EqualTo(1));
        }

        [Test]
        public void SeveralBrigadesGiveOneOrderTick()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);
            var cycle = CreateCycle(first, second);
            var order = new TestOrder(Guid.NewGuid(), 3);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, first, 1);
            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId, 1, second.BrigadeId));

            var registry = new OrderExecutionRegistry();
            var resolver = CreateResolver(
                cycle, Confirm(cycle, draft), registry);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(registry.TryGet(
                order.OrderId, out OrderExecutionState state), Is.True);
            Assert.That(state.CompletedPhases, Is.EqualTo(1));
        }

        [Test]
        public void TwoDifferentOrdersEachReceiveOneTick()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);
            var cycle = CreateCycle(first, second);
            var draft = CreateDraft(cycle);
            var firstOrder = new TestOrder(Guid.NewGuid(), 2);
            var secondOrder = new TestOrder(Guid.NewGuid(), 2);

            AddOrder(draft, firstOrder, first, 1);
            AddOrder(draft, secondOrder, second, 1);

            var registry = new OrderExecutionRegistry();
            var resolver = CreateResolver(
                cycle, Confirm(cycle, draft), registry);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.CommittedOrderCount, Is.EqualTo(2));
            Assert.That(registry.Count, Is.EqualTo(2));
        }

        [Test]
        public void FreeBrigadeGetsActivitySelection()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var plan = Confirm(cycle);
            var registry = new OrderExecutionRegistry();

            var resolver = new TurnPhaseResolver(
                cycle, plan, registry,
                id => new TestActivity(id));

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.SelectedActivityCount, Is.EqualTo(1));
            Assert.That(result.SelectedActivityBrigadeIds[0],
                Is.EqualTo(brigade.BrigadeId));
            Assert.That(result.CommittedOrderCount, Is.Zero);
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void BlockingOrderSuppressesActivity()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var order = new TestOrder(Guid.NewGuid(), 2);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, brigade, 1);

            int activityCalls = 0;

            var resolver = new TurnPhaseResolver(
                cycle,
                Confirm(cycle, draft),
                new OrderExecutionRegistry(),
                id =>
                {
                    activityCalls++;
                    return new TestActivity(id);
                });

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.CommittedOrderCount, Is.EqualTo(1));
            Assert.That(result.SelectedActivityCount, Is.Zero);
            Assert.That(activityCalls, Is.Zero);
        }

        [Test]
        public void TerminalOrderIsRejectedAndPhaseStillAdvances()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var order = new TestOrder(Guid.NewGuid(), 1);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, brigade, 1);

            var registry = new OrderExecutionRegistry();
            var completed = OrderExecutionState.Start(order);
            completed.ApplyProgressTick();
            registry.Add(completed);

            var resolver = CreateResolver(
                cycle, Confirm(cycle, draft), registry);

            PhaseResolutionResult result = resolver.ResolvePhase(1);

            Assert.That(result.RejectedOrderCount, Is.EqualTo(1));
            Assert.That(result.CommittedOrderCount, Is.Zero);
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(2));

            registry.TryGet(order.OrderId, out OrderExecutionState state);
            Assert.That(state.CompletedPhases, Is.EqualTo(1));
        }

        [Test]
        public void SamePhaseCannotBeResolvedTwice()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var order = new TestOrder(Guid.NewGuid(), 3);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, brigade, 1);
            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId, 2, brigade.BrigadeId));

            var registry = new OrderExecutionRegistry();
            var resolver = CreateResolver(
                cycle, Confirm(cycle, draft), registry);

            resolver.ResolvePhase(1);

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(1));

            registry.TryGet(order.OrderId, out OrderExecutionState state);
            Assert.That(state.CompletedPhases, Is.EqualTo(1));
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(2));
        }

        [Test]
        public void ActivityResolverFailureDoesNotMutatePhase()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);
            var cycle = CreateCycle(first, second);
            var order = new TestOrder(Guid.NewGuid(), 2);
            var draft = CreateDraft(cycle);

            AddOrder(draft, order, first, 1);

            var registry = new OrderExecutionRegistry();

            var resolver = new TurnPhaseResolver(
                cycle,
                Confirm(cycle, draft),
                registry,
                id => throw new InvalidOperationException(
                    "Test activity resolution failure."));

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(1));

            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void CarryOverUsesSameExecutionRegistry()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var order = new TestOrder(Guid.NewGuid(), 3);
            var registry = new OrderExecutionRegistry();

            var firstDraft = CreateDraft(cycle);
            AddOrder(firstDraft, order, brigade, 1);
            firstDraft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId, 2, brigade.BrigadeId));

            var firstResolver = CreateResolver(
                cycle, Confirm(cycle, firstDraft), registry);

            firstResolver.ResolvePhase(1);
            firstResolver.ResolvePhase(2);

            Assert.That(cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));

            Assert.That(
                cycle.TryBeginNextTurn(new[] { brigade }).IsSuccess,
                Is.True);

            var secondDraft = CreateDraft(cycle);
            AddOrder(secondDraft, order, brigade, 1);

            var secondResolver = CreateResolver(
                cycle, Confirm(cycle, secondDraft), registry);

            PhaseResolutionResult result =
                secondResolver.ResolvePhase(1);

            registry.TryGet(order.OrderId, out OrderExecutionState state);

            Assert.That(result.OrderEntries[0].OutcomeKind,
                Is.EqualTo(OrderExecutionOutcomeKind.Completed));
            Assert.That(state.IsCompleted, Is.True);
            Assert.That(state.CompletedPhases, Is.EqualTo(3));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void WrongExpectedPhaseDoesNotAdvanceCycle()
        {
            var cycle = CreateCycle();
            var plan = Confirm(cycle);
            var resolver = CreateResolver(
                cycle, plan, new OrderExecutionRegistry());

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(2));

            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
        }

        [Test]
        public void RegistryIdentityIsPreservedAfterCommit()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var draft = CreateDraft(cycle);
            var order = new TestOrder(Guid.NewGuid(), 2);

            AddOrder(draft, order, brigade, 1);

            var registry = new OrderExecutionRegistry();
            var resolver = CreateResolver(
                cycle, Confirm(cycle, draft), registry);

            resolver.ResolvePhase(1);

            Assert.That(resolver.ExecutionRegistry,
                Is.SameAs(registry));

            Assert.That(registry.TryGet(
                order.OrderId, out OrderExecutionState state), Is.True);
            Assert.That(state.CompletedPhases, Is.EqualTo(1));
        }

        [Test]
        public void ConstructorRejectsMismatchedParticipants()
        {
            var cycle = CreateCycle(CreateParticipant(1));
            var otherParticipant = CreateParticipant(1);

            var draft = new TurnPlanDraft(
                cycle.TurnNumber,
                cycle.ActionPhaseCount,
                new[] { otherParticipant });

            var plan = Confirm(cycle, draft);

            Assert.Throws<ArgumentException>(() =>
                CreateResolver(
                    cycle,
                    plan,
                    new OrderExecutionRegistry()));
        }


        [Test]
        public void InjectedPersistentProcessorIsUsedForEachPhase()
        {
            var cycle = CreateCycle();
            var plan = Confirm(cycle);
            var registry = new OrderExecutionRegistry();

            Guid processId = Guid.NewGuid();

            var processor = new CallbackPersistentProcessor(
                context => new PersistentWorldPhaseResult(
                    context,
                    new[] { processId }));

            var resolver = new TurnPhaseResolver(
                cycle,
                plan,
                registry,
                id => null,
                persistentWorldProcessor: processor);

            PhaseResolutionResult first = resolver.ResolvePhase(1);
            PhaseResolutionResult second = resolver.ResolvePhase(2);

            Assert.That(processor.CallCount, Is.EqualTo(2));
            Assert.That(first.ProcessedWorldProcessCount, Is.EqualTo(1));
            Assert.That(second.ProcessedWorldProcessCount, Is.EqualTo(1));
            Assert.That(first.ProcessedWorldProcessIds[0],
                Is.EqualTo(processId));
            Assert.That(second.ProcessedWorldProcessIds[0],
                Is.EqualTo(processId));

            Assert.That(cycle.Stage, Is.EqualTo(TurnStage.TurnEnd));
        }

        [Test]
        public void PersistentPreparationFailureDoesNotPublishOrderProgress()
        {
            TurnBrigadeParticipant brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var draft = CreateDraft(cycle);

            var order = new TestOrder(Guid.NewGuid(), 2);
            AddOrder(draft, order, brigade, 1);

            ConfirmedTurnPlan plan = Confirm(cycle, draft);
            var registry = new OrderExecutionRegistry();

            var processor = new CallbackPersistentProcessor(
                context => throw new InvalidOperationException(
                    "Injected persistent preparation failure."));

            var resolver = new TurnPhaseResolver(
                cycle,
                plan,
                registry,
                id => null,
                persistentWorldProcessor: processor);

            Assert.Throws<InvalidOperationException>(() =>
                resolver.ResolvePhase(1));

            Assert.That(processor.CallCount, Is.EqualTo(1));
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
            Assert.That(cycle.Stage, Is.EqualTo(TurnStage.ActionResolution));
            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.TryGet(order.OrderId, out _), Is.False);
        }

        [Test]
        public void WrongPersistentPhaseResultDoesNotCommitOrderProgress()
        {
            TurnBrigadeParticipant brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var draft = CreateDraft(cycle);

            var order = new TestOrder(Guid.NewGuid(), 2);
            AddOrder(draft, order, brigade, 1);

            var registry = new OrderExecutionRegistry();

            var processor = new CallbackPersistentProcessor(
                context => new PersistentWorldPhaseResult(
                    new PersistentWorldPhaseContext(
                        context.TurnNumber,
                        actionPhase: 2,
                        context.ActionPhaseCount),
                    Array.Empty<Guid>()));

            var resolver = new TurnPhaseResolver(
                cycle,
                Confirm(cycle, draft),
                registry,
                id => null,
                persistentWorldProcessor: processor);

            Assert.Throws<ArgumentException>(() => resolver.ResolvePhase(1));

            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
            Assert.That(registry.Count, Is.Zero);
        }

        private static TurnCycle CreateCycle(
            params TurnBrigadeParticipant[] participants)
        {
            return new TurnCycle(1, 2, participants);
        }

        private static TurnBrigadeParticipant CreateParticipant(
            int number = 1)
        {
            return new TurnBrigadeParticipant(Guid.NewGuid(), number);
        }

        private static TurnPlanDraft CreateDraft(TurnCycle cycle)
        {
            return new TurnPlanDraft(
                cycle.TurnNumber,
                cycle.ActionPhaseCount,
                cycle.ActiveBrigades);
        }

        private static ConfirmedTurnPlan Confirm(
            TurnCycle cycle,
            TurnPlanDraft draft = null)
        {
            var plan = ConfirmedTurnPlan.CreateSnapshot(
                draft ?? CreateDraft(cycle));

            Assert.That(cycle.TryConfirmPlanning().IsSuccess, Is.True);
            return plan;
        }

        private static TurnPhaseResolver CreateResolver(
            TurnCycle cycle,
            ConfirmedTurnPlan plan,
            OrderExecutionRegistry registry)
        {
            return new TurnPhaseResolver(
                cycle, plan, registry, id => null);
        }

        private static void AddOrder(
            TurnPlanDraft draft,
            TestOrder order,
            TurnBrigadeParticipant brigade,
            int phase)
        {
            Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        order.OrderId,
                        phase,
                        brigade.BrigadeId)).IsSuccess,
                Is.True);
        }

        private sealed class TestOrder : BastionTurnOrder
        {
            public TestOrder(Guid orderId, int phases)
                : base(orderId, phases, Guid.NewGuid())
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

        
        private sealed class CallbackPersistentProcessor :
            IPersistentWorldPhaseProcessor
        {
            private readonly Func<
                PersistentWorldPhaseContext,
                PersistentWorldPhaseResult> _prepare;

            public int CallCount { get; private set; }

            public CallbackPersistentProcessor(
                Func<PersistentWorldPhaseContext,
                    PersistentWorldPhaseResult> prepare)
            {
                _prepare = prepare ??
                    throw new ArgumentNullException(nameof(prepare));
            }

            public PersistentWorldPhaseResult PreparePhase(
                PersistentWorldPhaseContext context)
            {
                CallCount++;
                return _prepare(context);
            }
        }
    }
}
