using System;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Полный контекст одной попытки пройти
    /// через конкретный переход.
    ///
    /// Контекст содержит сами модули, чтобы будущие
    /// политики могли учитывать их контроль,
    /// повреждения и опасные состояния.
    /// </summary>
    public sealed class ModulePassageTraversalContext
    {
        public ModulePassage Passage { get; }

        public ModuleInstance FromModule { get; }

        public ModuleInstance ToModule { get; }

        public Guid FromModuleId =>
            FromModule.Id;

        public Guid ToModuleId =>
            ToModule.Id;

        public ModulePassageTraversalContext(
            ModulePassage passage,
            ModuleInstance fromModule,
            ModuleInstance toModule)
        {
            Passage = passage ??
                throw new ArgumentNullException(
                    nameof(passage));

            FromModule = fromModule ??
                throw new ArgumentNullException(
                    nameof(fromModule));

            ToModule = toModule ??
                throw new ArgumentNullException(
                    nameof(toModule));

            if (fromModule.Id == toModule.Id)
            {
                throw new ArgumentException(
                    "Traversal must connect two different modules.",
                    nameof(toModule));
            }

            if (!passage.ConnectsModule(
                    fromModule.Id) ||
                !passage.ConnectsModule(
                    toModule.Id))
            {
                throw new ArgumentException(
                    "Passage does not connect the supplied modules.");
            }
        }
    }
}