using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Редактируемый план одного игрового хода.
    ///
    /// На этапе 13.1 содержит только
    /// временную identity хода и immutable snapshot
    /// его участников.
    ///
    /// Orders и reservations добавляются
    /// последующими подэтапами Stage 13.
    /// </summary>
    public sealed class TurnPlanDraft
    {
        public int TurnNumber
        {
            get;
        }

        public int ActionPhaseCount
        {
            get;
        }

        public IReadOnlyList<
            TurnBrigadeParticipant>
                Participants
        {
            get;
        }

        public int ParticipantCount =>
            Participants.Count;

        public TurnPlanDraft(
            int turnNumber,
            int actionPhaseCount,
            IEnumerable<
                TurnBrigadeParticipant>
                    participants)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber),
                    turnNumber,
                    "Turn number must be positive.");
            }

            if (actionPhaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhaseCount),
                    actionPhaseCount,
                    "Action phase count must be positive.");
            }

            if (participants == null)
            {
                throw new ArgumentNullException(
                    nameof(participants));
            }

            TurnBrigadeParticipant[] participantArray =
                participants.ToArray();

            if (participantArray.Any(
                    participant =>
                        participant == null))
            {
                throw new ArgumentException(
                    "Participant collection " +
                    "cannot contain null.",
                    nameof(participants));
            }

            bool containsDuplicateIds =
                participantArray
                    .GroupBy(participant =>
                        participant.BrigadeId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateIds)
            {
                throw new ArgumentException(
                    "Participant collection " +
                    "contains duplicate brigade ids.",
                    nameof(participants));
            }

            TurnBrigadeParticipant[]
                orderedParticipants =
                    participantArray
                        .OrderBy(participant =>
                            participant.BrigadeNumber)
                        .ThenBy(participant =>
                            participant.BrigadeId)
                        .ToArray();

            TurnNumber =
                turnNumber;

            ActionPhaseCount =
                actionPhaseCount;

            Participants =
                new ReadOnlyCollection<
                    TurnBrigadeParticipant>(
                        orderedParticipants);
        }
    }
}