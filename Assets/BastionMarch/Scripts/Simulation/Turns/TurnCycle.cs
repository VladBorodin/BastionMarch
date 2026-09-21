using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Чистое Simulation-ядро временного цикла игры.
    ///
    /// Хранит номер хода, крупную стадию,
    /// конфигурацию Action Phase и immutable snapshot
    /// участников текущего хода.
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

        public int ActionPhaseCount
        {
            get;
            private set;
        }

        public int? CurrentActionPhase
        {
            get;
            private set;
        }

        public bool HasActiveActionPhase =>
            CurrentActionPhase.HasValue;

        public bool IsPlanningConfirmed =>
            Stage != TurnStage.Planning;

        public IReadOnlyList<
            TurnBrigadeParticipant>
                ActiveBrigades
        {
            get;
            private set;
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

            IReadOnlyList<
                TurnBrigadeParticipant>
                    participantSnapshot =
                        CreateParticipantSnapshot(
                            activeBrigades);

            TurnNumber =
                turnNumber;

            Stage =
                TurnStage.Planning;

            ActionPhaseCount =
                actionPhaseCount;

            CurrentActionPhase =
                null;

            ActiveBrigades =
                participantSnapshot;
        }

        /// <summary>
        /// Подтверждает Planning текущего хода
        /// и начинает первую Action Phase.
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
        /// Последняя Action Phase переводит
        /// цикл в TurnEnd.
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

        /// <summary>
        /// Завершает границу TurnEnd и начинает
        /// Planning следующего хода.
        ///
        /// Snapshot участников передаётся извне,
        /// поэтому TurnCycle не зависит от Bastion.
        /// </summary>
        public TurnTransitionResult
            TryBeginNextTurn(
                IEnumerable<
                    TurnBrigadeParticipant>
                        activeBrigades)
        {
            IReadOnlyList<
                TurnBrigadeParticipant>
                    nextParticipants =
                        CreateParticipantSnapshot(
                            activeBrigades);

            if (Stage !=
                TurnStage.TurnEnd)
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
                            .TurnEndNotReached);
            }

            int previousTurnNumber =
                TurnNumber;

            TurnStage previousStage =
                Stage;

            int? previousActionPhase =
                CurrentActionPhase;

            TurnNumber =
                checked(
                    TurnNumber + 1);

            Stage =
                TurnStage.Planning;

            CurrentActionPhase =
                null;

            ActiveBrigades =
                nextParticipants;

            return TurnTransitionResult.Success(
                previousTurnNumber:
                    previousTurnNumber,
                currentTurnNumber:
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

        private static IReadOnlyList<
            TurnBrigadeParticipant>
                CreateParticipantSnapshot(
                    IEnumerable<
                        TurnBrigadeParticipant>
                            activeBrigades)
        {
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

            TurnBrigadeParticipant[]
                orderedBrigades =
                    brigadeArray
                        .OrderBy(brigade =>
                            brigade.BrigadeNumber)
                        .ThenBy(brigade =>
                            brigade.BrigadeId)
                        .ToArray();

            return new ReadOnlyCollection<
                TurnBrigadeParticipant>(
                    orderedBrigades);
        }
    }
}