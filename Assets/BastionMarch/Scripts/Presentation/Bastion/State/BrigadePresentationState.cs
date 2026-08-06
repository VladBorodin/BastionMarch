using System;
using BastionMarch.Simulation.Crew;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок одной бригады.
    ///
    /// Содержит собственное состояние Brigade,
    /// а также её оперативное расположение в Bastion.
    /// </summary>
    public sealed class BrigadePresentationState
    {
        public Guid BrigadeId { get; }

        public int Number { get; }

        public BrigadeType Type { get; }

        public int CurrentPersonnel { get; }

        public int MaximumUsefulPersonnel { get; }

        public int Experience { get; }

        public int PeakExperience { get; }

        public int Morale { get; }

        public int Fatigue { get; }

        public string Nickname { get; }

        public bool HasVeteranTradition { get; }

        public bool IsDisbanded { get; }

        public Guid? CurrentModuleId { get; }

        public bool IsDeployed =>
            CurrentModuleId.HasValue;

        public bool IsWorking { get; }

        public BrigadePresentationState(
            Guid brigadeId,
            int number,
            BrigadeType type,
            int currentPersonnel,
            int maximumUsefulPersonnel,
            int experience,
            int peakExperience,
            int morale,
            int fatigue,
            string nickname,
            bool hasVeteranTradition,
            bool isDisbanded,
            Guid? currentModuleId,
            bool isWorking)
        {
            if (brigadeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(brigadeId));
            }

            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number));
            }

            if (!Enum.IsDefined(
                    typeof(BrigadeType),
                    type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type));
            }

            if (maximumUsefulPersonnel <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumUsefulPersonnel));
            }

            if (currentPersonnel < 0 ||
                currentPersonnel >
                maximumUsefulPersonnel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentPersonnel));
            }

            ValidatePercent(
                experience,
                nameof(experience));

            ValidatePercent(
                peakExperience,
                nameof(peakExperience));

            ValidatePercent(
                morale,
                nameof(morale));

            ValidatePercent(
                fatigue,
                nameof(fatigue));

            if (peakExperience < experience)
            {
                throw new ArgumentException(
                    "Peak experience cannot be lower " +
                    "than current experience.",
                    nameof(peakExperience));
            }

            if (currentModuleId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Current module id cannot be empty.",
                    nameof(currentModuleId));
            }

            if (isWorking &&
                !currentModuleId.HasValue)
            {
                throw new ArgumentException(
                    "Working brigade must be deployed.",
                    nameof(isWorking));
            }

            BrigadeId = brigadeId;
            Number = number;
            Type = type;

            CurrentPersonnel =
                currentPersonnel;

            MaximumUsefulPersonnel =
                maximumUsefulPersonnel;

            Experience = experience;
            PeakExperience = peakExperience;
            Morale = morale;
            Fatigue = fatigue;

            Nickname =
                nickname ?? string.Empty;

            HasVeteranTradition =
                hasVeteranTradition;

            IsDisbanded =
                isDisbanded;

            CurrentModuleId =
                currentModuleId;

            IsWorking =
                isWorking;
        }

        private static void ValidatePercent(
            int value,
            string parameterName)
        {
            if (value < 0 ||
                value > 100)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName);
            }
        }
    }
}