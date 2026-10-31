
using System;
using BastionMarch.Simulation.Crew;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Read-only результат выбора профильной работы
    /// для свободной бригады.
    ///
    /// Не подтверждает начало работы.
    /// </summary>
    public sealed class ModuleWorkDefaultAssessment
    {
        public Guid BrigadeId { get; }
        public Guid ModuleId { get; }
        public WorkType? WorkType { get; }

        public ModuleWorkDefaultFailureReason FailureReason { get; }

        public bool IsAllowed =>
            FailureReason == ModuleWorkDefaultFailureReason.None;

        private ModuleWorkDefaultAssessment(
            Guid brigadeId,
            Guid moduleId,
            WorkType? workType,
            ModuleWorkDefaultFailureReason failureReason)
        {
            BrigadeId = brigadeId;
            ModuleId = moduleId;
            WorkType = workType;
            FailureReason = failureReason;
        }

        internal static ModuleWorkDefaultAssessment Allowed(
            Guid brigadeId,
            Guid moduleId,
            WorkType workType)
        {
            return new ModuleWorkDefaultAssessment(
                brigadeId,
                moduleId,
                workType,
                ModuleWorkDefaultFailureReason.None);
        }

        internal static ModuleWorkDefaultAssessment Rejected(
            Guid brigadeId,
            ModuleWorkDefaultFailureReason reason)
        {
            if (reason == ModuleWorkDefaultFailureReason.None)
            {
                throw new ArgumentException(
                    "Rejected assessment requires a failure reason.",
                    nameof(reason));
            }

            return new ModuleWorkDefaultAssessment(
                brigadeId,
                Guid.Empty,
                null,
                reason);
        }
    }
}
