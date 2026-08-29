using System;
using System.Collections.Generic;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Power;
using NUnit.Framework;
using UnityEngine;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BrigadeViewLayoutTests
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
        public void ModuleSlotsAreDeterministic()
        {
            BastionGridLayout layout =
                CreateLayout();

            var modulePosition =
                new GridPosition(0, 0);

            var moduleSize =
                new GridSize(2, 1);

            Vector3 firstSlot =
                layout.GetModuleSlotCenterLocal(
                    modulePosition,
                    moduleSize,
                    slotIndex: 0,
                    slotCount: 5,
                    maxColumns: 4);

            Vector3 fifthSlot =
                layout.GetModuleSlotCenterLocal(
                    modulePosition,
                    moduleSize,
                    slotIndex: 4,
                    slotCount: 5,
                    maxColumns: 4);

            Assert.That(
                firstSlot.x,
                Is.EqualTo(0.75f)
                    .Within(0.0001f));

            Assert.That(
                firstSlot.y,
                Is.EqualTo(0.5f)
                    .Within(0.0001f));

            Assert.That(
                fifthSlot.x,
                Is.EqualTo(0.75f)
                    .Within(0.0001f));

            Assert.That(
                fifthSlot.y,
                Is.EqualTo(1.5f)
                    .Within(0.0001f));
        }

        [Test]
        public void BrigadeViewUsesAssignedModuleSlot()
        {
            BastionGridLayout layout =
                CreateLayout();

            ModulePresentationState moduleState =
                CreateModuleState(
                    new GridPosition(1, 1),
                    new GridSize(2, 1));

            BrigadePresentationState brigadeState =
                CreateBrigadeState(
                    moduleState.ModuleId,
                    isWorking: false);

            BrigadeView brigadeView =
                CreateBrigadeView();

            brigadeView.Bind(
                brigadeState,
                moduleState,
                layout,
                slotIndex: 1,
                slotCount: 3);

            Vector3 expectedPosition =
                layout.GetModuleSlotCenterLocal(
                    moduleState.Position,
                    moduleState.Size,
                    slotIndex: 1,
                    slotCount: 3,
                    maxColumns: 4);

            Assert.That(
                brigadeView.State,
                Is.SameAs(
                    brigadeState));

            Assert.That(
                brigadeView.ModuleState,
                Is.SameAs(
                    moduleState));

            Assert.That(
                brigadeView.transform.localPosition,
                Is.EqualTo(
                    expectedPosition));

            Assert.That(
                brigadeView.TechnicalLabel,
                Is.EqualTo(
                    "#7 Mechanic 5/6"));

            Assert.That(
                brigadeView.IsBound,
                Is.True);
        }

        [Test]
        public void BrigadeViewUpdatesWorkStateWithoutRecreation()
        {
            BastionGridLayout layout =
                CreateLayout();

            ModulePresentationState moduleState =
                CreateModuleState(
                    new GridPosition(0, 0),
                    new GridSize(1, 1));

            BrigadePresentationState idleState =
                CreateBrigadeState(
                    moduleState.ModuleId,
                    isWorking: false);

            BrigadeView brigadeView =
                CreateBrigadeView();

            brigadeView.Bind(
                idleState,
                moduleState,
                layout,
                slotIndex: 0,
                slotCount: 1);

            SpriteRenderer renderer =
                brigadeView.GetComponent<
                    SpriteRenderer>();

            Color idleColor =
                renderer.color;

            BrigadePresentationState workingState =
                CreateBrigadeState(
                    moduleState.ModuleId,
                    isWorking: true,
                    brigadeId:
                        idleState.BrigadeId);

            brigadeView.ApplyState(
                workingState,
                moduleState,
                slotIndex: 0,
                slotCount: 1);

            Assert.That(
                brigadeView.BrigadeId,
                Is.EqualTo(
                    idleState.BrigadeId));

            Assert.That(
                brigadeView.State,
                Is.SameAs(
                    workingState));

            Assert.That(
                renderer.color,
                Is.Not.EqualTo(
                    idleColor));
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

        private BrigadeView CreateBrigadeView()
        {
            var gameObject =
                new GameObject(
                    "TestBrigadeView");

            _createdObjects.Add(
                gameObject);

            gameObject.AddComponent<
                SpriteRenderer>();

            return gameObject.AddComponent<
                BrigadeView>();
        }

        private static ModulePresentationState
            CreateModuleState(
                GridPosition position,
                GridSize size)
        {
            return new ModulePresentationState(
                moduleId:
                    Guid.NewGuid(),
                definitionId:
                    "test.module",
                nameLocalizationKey:
                    "module.test.name",
                category:
                    default,
                type:
                    default,
                position:
                    position,
                size:
                    size,
                currentDurability:
                    100,
                maximumDurability:
                    100,
                technicalState:
                    ModuleTechnicalState.Operational,
                controlState:
                    ModuleControlState.Friendly,
                requestedPowerMode:
                    ModulePowerMode.Active,
                effectivePowerMode:
                    ModulePowerMode.Active,
                powerPriority:
                    default,
                occupyingBrigadeCount:
                    1,
                workingBrigadeCount:
                    0);
        }

        private static BrigadePresentationState
            CreateBrigadeState(
                Guid moduleId,
                bool isWorking,
                Guid? brigadeId = null)
        {
            return new BrigadePresentationState(
                brigadeId:
                    brigadeId ?? Guid.NewGuid(),
                number:
                    7,
                type:
                    BrigadeType.Mechanic,
                currentPersonnel:
                    5,
                maximumUsefulPersonnel:
                    6,
                experience:
                    50,
                peakExperience:
                    60,
                morale:
                    80,
                fatigue:
                    10,
                nickname:
                    "Test Brigade",
                hasVeteranTradition:
                    false,
                isDisbanded:
                    false,
                currentModuleId:
                    moduleId,
                isWorking:
                    isWorking);
        }
    }
}