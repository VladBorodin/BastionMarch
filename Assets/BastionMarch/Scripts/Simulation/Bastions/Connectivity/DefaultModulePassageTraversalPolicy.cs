using System;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Базовая политика этапа 11.
    ///
    /// Учитывает направление и текущее состояние перехода.
    /// Не учитывает питание, контроль, пожар и дым.
    /// </summary>
    public sealed class
        DefaultModulePassageTraversalPolicy
        : IModulePassageTraversalPolicy
    {
        public ModulePassageTraversalAssessment Evaluate(
            ModulePassageTraversalContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            ModulePassage passage =
                context.Passage;

            if (!passage.AllowsDirection(
                    context.FromModuleId,
                    context.ToModuleId))
            {
                return
                    ModulePassageTraversalAssessment.Rejected(
                        context,
                        ModulePassageTraversalFailureReason
                            .DirectionNotAllowed);
            }

            switch (passage.State)
            {
                case ModulePassageState.Open:
                    return
                        ModulePassageTraversalAssessment.Allowed(
                            context);

                case ModulePassageState.Closed:
                    return
                        ModulePassageTraversalAssessment.Rejected(
                            context,
                            ModulePassageTraversalFailureReason
                                .PassageClosed);

                case ModulePassageState.Locked:
                    return
                        ModulePassageTraversalAssessment.Rejected(
                            context,
                            ModulePassageTraversalFailureReason
                                .PassageLocked);

                case ModulePassageState.Blocked:
                    return
                        ModulePassageTraversalAssessment.Rejected(
                            context,
                            ModulePassageTraversalFailureReason
                                .PassageBlocked);

                case ModulePassageState.Destroyed:
                    return
                        ModulePassageTraversalAssessment.Rejected(
                            context,
                            ModulePassageTraversalFailureReason
                                .PassageDestroyed);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported passage state: {passage.State}.");
            }
        }
    }
}