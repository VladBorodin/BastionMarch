using System;

namespace BastionMarch.Simulation.Crew
{
    /// <summary>
    /// Бригада является минимальной управляемой единицей экипажа.
    ///
    /// Игра не отслеживает каждого человека отдельно.
    /// CurrentPersonnel представляет численность и боеспособность
    /// бригады в людях.
    /// </summary>
    public sealed class Brigade
    {
        public const int MinimumBrigadeCapacity = 3;
        public const int MaximumBrigadeCapacity = 12;

        public const int MinimumStatValue = 0;
        public const int MaximumStatValue = 100;

        public const int VeteranExperienceThreshold = 80;

        public Guid Id { get; }

        public int Number { get; }

        public string Nickname { get; private set; }

        public BrigadeType Type { get; private set; }

        public int CurrentPersonnel { get; private set; }

        public int MaximumPersonnel { get; }

        /// <summary>
        /// Текущая средняя подготовка состава.
        ///
        /// Пополнение рекрутами может снизить это значение.
        /// </summary>
        public int Experience { get; private set; }

        /// <summary>
        /// Наивысший опыт, достигнутый бригадой за её историю.
        ///
        /// Используется для сохранения ветеранских традиций
        /// после пополнения неопытными бойцами.
        /// </summary>
        public int PeakExperience { get; private set; }

        public int Morale { get; private set; }

        public int Fatigue { get; private set; }

        public bool IsDisbanded =>
            CurrentPersonnel == 0;

        /// <summary>
        /// Бригада сохраняет ветеранскую историю, пока существует.
        ///
        /// Текущая эффективность при этом может быть значительно ниже
        /// из-за потерь и пополнения рекрутами.
        /// </summary>
        public bool HasVeteranTradition =>
            !IsDisbanded &&
            PeakExperience >= VeteranExperienceThreshold;

        public int VacantPersonnelSlots =>
            MaximumPersonnel - CurrentPersonnel;

        public Brigade(
            int number,
            BrigadeType type,
            int currentPersonnel,
            int maximumPersonnel,
            int experience = 0,
            int morale = 100,
            int fatigue = 0,
            string nickname = "")
            : this(
                Guid.NewGuid(),
                number,
                type,
                currentPersonnel,
                maximumPersonnel,
                experience,
                morale,
                fatigue,
                nickname)
        {
        }

        public Brigade(
            Guid id,
            int number,
            BrigadeType type,
            int currentPersonnel,
            int maximumPersonnel,
            int experience = 0,
            int morale = 100,
            int fatigue = 0,
            string nickname = "")
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Brigade id cannot be empty.",
                    nameof(id));
            }

            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "Brigade number must be greater than zero.");
            }

            if (!Enum.IsDefined(typeof(BrigadeType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (maximumPersonnel < MinimumBrigadeCapacity ||
                maximumPersonnel > MaximumBrigadeCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPersonnel),
                    $"Brigade capacity must be between " +
                    $"{MinimumBrigadeCapacity} and " +
                    $"{MaximumBrigadeCapacity}.");
            }

            if (currentPersonnel <= 0 ||
                currentPersonnel > maximumPersonnel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentPersonnel),
                    "Current personnel must be greater than zero " +
                    "and cannot exceed brigade capacity.");
            }

            ValidateStat(experience, nameof(experience));
            ValidateStat(morale, nameof(morale));
            ValidateStat(fatigue, nameof(fatigue));

            Id = id;
            Number = number;
            Type = type;

            CurrentPersonnel = currentPersonnel;
            MaximumPersonnel = maximumPersonnel;

            Experience = experience;
            PeakExperience = experience;

            Morale = morale;
            Fatigue = fatigue;

            Nickname = NormalizeNickname(nickname);
        }

        /// <summary>
        /// Наносит потери бригаде.
        ///
        /// Возвращает фактическое количество потерянных людей.
        /// </summary>
        public int ApplyCasualties(int requestedCasualties)
        {
            if (requestedCasualties < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedCasualties));
            }

            int actualCasualties =
                Math.Min(
                    requestedCasualties,
                    CurrentPersonnel);

            CurrentPersonnel -= actualCasualties;

            return actualCasualties;
        }

        /// <summary>
        /// Пополняет бригаду новыми людьми.
        ///
        /// Опыт пересчитывается как среднее значение между
        /// существующим составом и прибывшим пополнением.
        ///
        /// Возвращает фактически добавленное количество людей.
        /// </summary>
        public int Reinforce(
            int requestedPersonnel,
            int reinforcementExperience = 0)
        {
            if (IsDisbanded)
            {
                throw new InvalidOperationException(
                    "A disbanded brigade cannot be reinforced.");
            }

            if (requestedPersonnel < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedPersonnel));
            }

            ValidateStat(
                reinforcementExperience,
                nameof(reinforcementExperience));

            int addedPersonnel =
                Math.Min(
                    requestedPersonnel,
                    VacantPersonnelSlots);

            if (addedPersonnel == 0)
            {
                return 0;
            }

            long existingExperienceTotal =
                (long)Experience * CurrentPersonnel;

            long reinforcementExperienceTotal =
                (long)reinforcementExperience * addedPersonnel;

            int newPersonnel =
                CurrentPersonnel + addedPersonnel;

            Experience = (int)(
                (existingExperienceTotal +
                 reinforcementExperienceTotal) /
                newPersonnel);

            CurrentPersonnel = newPersonnel;

            return addedPersonnel;
        }

        public void GainExperience(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Experience = ClampStat(Experience + amount);

            if (Experience > PeakExperience)
            {
                PeakExperience = Experience;
            }
        }

        public void ChangeMorale(int delta)
        {
            Morale = ClampStat(Morale + delta);
        }

        public void ChangeFatigue(int delta)
        {
            Fatigue = ClampStat(Fatigue + delta);
        }

        public void SetNickname(string nickname)
        {
            Nickname = NormalizeNickname(nickname);
        }

        /// <summary>
        /// Изменение специализации будет использоваться для перехода
        /// новобранцев в профильные бригады.
        ///
        /// Проверку условий специализации добавим в системе развития.
        /// </summary>
        internal void SetType(BrigadeType type)
        {
            if (!Enum.IsDefined(typeof(BrigadeType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            Type = type;
        }

        private static void ValidateStat(
            int value,
            string parameterName)
        {
            if (value < MinimumStatValue ||
                value > MaximumStatValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Value must be between " +
                    $"{MinimumStatValue} and {MaximumStatValue}.");
            }
        }

        private static int ClampStat(int value)
        {
            if (value < MinimumStatValue)
            {
                return MinimumStatValue;
            }

            if (value > MaximumStatValue)
            {
                return MaximumStatValue;
            }

            return value;
        }

        private static string NormalizeNickname(string nickname)
        {
            return string.IsNullOrWhiteSpace(nickname)
                ? string.Empty
                : nickname.Trim();
        }
    }
}