using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BastionMarch.Presentation.Localization
{
    public static class BastionTexts
    {
        public const string UiTable = "UI";
        public const string ModulesTable = "Modules";

        public static bool IsReady =>
            LocalizationSettings.InitializationOperation.IsDone;

        public static string CurrentLanguageCode =>
            LocalizationSettings.SelectedLocale?.Identifier.Code ?? string.Empty;

        public static string Ui(string key, params object[] arguments)
        {
            return Get(UiTable, key, arguments);
        }

        public static string ModuleName(string key)
        {
            return Get(ModulesTable, key, Array.Empty<object>());
        }

        // The enum values are machine-readable identifiers, not UI strings.
        // Changing one of these names requires updating the corresponding UI table.
        public static string EnumLabel<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return Ui($"enum.{typeof(TEnum).Name}.{value}");
        }

        public static bool TrySetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null)
                return false;

            LocalizationSettings.SelectedLocale = locale;
            return true;
        }

        private static string Get(string table, string key, object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    "Localization key cannot be empty.", nameof(key));

            return LocalizationSettings.StringDatabase.GetLocalizedString(
                table,
                key,
                LocalizationSettings.SelectedLocale,
                FallbackBehavior.UseProjectSettings,
                arguments);
        }
    }
}
