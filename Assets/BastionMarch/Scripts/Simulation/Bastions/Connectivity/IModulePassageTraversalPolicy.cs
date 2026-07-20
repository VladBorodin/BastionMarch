namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Определяет возможность прохода через переход.
    ///
    /// Разные игровые режимы смогут использовать
    /// разные или составные политики.
    /// </summary>
    public interface IModulePassageTraversalPolicy
    {
        ModulePassageTraversalAssessment Evaluate(
            ModulePassageTraversalContext context);
    }
}