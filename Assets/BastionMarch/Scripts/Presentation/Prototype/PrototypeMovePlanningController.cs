
using System;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns;
using BastionMarch.Simulation.Turns.Planning;
using UnityEngine;

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

        private string _status = "Выберите бригаду и целевой отсек.";

        public Guid? SelectedBrigadeId { get; private set; }

        public int SelectedActionPhase { get; private set; } = 1;

        public string LastStatus => _status;

        
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
            _status = "Бригада выбрана.";
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

            _status = result.IsSuccess
                ? $"Приказ добавлен в фазу {SelectedActionPhase}."
                : $"Приказ отклонён: {result.FailureReason}";

            if (result.IsSuccess)
            {
                RefreshPlanView();
            }

            return result;
        }

        private void OnGUI()
        {
            if (_session == null ||
                _session.Cycle.Stage != TurnStage.Planning)
            {
                return;
            }

            float x = Mathf.Max(10f, Screen.width - 350f);

            float panelHeight = Mathf.Clamp(Screen.height - 20f, 200f, 610f);

            GUILayout.BeginArea(new Rect(x, 10f, 340f, panelHeight), GUI.skin.box);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label($"BastionMarch | Ход {_session.Cycle.TurnNumber}");

            GUILayout.Label("P3.2 — Планирование перемещения");

            GUILayout.Space(8);

            GUILayout.Label("Бригада:");

            foreach (TurnBrigadeParticipant participant
                     in _session.Cycle.ActiveBrigades)
            {
                if (!_session.Bastion.TryGetBrigade(
                        participant.BrigadeId,
                        out Brigade brigade))
                {
                    continue;
                }

                string marker =
                    SelectedBrigadeId == brigade.Id ? "> " : "  ";

                string nickname =
                    string.IsNullOrEmpty(brigade.Nickname)
                        ? brigade.Type.ToString()
                        : brigade.Nickname;

                if (GUILayout.Button(
                        $"{marker}#{brigade.Number} {nickname}"))
                {
                    SelectBrigade(brigade.Id);
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Action Phase:");

            GUILayout.BeginHorizontal();

            for (int phase = 1;
                 phase <= _session.Cycle.ActionPhaseCount;
                 phase++)
            {
                string label =
                    phase == SelectedActionPhase
                        ? $"[{phase}]"
                        : phase.ToString();

                if (GUILayout.Button(label))
                {
                    SelectActionPhase(phase);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            var target = _moduleSelection != null
                ? _moduleSelection.SelectedState
                : null;

            GUILayout.Label(
                target == null
                    ? "Цель: выберите отсек мышью"
                    : $"Цель: {target.DefinitionId} " +
                      $"(X:{target.Position.X}, " +
                      $"этаж:{target.Position.Deck})");

            if (GUILayout.Button("Запланировать переход"))
            {
                if (!SelectedBrigadeId.HasValue)
                {
                    _status = "Сначала выберите бригаду.";
                }
                else if (target == null)
                {
                    _status = "Сначала выберите целевой отсек.";
                }
                else
                {
                    TryPlanSelectedMove();
                }
            }

            GUILayout.Space(8);

            GUILayout.Label($"Приказов в плане: {_session.Draft.OrderCount}");

            GUILayout.Label(_status);

            GUILayout.Space(12);

            _turnPlanView?.DrawTimeline();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
