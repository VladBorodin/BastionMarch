
namespace BastionMarch.Simulation.Turns.Activities
{
    public enum ModuleWorkDefaultFailureReason
    {
        None,
        BrigadeNotFound,
        BrigadeDisbanded,
        BrigadeNotDeployed,
        BrigadeAlreadyWorking,
        ModuleNotFound,
        NoMatchingWork,
        AmbiguousWork
    }
}
