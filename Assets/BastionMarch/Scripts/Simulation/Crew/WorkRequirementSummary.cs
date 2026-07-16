namespace BastionMarch.Simulation.Crew
{
    public sealed class WorkRequirementSummary
    {
        public WorkType WorkType { get; }

        public int MinimumPersonnel { get; }

        public int OptimalPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        public WorkRequirementSummary(
            WorkType workType,
            int minimumPersonnel,
            int optimalPersonnel,
            int maximumUsefulPersonnel)
        {
            WorkType = workType;
            MinimumPersonnel = minimumPersonnel;
            OptimalPersonnel = optimalPersonnel;
            MaximumUsefulPersonnel = maximumUsefulPersonnel;
        }
    }
}