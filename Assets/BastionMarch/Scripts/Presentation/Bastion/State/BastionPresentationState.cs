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
            : this(
                bastionId,
                name,
                width,
                deckCount,
                modules,
                Array.Empty<PassagePresentationState>())
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

        public BastionPresentationState(
            Guid bastionId,
            string name,
            int width,
            int deckCount,
            IEnumerable<ModulePresentationState>
                modules,
            IEnumerable<PassagePresentationState>
                passages)
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

            if (passages == null)
            {
                throw new ArgumentNullException(
                    nameof(passages));
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

            bool containsDuplicateModuleIds =
                moduleArray
                    .GroupBy(module =>
                        module.ModuleId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicateModuleIds)
            {
                throw new ArgumentException(
                    "Module collection contains duplicate ids.",
                    nameof(modules));
            }

            PassagePresentationState[] passageArray =
                passages.ToArray();

            if (passageArray.Any(
                    passage => passage == null))
            {
                throw new ArgumentException(
                    "Passage collection cannot contain null.",
                    nameof(passages));
            }

            bool containsDuplicatePassageIds =
                passageArray
                    .GroupBy(passage =>
                        passage.PassageId)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicatePassageIds)
            {
                throw new ArgumentException(
                    "Passage collection contains duplicate ids.",
                    nameof(passages));
            }

            var moduleIds =
                new HashSet<Guid>(
                    moduleArray.Select(
                        module =>
                            module.ModuleId));

            bool passageReferencesMissingModule =
                passageArray.Any(
                    passage =>
                        !moduleIds.Contains(
                            passage.SourceModuleId) ||
                        !moduleIds.Contains(
                            passage.TargetModuleId));

            if (passageReferencesMissingModule)
            {
                throw new ArgumentException(
                    "Passage references a module outside the snapshot.",
                    nameof(passages));
            }

            BastionId = bastionId;
            Name = name;
            Width = width;
            DeckCount = deckCount;

            Modules =
                new ReadOnlyCollection<
                    ModulePresentationState>(
                        moduleArray);

            Passages =
                new ReadOnlyCollection<
                    PassagePresentationState>(
                        passageArray);
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

        public bool TryGetPassage(
            Guid passageId,
            out PassagePresentationState passage)
        {
            passage =
                Passages.FirstOrDefault(
                    item =>
                        item.PassageId ==
                        passageId);

            return passage != null;
        }

        public IReadOnlyList<PassagePresentationState>
            Passages
        {
            get;
        }

        public int PassageCount =>
            Passages.Count;
    }
}