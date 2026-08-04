using System.Collections;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Modules;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BastionPrototypeSceneTests
    {
        private const string PrototypeSceneName =
            "BastionPrototype";

        [UnitySetUp]
        public IEnumerator LoadPrototypeScene()
        {
            AsyncOperation loading =
                SceneManager.LoadSceneAsync(
                    PrototypeSceneName,
                    LoadSceneMode.Single);

            Assert.That(
                loading,
                Is.Not.Null);

            while (!loading.isDone)
            {
                yield return null;
            }

            // Даём компонентам выполнить Start.
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeSceneBindsSimulationThroughPresenter()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            BastionGridView gridView =
                Object.FindAnyObjectByType<
                    BastionGridView>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(bastionView, Is.Not.Null);
            Assert.That(gridView, Is.Not.Null);

            Assert.That(
                bootstrap.Bastion,
                Is.Not.Null);

            Assert.That(
                presenter.SourceBastion,
                Is.SameAs(bootstrap.Bastion));

            Assert.That(
                presenter.CurrentState,
                Is.Not.Null);

            Assert.That(
                bastionView.State,
                Is.SameAs(
                    presenter.CurrentState));

            Assert.That(
                bastionView.ModuleViews.Count,
                Is.EqualTo(
                    presenter.CurrentState.ModuleCount));

            Assert.That(
                gridView.RenderedWidth,
                Is.EqualTo(
                    presenter.CurrentState.Width));

            Assert.That(
                gridView.RenderedDeckCount,
                Is.EqualTo(
                    presenter.CurrentState.DeckCount));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ModuleCanBeSelectedThroughController()
        {
            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            ModuleSelectionController
                selectionController =
                    Object.FindAnyObjectByType<
                        ModuleSelectionController>();

            Assert.That(
                bastionView,
                Is.Not.Null);

            Assert.That(
                selectionController,
                Is.Not.Null);

            ModuleView firstModuleView =
                bastionView.ModuleViews.First();

            selectionController.Select(
                firstModuleView);

            Assert.That(
                selectionController.SelectedView,
                Is.SameAs(firstModuleView));

            Assert.That(
                selectionController.SelectedState,
                Is.SameAs(firstModuleView.State));

            Assert.That(
                firstModuleView.IsSelected,
                Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PresenterRefreshesExistingModuleViewWithNewSnapshot()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionPresenter presenter =
                Object.FindAnyObjectByType<
                    BastionPresenter>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            ModulePresentationState beforeState =
                bastionView.State.Modules.First();

            bool viewFoundBefore =
                bastionView.TryGetModuleView(
                    beforeState.ModuleId,
                    out ModuleView beforeView);

            bool modelFound =
                bootstrap.Bastion.TryGetModule(
                    beforeState.ModuleId,
                    out ModuleInstance modelModule);

            Assert.That(viewFoundBefore, Is.True);
            Assert.That(modelFound, Is.True);

            int capturedDurability =
                beforeState.CurrentDurability;

            modelModule.ApplyDamage(1);

            // Старый снимок не меняется сам.
            Assert.That(
                beforeState.CurrentDurability,
                Is.EqualTo(capturedDurability));

            presenter.RefreshPresentation();

            bool viewFoundAfter =
                bastionView.TryGetModuleView(
                    beforeState.ModuleId,
                    out ModuleView afterView);

            Assert.That(viewFoundAfter, Is.True);

            // Существующий GameObject не пересоздан.
            Assert.That(
                afterView,
                Is.SameAs(beforeView));

            // Но он получил новый снимок.
            Assert.That(
                afterView.State,
                Is.Not.SameAs(beforeState));

            Assert.That(
                afterView.State.CurrentDurability,
                Is.EqualTo(
                    capturedDurability - 1));

            Assert.That(
                presenter.CurrentState,
                Is.SameAs(bastionView.State));

            yield return null;
        }
    }
}