using System;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Неизменяемая запись о бригаде,
    /// участвующей в текущем ходе.
    ///
    /// Не хранит Brigade и не является
    /// снимком всего её состояния.
    /// </summary>
    public sealed class TurnBrigadeParticipant
    {
        public Guid BrigadeId
        {
            get;
        }

        public int BrigadeNumber
        {
            get;
        }

        public TurnBrigadeParticipant(
            Guid brigadeId,
            int brigadeNumber)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            if (brigadeNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(brigadeNumber),
                    brigadeNumber,
                    "Brigade number must be positive.");
            }

            BrigadeId =
                brigadeId;

            BrigadeNumber =
                brigadeNumber;
        }
    }
}