
using System;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Modules;
using BastionMarch.Simulation.Modules.Catalog;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests.Turns.Planning
{
    [TestFixture]
    public sealed class BrigadeRoutePlanTests
    {
        private ModuleInstance[] _modules;
        private ModuleRoute _route;

        [SetUp]
        public void SetUp()
        {
            var grid = new BastionGrid(
                width: 4,
                deckCount: 1);

            ModuleDefinition definition =
                InitialModuleDefinitions.CreateCatalog()
                    .GetRequired(
                        ModuleDefinitionIds.SmallMachineRoom);

            _modules = new ModuleInstance[4];

            for (int x = 0; x < _modules.Length; x++)
            {
                ModulePlacementResult placement =
                    grid.TryPlaceModule(
                        definition,
                        new GridPosition(x, 0));

                Assert.That(placement.IsSuccess, Is.True);

                _modules[x] = placement.Module;
            }

            for (int i = 0; i < _modules.Length - 1; i++)
            {
                Assert.That(
                    grid.TryGetModuleAdjacency(
                        _modules[i].Id,
                        _modules[i + 1].Id,
                        out ModuleAdjacency adjacency),
                    Is.True);

                ModulePassagePlacementResult passage =
                    grid.TryAddPassage(
                        _modules[i].Id,
                        _modules[i + 1].Id,
                        adjacency.SharedBoundaries[0],
                        ModulePassageType.Door,
                        ModulePassageTraversalMode.Bidirectional);

                Assert.That(passage.IsSuccess, Is.True);
            }

            ModuleRouteSearchResult search =
                grid.FindModuleRoute(
                    _modules[0].Id,
                    _modules[3].Id);

            Assert.That(search.IsSuccess, Is.True);
            Assert.That(search.Route.StepCount, Is.EqualTo(3));

            _route = search.Route;
        }

        [Test]
        public void NewPlanStartsAtRouteSource()
        {
            var plan = new BrigadeRoutePlan(
                Guid.NewGuid(), _route);

            Assert.That(plan.IsActive, Is.True);
            Assert.That(plan.IsCompleted, Is.False);
            Assert.That(plan.IsBlocked, Is.False);

            Assert.That(plan.TotalStepCount, Is.EqualTo(3));
            Assert.That(plan.CompletedStepCount, Is.Zero);
            Assert.That(plan.RemainingStepCount, Is.EqualTo(3));

            Assert.That(plan.CurrentModuleId,
                Is.EqualTo(_modules[0].Id));

            Assert.That(plan.TargetModuleId,
                Is.EqualTo(_modules[3].Id));

            Assert.That(plan.NextStep,
                Is.SameAs(_route.Steps[0]));
        }

        [Test]
        public void CompletedStepsAdvanceInOrder()
        {
            var plan = new BrigadeRoutePlan(
                Guid.NewGuid(), _route);

            for (int index = 0; index < 3; index++)
            {
                ModuleRouteStep step = _route.Steps[index];

                Assert.That(
                    plan.TryAdvance(
                        step.PassageId,
                        step.FromModuleId,
                        step.ToModuleId),
                    Is.True);

                Assert.That(plan.CompletedStepCount,
                    Is.EqualTo(index + 1));

                Assert.That(plan.CurrentModuleId,
                    Is.EqualTo(_modules[index + 1].Id));
            }

            Assert.That(plan.IsCompleted, Is.True);
            Assert.That(plan.IsActive, Is.False);
            Assert.That(plan.RemainingStepCount, Is.Zero);
            Assert.That(plan.NextStep, Is.Null);

            ModuleRouteStep last = _route.Steps[2];

            Assert.That(
                plan.TryAdvance(
                    last.PassageId,
                    last.FromModuleId,
                    last.ToModuleId),
                Is.False);
        }

        [Test]
        public void WrongStepDoesNotChangeProgress()
        {
            var plan = new BrigadeRoutePlan(
                Guid.NewGuid(), _route);

            ModuleRouteStep second = _route.Steps[1];

            Assert.That(
                plan.TryAdvance(
                    second.PassageId,
                    second.FromModuleId,
                    second.ToModuleId),
                Is.False);

            Assert.That(plan.CompletedStepCount, Is.Zero);

            Assert.That(plan.CurrentModuleId,
                Is.EqualTo(_modules[0].Id));
        }

        [Test]
        public void BlockedPlanPreservesCompletedSteps()
        {
            var plan = new BrigadeRoutePlan(
                Guid.NewGuid(), _route);

            ModuleRouteStep first = _route.Steps[0];

            Assert.That(
                plan.TryAdvance(
                    first.PassageId,
                    first.FromModuleId,
                    first.ToModuleId),
                Is.True);

            Assert.That(plan.TryMarkBlocked(), Is.True);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.IsActive, Is.False);
            Assert.That(plan.IsCompleted, Is.False);

            Assert.That(plan.CompletedStepCount, Is.EqualTo(1));
            Assert.That(plan.RemainingStepCount, Is.EqualTo(2));

            Assert.That(plan.CurrentModuleId,
                Is.EqualTo(_modules[1].Id));

            ModuleRouteStep second = _route.Steps[1];

            Assert.That(
                plan.TryAdvance(
                    second.PassageId,
                    second.FromModuleId,
                    second.ToModuleId),
                Is.False);
        }
    }
}
