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

        public IReadOnlyList<PassagePresentationState>
            Passages
        {
            get;
        }

        public IReadOnlyList<BrigadePresentationState>
            Brigades
        {
            get;
        }

        public int ModuleCount =>
            Modules.Count;

        public int PassageCount =>
            Passages.Count;

        public int BrigadeCount =>
            Brigades.Count;

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
                Array.Empty<
                    PassagePresentationState>(),
                Array.Empty<
                    BrigadePresentationState>())
        {
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
            : this(
                bastionId,
                name,
                width,
                deckCount,
                modules,
                passages,
                Array.Empty<
                    BrigadePresentationState>())
        {
        }

        public BastionPresentationState(
            Guid bastionId,
            string name,
            int width,
            int deckCount,
            IEnumerable<ModulePresentationState>
                modules,
            IEnumerable<PassagePresentationState>
                passages,
            IEnumerable<BrigadePresentationState>
                brigades)
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

            if (brigades == null)
            {
                throw new ArgumentNullException(
                    nameof(brigades));
            }

            ModulePresentationState[] moduleArray =
                modules.ToArray();

            PassagePresentationState[] passageArray =
                passages.ToArray();

            BrigadePresentationState[] brigadeArray =
                brigades.ToArray();

            ValidateNoNullItems(
                moduleArray,
                nameof(modules));

            ValidateNoNullItems(
                passageArray,
                nameof(passages));

            ValidateNoNullItems(
                brigadeArray,
                nameof(brigades));

            ValidateUniqueIds(
                moduleArray.Select(
                    module =>
                        module.ModuleId),
                "Module collection contains duplicate ids.",
                nameof(modules));

            ValidateUniqueIds(
                passageArray.Select(
                    passage =>
                        passage.PassageId),
                "Passage collection contains duplicate ids.",
                nameof(passages));

            ValidateUniqueIds(
                brigadeArray.Select(
                    brigade =>
                        brigade.BrigadeId),
                "Brigade collection contains duplicate ids.",
                nameof(brigades));

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
                    "Passage references a module " +
                    "outside the snapshot.",
                    nameof(passages));
            }

            bool brigadeReferencesMissingModule =
                brigadeArray.Any(
                    brigade =>
                        brigade.CurrentModuleId.HasValue &&
                        !moduleIds.Contains(
                            brigade.CurrentModuleId.Value));

            if (brigadeReferencesMissingModule)
            {
                throw new ArgumentException(
                    "Brigade references a module " +
                    "outside the snapshot.",
                    nameof(brigades));
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

            Brigades =
                new ReadOnlyCollection<
                    BrigadePresentationState>(
                        brigadeArray);
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

        public bool TryGetBrigade(
            Guid brigadeId,
            out BrigadePresentationState brigade)
        {
            brigade =
                Brigades.FirstOrDefault(
                    item =>
                        item.BrigadeId ==
                        brigadeId);

            return brigade != null;
        }

        private static void ValidateNoNullItems<T>(
            IEnumerable<T> items,
            string parameterName)
            where T : class
        {
            if (items.Any(
                    item => item == null))
            {
                throw new ArgumentException(
                    "Collection cannot contain null.",
                    parameterName);
            }
        }

        private static void ValidateUniqueIds(
            IEnumerable<Guid> ids,
            string message,
            string parameterName)
        {
            bool containsDuplicates =
                ids.GroupBy(id => id)
                    .Any(group =>
                        group.Count() > 1);

            if (containsDuplicates)
            {
                throw new ArgumentException(
                    message,
                    parameterName);
            }
        }
    }
}