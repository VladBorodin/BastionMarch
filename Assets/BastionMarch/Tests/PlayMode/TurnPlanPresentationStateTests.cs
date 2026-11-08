using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Prototype;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class TurnPlanPresentationStateTests
    {
        private BastionPresentationState _bastionState;
        private TurnPlanDraft _draft;
        private BrigadePresentationState _brigade;
        private ModulePresentationState _target;

        [SetUp]
        public void SetUp()
        {
            var bastion = PrototypeBastionFactory.Create();

            _bastionState = BastionPresentationStateFactory.Capture(bastion);
            _brigade = _bastionState.Brigades.First(brigade => brigade.IsDeployed);
            _target = _bastionState.Modules.First(
                module => module.ModuleId != _brigade.CurrentModuleId.Value);

            _draft = new TurnPlanDraft(
                1,
                3,
                _bastionState.Brigades.Select(brigade =>
                    new TurnBrigadeParticipant(brigade.BrigadeId, brigade.Number)));
        }

        [Test]
        public void EmptyPlanContainsAllThreePhases()
        {
            var state = Capture();

            Assert.That(state.TurnNumber, Is.EqualTo(1));
            Assert.That(state.Phases.Count, Is.EqualTo(3));

            for (int i = 0; i < 3; i++)
            {
                Assert.That(state.Phases[i].ActionPhase, Is.EqualTo(i + 1));
                Assert.That(state.Phases[i].Entries, Is.Empty);
            }

            Assert.That(state.UnscheduledOrderCount, Is.Zero);
        }

        [Test]
        public void MoveAppearsOnlyInReservedPhase()
        {
            MoveBrigadeOrder move = AddMove(3);
            var state = Capture();

            Assert.That(state.Phases[0].Entries, Is.Empty);
            Assert.That(state.Phases[1].Entries, Is.Empty);
            Assert.That(state.Phases[2].Entries.Count, Is.EqualTo(1));

            TurnPlanEntryPresentationState entry = state.Phases[2].Entries[0];

            Assert.That(entry.OrderId, Is.EqualTo(move.OrderId));
            Assert.That(entry.BrigadeId, Is.EqualTo(_brigade.BrigadeId));
            Assert.That(entry.BrigadeNumber, Is.EqualTo(_brigade.Number));
            Assert.That(entry.Kind, Is.EqualTo(TurnPlanEntryKind.Move));
            Assert.That(entry.TargetModuleNameKey,
                Is.EqualTo(_target.NameLocalizationKey));
            Assert.That(entry.TargetX, Is.EqualTo(_target.Position.X));
            Assert.That(entry.TargetDeck, Is.EqualTo(_target.Position.Deck));
        }

        [Test]
        public void SnapshotDoesNotChangeAfterDraftMutation()
        {
            var before = Capture();
            AddMove(1);

            Assert.That(before.Phases[0].Entries, Is.Empty);

            var after = Capture();
            Assert.That(after.Phases[0].Entries.Count, Is.EqualTo(1));
            Assert.That(before.Phases[0].Entries, Is.Empty);
        }

        [Test]
        public void SnapshotCollectionsAreReadOnly()
        {
            AddMove(2);
            var state = Capture();

            Assert.That(
                ((IList<TurnPlanPhasePresentationState>)state.Phases).IsReadOnly,
                Is.True);

            Assert.That(
                ((IList<TurnPlanEntryPresentationState>)
                    state.Phases[1].Entries).IsReadOnly,
                Is.True);
        }

        private TurnPlanPresentationState Capture()
        {
            return TurnPlanPresentationStateFactory.Capture(
                _draft, _bastionState);
        }

        private MoveBrigadeOrder AddMove(int phase)
        {
            var move = new MoveBrigadeOrder(
                Guid.NewGuid(),
                _brigade.BrigadeId,
                _brigade.CurrentModuleId.Value,
                _target.ModuleId,
                Guid.NewGuid());

            Assert.That(
                _draft.TryAddOrderWithReservation(
                    move,
                    new PhaseReservation(move.OrderId, phase, _brigade.BrigadeId)),
                Is.True);

            return move;
        }
    }
}
