using System.Linq;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Power;
using NUnit.Framework;

namespace BastionMarch.Presentation.PlayModeTests
{
    [TestFixture]
    public sealed class BastionPresentationStateTests
    {
        private ModuleDefinitionCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog =
                InitialModuleDefinitions.CreateCatalog();
        }

        [Test]
        public void CapturesBastionAndModuleState()
        {
            var bastion =
                new Bastion(
                    name: "snapshot-test",
                    width: 6,
                    deckCount: 3);

            ModuleInstance module =
                bastion.TryInstallModule(
                        _catalog.GetRequired(
                            ModuleDefinitionIds
                                .StandardGeneratorRoom),
                        new GridPosition(1, 0))
                    .Module;

            module.SetPowerMode(
                ModulePowerMode.Active);

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            Assert.That(
                state.BastionId,
                Is.EqualTo(bastion.Id));

            Assert.That(
                state.Name,
                Is.EqualTo(bastion.Name));

            Assert.That(
                state.Width,
                Is.EqualTo(6));

            Assert.That(
                state.DeckCount,
                Is.EqualTo(3));

            Assert.That(
                state.ModuleCount,
                Is.EqualTo(1));

            ModulePresentationState moduleState =
                state.Modules[0];

            Assert.That(
                moduleState.ModuleId,
                Is.EqualTo(module.Id));

            Assert.That(
                moduleState.DefinitionId,
                Is.EqualTo(
                    module.Definition.Id));

            Assert.That(
                moduleState.Position,
                Is.EqualTo(
                    module.Position));

            Assert.That(
                moduleState.CurrentDurability,
                Is.EqualTo(
                    module.CurrentDurability));

            Assert.That(
                moduleState.RequestedPowerMode,
                Is.EqualTo(
                    ModulePowerMode.Active));
        }

        [Test]
        public void CapturedStateDoesNotChangeWithSimulation()
        {
            var bastion =
                new Bastion(
                    name: "immutable-snapshot-test",
                    width: 6,
                    deckCount: 3);

            ModuleInstance module =
                bastion.TryInstallModule(
                        _catalog.GetRequired(
                            ModuleDefinitionIds
                                .SmallMachineRoom),
                        new GridPosition(0, 0))
                    .Module;

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            ModulePresentationState captured =
                state.Modules[0];

            int capturedDurability =
                captured.CurrentDurability;

            ModulePowerMode capturedPowerMode =
                captured.RequestedPowerMode;

            module.ApplyDamage(
                module.Definition.MaxDurability);

            module.SetPowerMode(
                ModulePowerMode.Offline);

            Assert.That(
                captured.CurrentDurability,
                Is.EqualTo(
                    capturedDurability));

            Assert.That(
                captured.RequestedPowerMode,
                Is.EqualTo(
                    capturedPowerMode));

            Assert.That(
                module.TechnicalState,
                Is.EqualTo(
                    ModuleTechnicalState.Destroyed));

            Assert.That(
                module.RequestedPowerMode,
                Is.EqualTo(
                    ModulePowerMode.Offline));
        }

        [Test]
        public void ModulesAreCapturedInDeterministicGridOrder()
        {
            var bastion =
                new Bastion(
                    name: "ordering-test",
                    width: 6,
                    deckCount: 3);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds
                        .SmallMachineRoom);

            ModuleInstance upperRight =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(3, 1))
                    .Module;

            ModuleInstance lower =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(1, 0))
                    .Module;

            ModuleInstance upperLeft =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(0, 1))
                    .Module;

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            CollectionAssert.AreEqual(
                new[]
                {
                    lower.Id,
                    upperLeft.Id,
                    upperRight.Id
                },
                state.Modules
                    .Select(module =>
                        module.ModuleId)
                    .ToArray());
        }

        [Test]
        public void CapturesPassageState()
        {
            var bastion =
                new Bastion(
                    name: "passage-snapshot-test",
                    width: 4,
                    deckCount: 2);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds
                        .SmallMachineRoom);

            ModuleInstance left =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(0, 0))
                    .Module;

            ModuleInstance right =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(1, 0))
                    .Module;

            bool adjacencyFound =
                bastion.TryGetModuleAdjacency(
                    left.Id,
                    right.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(
                adjacencyFound,
                Is.True);

            ModulePassagePlacementResult placement =
                bastion.TryInstallPassage(
                    left.Id,
                    right.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(
                placement.IsSuccess,
                Is.True);

            placement.Passage.SetState(
                ModulePassageState.Locked);

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            Assert.That(
                state.PassageCount,
                Is.EqualTo(1));

            PassagePresentationState passageState =
                state.Passages[0];

            Assert.That(
                passageState.PassageId,
                Is.EqualTo(
                    placement.Passage.Id));

            Assert.That(
                passageState.SourceModuleId,
                Is.EqualTo(left.Id));

            Assert.That(
                passageState.TargetModuleId,
                Is.EqualTo(right.Id));

            Assert.That(
                passageState.Type,
                Is.EqualTo(
                    ModulePassageType.Door));

            Assert.That(
                passageState.State,
                Is.EqualTo(
                    ModulePassageState.Locked));

            Assert.That(
                passageState.IsHorizontal,
                Is.True);
        }

        [Test]
        public void CapturedPassageDoesNotChangeWithSimulation()
        {
            var bastion =
                new Bastion(
                    name: "immutable-passage-test",
                    width: 4,
                    deckCount: 2);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds
                        .SmallMachineRoom);

            ModuleInstance left =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(0, 0))
                    .Module;

            ModuleInstance right =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(1, 0))
                    .Module;

            bastion.TryGetModuleAdjacency(
                left.Id,
                right.Id,
                out ModuleAdjacency adjacency);

            ModulePassage passage =
                bastion.TryInstallPassage(
                        left.Id,
                        right.Id,
                        adjacency.SharedBoundaries[0],
                        ModulePassageType.Door,
                        ModulePassageTraversalMode
                            .Bidirectional)
                    .Passage;

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            PassagePresentationState captured =
                state.Passages[0];

            passage.SetState(
                ModulePassageState.Blocked);

            Assert.That(
                captured.State,
                Is.EqualTo(
                    ModulePassageState.Open));

            Assert.That(
                passage.State,
                Is.EqualTo(
                    ModulePassageState.Blocked));
        }

        [Test]
        public void PassagesAreCapturedInDeterministicBoundaryOrder()
        {
            var bastion =
                new Bastion(
                    name: "passage-order-test",
                    width: 4,
                    deckCount: 2);

            ModuleDefinition definition =
                _catalog.GetRequired(
                    ModuleDefinitionIds
                        .SmallMachineRoom);

            ModuleInstance first =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(0, 0))
                    .Module;

            ModuleInstance second =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(1, 0))
                    .Module;

            ModuleInstance third =
                bastion.TryInstallModule(
                        definition,
                        new GridPosition(2, 0))
                    .Module;

            ModulePassage rightPassage =
                AddDoor(
                    bastion,
                    second,
                    third);

            ModulePassage leftPassage =
                AddDoor(
                    bastion,
                    first,
                    second);

            BastionPresentationState state =
                BastionPresentationStateFactory
                    .Capture(bastion);

            CollectionAssert.AreEqual(
                new[]
                {
                    leftPassage.Id,
                    rightPassage.Id
                },
                state.Passages
                    .Select(passage =>
                        passage.PassageId)
                    .ToArray());
        }

        private static ModulePassage AddDoor(
            Bastion bastion,
            ModuleInstance source,
            ModuleInstance target)
        {
            bool adjacencyFound =
                bastion.TryGetModuleAdjacency(
                    source.Id,
                    target.Id,
                    out ModuleAdjacency adjacency);

            Assert.That(
                adjacencyFound,
                Is.True);

            ModulePassagePlacementResult result =
                bastion.TryInstallPassage(
                    source.Id,
                    target.Id,
                    adjacency.SharedBoundaries[0],
                    ModulePassageType.Door,
                    ModulePassageTraversalMode
                        .Bidirectional);

            Assert.That(
                result.IsSuccess,
                Is.True);

            return result.Passage;
        }
    }
}