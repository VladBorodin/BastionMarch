
using System;
using BastionMarch.Simulation.Turns.Activities;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Намерение выполнить один tick текущей
    /// деятельности бригады в Action Phase.
    ///
    /// Не выполняет Activity и не изменяет мир.
    /// </summary>
    public sealed class BrigadeActivityPhaseIntent
    {
        public IBrigadeActivity Activity
        {
            get;
        }

        public Guid BrigadeId =>
            Activity.BrigadeId;

        public int ActionPhase
        {
            get;
        }

        public BrigadeActivityPhaseIntent(
            IBrigadeActivity activity,
            int actionPhase)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(
                    nameof(activity));
            }

            if (activity.BrigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Activity brigade id cannot be empty.",
                    nameof(activity));
            }

            if (actionPhase < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPhase),
                    actionPhase,
                    "Action phase must be positive.");
            }

            Activity = activity;
            ActionPhase = actionPhase;
        }
    }
}
