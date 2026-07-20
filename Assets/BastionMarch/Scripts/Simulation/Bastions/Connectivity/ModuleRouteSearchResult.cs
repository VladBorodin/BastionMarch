using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    public sealed class ModuleRouteSearchResult
    {
        public ModuleRoute Route { get; }

        public ModuleRouteFailureReason FailureReason
        {
            get;
        }

        /// <summary>
        /// Непосредственные препятствия на границе
        /// области, доступной из исходного модуля.
        ///
        /// Заполняется для TraversalBlocked.
        /// </summary>
        public IReadOnlyList<
            ModulePassageTraversalAssessment>
                BlockingAssessments
        {
            get;
        }

        public bool IsSuccess =>
            Route != null &&
            FailureReason ==
                ModuleRouteFailureReason.None;

        private ModuleRouteSearchResult(
            ModuleRoute route,
            ModuleRouteFailureReason failureReason,
            IEnumerable<
                ModulePassageTraversalAssessment>
                    blockingAssessments)
        {
            if (blockingAssessments == null)
            {
                throw new ArgumentNullException(
                    nameof(blockingAssessments));
            }

            ModulePassageTraversalAssessment[] blockers =
                blockingAssessments
                    .Where(item => item != null)
                    .OrderBy(item =>
                        item.FailureReason)
                    .ThenBy(item =>
                        item.FromModuleId)
                    .ThenBy(item =>
                        item.ToModuleId)
                    .ThenBy(item =>
                        item.PassageId)
                    .ToArray();

            Route = route;
            FailureReason = failureReason;

            BlockingAssessments =
                new ReadOnlyCollection<
                    ModulePassageTraversalAssessment>(
                        blockers);
        }

        public static ModuleRouteSearchResult Success(
            ModuleRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(
                    nameof(route));
            }

            return new ModuleRouteSearchResult(
                route,
                ModuleRouteFailureReason.None,
                Array.Empty<
                    ModulePassageTraversalAssessment>());
        }

        public static ModuleRouteSearchResult Failure(
            ModuleRouteFailureReason failureReason)
        {
            return Failure(
                failureReason,
                Array.Empty<
                    ModulePassageTraversalAssessment>());
        }

        public static ModuleRouteSearchResult Failure(
            ModuleRouteFailureReason failureReason,
            IEnumerable<
                ModulePassageTraversalAssessment>
                    blockingAssessments)
        {
            if (failureReason ==
                ModuleRouteFailureReason.None)
            {
                throw new ArgumentException(
                    "Failed route search must contain a reason.",
                    nameof(failureReason));
            }

            return new ModuleRouteSearchResult(
                route: null,
                failureReason: failureReason,
                blockingAssessments:
                    blockingAssessments);
        }
    }
}