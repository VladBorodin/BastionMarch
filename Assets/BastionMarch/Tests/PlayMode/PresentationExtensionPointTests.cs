using BastionMarch.Presentation.Bastions;
using NUnit.Framework;
using UnityEngine;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class PresentationExtensionPointTests
    {
        [Test]
        public void TurnPlanViewCanBeShownAndHidden()
        {
            var gameObject =
                new GameObject(
                    "TestTurnPlanView");

            try
            {
                TurnPlanView view =
                    gameObject.AddComponent<
                        TurnPlanView>();

                Assert.That(
                    view.IsVisible,
                    Is.True);

                view.Hide();

                Assert.That(
                    view.IsVisible,
                    Is.False);

                view.Show();

                Assert.That(
                    view.IsVisible,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void CombatEffectPresenterExistsAsExtensionPoint()
        {
            var gameObject =
                new GameObject(
                    "TestCombatEffectPresenter");

            try
            {
                CombatEffectPresenter presenter =
                    gameObject.AddComponent<
                        CombatEffectPresenter>();

                Assert.That(
                    presenter,
                    Is.Not.Null);

                Assert.DoesNotThrow(
                    presenter.Clear);
            }
            finally
            {
                Object.DestroyImmediate(
                    gameObject);
            }
        }
    }
}