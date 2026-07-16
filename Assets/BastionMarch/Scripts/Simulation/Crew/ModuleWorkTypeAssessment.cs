using System;

namespace BastionMarch.Simulation.Crew
{
    public sealed class ModuleWorkTypeAssessment
    {
        public WorkType WorkType { get; }

        public double EffectivePersonnel { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        /// <summary>
        /// Отношение эффективного персонала к оптимальному.
        ///
        /// Может превышать 1, но ограничивается значением 1.25.
        /// </summary>
        public double EfficiencyRatio { get; }

        public int EfficiencyPercent =>
            (int)Math.Round(EfficiencyRatio * 100);

        public bool IsMinimumMet =>
            EffectivePersonnel + 0.0001 >= MinimumPersonnel;

        public ModuleWorkTypeAssessment(
            WorkType workType,
            double effectivePersonnel,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumUsefulPersonnel)
        {
            if (effectivePersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectivePersonnel));
            }

            WorkType = workType;
            EffectivePersonnel = effectivePersonnel;

            MinimumPersonnel = minimumPersonnel;
            OptimalPersonnel = optimalPersonnel;
            MaximumUsefulPersonnel = maximumUsefulPersonnel;

            if (optimalPersonnel == 0)
            {
                EfficiencyRatio = 1.0;
            }
            else
            {
                EfficiencyRatio =
                    Math.Min(
                        1.25,
                        effectivePersonnel /
                        optimalPersonnel);
            }
        }
    }
}