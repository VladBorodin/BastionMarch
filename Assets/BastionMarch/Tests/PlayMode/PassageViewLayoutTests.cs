using System;
using System.Collections.Generic;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using NUnit.Framework;
using UnityEngine;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class PassageViewLayoutTests
    {
        private readonly List<GameObject>
            _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (
                GameObject createdObject
                in _createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObject);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void SameDeckBoundaryCenterLiesOnSharedWall()
        {
            BastionGridLayout layout =
                CreateLayout();

            var boundary =
                new GridBoundarySegment(
                    new GridPosition(0, 0),
                    new GridPosition(1, 0));

            Vector3 center =
                layout.GetBoundaryCenterLocal(
                    boundary);

            Assert.That(
                center.x,
                Is.EqualTo(3f)
                    .Within(0.0001f));

            Assert.That(
                center.y,
                Is.EqualTo(1f)
                    .Within(0.0001f));

            Assert.That(
                center.z,
                Is.EqualTo(0f)
                    .Within(0.0001f));
        }

        [Test]
        public void StackedBoundaryCenterLiesOnSharedFloor()
        {
            BastionGridLayout layout =
                CreateLayout();

            var boundary =
                new GridBoundarySegment(
                    new GridPosition(2, 0),
                    new GridPosition(2, 1));

            Vector3 center =
                layout.GetBoundaryCenterLocal(
                    boundary);

            Assert.That(
                center.x,
                Is.EqualTo(7.5f)
                    .Within(0.0001f));

            Assert.That(
                center.y,
                Is.EqualTo(2f)
                    .Within(0.0001f));

            Assert.That(
                center.z,
                Is.EqualTo(0f)
                    .Within(0.0001f));
        }

        [Test]
        public void PassageViewUsesBoundaryOrientation()
        {
            BastionGridLayout layout =
                CreateLayout();

            PassageView passageView =
                CreatePassageView();

            Guid passageId =
                Guid.NewGuid();

            Guid sourceModuleId =
                Guid.NewGuid();

            Guid targetModuleId =
                Guid.NewGuid();

            var horizontalState =
                new PassagePresentationState(
                    passageId,
                    sourceModuleId,
                    targetModuleId,
                    new GridBoundarySegment(
                        new GridPosition(0, 0),
                        new GridPosition(1, 0)),
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional,
                    ModulePassageState.Open);

            passageView.Bind(
                horizontalState,
                layout);

            Assert.That(
                passageView.transform.localScale.y,
                Is.GreaterThan(
                    passageView.transform.localScale.x));

            var verticalState =
                new PassagePresentationState(
                    passageId,
                    sourceModuleId,
                    targetModuleId,
                    new GridBoundarySegment(
                        new GridPosition(0, 0),
                        new GridPosition(0, 1)),
                    ModulePassageType.Hatch,
                    ModulePassageTraversalMode
                        .Bidirectional,
                    ModulePassageState.Closed);

            passageView.ApplyState(
                verticalState);

            Assert.That(
                passageView.transform.localScale.x,
                Is.GreaterThan(
                    passageView.transform.localScale.y));

            Assert.That(
                passageView.State,
                Is.SameAs(
                    verticalState));
        }

        private BastionGridLayout CreateLayout()
        {
            var gameObject =
                new GameObject(
                    "TestBastionGridLayout");

            _createdObjects.Add(
                gameObject);

            return gameObject.AddComponent<
                BastionGridLayout>();
        }

        private PassageView CreatePassageView()
        {
            var gameObject =
                new GameObject(
                    "TestPassageView");

            _createdObjects.Add(
                gameObject);

            gameObject.AddComponent<
                SpriteRenderer>();

            return gameObject.AddComponent<
                PassageView>();
        }
    }
}