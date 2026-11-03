
using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Управляет жизненным циклом хода одного Bastion.
    ///
    /// Не содержит Unity API и не управляет View.
    /// M1: одна игровая машина.
    /// </summary>
    public sealed class BastionTurnSession
    {
        private readonly BrigadeWorkProfileCatalog _profiles;

        private readonly TurnPlanningCoordinator _planningCoordinator =
            new TurnPlanningCoordinator();

        private TurnPhaseResolver _resolver;

        public Bastion Bastion { get; }

        public TurnCycle Cycle { get; }

        public TurnPlanDraft Draft { get; private set; }

        public ConfirmedTurnPlan ConfirmedPlan { get; private set; }

        public OrderExecutionRegistry ExecutionRegistry { get; }

        public PhaseResolutionResult LastPhaseResult { get; private set; }

        public BastionTurnSession(
            Bastion bastion,
            BrigadeWorkProfileCatalog profiles,
            int actionPhaseCount = TurnCycle.DefaultActionPhaseCount)
        {
            Bastion = bastion ??
                throw new ArgumentNullException(nameof(bastion));

            _profiles = profiles ??
                throw new ArgumentNullException(nameof(profiles));

            Cycle = new TurnCycle(
                TurnCycle.FirstTurnNumber,
                actionPhaseCount,
                CaptureParticipants());

            ExecutionRegistry = new OrderExecutionRegistry();

            Draft = CreateDraft();
        }

        public TurnPlanningConfirmationResult ConfirmPlanning()
        {
            TurnPlanningConfirmationResult result =
                _planningCoordinator.TryConfirmPlan(Cycle, Draft);

            if (!result.IsSuccess)
            {
                return result;
            }

            ConfirmedPlan = result.ConfirmedPlan;

            _resolver = new TurnPhaseResolver(
                Cycle,
                ConfirmedPlan,
                ExecutionRegistry,
                brigadeId => ModuleWorkActivityResolver.Resolve(
                    Bastion, brigadeId),
                Bastion,
                _profiles);

            return result;
        }

        public PhaseResolutionResult ResolveCurrentPhase()
        {
            if (_resolver == null ||
                Cycle.Stage != TurnStage.ActionResolution ||
                !Cycle.CurrentActionPhase.HasValue)
            {
                throw new InvalidOperationException(
                    "No active Action Phase to resolve.");
            }

            PhaseResolutionResult result =
                _resolver.ResolvePhase(
                    Cycle.CurrentActionPhase.Value);

            LastPhaseResult = result;

            return result;
        }

        public TurnTransitionResult BeginNextTurn()
        {
            TurnTransitionResult transition =
                Cycle.TryBeginNextTurn(CaptureParticipants());

            if (!transition.IsSuccess)
            {
                return transition;
            }

            Draft = CreateDraft();
            ConfirmedPlan = null;
            LastPhaseResult = null;
            _resolver = null;

            return transition;
        }

        private TurnPlanDraft CreateDraft()
        {
            return new TurnPlanDraft(
                Cycle.TurnNumber,
                Cycle.ActionPhaseCount,
                Cycle.ActiveBrigades);
        }

        private TurnBrigadeParticipant[] CaptureParticipants()
        {
            return Bastion.Brigades
                .Where(brigade => !brigade.IsDisbanded)
                .Select(brigade =>
                    new TurnBrigadeParticipant(
                        brigade.Id,
                        brigade.Number))
                .ToArray();
        }
    }
}
