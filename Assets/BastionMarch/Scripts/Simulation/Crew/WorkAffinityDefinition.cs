using System;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Пригодность конкретного типа бригады к одному виду работы.
    /// </summary>
    public sealed class WorkAffinityDefinition
    {
        public WorkType WorkType { get; }

        /// <summary>
        /// Эффективность специализации от 0 до 100 процентов.
        ///
        /// Опыт, мораль и усталость рассчитываются отдельно.
        /// </summary>
        public int EfficiencyPercent { get; }

        public WorkAffinityDefinition(
            WorkType workType,
            int efficiencyPercent)
        {
            if (!Enum.IsDefined(typeof(WorkType), workType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workType));
            }

            if (efficiencyPercent < 0 ||
                efficiencyPercent > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(efficiencyPercent),
                    "Efficiency must be between 0 and 100.");
            }

            WorkType = workType;
            EfficiencyPercent = efficiencyPercent;
        }
    }
}