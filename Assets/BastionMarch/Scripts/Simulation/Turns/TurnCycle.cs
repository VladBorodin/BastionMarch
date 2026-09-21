using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Чистое Simulation-ядро временного цикла игры.
    ///
    /// Хранит номер хода, крупную стадию
    /// и конфигурацию Action Phase.
    ///
    /// Конкретные переходы между стадиями,
    /// планы и приказы добавляются
    /// последующими подэтапами.
    /// </summary>
    public sealed class TurnCycle
    {
        public const int FirstTurnNumber = 1;

        public const int DefaultActionPhaseCount = 2;

        public const int MinimumActionPhaseCount = 1;

        public int TurnNumber
        {
            get;
            private set;
        }

        public TurnStage Stage
        {
            get;
            private set;
        }

        /// <summary>
        /// Количество Action Phase
        /// в одном ходе.
        ///
        /// Стандарт первой версии — две,
        /// но временная модель не зависит
        /// от конкретного числа фаз.
        /// </summary>
        public int ActionPhaseCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Номер текущей Action Phase,
        /// начиная с 1.
        ///
        /// Null означает, что цикл сейчас
        /// не находится внутри Action Resolution.
        /// </summary>
        public int? CurrentActionPhase
        {
            get;
            private set;
        }

        public bool HasActiveActionPhase =>
            CurrentActionPhase.HasValue;

        /// <summary>
        /// Planning текущего хода уже подтверждён.
        ///
        /// После подтверждения TurnCycle покидает
        /// стадию Planning, поэтому отдельное
        /// дублирующее поле состояния не требуется.
        /// </summary>
        public bool IsPlanningConfirmed =>
            Stage != TurnStage.Planning;

        public IReadOnlyList<TurnBrigadeParticipant>
            ActiveBrigades
        {
            get;
        }

        public int ActiveBrigadeCount =>
            ActiveBrigades.Count;

        public TurnCycle()
            : this(
                FirstTurnNumber,
                DefaultActionPhaseCount)
        {
        }

        public TurnCycle(
            int turnNumber)
            : this(
                turnNumber,
                DefaultActionPhaseCount)
        {
        }

        public TurnCycle(
            int turnNumber,
            int actionPhaseCount)
            : this(
                turnNumber,
                actionPhaseCount,
                Array.Empty<
                    TurnBrigadeParticipant>())
        {
        }

        /// <summary>
        /// Создаёт цикл с известного номера хода
        /// и заданным числом Action Phase.
        ///
        /// Новый цикл всегда начинается
        /// со стадии Planning.
        /// </summary>
       public TurnCycle(
            int turnNumber,
            int actionPhaseCount,
            IEnumerable<TurnBrigadeParticipant>
                activeBrigades)
        {
            if (turnNumber <
                FirstTurnNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber),
                    turnNumber,
                    "Turn number must be positive.");
            }

            if (actionPhaseCount <
                MinimumActionPhaseCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhaseCount),
                    actionPhaseCount,
                    "Action phase count must be positive.");
            }

            if (activeBrigades == null)
            {
                throw new ArgumentNullException(
                    nameof(activeBrigades));
            }

            TurnBrigadeParticipant[] brigadeArray =
                activeBrigades.ToArray();

            if (brigadeArray.Any(
                    brigade =>
                        brigade == null))
            {
                throw new ArgumentException(
                    "Active brigade collection " +
                    "cannot contain null.",
                    nameof(activeBrigades));
            }

            bool containsDuplicateIds =
                brigadeArray
                    .GroupBy(brigade =>
                        brigade.BrigadeId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateIds)
            {
                throw new ArgumentException(
                    "Active brigade collection " +
                    "contains duplicate brigade ids.",
                    nameof(activeBrigades));
            }

            TurnBrigadeParticipant[] orderedBrigades =
                brigadeArray
                    .OrderBy(brigade =>
                        brigade.BrigadeNumber)
                    .ThenBy(brigade =>
                        brigade.BrigadeId)
                    .ToArray();

            TurnNumber =
                turnNumber;

            Stage =
                TurnStage.Planning;

            ActionPhaseCount =
                actionPhaseCount;

            CurrentActionPhase =
                null;

            ActiveBrigades =
                new ReadOnlyCollection<
                    TurnBrigadeParticipant>(
                        orderedBrigades);
        }

        /// <summary>
        /// Подтверждает Planning текущего хода
        /// и начинает первую Action Phase.
        ///
        /// На этапе 12.4 никакой TurnPlan
        /// ещё не существует: операция меняет
        /// только временное состояние цикла.
        /// </summary>
        public TurnTransitionResult
            TryConfirmPlanning()
        {
            if (Stage !=
                TurnStage.Planning)
            {
                return TurnTransitionResult.Failure(
                    turnNumber:
                        TurnNumber,
                    stage:
                        Stage,
                    currentActionPhase:
                        CurrentActionPhase,
                    failureReason:
                        TurnTransitionFailureReason
                            .PlanningAlreadyConfirmed);
            }

            TurnStage previousStage =
                Stage;

            int? previousActionPhase =
                CurrentActionPhase;

            Stage =
                TurnStage.ActionResolution;

            CurrentActionPhase =
                1;

            return TurnTransitionResult.Success(
                turnNumber:
                    TurnNumber,
                previousStage:
                    previousStage,
                currentStage:
                    Stage,
                previousActionPhase:
                    previousActionPhase,
                currentActionPhase:
                    CurrentActionPhase);
        }

        /// <summary>
        /// Завершает текущую Action Phase.
        ///
        /// Если текущая фаза не последняя,
        /// активирует следующую.
        ///
        /// Если текущая фаза последняя,
        /// завершает Action Resolution
        /// и переводит цикл в TurnEnd.
        /// </summary>
        public TurnTransitionResult
            TryAdvanceActionPhase()
        {
            if (Stage !=
                    TurnStage.ActionResolution ||
                !CurrentActionPhase.HasValue)
            {
                return TurnTransitionResult.Failure(
                    turnNumber:
                        TurnNumber,
                    stage:
                        Stage,
                    currentActionPhase:
                        CurrentActionPhase,
                    failureReason:
                        TurnTransitionFailureReason
                            .ActionResolutionNotActive);
            }

            TurnStage previousStage =
                Stage;

            int? previousActionPhase =
                CurrentActionPhase;

            if (CurrentActionPhase.Value <
                ActionPhaseCount)
            {
                CurrentActionPhase =
                    CurrentActionPhase.Value + 1;
            }
            else
            {
                Stage =
                    TurnStage.TurnEnd;

                CurrentActionPhase =
                    null;
            }

            return TurnTransitionResult.Success(
                turnNumber:
                    TurnNumber,
                previousStage:
                    previousStage,
                currentStage:
                    Stage,
                previousActionPhase:
                    previousActionPhase,
                currentActionPhase:
                    CurrentActionPhase);
        }
    }
}