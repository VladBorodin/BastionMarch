using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns
{
    [TestFixture]
    public sealed class TurnLifecycleTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void EmptyTurnCompletesFullLifecycle()
        {
            var cycle =
                new TurnCycle();

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(1));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.ActiveBrigades,
                Is.Empty);

            TurnTransitionResult confirm =
                cycle.TryConfirmPlanning();

            Assert.That(
                confirm.IsSuccess,
                Is.True);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.ActionResolution));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(1));

            TurnTransitionResult firstAdvance =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                firstAdvance.IsSuccess,
                Is.True);

            Assert.That(
                cycle.CurrentActionPhase,
                Is.EqualTo(2));

            TurnTransitionResult secondAdvance =
                cycle.TryAdvanceActionPhase();

            Assert.That(
                secondAdvance.IsSuccess,
                Is.True);

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            TurnTransitionResult nextTurn =
                cycle.TryBeginNextTurn(
                    Array.Empty<
                        TurnBrigadeParticipant>());

            Assert.That(
                nextTurn.IsSuccess,
                Is.True);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(2));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            Assert.That(
                cycle.IsPlanningConfirmed,
                Is.False);
        }

        [Test]
        public void FullLifecycleSupportsCustomActionPhaseCount()
        {
            const int phaseCount = 5;

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount:
                        phaseCount);

            cycle.TryConfirmPlanning();

            for (int expectedPhase = 1;
                 expectedPhase <= phaseCount;
                 expectedPhase++)
            {
                Assert.That(
                    cycle.Stage,
                    Is.EqualTo(
                        TurnStage.ActionResolution));

                Assert.That(
                    cycle.CurrentActionPhase,
                    Is.EqualTo(
                        expectedPhase));

                TurnTransitionResult result =
                    cycle.TryAdvanceActionPhase();

                Assert.That(
                    result.IsSuccess,
                    Is.True);
            }

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);

            cycle.TryBeginNextTurn(
                Array.Empty<
                    TurnBrigadeParticipant>());

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(
                    phaseCount));

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(2));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));
        }

        [Test]
        public void ConsecutiveEmptyTurnsRemainConsistent()
        {
            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 3);

            const int turnsToComplete = 5;

            for (int completedTurns = 0;
                 completedTurns < turnsToComplete;
                 completedTurns++)
            {
                int expectedTurnNumber =
                    completedTurns + 1;

                Assert.That(
                    cycle.TurnNumber,
                    Is.EqualTo(
                        expectedTurnNumber));

                Assert.That(
                    cycle.Stage,
                    Is.EqualTo(
                        TurnStage.Planning));

                CompleteCurrentTurn(
                    cycle);

                Assert.That(
                    cycle.Stage,
                    Is.EqualTo(
                        TurnStage.TurnEnd));

                TurnTransitionResult nextTurn =
                    cycle.TryBeginNextTurn(
                        Array.Empty<
                            TurnBrigadeParticipant>());

                Assert.That(
                    nextTurn.IsSuccess,
                    Is.True);
            }

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(
                    turnsToComplete + 1));

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.Planning));

            Assert.That(
                cycle.ActionPhaseCount,
                Is.EqualTo(3));

            Assert.That(
                cycle.ActiveBrigades,
                Is.Empty);
        }

        [Test]
        public void NextTurnUsesFreshBastionParticipantSnapshot()
        {
            var bastion =
                new Bastion(
                    name:
                        "turn-lifecycle-participants",
                    width: 4,
                    deckCount: 1);

            ModuleInstance module =
                InstallSmallModule(
                    bastion);

            Brigade first =
                CreateBrigade(
                    number: 1);

            Brigade second =
                CreateBrigade(
                    number: 2);

            Assert.That(
                bastion.TryAddBrigade(
                    first),
                Is.True);

            Assert.That(
                bastion.TryAddBrigade(
                    second),
                Is.True);

            Assert.That(
                bastion.TryDeployBrigadeToModule(
                        first.Id,
                        module.Id)
                    .IsSuccess,
                Is.True);

            IReadOnlyList<
                TurnBrigadeParticipant>
                    firstTurnParticipants =
                        TurnBrigadeParticipantFactory
                            .CaptureActive(
                                bastion);

            var cycle =
                new TurnCycle(
                    turnNumber: 1,
                    actionPhaseCount: 2,
                    activeBrigades:
                        firstTurnParticipants);

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0].BrigadeId,
                Is.EqualTo(
                    first.Id));

            CompleteCurrentTurn(
                cycle);

            // Мир меняется уже после snapshot
            // первого хода.
            first.ApplyCasualties(
                first.CurrentPersonnel);

            Assert.That(
                first.IsDisbanded,
                Is.True);

            Assert.That(
                bastion.TryDeployBrigadeToModule(
                        second.Id,
                        module.Id)
                    .IsSuccess,
                Is.True);

            // Snapshot текущего Turn 1 не меняется.
            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0].BrigadeId,
                Is.EqualTo(
                    first.Id));

            IReadOnlyList<
                TurnBrigadeParticipant>
                    secondTurnParticipants =
                        TurnBrigadeParticipantFactory
                            .CaptureActive(
                                bastion);

            Assert.That(
                secondTurnParticipants.Count,
                Is.EqualTo(1));

            Assert.That(
                secondTurnParticipants[0].BrigadeId,
                Is.EqualTo(
                    second.Id));

            TurnTransitionResult nextTurn =
                cycle.TryBeginNextTurn(
                    secondTurnParticipants);

            Assert.That(
                nextTurn.IsSuccess,
                Is.True);

            Assert.That(
                cycle.TurnNumber,
                Is.EqualTo(2));

            Assert.That(
                cycle.ActiveBrigadeCount,
                Is.EqualTo(1));

            Assert.That(
                cycle.ActiveBrigades[0].BrigadeId,
                Is.EqualTo(
                    second.Id));
        }

        [Test]
        public void EqualInputsProduceEqualTurnTrace()
        {
            Guid firstId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000001");

            Guid secondId =
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000002");

            var inputA =
                new[]
                {
                    new TurnBrigadeParticipant(
                        secondId,
                        brigadeNumber: 2),

                    new TurnBrigadeParticipant(
                        firstId,
                        brigadeNumber: 1)
                };

            var inputB =
                new[]
                {
                    new TurnBrigadeParticipant(
                        secondId,
                        brigadeNumber: 2),

                    new TurnBrigadeParticipant(
                        firstId,
                        brigadeNumber: 1)
                };

            var firstCycle =
                new TurnCycle(
                    turnNumber: 4,
                    actionPhaseCount: 3,
                    activeBrigades:
                        inputA);

            var secondCycle =
                new TurnCycle(
                    turnNumber: 4,
                    actionPhaseCount: 3,
                    activeBrigades:
                        inputB);

            IReadOnlyList<string> firstTrace =
                CaptureLifecycleTrace(
                    firstCycle);

            IReadOnlyList<string> secondTrace =
                CaptureLifecycleTrace(
                    secondCycle);

            CollectionAssert.AreEqual(
                firstTrace,
                secondTrace);

            CollectionAssert.AreEqual(
                firstCycle.ActiveBrigades
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray(),
                secondCycle.ActiveBrigades
                    .Select(participant =>
                        participant.BrigadeId)
                    .ToArray());
        }

        private ModuleInstance InstallSmallModule(
            Bastion bastion)
        {
            ModulePlacementResult result =
                bastion.TryInstallModule(
                    _catalog.GetRequired(
                        ModuleDefinitionIds
                            .SmallMachineRoom),
                    new GridPosition(
                        0,
                        0));

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Module;
        }

        private static Brigade CreateBrigade(
            int number)
        {
            return new Brigade(
                number:
                    number,
                type:
                    BrigadeType.Recruit,
                currentPersonnel:
                    4,
                maximumPersonnel:
                    4);
        }

        private static void CompleteCurrentTurn(
            TurnCycle cycle)
        {
            TurnTransitionResult confirm =
                cycle.TryConfirmPlanning();

            Assert.That(
                confirm.IsSuccess,
                Is.True);

            for (int phase = 0;
                 phase < cycle.ActionPhaseCount;
                 phase++)
            {
                TurnTransitionResult advance =
                    cycle.TryAdvanceActionPhase();

                Assert.That(
                    advance.IsSuccess,
                    Is.True);
            }

            Assert.That(
                cycle.Stage,
                Is.EqualTo(
                    TurnStage.TurnEnd));

            Assert.That(
                cycle.CurrentActionPhase,
                Is.Null);
        }

        private static IReadOnlyList<string>
            CaptureLifecycleTrace(
                TurnCycle cycle)
        {
            var trace =
                new List<string>();

            AddTraceEntry(
                trace,
                cycle);

            TurnTransitionResult confirm =
                cycle.TryConfirmPlanning();

            Assert.That(
                confirm.IsSuccess,
                Is.True);

            AddTraceEntry(
                trace,
                cycle);

            while (cycle.Stage ==
                   TurnStage.ActionResolution)
            {
                TurnTransitionResult advance =
                    cycle.TryAdvanceActionPhase();

                Assert.That(
                    advance.IsSuccess,
                    Is.True);

                AddTraceEntry(
                    trace,
                    cycle);
            }

            TurnTransitionResult nextTurn =
                cycle.TryBeginNextTurn(
                    cycle.ActiveBrigades);

            Assert.That(
                nextTurn.IsSuccess,
                Is.True);

            AddTraceEntry(
                trace,
                cycle);

            return trace;
        }

        private static void AddTraceEntry(
            ICollection<string> trace,
            TurnCycle cycle)
        {
            string actionPhase =
                cycle.CurrentActionPhase
                    .HasValue
                    ? cycle.CurrentActionPhase
                        .Value
                        .ToString()
                    : "none";

            string brigadeIds =
                string.Join(
                    ",",
                    cycle.ActiveBrigades
                        .Select(participant =>
                            participant.BrigadeId));

            trace.Add(
                $"Turn={cycle.TurnNumber};" +
                $"Stage={cycle.Stage};" +
                $"Phase={actionPhase};" +
                $"Brigades={brigadeIds}");
        }
    }
}