
using System;
using System.Linq;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Turns.Activities
{
    /// <summary>
    /// Определяет единственную очевидную профильную
    /// работу для idle бригады.
    ///
    /// Не запускает работу и не изменяет Bastion.
    /// </summary>
    public static class ModuleWorkDefaultPolicy
    {
        public const int PrimaryAffinityPercent = 100;

        public static ModuleWorkDefaultAssessment Assess(
            Bastion bastion,
            Guid brigadeId,
            BrigadeWorkProfileCatalog profiles)
        {
            if (bastion == null)
                throw new ArgumentNullException(nameof(bastion));

            if (brigadeId == Guid.Empty)
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));

            if (profiles == null)
                throw new ArgumentNullException(nameof(profiles));

            ModuleWorkDefaultAssessment Reject(
                ModuleWorkDefaultFailureReason reason)
            {
                return ModuleWorkDefaultAssessment.Rejected(
                    brigadeId, reason);
            }

            if (!bastion.TryGetBrigade(brigadeId, out Brigade brigade))
                return Reject(ModuleWorkDefaultFailureReason.BrigadeNotFound);

            if (brigade.IsDisbanded)
                return Reject(ModuleWorkDefaultFailureReason.BrigadeDisbanded);

            if (!bastion.TryGetBrigadeOperationalState(
                    brigadeId, out BrigadeOperationalState state) ||
                !state.IsDeployed)
            {
                return Reject(ModuleWorkDefaultFailureReason.BrigadeNotDeployed);
            }

            if (state.IsWorking)
                return Reject(ModuleWorkDefaultFailureReason.BrigadeAlreadyWorking);

            if (!bastion.TryGetBrigadeLocation(
                    brigadeId, out ModuleInstance module))
            {
                return Reject(ModuleWorkDefaultFailureReason.ModuleNotFound);
            }

            BrigadeWorkProfile profile = profiles.GetRequired(brigade.Type);

            ModuleWorkRequirement[] matches =
                module.Definition.CrewRequirement.WorkRequirements
                    .Where(requirement =>
                        requirement.MaximumUsefulPersonnel > 0 &&
                        profile.GetEfficiencyPercent(requirement.WorkType) ==
                            PrimaryAffinityPercent)
                    .ToArray();

            if (matches.Length == 0)
                return Reject(ModuleWorkDefaultFailureReason.NoMatchingWork);

            if (matches.Length > 1)
                return Reject(ModuleWorkDefaultFailureReason.AmbiguousWork);

            return ModuleWorkDefaultAssessment.Allowed(
                brigadeId,
                module.Id,
                matches[0].WorkType);
        }
    }
}
