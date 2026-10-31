
using System;
using System.Linq;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Orders
{
    [TestFixture]
    public sealed class MoveBrigadeOrderTests
    {
        [Test]
        public void MoveStoresAllIdentities()
        {
            Guid orderId = Guid.NewGuid();
            Guid brigadeId = Guid.NewGuid();
            Guid sourceId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();
            Guid passageId = Guid.NewGuid();

            var order = new MoveBrigadeOrder(
                orderId, brigadeId, sourceId, targetId, passageId);

            Assert.That(order.OrderId, Is.EqualTo(orderId));
            Assert.That(order.BrigadeId, Is.EqualTo(brigadeId));
            Assert.That(order.SourceModuleId, Is.EqualTo(sourceId));
            Assert.That(order.TargetModuleId, Is.EqualTo(targetId));
            Assert.That(order.PassageId, Is.EqualTo(passageId));
        }

        [Test]
        public void MoveRequiresExactlyOnePhase()
        {
            MoveBrigadeOrder order = CreateOrder();

            Assert.That(order.RequiredPhases, Is.EqualTo(1));
            Assert.That(
                MoveBrigadeOrder.MovementRequiredPhases,
                Is.EqualTo(1));
        }

        [Test]
        public void MoveIsBrigadeScopedOrder()
        {
            MoveBrigadeOrder order = CreateOrder();

            Assert.That(order, Is.InstanceOf<IBrigadeScopedOrder>());
            Assert.That(order, Is.InstanceOf<ITurnOrder>());
        }

        [Test]
        public void MoveRejectsEmptyOrderId()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateOrder(orderId: Guid.Empty));
        }

        [Test]
        public void MoveRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateOrder(brigadeId: Guid.Empty));
        }

        [TestCase("SourceModuleId")]
        [TestCase("TargetModuleId")]
        [TestCase("PassageId")]
        public void MoveRejectsEmptyMovementIdentity(string field)
        {
            Guid sourceId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();
            Guid passageId = Guid.NewGuid();

            switch (field)
            {
                case "SourceModuleId":
                    sourceId = Guid.Empty;
                    break;

                case "TargetModuleId":
                    targetId = Guid.Empty;
                    break;

                case "PassageId":
                    passageId = Guid.Empty;
                    break;
            }

            Assert.Throws<ArgumentException>(() =>
                new MoveBrigadeOrder(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    sourceId,
                    targetId,
                    passageId));
        }

        [Test]
        public void MoveRejectsSameSourceAndTarget()
        {
            Guid moduleId = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() =>
                new MoveBrigadeOrder(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    moduleId,
                    moduleId,
                    Guid.NewGuid()));
        }

        [Test]
        public void ConstructorDoesNotRequireBastionState()
        {
            // Construction checks identities, not world availability.
            MoveBrigadeOrder order = CreateOrder();

            Assert.That(order.SourceModuleId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(order.TargetModuleId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(order.PassageId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void PublicOrderPropertiesAreReadOnly()
        {
            string[] propertyNames =
            {
                nameof(MoveBrigadeOrder.OrderId),
                nameof(MoveBrigadeOrder.RequiredPhases),
                nameof(MoveBrigadeOrder.BrigadeId),
                nameof(MoveBrigadeOrder.SourceModuleId),
                nameof(MoveBrigadeOrder.TargetModuleId),
                nameof(MoveBrigadeOrder.PassageId)
            };

            foreach (string propertyName in propertyNames)
            {
                Assert.That(
                    typeof(MoveBrigadeOrder)
                        .GetProperty(propertyName).CanWrite,
                    Is.False,
                    propertyName);
            }
        }

        [Test]
        public void MoveCanBeConfirmedInTurnPlan()
        {
            var participant = CreateParticipant();
            var cycle = CreateCycle(participant);
            var draft = CreateDraft(cycle);

            var order = CreateOrder(brigadeId: participant.BrigadeId);

            Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);
            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        order.OrderId, 1, participant.BrigadeId)).IsSuccess,
                Is.True);

            var coordinator = new TurnPlanningCoordinator();
            TurnPlanningConfirmationResult confirmation =
                coordinator.TryConfirmPlan(cycle, draft);

            Assert.That(confirmation.IsSuccess, Is.True);
            Assert.That(confirmation.ConfirmedPlan.OrderCount, Is.EqualTo(1));
            Assert.That(confirmation.ConfirmedPlan.ReservationCount,
                Is.EqualTo(1));
            Assert.That(
                confirmation.ConfirmedPlan.Orders[0],
                Is.SameAs(order));
            Assert.That(cycle.CurrentActionPhase, Is.EqualTo(1));
        }

        [Test]
        public void ReservationForDifferentBrigadeFailsValidation()
        {
            var first = CreateParticipant(1);
            var second = CreateParticipant(2);
            var cycle = CreateCycle(first, second);
            var draft = CreateDraft(cycle);

            var order = CreateOrder(brigadeId: first.BrigadeId);

            Assert.That(draft.TryAddOrder(order).IsSuccess, Is.True);

            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        order.OrderId, 1, second.BrigadeId)).IsSuccess,
                Is.True);

            TurnPlanAssessment assessment =
                TurnPlanValidator.Assess(cycle, draft);

            Assert.That(assessment.IsValid, Is.False);
            Assert.That(
                assessment.FailureReason,
                Is.EqualTo(
                    TurnPlanFailureReason
                        .BrigadeOrderReservationMismatch));
        }

        [Test]
        public void BrigadeCanPlanTwoSeparateMovesInDifferentPhases()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var draft = CreateDraft(cycle);

            Guid moduleA = Guid.NewGuid();
            Guid moduleB = Guid.NewGuid();
            Guid moduleC = Guid.NewGuid();

            var firstMove = new MoveBrigadeOrder(
                Guid.NewGuid(),
                brigade.BrigadeId,
                moduleA,
                moduleB,
                Guid.NewGuid());

            var secondMove = new MoveBrigadeOrder(
                Guid.NewGuid(),
                brigade.BrigadeId,
                moduleB,
                moduleC,
                Guid.NewGuid());

            Assert.That(draft.TryAddOrder(firstMove).IsSuccess, Is.True);
            Assert.That(draft.TryAddOrder(secondMove).IsSuccess, Is.True);

            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        firstMove.OrderId, 1, brigade.BrigadeId)).IsSuccess,
                Is.True);

            Assert.That(
                draft.TryAddReservation(
                    new PhaseReservation(
                        secondMove.OrderId, 2, brigade.BrigadeId)).IsSuccess,
                Is.True);

            Assert.That(TurnPlanValidator.Assess(cycle, draft).IsValid,
                Is.True);

            Assert.That(draft.OrderCount, Is.EqualTo(2));
            Assert.That(draft.ReservationCount, Is.EqualTo(2));
        }

        [Test]
        public void PhaseIntentPreservesMoveOrderReference()
        {
            var brigade = CreateParticipant();
            var cycle = CreateCycle(brigade);
            var draft = CreateDraft(cycle);
            var order = CreateOrder(brigadeId: brigade.BrigadeId);

            draft.TryAddOrder(order);
            draft.TryAddReservation(
                new PhaseReservation(
                    order.OrderId, 1, brigade.BrigadeId));

            ConfirmedTurnPlan plan = ConfirmedTurnPlan.CreateSnapshot(draft);

            var intents = OrderPhaseIntentFactory.CreateForPhase(plan, 1);

            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Order, Is.SameAs(order));
            Assert.That(intents[0].Order, Is.TypeOf<MoveBrigadeOrder>());
            Assert.That(intents[0].ReservedBrigadeCount, Is.EqualTo(1));
            Assert.That(
                intents[0].ReservedBrigadeIds.Single(),
                Is.EqualTo(brigade.BrigadeId));
        }

        private static MoveBrigadeOrder CreateOrder(
            Guid? orderId = null,
            Guid? brigadeId = null)
        {
            return new MoveBrigadeOrder(
                orderId ?? Guid.NewGuid(),
                brigadeId ?? Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());
        }

        private static TurnBrigadeParticipant CreateParticipant(int number = 1)
        {
            return new TurnBrigadeParticipant(Guid.NewGuid(), number);
        }

        private static TurnCycle CreateCycle(
            params TurnBrigadeParticipant[] participants)
        {
            return new TurnCycle(1, 2, participants);
        }

        private static TurnPlanDraft CreateDraft(TurnCycle cycle)
        {
            return new TurnPlanDraft(
                cycle.TurnNumber,
                cycle.ActionPhaseCount,
                cycle.ActiveBrigades);
        }
    }
}
