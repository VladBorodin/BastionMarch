using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Детерминированный маршрут между двумя модулями.
    ///
    /// Каждый шаг соответствует одному непосредственному
    /// переходу через ModulePassage.
    /// </summary>
    public sealed class ModuleRoute
    {
        public Guid SourceModuleId { get; }

        public Guid TargetModuleId { get; }

        public IReadOnlyList<ModuleRouteStep> Steps { get; }

        /// <summary>
        /// Последовательность посещаемых модулей,
        /// включая начальный и конечный.
        /// </summary>
        public IReadOnlyList<Guid> ModuleIds { get; }

        public int StepCount =>
            Steps.Count;

        /// <summary>
        /// Текущая оценка стоимости обычного перемещения.
        ///
        /// Один непосредственный переход между отсеками
        /// в будущем расходует одно действие.
        /// </summary>
        public int RequiredMovementActions =>
            Steps.Count;

        internal ModuleRoute(
            Guid sourceModuleId,
            Guid targetModuleId,
            IEnumerable<ModuleRouteStep> steps)
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

            if (steps == null)
            {
                throw new ArgumentNullException(
                    nameof(steps));
            }

            ModuleRouteStep[] stepArray =
                steps.ToArray();

            if (sourceModuleId == targetModuleId)
            {
                if (stepArray.Length != 0)
                {
                    throw new ArgumentException(
                        "A route to the same module must be empty.",
                        nameof(steps));
                }
            }
            else if (stepArray.Length == 0)
            {
                throw new ArgumentException(
                    "A route between different modules must contain steps.",
                    nameof(steps));
            }

            Guid currentModuleId =
                sourceModuleId;

            foreach (ModuleRouteStep step in stepArray)
            {
                if (step == null)
                {
                    throw new ArgumentException(
                        "Route cannot contain null steps.",
                        nameof(steps));
                }

                if (step.FromModuleId !=
                    currentModuleId)
                {
                    throw new ArgumentException(
                        "Route steps must form a continuous chain.",
                        nameof(steps));
                }

                currentModuleId =
                    step.ToModuleId;
            }

            if (currentModuleId != targetModuleId)
            {
                throw new ArgumentException(
                    "Route does not end at the requested target module.",
                    nameof(steps));
            }

            SourceModuleId = sourceModuleId;
            TargetModuleId = targetModuleId;

            Steps =
                new ReadOnlyCollection<ModuleRouteStep>(
                    stepArray);

            var moduleIds =
                new List<Guid>(
                    stepArray.Length + 1)
                {
                    sourceModuleId
                };

            moduleIds.AddRange(
                stepArray.Select(
                    step => step.ToModuleId));

            ModuleIds =
                new ReadOnlyCollection<Guid>(
                    moduleIds);
        }
    }
}