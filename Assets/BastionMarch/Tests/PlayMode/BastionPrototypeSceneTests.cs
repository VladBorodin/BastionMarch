using System.Collections;
using System.Linq;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        public IEnumerator PrototypeSceneBindsSimulationBastion()
        {
            BastionPrototypeBootstrap bootstrap =
                Object.FindAnyObjectByType<
                    BastionPrototypeBootstrap>();

            BastionView bastionView =
                Object.FindAnyObjectByType<
                    BastionView>();

            BastionGridView gridView =
                Object.FindAnyObjectByType<
                    BastionGridView>();

            Assert.That(
                bootstrap,
                Is.Not.Null);

            Assert.That(
                bastionView,
                Is.Not.Null);

            Assert.That(
                gridView,
                Is.Not.Null);

            Assert.That(
                bootstrap.Bastion,
                Is.Not.Null);

            Assert.That(
                bastionView.IsBound,
                Is.True);

            Assert.That(
                bastionView.Bastion,
                Is.SameAs(bootstrap.Bastion));

            Assert.That(
                bastionView.ModuleViews.Count,
                Is.EqualTo(
                    bootstrap.Bastion.ModuleCount));

            Assert.That(
                gridView.RenderedWidth,
                Is.EqualTo(
                    bootstrap.Bastion.Width));

            Assert.That(
                gridView.RenderedDeckCount,
                Is.EqualTo(
                    bootstrap.Bastion.DeckCount));

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
                selectionController.SelectedModule,
                Is.SameAs(firstModuleView.Module));

            Assert.That(
                firstModuleView.IsSelected,
                Is.True);

            yield return null;
        }
    }
}