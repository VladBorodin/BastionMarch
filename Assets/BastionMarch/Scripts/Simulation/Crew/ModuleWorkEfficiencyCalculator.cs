using System;
using System.Collections.Generic;
using System.Linq;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Crew
{
    public static class ModuleWorkEfficiencyCalculator
    {
        private const double MinimumOvercrowdingMultiplier = 0.50;

        private const double OvercrowdingPenaltyPerPerson = 0.05;

        public static ModuleWorkEfficiencyAssessment Calculate(
            ModuleInstance module,
            IEnumerable<Brigade> workingBrigades,
            int totalOccupyingPersonnel,
            BrigadeWorkProfileCatalog profileCatalog)
        {
            if (totalOccupyingPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalOccupyingPersonnel));
            }

            if (workingBrigades == null)
            {
                throw new ArgumentNullException(
                    nameof(workingBrigades));
            }

            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (profileCatalog == null)
            {
                throw new ArgumentNullException(
                    nameof(profileCatalog));
            }

            List<Brigade> workers =
                workingBrigades
                    .Where(brigade =>
                        brigade != null &&
                        !brigade.IsDisbanded)
                    .ToList();

            int totalWorkingPersonnel =
                workers.Sum(
                    brigade => brigade.CurrentPersonnel);

            if (totalWorkingPersonnel >
                totalOccupyingPersonnel)
            {
                throw new ArgumentException(
                    "Working personnel cannot exceed occupying personnel.",
                    nameof(totalOccupyingPersonnel));
            }

            CrewRequirement crewRequirement =
                module.Definition.CrewRequirement;

            double overcrowdingMultiplier =
                CalculateOvercrowdingMultiplier(
                    totalOccupyingPersonnel,
                    crewRequirement.MaximumUsefulPersonnel);

            var workAssessments =
                new List<ModuleWorkTypeAssessment>();

            foreach (
                ModuleWorkRequirement requirement
                in crewRequirement.WorkRequirements)
            {
                double effectivePersonnel = 0;

                foreach (Brigade brigade in workers)
                {
                    BrigadeWorkProfile profile =
                        profileCatalog.GetRequired(
                            brigade.Type);

                    int affinityPercent =
                        profile.GetEfficiencyPercent(
                            requirement.WorkType);

                    effectivePersonnel +=
                        CalculateBrigadeContribution(
                            brigade,
                            affinityPercent);
                }

                workAssessments.Add(
                    new ModuleWorkTypeAssessment(
                        requirement.WorkType,
                        effectivePersonnel,
                        requirement.MinimumPersonnel,
                        requirement.OptimalPersonnel,
                        requirement.MaximumUsefulPersonnel));
            }

            double overallEfficiency =
                CalculateOverallEfficiency(
                    workAssessments);

            overallEfficiency *=
                overcrowdingMultiplier;

            return new ModuleWorkEfficiencyAssessment(
                moduleId: module.Id,
                workingBrigadeCount: workers.Count,
                totalWorkingPersonnel:
                    totalWorkingPersonnel,
                totalOccupyingPersonnel:
                    totalOccupyingPersonnel,
                overcrowdingMultiplier:
                    overcrowdingMultiplier,
                overallEfficiencyRatio:
                    overallEfficiency,
                byWorkType:
                    workAssessments);
        }

        private static double CalculateBrigadeContribution(
            Brigade brigade,
            int affinityPercent)
        {
            double affinityMultiplier =
                affinityPercent / 100.0;

            // Опыт: от 80% до 120%.
            double experienceMultiplier =
                0.80 +
                brigade.Experience * 0.004;

            // Мораль: от 70% до 100%.
            double moraleMultiplier =
                0.70 +
                brigade.Morale * 0.003;

            // Усталость: от 100% до 60%.
            double fatigueMultiplier =
                1.00 -
                brigade.Fatigue * 0.004;

            return brigade.CurrentPersonnel
                   * affinityMultiplier
                   * experienceMultiplier
                   * moraleMultiplier
                   * fatigueMultiplier;
        }

        private static double
            CalculateOvercrowdingMultiplier(
                int totalPersonnel,
                int maximumUsefulPersonnel)
        {
            if (maximumUsefulPersonnel <= 0 ||
                totalPersonnel <= maximumUsefulPersonnel)
            {
                return 1.0;
            }

            int excessPersonnel =
                totalPersonnel -
                maximumUsefulPersonnel;

            return Math.Max(
                MinimumOvercrowdingMultiplier,
                1.0 -
                excessPersonnel *
                OvercrowdingPenaltyPerPerson);
        }

        private static double CalculateOverallEfficiency(
            IReadOnlyCollection<ModuleWorkTypeAssessment>
                assessments)
        {
            if (assessments.Count == 0)
            {
                return 1.0;
            }

            double totalWeight = 0;
            double weightedEfficiency = 0;

            foreach (
                ModuleWorkTypeAssessment assessment
                in assessments)
            {
                int weight =
                    Math.Max(
                        1,
                        assessment.OptimalPersonnel);

                totalWeight += weight;

                weightedEfficiency +=
                    assessment.EfficiencyRatio *
                    weight;
            }

            double result =
                weightedEfficiency / totalWeight;

            ModuleWorkTypeAssessment[] unmetMinimums =
                assessments
                    .Where(item => !item.IsMinimumMet)
                    .ToArray();

            if (unmetMinimums.Length > 0)
            {
                double bottleneck =
                    unmetMinimums.Min(
                        item => item.EfficiencyRatio);

                result =
                    Math.Min(result, bottleneck);
            }

            return result;
        }
    }
}