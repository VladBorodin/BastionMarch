
using System;
using BastionMarch.Simulation.Bastions;

namespace BastionMarch.Simulation.Turns.Planning
{
    /// <summary>
    /// Сохраняет полный маршрут бригады и прогресс
    /// его выполнения между Action Phase и ходами.
    ///
    /// Не выполняет перемещения самостоятельно.
    /// Продвижение разрешено только после успешного
    /// исполнения соответствующего MoveBrigadeOrder.
    /// </summary>
    public sealed class BrigadeRoutePlan
    {
        public Guid BrigadeId { get; }

        public ModuleRoute Route { get; }

        public int CompletedStepCount { get; private set; }

        public int TotalStepCount => Route.StepCount;

        public int RemainingStepCount =>
            TotalStepCount - CompletedStepCount;

        public bool IsCompleted =>
            CompletedStepCount == TotalStepCount;

        public bool IsBlocked { get; private set; }

        public bool IsActive =>
            !IsCompleted && !IsBlocked;

        public Guid CurrentModuleId =>
            Route.ModuleIds[CompletedStepCount];

        public Guid TargetModuleId =>
            Route.TargetModuleId;

        public ModuleRouteStep NextStep =>
            IsActive
                ? Route.Steps[CompletedStepCount]
                : null;

        public BrigadeRoutePlan(
            Guid brigadeId,
            ModuleRoute route)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            Route = route ??
                throw new ArgumentNullException(nameof(route));

            if (route.StepCount == 0)
            {
                throw new ArgumentException(
                    "Route must contain at least one step.",
                    nameof(route));
            }

            BrigadeId = brigadeId;
            CompletedStepCount = 0;
        }

        /// <summary>
        /// Подтверждает завершение очередного шага.
        ///
        /// Вызывается координатором только после
        /// успешного world commit.
        /// </summary>
        public bool TryAdvance(
            Guid passageId,
            Guid fromModuleId,
            Guid toModuleId)
        {
            if (!IsActive)
            {
                return false;
            }

            ModuleRouteStep expected = NextStep;

            if (expected.PassageId != passageId ||
                expected.FromModuleId != fromModuleId ||
                expected.ToModuleId != toModuleId)
            {
                return false;
            }

            CompletedStepCount++;

            return true;
        }

        /// <summary>
        /// Останавливает дальнейшее продвижение.
        /// Уже выполненные шаги сохраняются.
        ///
        /// Перепланирование выполняется созданием
        /// нового маршрута от текущего отсека.
        /// </summary>
        public bool TryMarkBlocked()
        {
            if (!IsActive)
            {
                return false;
            }

            IsBlocked = true;

            return true;
        }
    }
}
