using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Копирует результат маршрутизации Simulation
    /// в независимое состояние Presentation.
    /// </summary>
    public static class RoutePresentationStateFactory
    {
        public static RoutePresentationState
            CaptureSearchResult(
                Guid sourceModuleId,
                Guid targetModuleId,
                ModuleRouteSearchResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            RouteStepPresentationState[] steps;

            if (result.IsSuccess)
            {
                ModuleRoute route =
                    result.Route;

                if (route.SourceModuleId !=
                        sourceModuleId ||
                    route.TargetModuleId !=
                        targetModuleId)
                {
                    throw new ArgumentException(
                        "Route endpoints do not match " +
                        "the requested endpoints.",
                        nameof(result));
                }

                steps =
                    route.Steps
                        .Select(CaptureStep)
                        .ToArray();
            }
            else
            {
                steps =
                    Array.Empty<
                        RouteStepPresentationState>();
            }

            RouteBlockerPresentationState[] blockers =
                result.BlockingAssessments
                    .Select(CaptureBlocker)
                    .ToArray();

            return new RoutePresentationState(
                sourceModuleId:
                    sourceModuleId,
                targetModuleId:
                    targetModuleId,
                failureReason:
                    result.FailureReason,
                steps:
                    steps,
                blockingAssessments:
                    blockers);
        }

        public static RouteStepPresentationState
            CaptureStep(
                ModuleRouteStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(
                    nameof(step));
            }

            return new RouteStepPresentationState(
                passageId:
                    step.PassageId,
                fromModuleId:
                    step.FromModuleId,
                toModuleId:
                    step.ToModuleId,
                passageType:
                    step.PassageType,
                boundary:
                    step.Boundary);
        }

        public static RouteBlockerPresentationState
            CaptureBlocker(
                ModulePassageTraversalAssessment
                    assessment)
        {
            if (assessment == null)
            {
                throw new ArgumentNullException(
                    nameof(assessment));
            }

            if (assessment.IsAllowed)
            {
                throw new ArgumentException(
                    "Allowed traversal cannot be " +
                    "captured as a blocker.",
                    nameof(assessment));
            }

            return new RouteBlockerPresentationState(
                passageId:
                    assessment.PassageId,
                fromModuleId:
                    assessment.FromModuleId,
                toModuleId:
                    assessment.ToModuleId,
                failureReason:
                    assessment.FailureReason);
        }
    }
}