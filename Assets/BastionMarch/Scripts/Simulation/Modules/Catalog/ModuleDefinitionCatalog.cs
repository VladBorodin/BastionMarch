using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BastionMarch.Simulation.Modules.Catalog
{
    /// <summary>
    /// Доступный игре каталог типов модулей.
    ///
    /// Сейчас он заполняется кодом. Позднее источником данных
    /// сможет стать JSON, база данных или Unity ScriptableObject,
    /// не изменяя саму модель каталога.
    /// </summary>
    public sealed class ModuleDefinitionCatalog
    {
        private readonly Dictionary<string, ModuleDefinition> _byId;

        public IReadOnlyList<ModuleDefinition> All { get; }

        public ModuleDefinitionCatalog(
            IEnumerable<ModuleDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var definitionList = definitions.ToList();

            if (definitionList.Count == 0)
            {
                throw new ArgumentException(
                    "Module catalog cannot be empty.",
                    nameof(definitions));
            }

            var duplicateId = definitionList
                .GroupBy(
                    definition => definition.Id,
                    StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateId != null)
            {
                throw new ArgumentException(
                    $"Duplicate module definition id: {duplicateId.Key}",
                    nameof(definitions));
            }

            _byId = definitionList.ToDictionary(
                definition => definition.Id,
                StringComparer.Ordinal);

            All = new ReadOnlyCollection<ModuleDefinition>(
                definitionList);
        }

        public ModuleDefinition GetRequired(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Module definition id cannot be empty.",
                    nameof(id));
            }

            if (!_byId.TryGetValue(id, out var definition))
            {
                throw new KeyNotFoundException(
                    $"Module definition '{id}' was not found.");
            }

            return definition;
        }

        public bool TryGet(
            string id,
            out ModuleDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(id, out definition);
        }
    }
}