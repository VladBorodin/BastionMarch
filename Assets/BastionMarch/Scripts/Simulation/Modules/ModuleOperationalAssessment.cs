using System;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Power;

namespace BastionMarch.Simulation.Modules
{
    /// <summary>
    /// Оценка способности модуля выполнять активную работу
    /// в текущем состоянии.
    /// </summary>
    public sealed class ModuleOperationalAssessment
    {
        public Guid ModuleId { get; }

        public ModuleTechnicalState TechnicalState { get; }

        public ModuleControlState ControlState { get; }

        public ModulePowerMode EffectivePowerMode { get; }

        public ModuleOperationalIssue Issues { get; }

        public ModuleWorkEfficiencyAssessment WorkEfficiency
        {
            get;
        }

        /// <summary>
        /// Множитель от текущего технического состояния.
        /// </summary>
        public double TechnicalEfficiencyMultiplier { get; }

        /// <summary>
        /// Итоговая эффективность активной работы.
        ///
        /// При наличии блокирующей причины равна нулю.
        /// </summary>
        public double OverallEfficiencyRatio { get; }

        public int OverallEfficiencyPercent =>
            (int)Math.Round(OverallEfficiencyRatio * 100);

        public bool CanPerformActiveWork { get; }

        public ModuleOperationalAssessment(
            Guid moduleId,
            ModuleTechnicalState technicalState,
            ModuleControlState controlState,
            ModulePowerMode effectivePowerMode,
            ModuleOperationalIssue issues,
            ModuleWorkEfficiencyAssessment workEfficiency,
            double technicalEfficiencyMultiplier,
            double overallEfficiencyRatio,
            bool canPerformActiveWork)
        {
            if (moduleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Module id cannot be empty.",
                    nameof(moduleId));
            }

            if (workEfficiency == null)
            {
                throw new ArgumentNullException(
                    nameof(workEfficiency));
            }

            if (technicalEfficiencyMultiplier < 0 ||
                technicalEfficiencyMultiplier > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(technicalEfficiencyMultiplier));
            }

            if (overallEfficiencyRatio < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(overallEfficiencyRatio));
            }

            ModuleId = moduleId;
            TechnicalState = technicalState;
            ControlState = controlState;
            EffectivePowerMode = effectivePowerMode;
            Issues = issues;
            WorkEfficiency = workEfficiency;

            TechnicalEfficiencyMultiplier =
                technicalEfficiencyMultiplier;

            OverallEfficiencyRatio =
                overallEfficiencyRatio;

            CanPerformActiveWork =
                canPerformActiveWork;
        }

        public bool HasIssue(
            ModuleOperationalIssue issue)
        {
            return (Issues & issue) == issue;
        }
    }
}