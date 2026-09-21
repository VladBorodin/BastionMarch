using System;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Чистое Simulation-ядро временного цикла игры.
    ///
    /// На этапе 12.1 хранит только номер хода
    /// и его крупную стадию.
    ///
    /// Конкретные Action Phase, планы и приказы
    /// добавляются последующими подэтапами.
    /// </summary>
    public sealed class TurnCycle
    {
        public const int FirstTurnNumber = 1;

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

        public TurnCycle()
            : this(
                FirstTurnNumber)
        {
        }

        /// <summary>
        /// Позволяет восстановить цикл с известного
        /// номера хода без введения save/load логики.
        ///
        /// Любой созданный таким способом цикл
        /// начинает работу со стадии Planning.
        /// </summary>
        public TurnCycle(
            int turnNumber)
        {
            if (turnNumber <
                FirstTurnNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber),
                    turnNumber,
                    "Turn number must be positive.");
            }

            TurnNumber =
                turnNumber;

            Stage =
                TurnStage.Planning;
        }
    }
}