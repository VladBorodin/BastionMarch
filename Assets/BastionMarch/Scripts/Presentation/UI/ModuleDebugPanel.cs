using System;
using System.Text;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Presentation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BastionMarch.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class ModuleDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ModuleSelectionController _selectionController;
        [SerializeField] private TMP_Text _moduleInfoText;

        private readonly StringBuilder _builder = new StringBuilder(capacity: 512);

        private void Reset()
        {
            if (_moduleInfoText == null)
                _moduleInfoText = GetComponentInChildren<TMP_Text>(true);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (_selectionController != null)
                _selectionController.SelectionChanged += HandleSelectionChanged;

            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        }

        private void Start()
        {
            Refresh(_selectionController.SelectedState);
        }

        private void OnDisable()
        {
            if (_selectionController != null)
                _selectionController.SelectionChanged -= HandleSelectionChanged;

            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void HandleSelectionChanged(ModulePresentationState module)
        {
            Refresh(module);
        }

        private void HandleLocaleChanged(Locale locale)
        {
            Refresh(_selectionController?.SelectedState);
        }

        public void Refresh(ModulePresentationState module)
        {
            ResolveReferences();

            if (module == null)
            {
                _moduleInfoText.text = BastionTexts.Ui(
                    UiTextKeys.DebugSelectModule);
                return;
            }

            _builder.Clear();
            _builder.AppendLine(BastionTexts.Ui(UiTextKeys.DebugHeader));

            AppendLine(UiTextKeys.DebugInstanceId, module.ModuleId.ToString());
            AppendLine(UiTextKeys.DebugDefinitionId, module.DefinitionId);
            AppendLine(UiTextKeys.DebugNameKey, module.NameLocalizationKey);
            AppendLine(UiTextKeys.DebugName,
                BastionTexts.ModuleName(module.NameLocalizationKey));
            AppendLine(UiTextKeys.DebugType,
                BastionTexts.EnumLabel(module.Type));
            AppendLine(UiTextKeys.DebugCategory,
                BastionTexts.EnumLabel(module.Category));
            AppendLine(UiTextKeys.DebugPosition,
                BastionTexts.Ui(
                    UiTextKeys.DebugPositionValue,
                    module.Position.X,
                    module.Position.Deck));
            AppendLine(UiTextKeys.DebugSize,
                BastionTexts.Ui(
                    UiTextKeys.DebugSizeValue,
                    module.Size.Width,
                    module.Size.Height));
            AppendLine(UiTextKeys.DebugDurability,
                BastionTexts.Ui(
                    UiTextKeys.DebugDurabilityValue,
                    module.CurrentDurability,
                    module.MaximumDurability));
            AppendLine(UiTextKeys.DebugTechnicalState,
                BastionTexts.EnumLabel(module.TechnicalState));
            AppendLine(UiTextKeys.DebugControlState,
                BastionTexts.EnumLabel(module.ControlState));
            AppendLine(UiTextKeys.DebugRequestedPower,
                BastionTexts.EnumLabel(module.RequestedPowerMode));
            AppendLine(UiTextKeys.DebugEffectivePower,
                BastionTexts.EnumLabel(module.EffectivePowerMode));
            AppendLine(UiTextKeys.DebugPowerPriority,
                BastionTexts.EnumLabel(module.PowerPriority));
            AppendLine(UiTextKeys.DebugOccupyingBrigades,
                module.OccupyingBrigadeCount.ToString());
            AppendLine(UiTextKeys.DebugWorkingBrigades,
                module.WorkingBrigadeCount.ToString());

            _moduleInfoText.text = _builder.ToString();
        }

        private void AppendLine(string labelKey, string value)
        {
            _builder.AppendLine(BastionTexts.Ui(
                UiTextKeys.DebugField,
                BastionTexts.Ui(labelKey),
                value));
        }

        private void ResolveReferences()
        {
            if (_moduleInfoText == null)
                _moduleInfoText = GetComponentInChildren<TMP_Text>(true);

            if (_selectionController == null)
                throw new InvalidOperationException(
                    "ModuleDebugPanel requires ModuleSelectionController.");

            if (_moduleInfoText == null)
                throw new InvalidOperationException(
                    "ModuleDebugPanel requires TMP_Text.");
        }
    }
}
