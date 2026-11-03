
namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Подготавливает read-only результат для
    /// persistent world processes текущей Action Phase.
    ///
    /// PreparePhase не должен:
    /// - изменять Bastion или другое world state;
    /// - продвигать TurnCycle;
    /// - изменять OrderExecutionRegistry;
    /// - выполнять необратимые игровые эффекты.
    ///
    /// Реальный world commit будет отдельным шагом,
    /// когда появятся persistent игровые процессы.
    /// </summary>
    public interface IPersistentWorldPhaseProcessor
    {
        PersistentWorldPhaseResult PreparePhase(
            PersistentWorldPhaseContext context);
    }
}
