using System;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Power;

namespace BastionMarch.Simulation.Modules
{
    public static class ModuleOperationalEfficiencyCalculator
    {
        private const double MaximumOperationalEfficiency = 1.25;

        public static ModuleOperationalAssessment Calculate(
            ModuleInstance module,
            ModuleWorkEfficiencyAssessment workEfficiency)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (workEfficiency == null)
            {
                throw new ArgumentNullException(
                    nameof(workEfficiency));
            }

            ModuleOperationalIssue issues =
                ResolveIssues(
                    module,
                    workEfficiency);

            double technicalMultiplier =
                ResolveTechnicalEfficiencyMultiplier(
                    module.TechnicalState);

            bool canPerformActiveWork =
                !HasBlockingIssue(issues);

            double overallEfficiency =
                canPerformActiveWork
                    ? Math.Min(
                        MaximumOperationalEfficiency,
                        workEfficiency.OverallEfficiencyRatio *
                        technicalMultiplier)
                    : 0;

            return new ModuleOperationalAssessment(
                moduleId: module.Id,
                technicalState: module.TechnicalState,
                controlState: module.ControlState,
                effectivePowerMode: module.EffectivePowerMode,
                issues: issues,
                workEfficiency: workEfficiency,
                technicalEfficiencyMultiplier:
                    technicalMultiplier,
                overallEfficiencyRatio:
                    overallEfficiency,
                canPerformActiveWork:
                    canPerformActiveWork);
        }

        private static ModuleOperationalIssue ResolveIssues(
            ModuleInstance module,
            ModuleWorkEfficiencyAssessment workEfficiency)
        {
            ModuleOperationalIssue issues =
                ModuleOperationalIssue.None;

            switch (module.TechnicalState)
            {
                case ModuleTechnicalState.Damaged:
                    issues |=
                        ModuleOperationalIssue.Damaged;
                    break;

                case ModuleTechnicalState.Critical:
                    issues |=
                        ModuleOperationalIssue.CriticalDamage;
                    break;

                case ModuleTechnicalState.Destroyed:
                    issues |=
                        ModuleOperationalIssue.Destroyed;
                    break;
            }

            if (module.ControlState !=
                ModuleControlState.Friendly)
            {
                issues |=
                    ModuleOperationalIssue
                        .NotFriendlyControlled;
            }

            if (module.EffectivePowerMode !=
                ModulePowerMode.Active)
            {
                issues |=
                    ModuleOperationalIssue
                        .InactivePowerMode;
            }

            if (!workEfficiency.CanOperate)
            {
                issues |=
                    ModuleOperationalIssue
                        .InsufficientQualifiedPersonnel;
            }

            if (workEfficiency.OvercrowdingMultiplier < 1.0)
            {
                issues |=
                    ModuleOperationalIssue.Overcrowded;
            }

            return issues;
        }

        private static double
            ResolveTechnicalEfficiencyMultiplier(
                ModuleTechnicalState technicalState)
        {
            switch (technicalState)
            {
                case ModuleTechnicalState.Operational:
                    return 1.0;

                case ModuleTechnicalState.Damaged:
                    return 0.75;

                case ModuleTechnicalState.Critical:
                    return 0.40;

                case ModuleTechnicalState.Destroyed:
                    return 0;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(technicalState));
            }
        }

        private static bool HasBlockingIssue(
            ModuleOperationalIssue issues)
        {
            const ModuleOperationalIssue blockingIssues =
                ModuleOperationalIssue.Destroyed |
                ModuleOperationalIssue.NotFriendlyControlled |
                ModuleOperationalIssue.InactivePowerMode |
                ModuleOperationalIssue
                    .InsufficientQualifiedPersonnel;

            return (issues & blockingIssues) != 0;
        }
    }
}