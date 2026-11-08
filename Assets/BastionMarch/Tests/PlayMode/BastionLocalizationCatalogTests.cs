using System;
using System.Collections;
using BastionMarch.Presentation.Localization;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BastionLocalizationCatalogTests
    {
        [UnityTest]
        public IEnumerator TablesTranslateAllDisplayedEnumValues()
        {
            while (!BastionTexts.IsReady)
                yield return null;

            string original = BastionTexts.CurrentLanguageCode;
            try
            {
                foreach (string code in new[] { "ru", "en" })
                {
                    Assert.That(BastionTexts.TrySetLanguage(code), Is.True);
                    CheckEnum<BrigadeType>();
                    CheckEnum<ModuleType>();
                    CheckEnum<ModuleCategory>();
                    CheckEnum<ModuleTechnicalState>();
                    CheckEnum<ModuleControlState>();
                    CheckEnum<ModulePowerMode>();
                    CheckEnum<PowerPriority>();
                    CheckEnum<MovePlanningFailureReason>();
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    BastionTexts.TrySetLanguage(original);
            }
        }

        [UnityTest]
        public IEnumerator BothLocalesTranslateModuleNamesAndFullUiSentences()
        {
            while (!BastionTexts.IsReady)
                yield return null;

            string original = BastionTexts.CurrentLanguageCode;
            try
            {
                Assert.That(BastionTexts.TrySetLanguage("ru"), Is.True);
                Assert.That(BastionTexts.ModuleName(
                    ModuleLocalizationKeys.StandardRepairBayName),
                    Is.EqualTo("Ремонтный отсек"));
                Assert.That(BastionTexts.Ui(UiTextKeys.TurnHeader, 3),
                    Is.EqualTo("BastionMarch | Ход 3"));
                Assert.That(BastionTexts.Ui(UiTextKeys.TargetModule,
                    "Ремонтный отсек", 2, 1),
                    Is.EqualTo("Цель: Ремонтный отсек (X: 2, палуба: 1)"));

                Assert.That(BastionTexts.TrySetLanguage("en"), Is.True);
                Assert.That(BastionTexts.ModuleName(
                    ModuleLocalizationKeys.StandardRepairBayName),
                    Is.EqualTo("Repair bay"));
                Assert.That(BastionTexts.Ui(UiTextKeys.TurnHeader, 3),
                    Is.EqualTo("BastionMarch | Turn 3"));
                Assert.That(BastionTexts.Ui(UiTextKeys.TargetModule,
                    "Repair bay", 2, 1),
                    Is.EqualTo("Target: Repair bay (X: 2, deck: 1)"));
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    BastionTexts.TrySetLanguage(original);
            }
        }

        private static void CheckEnum<TEnum>() where TEnum : struct, Enum
        {
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                string translated = BastionTexts.EnumLabel(value);
                string key = $"enum.{typeof(TEnum).Name}.{value}";

                Assert.That(translated, Is.Not.Null.And.Not.Empty, key);
                Assert.That(translated, Is.Not.EqualTo(key), key);
                Assert.That(translated, Does.Not.Contain("No translation found"), key);
            }
        }
    }
}
