using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;

namespace BastionMarch.Simulation.Turns
{
    /// <summary>
    /// Создаёт детерминированный snapshot
    /// активных бригад для нового хода.
    ///
    /// Фабрика читает Bastion,
    /// но не изменяет его.
    /// </summary>
    public static class TurnBrigadeParticipantFactory
    {
        public static IReadOnlyList<
            TurnBrigadeParticipant> CaptureActive(
                Bastion bastion)
        {
            if (bastion == null)
            {
                throw new ArgumentNullException(
                    nameof(bastion));
            }

            TurnBrigadeParticipant[] participants =
                bastion.Brigades
                    .Where(brigade =>
                        IsActive(
                            bastion,
                            brigade))
                    .OrderBy(brigade =>
                        brigade.Number)
                    .ThenBy(brigade =>
                        brigade.Id)
                    .Select(brigade =>
                        new TurnBrigadeParticipant(
                            brigade.Id,
                            brigade.Number))
                    .ToArray();

            return new ReadOnlyCollection<
                TurnBrigadeParticipant>(
                    participants);
        }

        private static bool IsActive(
            Bastion bastion,
            Brigade brigade)
        {
            if (brigade == null ||
                brigade.IsDisbanded)
            {
                return false;
            }

            bool stateFound =
                bastion.TryGetBrigadeOperationalState(
                    brigade.Id,
                    out var operationalState);

            return stateFound &&
                   operationalState.IsDeployed;
        }
    }
}