using System;
using System.Collections;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Orders;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using Object = UnityEngine.Object;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BastionPrototypeSceneTests
    {
        private const string PrototypeSceneName = "BastionPrototype";

        [UnityTest]
        public IEnumerator PrototypeSceneCanPlanMoveFromSelectedModule()
        {
            BastionPrototypeBootstrap bootstrap = Object.FindAnyObjectByType<BastionPrototypeBootstrap>();

            PrototypeMovePlanningController planning = Object.FindAnyObjectByType<PrototypeMovePlanningController>();

            PrototypeRouteController routeController = Object.FindAnyObjectByType<PrototypeRouteController>();

            ModuleSelectionController selection = Object.FindAnyObjectByType<ModuleSelectionController>();

            BastionView bastionView = Object.FindAnyObjectByType<BastionView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(planning, Is.Not.Null);
            Assert.That(routeController, Is.Not.Null);
            Assert.That(selection, Is.Not.Null);
            Assert.That(bastionView, Is.Not.Null);

            // Даём технической сцене построить маршрут.
            yield return null;

            Assert.That(routeController.CurrentBrigadeId.HasValue, Is.True);
            Assert.That(routeController.CurrentRoute.IsSuccess, Is.True);
            Assert.That(routeController.CurrentRoute.StepCount,
                Is.GreaterThan(0));

            Guid brigadeId = routeController.CurrentBrigadeId.Value;

            // Берём первый шаг существующего доступного маршрута.
            // Это гарантированно непосредственный соседний отсек.
            Guid targetModuleId =
                routeController.CurrentRoute.Steps[0].ToModuleId;

            Assert.That(
                bastionView.TryGetModuleView(
                    targetModuleId, out ModuleView targetView),
                Is.True);

            Assert.That(planning.SelectBrigade(brigadeId), Is.True);
            Assert.That(planning.SelectActionPhase(1), Is.True);

            selection.Select(targetView);

            MovePlanningResult result =
                planning.TryPlanSelectedMove();

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(bootstrap.TurnSession.Draft.OrderCount,
                Is.EqualTo(1));

            Assert.That(bootstrap.TurnSession.Draft.ReservationCount,
                Is.EqualTo(1));

            Assert.That(
                bootstrap.TurnSession.Draft.Orders.Single(),
                Is.TypeOf<MoveBrigadeOrder>());

            Assert.That(
                bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            // Планирование не перемещает бригаду.
            Assert.That(
                bootstrap.Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance actualModule),
                Is.True);

            Assert.That(actualModule.Id,
                Is.EqualTo(routeController.CurrentRoute.SourceModuleId));

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlannedMoveUpdatesVisibleTimeline()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<BastionPrototypeBootstrap>();

            PrototypeMovePlanningController planning =
                Object.FindAnyObjectByType<PrototypeMovePlanningController>();

            PrototypeRouteController routes =
                Object.FindAnyObjectByType<PrototypeRouteController>();

            ModuleSelectionController selection =
                Object.FindAnyObjectByType<ModuleSelectionController>();

            BastionView bastionView =
                Object.FindAnyObjectByType<BastionView>();

            TurnPlanView planView =
                Object.FindAnyObjectByType<TurnPlanView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(planning, Is.Not.Null);
            Assert.That(routes, Is.Not.Null);
            Assert.That(selection, Is.Not.Null);
            Assert.That(planView, Is.Not.Null);
            Assert.That(planView.State, Is.Not.Null);

            Assert.That(planView.State.Phases.Count,
                Is.EqualTo(bootstrap.TurnSession.Cycle.ActionPhaseCount));

            Assert.That(planView.State.Phases[0].Entries,
                Is.Empty);

            yield return null;

            Assert.That(routes.CurrentBrigadeId.HasValue, Is.True);
            Assert.That(routes.CurrentRoute.IsSuccess, Is.True);

            Guid brigadeId = routes.CurrentBrigadeId.Value;

            Guid targetId =
                routes.CurrentRoute.Steps[0].ToModuleId;

            Assert.That(
                bastionView.TryGetModuleView(
                    targetId, out ModuleView targetView),
                Is.True);

            Assert.That(planning.SelectBrigade(brigadeId),
                Is.True);

            Assert.That(planning.SelectActionPhase(1),
                Is.True);

            selection.Select(targetView);

            TurnPlanPresentationState oldState = planView.State;

            MovePlanningResult result =
                planning.TryPlanSelectedMove();

            Assert.That(result.IsSuccess, Is.True);

            Assert.That(planView.State,
                Is.Not.SameAs(oldState));

            Assert.That(oldState.Phases[0].Entries, Is.Empty);

            Assert.That(planView.State.Phases[0].Entries.Count,
                Is.EqualTo(1));

            Assert.That(
                planView.State.Phases[0].Entries[0].OrderId,
                Is.EqualTo(result.Order.OrderId));

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            yield return null;
        }

        [UnitySetUp]
        public IEnumerator LoadPrototypeScene()
        {
            AsyncOperation loading =
                SceneManager.LoadSceneAsync(
                    PrototypeSceneName,
                    LoadSceneMode.Single);

            Assert.That(
                loading,
                Is.Not.Null);

            while (!loading.isDone)
            {
                yield return null;
            }

            // Даём компонентам выполнить Start.
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeSceneBindsSimulationThroughPresenter()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            BastionGridView gridView =
                Object.FindAnyObjectByType<
                    BastionGridView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(bastionView, Is.Not.Null);
            Assert.That(gridView, Is.Not.Null);

            Assert.That(
                bootstrap.Bastion,
                Is.Not.Null);

            Assert.That(
                presenter.SourceBastion,
                Is.SameAs(bootstrap.Bastion));

            Assert.That(
                presenter.CurrentState,
                Is.Not.Null);

            Assert.That(
                bastionView.State,
                Is.SameAs(
                    presenter.CurrentState));

            Assert.That(
                bastionView.ModuleViews.Count,
                Is.EqualTo(
                    presenter.CurrentState.ModuleCount));

            Assert.That(
                gridView.RenderedWidth,
                Is.EqualTo(
                    presenter.CurrentState.Width));

            Assert.That(
                gridView.RenderedDeckCount,
                Is.EqualTo(
                    presenter.CurrentState.DeckCount));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ModuleCanBeSelectedThroughController()
        {
            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            ModuleSelectionController
                selectionController =
                    Object.FindAnyObjectByType<
                        ModuleSelectionController>();

            Assert.That(
                bastionView,
                Is.Not.Null);

            Assert.That(
                selectionController,
                Is.Not.Null);

            ModuleView firstModuleView =
                bastionView.ModuleViews.First();

            selectionController.Select(
                firstModuleView);

            Assert.That(
                selectionController.SelectedView,
                Is.SameAs(firstModuleView));

            Assert.That(
                selectionController.SelectedState,
                Is.SameAs(firstModuleView.State));

            Assert.That(
                firstModuleView.IsSelected,
                Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PresenterRefreshesExistingModuleViewWithNewSnapshot()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            ModulePresentationState beforeState =
                bastionView.State.Modules.First();

            bool viewFoundBefore =
                bastionView.TryGetModuleView(
                    beforeState.ModuleId,
                    out ModuleView beforeView);

            bool modelFound =
                bootstrap.Bastion.TryGetModule(
                    beforeState.ModuleId,
                    out ModuleInstance modelModule);

            Assert.That(viewFoundBefore, Is.True);
            Assert.That(modelFound, Is.True);

            int capturedDurability =
                beforeState.CurrentDurability;

            modelModule.ApplyDamage(1);

            // Старый снимок не меняется сам.
            Assert.That(
                beforeState.CurrentDurability,
                Is.EqualTo(capturedDurability));

            presenter.RefreshPresentation();

            bool viewFoundAfter =
                bastionView.TryGetModuleView(
                    beforeState.ModuleId,
                    out ModuleView afterView);

            Assert.That(viewFoundAfter, Is.True);

            // Существующий GameObject не пересоздан.
            Assert.That(
                afterView,
                Is.SameAs(beforeView));

            // Но он получил новый снимок.
            Assert.That(
                afterView.State,
                Is.Not.SameAs(beforeState));

            Assert.That(
                afterView.State.CurrentDurability,
                Is.EqualTo(
                    capturedDurability - 1));

            Assert.That(
                presenter.CurrentState,
                Is.SameAs(bastionView.State));

            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeSceneCreatesPassageViews()
        {
            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            Assert.That(
                presenter,
                Is.Not.Null);

            Assert.That(
                bastionView,
                Is.Not.Null);

            Assert.That(
                presenter.CurrentState,
                Is.Not.Null);

            Assert.That(
                presenter.CurrentState.PassageCount,
                Is.GreaterThan(0));

            Assert.That(
                bastionView.PassageViews.Count,
                Is.EqualTo(
                    presenter.CurrentState.PassageCount));

            foreach (
                PassageView passageView
                in bastionView.PassageViews)
            {
                Assert.That(
                    passageView,
                    Is.Not.Null);

                Assert.That(
                    passageView.IsBound,
                    Is.True);

                Assert.That(
                    passageView.State,
                    Is.Not.Null);

                bool stateFound =
                    presenter.CurrentState.TryGetPassage(
                        passageView.PassageId,
                        out PassagePresentationState
                            passageState);

                Assert.That(
                    stateFound,
                    Is.True);

                Assert.That(
                    passageView.State,
                    Is.SameAs(
                        passageState));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PresenterRefreshesExistingPassageView()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(bastionView, Is.Not.Null);

            PassagePresentationState beforeState =
                presenter.CurrentState.Passages.First();

            bool viewFoundBefore =
                bastionView.TryGetPassageView(
                    beforeState.PassageId,
                    out PassageView beforeView);

            Assert.That(
                viewFoundBefore,
                Is.True);

            ModulePassage modelPassage =
                bootstrap.Bastion.Passages.First(
                    passage =>
                        passage.Id ==
                        beforeState.PassageId);

            ModulePassageState newState =
                modelPassage.State ==
                ModulePassageState.Blocked
                    ? ModulePassageState.Open
                    : ModulePassageState.Blocked;

            modelPassage.SetState(
                newState);

            // Старый снимок не меняется самостоятельно.
            Assert.That(
                beforeState.State,
                Is.Not.EqualTo(
                    newState));

            presenter.RefreshPresentation();

            bool viewFoundAfter =
                bastionView.TryGetPassageView(
                    beforeState.PassageId,
                    out PassageView afterView);

            Assert.That(
                viewFoundAfter,
                Is.True);

            // GameObject не пересоздан.
            Assert.That(
                afterView,
                Is.SameAs(
                    beforeView));

            // Но новый снимок получен.
            Assert.That(
                afterView.State,
                Is.Not.SameAs(
                    beforeState));

            Assert.That(
                afterView.State.State,
                Is.EqualTo(
                    newState));

            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeSceneCreatesDeployedBrigadeViews()
        {
            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            Assert.That(
                presenter,
                Is.Not.Null);

            Assert.That(
                bastionView,
                Is.Not.Null);

            BrigadePresentationState[] deployedBrigades =
                presenter.CurrentState.Brigades
                    .Where(brigade =>
                        brigade.IsDeployed)
                    .ToArray();

            Assert.That(
                deployedBrigades.Length,
                Is.GreaterThan(0));

            Assert.That(
                bastionView.BrigadeViews.Count,
                Is.EqualTo(
                    deployedBrigades.Length));

            foreach (
                BrigadePresentationState brigadeState
                in deployedBrigades)
            {
                bool viewFound =
                    bastionView.TryGetBrigadeView(
                        brigadeState.BrigadeId,
                        out BrigadeView brigadeView);

                Assert.That(
                    viewFound,
                    Is.True);

                Assert.That(
                    brigadeView,
                    Is.Not.Null);

                Assert.That(
                    brigadeView.IsBound,
                    Is.True);

                Assert.That(
                    brigadeView.State,
                    Is.SameAs(
                        brigadeState));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PresenterRefreshesExistingBrigadeView()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            Assert.That(
                bootstrap,
                Is.Not.Null);

            Assert.That(
                presenter,
                Is.Not.Null);

            Assert.That(
                bastionView,
                Is.Not.Null);

            BrigadePresentationState beforeState =
                presenter.CurrentState.Brigades
                    .First(brigade =>
                        brigade.IsWorking);

            bool viewFoundBefore =
                bastionView.TryGetBrigadeView(
                    beforeState.BrigadeId,
                    out BrigadeView beforeView);

            Assert.That(
                viewFoundBefore,
                Is.True);

            BrigadeOperationalResult stopResult =
                bootstrap.Bastion
                    .TryStopBrigadeWork(
                        beforeState.BrigadeId);

            Assert.That(
                stopResult.IsSuccess,
                Is.True);

            // Старый снимок остаётся неизменным.
            Assert.That(
                beforeState.IsWorking,
                Is.True);

            presenter.RefreshPresentation();

            bool viewFoundAfter =
                bastionView.TryGetBrigadeView(
                    beforeState.BrigadeId,
                    out BrigadeView afterView);

            Assert.That(
                viewFoundAfter,
                Is.True);

            // Тот же Unity-объект.
            Assert.That(
                afterView,
                Is.SameAs(
                    beforeView));

            // Но снимок уже новый.
            Assert.That(
                afterView.State,
                Is.Not.SameAs(
                    beforeState));

            Assert.That(
                afterView.State.IsWorking,
                Is.False);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeRouteControllerShowsReachableRoute()
        {
            PrototypeRouteController routeController =
                Object.FindAnyObjectByType<
                    PrototypeRouteController>();

            RouteView routeView =
                Object.FindAnyObjectByType<
                    RouteView>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            Assert.That(
                routeController,
                Is.Not.Null);

            Assert.That(
                routeView,
                Is.Not.Null);

            Assert.That(
                presenter,
                Is.Not.Null);

            // Даём Start() контроллера выполнить
            // построение технического маршрута.
            yield return null;

            Assert.That(
                routeController.CurrentBrigadeId.HasValue,
                Is.True);

            Assert.That(
                routeController.CurrentRoute,
                Is.Not.Null);

            Assert.That(
                routeController.CurrentRoute.IsSuccess,
                Is.True);

            Assert.That(
                routeController.CurrentRoute.StepCount,
                Is.GreaterThan(0));

            Assert.That(
                routeView.State,
                Is.SameAs(
                    routeController.CurrentRoute));

            Assert.That(
                routeView.IsSuccessfulRoute,
                Is.True);

            Assert.That(
                routeView.RenderedPointCount,
                Is.EqualTo(
                    routeController.CurrentRoute.StepCount *
                    2 +
                    1));

            bool brigadeFound =
                presenter.CurrentState.TryGetBrigade(
                    routeController
                        .CurrentBrigadeId.Value,
                    out BrigadePresentationState
                        brigadeState);

            Assert.That(
                brigadeFound,
                Is.True);

            Assert.That(
                brigadeState.CurrentModuleId,
                Is.EqualTo(
                    routeController.CurrentRoute
                        .SourceModuleId));
        }

        [UnityTest]
        public IEnumerator PrototypeSceneInitializesTurnSession()
        {
            BastionPrototypeBootstrap bootstrap = Object.FindAnyObjectByType<BastionPrototypeBootstrap>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Bastion, Is.Not.Null);
            Assert.That(bootstrap.TurnSession, Is.Not.Null);

            BastionTurnSession session = bootstrap.TurnSession;

            Assert.That(session.Bastion, Is.SameAs(bootstrap.Bastion));

            Assert.That(session.Cycle.Stage, Is.EqualTo(TurnStage.Planning));

            Assert.That(session.Cycle.TurnNumber, Is.EqualTo(1));

            Assert.That(session.Draft.ParticipantCount, Is.EqualTo(bootstrap.Bastion.BrigadeCount));

            Assert.That(session.ConfirmedPlan, Is.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeUiExecutesTurnAndStartsNextTurn()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<BastionPrototypeBootstrap>();

            PrototypeMovePlanningController planning =
                Object.FindAnyObjectByType<PrototypeMovePlanningController>();

            PrototypeRouteController routes =
                Object.FindAnyObjectByType<PrototypeRouteController>();

            ModuleSelectionController selection =
                Object.FindAnyObjectByType<ModuleSelectionController>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<BastionView>();

            TurnPlanView planView =
                Object.FindAnyObjectByType<TurnPlanView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(planning, Is.Not.Null);
            Assert.That(routes, Is.Not.Null);
            Assert.That(selection, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(bastionView, Is.Not.Null);
            Assert.That(planView, Is.Not.Null);

            yield return null;

            Assert.That(routes.CurrentRoute.IsSuccess, Is.True);
            Assert.That(routes.CurrentRoute.StepCount, Is.GreaterThan(0));

            Guid brigadeId = routes.CurrentBrigadeId.Value;
            Guid sourceId = routes.CurrentRoute.SourceModuleId;
            Guid targetId =
                routes.CurrentRoute.Steps[0].ToModuleId;

            Assert.That(
                bastionView.TryGetModuleView(
                    targetId, out ModuleView targetView),
                Is.True);

            Assert.That(planning.SelectBrigade(brigadeId), Is.True);
            Assert.That(planning.SelectActionPhase(1), Is.True);

            selection.Select(targetView);

            MovePlanningResult planned =
                planning.TryPlanSelectedMove();

            Assert.That(planned.IsSuccess, Is.True);

            // До подтверждения и исполнения бригада не движется.
            Assert.That(
                bootstrap.Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance beforeModule),
                Is.True);

            Assert.That(beforeModule.Id, Is.EqualTo(sourceId));

            TurnPlanningConfirmationResult confirmation =
                planning.ConfirmCurrentPlan();

            Assert.That(confirmation.IsSuccess, Is.True);

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.ActionResolution));

            Assert.That(
                bootstrap.TurnSession.Cycle.CurrentActionPhase,
                Is.EqualTo(1));

            // Подтверждение само по себе не выполняет приказ.
            Assert.That(
                bootstrap.Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance confirmedModule),
                Is.True);

            Assert.That(confirmedModule.Id, Is.EqualTo(sourceId));

            BastionPresentationState previousPresentation =
                presenter.CurrentState;

            PhaseResolutionResult first =
                planning.ResolveActivePhase();

            Assert.That(first.ActionPhase, Is.EqualTo(1));
            Assert.That(first.CommittedOrderCount, Is.EqualTo(1));

            // Вот здесь перемещение действительно произошло.
            Assert.That(
                bootstrap.Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance movedModule),
                Is.True);

            Assert.That(movedModule.Id, Is.EqualTo(targetId));

            Assert.That(
                presenter.CurrentState,
                Is.Not.SameAs(previousPresentation));

            Assert.That(
                presenter.CurrentState.TryGetBrigade(
                    brigadeId, out BrigadePresentationState brigadeState),
                Is.True);

            Assert.That(
                brigadeState.CurrentModuleId,
                Is.EqualTo(targetId));

            // Старый технический маршрут больше не актуален.
            Assert.That(routes.CurrentRoute, Is.Null);

            Assert.That(
                bootstrap.TurnSession.Cycle.CurrentActionPhase,
                Is.EqualTo(2));

            PhaseResolutionResult second =
                planning.ResolveActivePhase();

            Assert.That(second.ActionPhase, Is.EqualTo(2));

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.TurnEnd));

            TurnTransitionResult next =
                planning.StartNextTurn();

            Assert.That(next.IsSuccess, Is.True);

            Assert.That(bootstrap.TurnSession.Cycle.TurnNumber,
                Is.EqualTo(2));

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            Assert.That(bootstrap.TurnSession.Draft.OrderCount,
                Is.Zero);

            Assert.That(planView.State.TurnNumber,
                Is.EqualTo(2));

            Assert.That(planView.State.Phases[0].Entries,
                Is.Empty);

            Assert.That(planning.SelectedBrigadeId,
                Is.Null);

            // Новый ход не возвращает бригаду на прежнее место.
            Assert.That(
                bootstrap.Bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance finalModule),
                Is.True);

            Assert.That(finalModule.Id, Is.EqualTo(targetId));

            yield return null;
        }
        
        [UnityTest]
        public IEnumerator PrototypeUiCanCompleteTurnWithoutOrders()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<BastionPrototypeBootstrap>();

            PrototypeMovePlanningController planning =
                Object.FindAnyObjectByType<PrototypeMovePlanningController>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(planning, Is.Not.Null);

            Assert.That(bootstrap.TurnSession.Draft.OrderCount,
                Is.Zero);

            Assert.That(
                planning.ConfirmCurrentPlan().IsSuccess,
                Is.True);

            int phaseCount =
                bootstrap.TurnSession.Cycle.ActionPhaseCount;

            for (int phase = 1; phase <= phaseCount; phase++)
            {
                PhaseResolutionResult result =
                    planning.ResolveActivePhase();

                Assert.That(result.ActionPhase,
                    Is.EqualTo(phase));
            }

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.TurnEnd));

            Assert.That(planning.StartNextTurn().IsSuccess,
                Is.True);

            Assert.That(bootstrap.TurnSession.Cycle.TurnNumber,
                Is.EqualTo(2));

            Assert.That(bootstrap.TurnSession.Cycle.Stage,
                Is.EqualTo(TurnStage.Planning));

            yield return null;
        }
    }
}