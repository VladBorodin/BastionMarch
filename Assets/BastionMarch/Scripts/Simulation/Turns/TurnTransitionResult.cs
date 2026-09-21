using System;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Неизменяемый результат перехода
    /// временного состояния TurnCycle.
    ///
    /// Хранит состояние до и после попытки.
    /// </summary>
    public sealed class TurnTransitionResult
    {
        public bool IsSuccess =>
            FailureReason ==
            TurnTransitionFailureReason.None;

        public TurnTransitionFailureReason
            FailureReason
        {
            get;
        }

        public int PreviousTurnNumber
        {
            get;
        }

        public int CurrentTurnNumber
        {
            get;
        }

        /// <summary>
        /// Совместимый shorthand для текущего
        /// номера хода после операции.
        /// </summary>
        public int TurnNumber =>
            CurrentTurnNumber;

        public TurnStage PreviousStage
        {
            get;
        }

        public TurnStage CurrentStage
        {
            get;
        }

        public int? PreviousActionPhase
        {
            get;
        }

        public int? CurrentActionPhase
        {
            get;
        }

        private TurnTransitionResult(
            int previousTurnNumber,
            int currentTurnNumber,
            TurnStage previousStage,
            TurnStage currentStage,
            int? previousActionPhase,
            int? currentActionPhase,
            TurnTransitionFailureReason
                failureReason)
        {
            if (previousTurnNumber <
                TurnCycle.FirstTurnNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(previousTurnNumber));
            }

            if (currentTurnNumber <
                TurnCycle.FirstTurnNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTurnNumber));
            }

            PreviousTurnNumber =
                previousTurnNumber;

            CurrentTurnNumber =
                currentTurnNumber;

            PreviousStage =
                previousStage;

            CurrentStage =
                currentStage;

            PreviousActionPhase =
                previousActionPhase;

            CurrentActionPhase =
                currentActionPhase;

            FailureReason =
                failureReason;
        }

        /// <summary>
        /// Успешный переход внутри одного хода.
        /// </summary>
        public static TurnTransitionResult Success(
            int turnNumber,
            TurnStage previousStage,
            TurnStage currentStage,
            int? previousActionPhase,
            int? currentActionPhase)
        {
            return Success(
                previousTurnNumber:
                    turnNumber,
                currentTurnNumber:
                    turnNumber,
                previousStage:
                    previousStage,
                currentStage:
                    currentStage,
                previousActionPhase:
                    previousActionPhase,
                currentActionPhase:
                    currentActionPhase);
        }

        /// <summary>
        /// Успешный переход, который может
        /// изменить номер хода.
        /// </summary>
        public static TurnTransitionResult Success(
            int previousTurnNumber,
            int currentTurnNumber,
            TurnStage previousStage,
            TurnStage currentStage,
            int? previousActionPhase,
            int? currentActionPhase)
        {
            return new TurnTransitionResult(
                previousTurnNumber,
                currentTurnNumber,
                previousStage,
                currentStage,
                previousActionPhase,
                currentActionPhase,
                TurnTransitionFailureReason.None);
        }

        public static TurnTransitionResult Failure(
            int turnNumber,
            TurnStage stage,
            int? currentActionPhase,
            TurnTransitionFailureReason
                failureReason)
        {
            if (failureReason ==
                TurnTransitionFailureReason.None)
            {
                throw new ArgumentException(
                    "Failure result requires " +
                    "a non-None failure reason.",
                    nameof(failureReason));
            }

            return new TurnTransitionResult(
                previousTurnNumber:
                    turnNumber,
                currentTurnNumber:
                    turnNumber,
                previousStage:
                    stage,
                currentStage:
                    stage,
                previousActionPhase:
                    currentActionPhase,
                currentActionPhase:
                    currentActionPhase,
                failureReason:
                    failureReason);
        }
    }
}