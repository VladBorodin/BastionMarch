using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Явная граница conflict resolution
    /// между read-only assessment и commit.
    ///
    /// На текущем этапе дополнительных
    /// меж-intent конфликтов ещё нет:
    ///
    /// passed assessment -> Approved
    /// failed assessment -> RejectedByAssessment
    ///
    /// Будущие conflict rules добавляются здесь
    /// без превращения порядка foreach в initiative.
    /// </summary>
    public static class OrderPhaseConflictResolver
    {
        public static IReadOnlyList<
            ResolvedOrderPhaseIntent>
                Resolve(
                    IEnumerable<
                        AssessedOrderPhaseIntent>
                            assessedIntents)
        {
            if (assessedIntents == null)
            {
                throw new ArgumentNullException(
                    nameof(assessedIntents));
            }

            AssessedOrderPhaseIntent[]
                candidateArray =
                    assessedIntents.ToArray();

            if (candidateArray.Any(
                    candidate =>
                        candidate == null))
            {
                throw new ArgumentException(
                    "Assessed intent collection " +
                    "cannot contain null.",
                    nameof(assessedIntents));
            }

            bool containsDuplicateOrderPhases =
                candidateArray
                    .GroupBy(candidate =>
                        new
                        {
                            candidate.OrderId,
                            candidate.ActionPhase
                        })
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateOrderPhases)
            {
                throw new ArgumentException(
                    "Assessed intent collection " +
                    "contains duplicate order-phase " +
                    "candidates.",
                    nameof(assessedIntents));
            }

            AssessedOrderPhaseIntent[]
                orderedCandidates =
                    candidateArray
                        .OrderBy(candidate =>
                            candidate.ActionPhase)
                        .ThenBy(candidate =>
                            candidate.OrderId)
                        .ToArray();

            var resolutions =
                new List<
                    ResolvedOrderPhaseIntent>(
                        orderedCandidates.Length);

            foreach (AssessedOrderPhaseIntent
                     candidate in orderedCandidates)
            {
                if (candidate.PassedAssessment)
                {
                    resolutions.Add(
                        ResolvedOrderPhaseIntent
                            .Approved(
                                candidate));
                }
                else
                {
                    resolutions.Add(
                        ResolvedOrderPhaseIntent
                            .RejectedByAssessment(
                                candidate));
                }
            }

            return new ReadOnlyCollection<
                ResolvedOrderPhaseIntent>(
                    resolutions);
        }
    }
}