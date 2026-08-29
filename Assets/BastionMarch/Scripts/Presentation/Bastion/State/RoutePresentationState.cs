using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый Presentation-снимок
    /// результата поиска маршрута.
    ///
    /// Это временное состояние интерфейса,
    /// а не часть BastionPresentationState.
    /// </summary>
    public sealed class RoutePresentationState
    {
        public Guid SourceModuleId { get; }

        public Guid TargetModuleId { get; }

        public ModuleRouteFailureReason FailureReason
        {
            get;
        }

        public IReadOnlyList<RouteStepPresentationState>
            Steps
        {
            get;
        }

        public IReadOnlyList<
            RouteBlockerPresentationState>
                BlockingAssessments
        {
            get;
        }

        public IReadOnlyList<Guid> ModuleIds
        {
            get;
        }

        public bool IsSuccess =>
            FailureReason ==
            ModuleRouteFailureReason.None;

        public int StepCount =>
            Steps.Count;

        public int RequiredMovementActions =>
            IsSuccess
                ? Steps.Count
                : 0;

        public bool HasBlockingAssessments =>
            BlockingAssessments.Count > 0;

        public RoutePresentationState(
            Guid sourceModuleId,
            Guid targetModuleId,
            ModuleRouteFailureReason failureReason,
            IEnumerable<RouteStepPresentationState>
                steps,
            IEnumerable<RouteBlockerPresentationState>
                blockingAssessments)
        {
            if (sourceModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Source module id cannot be empty.",
                    nameof(sourceModuleId));
            }

            if (targetModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Target module id cannot be empty.",
                    nameof(targetModuleId));
            }

            if (!Enum.IsDefined(
                    typeof(ModuleRouteFailureReason),
                    failureReason))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failureReason));
            }

            if (steps == null)
            {
                throw new ArgumentNullException(
                    nameof(steps));
            }

            if (blockingAssessments == null)
            {
                throw new ArgumentNullException(
                    nameof(blockingAssessments));
            }

            RouteStepPresentationState[] stepArray =
                steps.ToArray();

            RouteBlockerPresentationState[] blockerArray =
                blockingAssessments.ToArray();

            if (stepArray.Any(
                    step => step == null))
            {
                throw new ArgumentException(
                    "Route cannot contain null steps.",
                    nameof(steps));
            }

            if (blockerArray.Any(
                    blocker => blocker == null))
            {
                throw new ArgumentException(
                    "Blocking assessments cannot " +
                    "contain null.",
                    nameof(blockingAssessments));
            }

            bool isSuccess =
                failureReason ==
                ModuleRouteFailureReason.None;

            if (isSuccess)
            {
                if (blockerArray.Length != 0)
                {
                    throw new ArgumentException(
                        "Successful route cannot " +
                        "contain blockers.",
                        nameof(blockingAssessments));
                }

                ValidateSuccessfulRoute(
                    sourceModuleId,
                    targetModuleId,
                    stepArray);
            }
            else
            {
                if (stepArray.Length != 0)
                {
                    throw new ArgumentException(
                        "Failed route cannot " +
                        "contain route steps.",
                        nameof(steps));
                }

                if (failureReason !=
                        ModuleRouteFailureReason
                            .TraversalBlocked &&
                    blockerArray.Length != 0)
                {
                    throw new ArgumentException(
                        "Only TraversalBlocked result " +
                        "may contain blockers.",
                        nameof(blockingAssessments));
                }
            }

            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;
            FailureReason = failureReason;

            Steps =
                new ReadOnlyCollection<
                    RouteStepPresentationState>(
                        stepArray);

            BlockingAssessments =
                new ReadOnlyCollection<
                    RouteBlockerPresentationState>(
                        blockerArray);

            ModuleIds =
                new ReadOnlyCollection<Guid>(
                    BuildModuleIds(
                        sourceModuleId,
                        stepArray,
                        isSuccess));
        }

        private static void ValidateSuccessfulRoute(
            Guid sourceModuleId,
            Guid targetModuleId,
            IReadOnlyList<
                RouteStepPresentationState> steps)
        {
            if (sourceModuleId == targetModuleId)
            {
                if (steps.Count != 0)
                {
                    throw new ArgumentException(
                        "Route to the same module " +
                        "must contain no steps.",
                        nameof(steps));
                }

                return;
            }

            if (steps.Count == 0)
            {
                throw new ArgumentException(
                    "Route between different modules " +
                    "must contain steps.",
                    nameof(steps));
            }

            Guid currentModuleId =
                sourceModuleId;

            foreach (
                RouteStepPresentationState step
                in steps)
            {
                if (step.FromModuleId !=
                    currentModuleId)
                {
                    throw new ArgumentException(
                        "Route steps must form " +
                        "a continuous chain.",
                        nameof(steps));
                }

                currentModuleId =
                    step.ToModuleId;
            }

            if (currentModuleId !=
                targetModuleId)
            {
                throw new ArgumentException(
                    "Route does not end at " +
                    "the target module.",
                    nameof(steps));
            }
        }

        private static Guid[] BuildModuleIds(
            Guid sourceModuleId,
            IReadOnlyList<
                RouteStepPresentationState> steps,
            bool isSuccess)
        {
            if (!isSuccess)
            {
                return Array.Empty<Guid>();
            }

            var moduleIds =
                new List<Guid>(
                    steps.Count + 1)
                {
                    sourceModuleId
                };

            moduleIds.AddRange(
                steps.Select(
                    step =>
                        step.ToModuleId));

            return moduleIds.ToArray();
        }
    }
}