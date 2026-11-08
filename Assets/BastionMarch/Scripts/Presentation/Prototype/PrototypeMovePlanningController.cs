using System;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Localization;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Planning;
using BastionMarch.Simulation.Turns.Resolution;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BastionMarch.Presentation.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMovePlanningController : MonoBehaviour
    {
        private BastionTurnSession _session;
        private ModuleSelectionController _moduleSelection;
        private TurnPlanView _turnPlanView;
        private BastionPresenter _presenter;
        private Vector2 _scrollPosition;

        private string _statusKey = UiTextKeys.Instructions;
        private object[] _statusArguments = Array.Empty<object>();
        private MovePlanningFailureReason? _statusFailure;

        public Guid? SelectedBrigadeId { get; private set; }
        public int SelectedActionPhase { get; private set; } = 1;

        // Resolve the message at display time so switching languages also
        // updates previously reported failures.
        public string LastStatus => _statusFailure.HasValue
            ? BastionTexts.Ui(
                UiTextKeys.MoveRejected,
                BastionTexts.EnumLabel(_statusFailure.Value))
            : BastionTexts.Ui(_statusKey, _statusArguments);

        public void Initialize(
            BastionTurnSession session,
            ModuleSelectionController moduleSelection,
            TurnPlanView turnPlanView,
            BastionPresenter presenter)
        {
            _session = session ??
                throw new ArgumentNullException(nameof(session));
            _moduleSelection = moduleSelection ??
                throw new ArgumentNullException(nameof(moduleSelection));
            _turnPlanView = turnPlanView ??
                throw new ArgumentNullException(nameof(turnPlanView));
            _presenter = presenter ??
                throw new ArgumentNullException(nameof(presenter));

            RefreshPlanView();
        }

        public void RefreshPlanView()
        {
            if (_session == null ||
                _turnPlanView == null ||
                _presenter == null ||
                _presenter.CurrentState == null)
            {
                throw new InvalidOperationException(
                    "Planning presentation is not initialized.");
            }

            _turnPlanView.Render(
                TurnPlanPresentationStateFactory.Capture(
                    _session.Draft,
                    _presenter.CurrentState));
        }

        public bool SelectBrigade(Guid brigadeId)
        {
            if (_session == null ||
                _session.Cycle.Stage != TurnStage.Planning)
            {
                return false;
            }

            if (!_session.Cycle.ActiveBrigades.Any(p =>
                    p.BrigadeId == brigadeId))
            {
                return false;
            }

            SelectedBrigadeId = brigadeId;
            SetStatus(UiTextKeys.BrigadeSelected);
            return true;
        }

        public bool SelectActionPhase(int actionPhase)
        {
            if (_session == null ||
                _session.Cycle.Stage != TurnStage.Planning ||
                actionPhase < 1 ||
                actionPhase > _session.Cycle.ActionPhaseCount)
            {
                return false;
            }

            SelectedActionPhase = actionPhase;
            return true;
        }

        public MovePlanningResult TryPlanSelectedMove()
        {
            if (_session == null ||
                _moduleSelection == null ||
                !SelectedBrigadeId.HasValue ||
                _moduleSelection.SelectedState == null)
            {
                throw new InvalidOperationException(
                    "Select a brigade and a target module first.");
            }

            MovePlanningResult result = _session.TryPlanMove(
                SelectedBrigadeId.Value,
                _moduleSelection.SelectedState.ModuleId,
                SelectedActionPhase);

            if (result.IsSuccess)
            {
                SetStatus(UiTextKeys.MoveAdded, SelectedActionPhase);
                RefreshPlanView();
            }
            else
            {
                _statusKey = UiTextKeys.MoveRejected;
                _statusArguments = Array.Empty<object>();
                _statusFailure = result.FailureReason;
            }

            return result;
        }

        public TurnPlanningConfirmationResult ConfirmCurrentPlan()
        {
            if (_session == null)
            {
                throw new InvalidOperationException("Turn session is not initialized.");
            }

            TurnPlanningConfirmationResult result = _session.ConfirmPlanning();

            SetStatus(result.IsSuccess ? UiTextKeys.TurnConfirmed : UiTextKeys.TurnConfirmFailed);

            return result;
        }

        public PhaseResolutionResult ResolveActivePhase()
        {
            if (_session == null || _presenter == null || _session.Cycle.Stage != TurnStage.ActionResolution)
            {
                throw new InvalidOperationException("Action resolution is not active.");
            }

            PhaseResolutionResult result = _session.ResolveCurrentPhase();

            // Simulation уже изменилась.
            // Создаём новый снимок для Presentation.
            _presenter.RefreshPresentation();

            // Технический маршрут был построен из старого
            // положения бригады и теперь может быть неверным.
            PrototypeRouteController routes = FindAnyObjectByType<PrototypeRouteController>();

            routes?.ClearRoute();

            SetStatus(
                UiTextKeys.TurnPhaseResolved,
                result.ActionPhase,
                result.CommittedOrderCount,
                result.ModuleWorkTickCount);

            return result;
        }

        private void SetStatus(string key, params object[] arguments)
        {
            _statusKey = key;
            _statusArguments = arguments;
            _statusFailure = null;
        }
        
        public TurnTransitionResult StartNextTurn()
        {
            if (_session == null)
            {
                throw new InvalidOperationException("Turn session is not initialized.");
            }

            TurnTransitionResult result = _session.BeginNextTurn();

            if (!result.IsSuccess)
            {
                SetStatus(UiTextKeys.TurnNextTurnFailed);
                return result;
            }

            SelectedBrigadeId = null;
            SelectedActionPhase = 1;
            _scrollPosition = Vector2.zero;

            _moduleSelection.ClearSelection();

            // Новый TurnPlanDraft: обновляем временную шкалу.
            RefreshPlanView();

            SetStatus(
                UiTextKeys.TurnNextTurnStarted,
                result.CurrentTurnNumber);

            return result;
        }

        private void OnGUI()
        {
            if (_session == null || !BastionTexts.IsReady)
            {
                return;
            }

            float x = Mathf.Max(10f, Screen.width - 350f);
            float panelHeight = Mathf.Clamp(
                Screen.height - 20f, 200f, 610f);

            GUILayout.BeginArea(
                new Rect(x, 10f, 340f, panelHeight),
                GUI.skin.box);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.BeginHorizontal();

            foreach (Locale locale in
                    LocalizationSettings.AvailableLocales.Locales)
            {
                string languageCode = locale.Identifier.Code;

                if (GUILayout.Button(
                        languageCode.ToUpperInvariant()))
                {
                    BastionTexts.TrySetLanguage(languageCode);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(BastionTexts.Ui(
                UiTextKeys.TurnHeader,
                _session.Cycle.TurnNumber));
            
            if (_session.Cycle.Stage != TurnStage.Planning)
            {
                if (_session.Cycle.Stage == TurnStage.ActionResolution)
                {
                    GUILayout.Label(BastionTexts.Ui(
                        UiTextKeys.TurnCurrentPhase,
                        _session.Cycle.CurrentActionPhase.Value));

                    GUILayout.Space(8);

                    if (GUILayout.Button(
                            BastionTexts.Ui(UiTextKeys.TurnResolvePhase)))
                    {
                        ResolveActivePhase();
                    }
                }
                else if (_session.Cycle.Stage == TurnStage.TurnEnd)
                {
                    GUILayout.Label(
                        BastionTexts.Ui(UiTextKeys.TurnEnd));

                    GUILayout.Space(8);

                    if (GUILayout.Button(
                            BastionTexts.Ui(UiTextKeys.TurnNextTurn)))
                    {
                        StartNextTurn();
                    }
                }

                GUILayout.Space(8);
                GUILayout.Label(LastStatus);

                GUILayout.Space(12);
                _turnPlanView?.DrawTimeline();

                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(BastionTexts.Ui(UiTextKeys.PlanningTitle));
            GUILayout.Space(8);
            GUILayout.Label(BastionTexts.Ui(UiTextKeys.BrigadeLabel));

            foreach (TurnBrigadeParticipant participant in
                     _session.Cycle.ActiveBrigades)
            {
                if (!_session.Bastion.TryGetBrigade(
                        participant.BrigadeId,
                        out Brigade brigade))
                {
                    continue;
                }

                string marker = SelectedBrigadeId == brigade.Id
                    ? "> "
                    : string.Empty;

                // Player-created nicknames are data: display them unchanged.
                string nickname = string.IsNullOrWhiteSpace(brigade.Nickname)
                    ? BastionTexts.EnumLabel(brigade.Type)
                    : brigade.Nickname;

                if (GUILayout.Button(marker + BastionTexts.Ui(
                        UiTextKeys.BrigadeButton,
                        brigade.Number,
                        nickname)))
                {
                    SelectBrigade(brigade.Id);
                }
            }

            GUILayout.Space(8);
            GUILayout.Label(BastionTexts.Ui(UiTextKeys.ActionPhaseLabel));
            GUILayout.BeginHorizontal();

            for (int phase = 1;
                 phase <= _session.Cycle.ActionPhaseCount;
                 phase++)
            {
                string label = phase == SelectedActionPhase
                    ? $"[{phase}]"
                    : phase.ToString();

                if (GUILayout.Button(label))
                    SelectActionPhase(phase);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            ModulePresentationState target =
                _moduleSelection?.SelectedState;

            GUILayout.Label(target == null
                ? BastionTexts.Ui(UiTextKeys.TargetEmpty)
                : BastionTexts.Ui(
                    UiTextKeys.TargetModule,
                    BastionTexts.ModuleName(target.NameLocalizationKey),
                    target.Position.X,
                    target.Position.Deck));

            if (GUILayout.Button(BastionTexts.Ui(UiTextKeys.PlanMove)))
            {
                if (!SelectedBrigadeId.HasValue)
                    SetStatus(UiTextKeys.SelectBrigade);
                else if (target == null)
                    SetStatus(UiTextKeys.SelectTarget);
                else
                    TryPlanSelectedMove();
            }

            GUILayout.Space(8);

            if (GUILayout.Button(
                    BastionTexts.Ui(UiTextKeys.TurnConfirmPlan)))
            {
                ConfirmCurrentPlan();
            }
            GUILayout.Label(BastionTexts.Ui(
                UiTextKeys.OrderCount,
                _session.Draft.OrderCount));
            GUILayout.Label(LastStatus);
            GUILayout.Space(12);
            _turnPlanView?.DrawTimeline();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
