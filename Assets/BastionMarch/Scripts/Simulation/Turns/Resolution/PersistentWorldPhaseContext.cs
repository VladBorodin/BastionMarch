
using System;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Неизменяемая временная identity Action Phase
    /// для persistent world processes.
    ///
    /// Не хранит TurnCycle, Bastion или Orders.
    /// </summary>
    public sealed class PersistentWorldPhaseContext
    {
        public int TurnNumber { get; }

        public int ActionPhase { get; }

        public int ActionPhaseCount { get; }

        public bool IsLastActionPhase =>
            ActionPhase == ActionPhaseCount;

        public PersistentWorldPhaseContext(
            int turnNumber,
            int actionPhase,
            int actionPhaseCount)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber));
            }

            if (actionPhaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhaseCount));
            }

            if (actionPhase < 1 ||
                actionPhase > actionPhaseCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase));
            }

            TurnNumber = turnNumber;
            ActionPhase = actionPhase;
            ActionPhaseCount = actionPhaseCount;
        }
    }
}
