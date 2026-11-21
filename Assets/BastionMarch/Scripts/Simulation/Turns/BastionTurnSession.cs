using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Modules;

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
        private readonly Dictionary<Guid, BrigadeRoutePlan> _routePlans =
            new Dictionary<Guid, BrigadeRoutePlan>();

        private readonly Dictionary<Guid, MoveBrigadeOrder> _scheduledRouteMoves =
            new Dictionary<Guid, MoveBrigadeOrder>();

        public IReadOnlyList<BrigadeRoutePlan> RoutePlans =>
            _routePlans.Values
                .OrderBy(plan => plan.BrigadeId)
                .ToArray();

        public bool TryGetRoutePlan(
            Guid brigadeId,
            out BrigadeRoutePlan routePlan)
        {
            return _routePlans.TryGetValue(brigadeId, out routePlan);
        }

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

        public MovePlanningResult TryPlanMove(
            Guid brigadeId,
            Guid targetModuleId,
            int actionPhase)
        {
            MovePlanningResult Reject(MovePlanningFailureReason reason)
            {
                return MovePlanningResult.Rejected(reason);
            }

            if (Cycle.Stage != TurnStage.Planning)
                return Reject(MovePlanningFailureReason.PlanningNotActive);

            if (actionPhase < 1 ||
                actionPhase > Cycle.ActionPhaseCount)
            {
                return Reject(
                    MovePlanningFailureReason.ActionPhaseOutOfRange);
            }

            if (!Cycle.ActiveBrigades.Any(p =>
                    p.BrigadeId == brigadeId))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeNotParticipant);
            }

            if (!Bastion.TryGetBrigade(
                    brigadeId, out Brigade brigade) ||
                brigade.IsDisbanded)
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeUnavailable);
            }

            if (_routePlans.TryGetValue(
                    brigadeId, out BrigadeRoutePlan activeRoute) &&
                activeRoute.IsActive)
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeMoveAlreadyPlanned);
            }

            if (Draft.Orders.OfType<MoveBrigadeOrder>()
                .Any(order => order.BrigadeId == brigadeId))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeMoveAlreadyPlanned);
            }

            if (Draft.PhaseReservations.Any(r =>
                    r.BrigadeId == brigadeId &&
                    r.ActionPhase == actionPhase))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadePhaseReserved);
            }

            if (!Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance source))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeNotDeployed);
            }

            if (source.Id == targetModuleId)
                return Reject(MovePlanningFailureReason.SameModule);

            if (targetModuleId == Guid.Empty ||
                !Bastion.TryGetModule(targetModuleId, out _))
            {
                return Reject(
                    MovePlanningFailureReason.TargetModuleNotFound);
            }

            // Один MoveBrigadeOrder = один прямой переход.
            // Многошаговый маршрут здесь не разворачиваем.
            ModulePassage selectedPassage = null;

            foreach (ModulePassage passage in
                    Bastion.Passages.OrderBy(p => p.Id))
            {
                if (!passage.ConnectsModule(source.Id) ||
                    !passage.ConnectsModule(targetModuleId))
                {
                    continue;
                }

                BrigadeMoveAssessment assessment =
                    Bastion.AssessBrigadeMove(
                        brigadeId,
                        source.Id,
                        targetModuleId,
                        passage.Id);

                if (assessment.IsAllowed)
                {
                    selectedPassage = passage;
                    break;
                }
            }

            if (selectedPassage == null)
            {
                return Reject(
                    MovePlanningFailureReason.NoTraversablePassage);
            }

            var order = new MoveBrigadeOrder(
                Guid.NewGuid(),
                brigadeId,
                source.Id,
                targetModuleId,
                selectedPassage.Id);

            var reservation = new PhaseReservation(
                order.OrderId,
                actionPhase,
                brigadeId);

            if (!Draft.TryAddOrderWithReservation(order, reservation))
            {
                return Reject(
                    MovePlanningFailureReason.PlanRegistrationFailed);
            }

            return MovePlanningResult.Success(order);
        }

        public RoutePlanningResult TryPlanRoute(
            Guid brigadeId,
            Guid targetModuleId,
            int startActionPhase)
        {
            RoutePlanningResult Reject(MovePlanningFailureReason reason)
            {
                return RoutePlanningResult.Rejected(reason);
            }

            if (Cycle.Stage != TurnStage.Planning)
                return Reject(MovePlanningFailureReason.PlanningNotActive);

            if (startActionPhase < 1 ||
                startActionPhase > Cycle.ActionPhaseCount)
            {
                return Reject(
                    MovePlanningFailureReason.ActionPhaseOutOfRange);
            }

            if (!Cycle.ActiveBrigades.Any(p => p.BrigadeId == brigadeId))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeNotParticipant);
            }

            if (!Bastion.TryGetBrigade(brigadeId, out Brigade brigade) ||
                brigade.IsDisbanded)
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeUnavailable);
            }

            if (_routePlans.TryGetValue(brigadeId, out BrigadeRoutePlan existing) &&
                existing.IsActive)
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeMoveAlreadyPlanned);
            }

            // Пока одна бригада получает либо один обычный Move,
            // либо один маршрут за ход.
            if (Draft.Orders.OfType<IBrigadeScopedOrder>()
                    .Any(order => order.BrigadeId == brigadeId) ||
                Draft.PhaseReservations.Any(
                    reservation => reservation.BrigadeId == brigadeId))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeMoveAlreadyPlanned);
            }

            if (!Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance source))
            {
                return Reject(
                    MovePlanningFailureReason.BrigadeNotDeployed);
            }

            if (source.Id == targetModuleId)
                return Reject(MovePlanningFailureReason.SameModule);

            if (targetModuleId == Guid.Empty ||
                !Bastion.TryGetModule(targetModuleId, out _))
            {
                return Reject(
                    MovePlanningFailureReason.TargetModuleNotFound);
            }

            ModuleRouteSearchResult search =
                Bastion.FindModuleRoute(source.Id, targetModuleId);

            if (!search.IsSuccess || search.Route.StepCount == 0)
            {
                return Reject(
                    MovePlanningFailureReason.NoTraversablePassage);
            }

            var routePlan = new BrigadeRoutePlan(
                brigadeId,
                search.Route);

            if (!TryScheduleRouteSteps(routePlan, startActionPhase))
            {
                return Reject(
                    MovePlanningFailureReason.PlanRegistrationFailed);
            }

            // Публикуем маршрут только после регистрации приказов.
            _routePlans[brigadeId] = routePlan;

            return RoutePlanningResult.Success(routePlan);
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

            foreach (OrderPhaseJournalEntry entry in result.OrderEntries)
            {
                if (!_scheduledRouteMoves.TryGetValue(
                        entry.OrderId, out MoveBrigadeOrder move))
                {
                    continue;
                }

                BrigadeRoutePlan routePlan = _routePlans[move.BrigadeId];

                if (!routePlan.IsActive)
                    continue;

                if (!entry.WasCommitted ||
                    entry.OutcomeKind != OrderExecutionOutcomeKind.Completed)
                {
                    routePlan.TryMarkBlocked();
                    continue;
                }

                // Сверяем итоговый мир с ожидаемым переходом.
                if (!Bastion.TryGetBrigadeLocation(
                        move.BrigadeId, out ModuleInstance actual) ||
                    actual.Id != move.TargetModuleId)
                {
                    throw new InvalidOperationException(
                        "Completed move does not match Bastion location.");
                }

                if (!routePlan.TryAdvance(
                        move.PassageId,
                        move.SourceModuleId,
                        move.TargetModuleId))
                {
                    throw new InvalidOperationException(
                        "Completed move does not match route progress.");
                }
            }

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

            // Старые приказы уже относятся к предыдущему ходу.
            _scheduledRouteMoves.Clear();

            // Продолжаем только активные, незавершённые маршруты.
            foreach (BrigadeRoutePlan routePlan in
                    _routePlans.Values
                        .Where(plan => plan.IsActive)
                        .OrderBy(plan => plan.BrigadeId)
                        .ToArray())
            {
                if (!TryScheduleRouteSteps(routePlan, firstActionPhase: 1))
                {
                    // Например, бригада исчезла, сменила положение
                    // или её следующий переход перестал быть доступным.
                    routePlan.TryMarkBlocked();
                }
            }

            return transition;
        }

        private bool TryScheduleRouteSteps(
            BrigadeRoutePlan routePlan,
            int firstActionPhase)
        {
            if (routePlan == null)
                throw new ArgumentNullException(nameof(routePlan));

            if (!routePlan.IsActive ||
                firstActionPhase < 1 ||
                firstActionPhase > Cycle.ActionPhaseCount)
            {
                return false;
            }

            // Сверяем ожидаемую позицию с реальным Bastion.
            if (!Bastion.TryGetBrigadeLocation(
                    routePlan.BrigadeId, out ModuleInstance current) ||
                current.Id != routePlan.CurrentModuleId)
            {
                return false;
            }

            // Только первый шаг можно проверить целиком сейчас.
            // Последующие должны проверяться в момент исполнения.
            ModuleRouteStep firstStep = routePlan.NextStep;

            BrigadeMoveAssessment firstAssessment =
                Bastion.AssessBrigadeMove(
                    routePlan.BrigadeId,
                    firstStep.FromModuleId,
                    firstStep.ToModuleId,
                    firstStep.PassageId);

            if (!firstAssessment.IsAllowed)
                return false;

            int availablePhases =
                Cycle.ActionPhaseCount - firstActionPhase + 1;

            int stepCount = Math.Min(
                routePlan.RemainingStepCount,
                availablePhases);

            var registrations =
                new List<(ITurnOrder Order, PhaseReservation Reservation)>();

            for (int offset = 0; offset < stepCount; offset++)
            {
                ModuleRouteStep step = routePlan.Route.Steps[
                    routePlan.CompletedStepCount + offset];

                var order = new MoveBrigadeOrder(
                    Guid.NewGuid(),
                    routePlan.BrigadeId,
                    step.FromModuleId,
                    step.ToModuleId,
                    step.PassageId);

                var reservation = new PhaseReservation(
                    order.OrderId,
                    firstActionPhase + offset,
                    routePlan.BrigadeId);

                registrations.Add((order, reservation));
            }

            if (!Draft.TryAddOrdersWithReservations(registrations))
                return false;

            foreach (var registration in registrations)
            {
                var order = (MoveBrigadeOrder)registration.Order;
                _scheduledRouteMoves.Add(order.OrderId, order);
            }

            return true;
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
