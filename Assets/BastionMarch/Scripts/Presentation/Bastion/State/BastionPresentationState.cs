using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Presentation.Bastions.State
{
    /// <summary>
    /// Неизменяемый снимок данных бастиона,
    /// необходимых текущему Presentation-слою.
    /// </summary>
    public sealed class BastionPresentationState
    {
        public Guid BastionId { get; }

        public string Name { get; }

        public int Width { get; }

        public int DeckCount { get; }

        public IReadOnlyList<ModulePresentationState>
            Modules
        {
            get;
        }

        public int ModuleCount =>
            Modules.Count;

        public BastionPresentationState(
            Guid bastionId,
            string name,
            int width,
            int deckCount,
            IEnumerable<ModulePresentationState>
                modules)
        {
            if (bastionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Bastion id cannot be empty.",
                    nameof(bastionId));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Bastion name cannot be empty.",
                    nameof(name));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width));
            }

            if (deckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deckCount));
            }

            if (modules == null)
            {
                throw new ArgumentNullException(
                    nameof(modules));
            }

            ModulePresentationState[] moduleArray =
                modules.ToArray();

            if (moduleArray.Any(
                    module => module == null))
            {
                throw new ArgumentException(
                    "Module collection cannot contain null.",
                    nameof(modules));
            }

            bool containsDuplicateIds =
                moduleArray
                    .GroupBy(module =>
                        module.ModuleId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateIds)
            {
                throw new ArgumentException(
                    "Module collection contains duplicate ids.",
                    nameof(modules));
            }

            BastionId = bastionId;
            Name = name;
            Width = width;
            DeckCount = deckCount;

            Modules =
                new ReadOnlyCollection<
                    ModulePresentationState>(
                        moduleArray);
        }

        public bool TryGetModule(
            Guid moduleId,
            out ModulePresentationState module)
        {
            module =
                Modules.FirstOrDefault(
                    item =>
                        item.ModuleId ==
                        moduleId);

            return module != null;
        }
    }
}