using System;
using System.Text;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using TMPro;
using UnityEngine;

namespace BastionMarch.Presentation.UI
{
    /// <summary>
    /// Разработческая панель текущего состояния
    /// выбранного модуля.
    ///
    /// Не является финальным локализованным UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModuleDebugPanel : MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private ModuleSelectionController
            _selectionController;

        [SerializeField]
        private TMP_Text _moduleInfoText;

        private readonly StringBuilder _builder =
            new StringBuilder(capacity: 512);

        private void Reset()
        {
            if (_moduleInfoText == null)
            {
                _moduleInfoText =
                    GetComponentInChildren<TMP_Text>(
                        includeInactive: true);
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (_selectionController != null)
            {
                _selectionController.SelectionChanged +=
                    HandleSelectionChanged;
            }
        }

        private void Start()
        {
            Refresh(
                _selectionController.SelectedState);
        }

        private void OnDisable()
        {
            if (_selectionController != null)
            {
                _selectionController.SelectionChanged -=
                    HandleSelectionChanged;
            }
        }

        private void HandleSelectionChanged(
            ModulePresentationState module)
        {
            Refresh(module);
        }

        public void Refresh(
            ModulePresentationState module)
        {
            ResolveReferences();

            if (module == null)
            {
                _moduleInfoText.text =
                    "Select a module";

                return;
            }

            _builder.Clear();

            AppendLine(
                "MODULE INSTANCE",
                string.Empty);

            AppendLine(
                "Instance Id",
                module.ModuleId.ToString());

            AppendLine(
                "Definition Id",
                module.DefinitionId);

            AppendLine(
                "Name Key",
                module.NameLocalizationKey);

            AppendLine(
                "Type",
                module.Type.ToString());

            AppendLine(
                "Category",
                module.Category.ToString());

            AppendLine(
                "Position",
                $"X={module.Position.X}, " +
                $"Deck={module.Position.Deck}");

            AppendLine(
                "Size",
                $"{module.Size.Width}" +
                " × " +
                $"{module.Size.Height}");

            AppendLine(
                "Durability",
                $"{module.CurrentDurability}" +
                " / " +
                $"{module.MaximumDurability}");

            AppendLine(
                "Technical State",
                module.TechnicalState.ToString());

            AppendLine(
                "Control State",
                module.ControlState.ToString());

            AppendLine(
                "Requested Power",
                module.RequestedPowerMode.ToString());

            AppendLine(
                "Effective Power",
                module.EffectivePowerMode.ToString());

            AppendLine(
                "Power Priority",
                module.PowerPriority.ToString());

            AppendLine(
                "Occupying Brigades",
                module.OccupyingBrigadeCount.ToString());

            AppendLine(
                "Working Brigades",
                module.WorkingBrigadeCount.ToString());

            _moduleInfoText.text =
                _builder.ToString();
        }

        private void AppendLine(
            string label,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _builder
                    .AppendLine(label);

                return;
            }

            _builder
                .Append(label)
                .Append(": ")
                .AppendLine(value);
        }

        private void ResolveReferences()
        {
            if (_moduleInfoText == null)
            {
                _moduleInfoText =
                    GetComponentInChildren<TMP_Text>(
                        includeInactive: true);
            }

            if (_selectionController == null)
            {
                throw new InvalidOperationException(
                    "ModuleDebugPanel requires " +
                    "ModuleSelectionController.");
            }

            if (_moduleInfoText == null)
            {
                throw new InvalidOperationException(
                    "ModuleDebugPanel requires TMP_Text.");
            }
        }
    }
}