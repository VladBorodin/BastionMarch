using System;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Неизменяемый результат перехода
    /// временного состояния TurnCycle.
    ///
    /// Хранит состояние до и после попытки,
    /// поэтому пригоден для тестов,
    /// диагностики и будущего журнала хода.
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

        public int TurnNumber
        {
            get;
        }

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
            int turnNumber,
            TurnStage previousStage,
            TurnStage currentStage,
            int? previousActionPhase,
            int? currentActionPhase,
            TurnTransitionFailureReason
                failureReason)
        {
            if (turnNumber <
                TurnCycle.FirstTurnNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber));
            }

            TurnNumber =
                turnNumber;

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

        public static TurnTransitionResult Success(
            int turnNumber,
            TurnStage previousStage,
            TurnStage currentStage,
            int? previousActionPhase,
            int? currentActionPhase)
        {
            return new TurnTransitionResult(
                turnNumber,
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
                turnNumber,
                stage,
                stage,
                currentActionPhase,
                currentActionPhase,
                failureReason);
        }
    }
}