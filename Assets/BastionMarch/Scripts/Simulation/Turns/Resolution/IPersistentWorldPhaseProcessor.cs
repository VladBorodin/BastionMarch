
namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Точка расширения для обработки длительно
    /// существующих процессов игрового мира.
    ///
    /// Вызывается после commit основных intents
    /// и до завершения Action Phase.
    ///
    /// Конкретная реализация владеет доступом
    /// к соответствующей части world state.
    /// </summary>
    public interface IPersistentWorldPhaseProcessor
    {
        PersistentWorldPhaseResult AdvancePhase(
            PersistentWorldPhaseContext context);
    }
}
