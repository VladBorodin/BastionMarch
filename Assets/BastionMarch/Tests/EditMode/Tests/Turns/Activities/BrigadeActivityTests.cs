using System;
using System.Linq;
using BastionMarch.Simulation.Turns.Activities;
using BastionMarch.Simulation.Turns.Orders;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Activities
{
    [TestFixture]
    public sealed class BrigadeActivityTests
    {
        [Test]
        public void ActivityStoresBrigadeIdentity()
        {
            Guid brigadeId =
                Guid.NewGuid();

            var activity =
                new TestBrigadeActivity(
                    brigadeId);

            Assert.That(
                activity.BrigadeId,
                Is.EqualTo(brigadeId));
        }

        [Test]
        public void ActivityRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new TestBrigadeActivity(
                        Guid.Empty));
        }

        [Test]
        public void ActivityContractContainsOnlyBrigadeIdentity()
        {
            string[] propertyNames =
                typeof(IBrigadeActivity)
                    .GetProperties()
                    .Select(property =>
                        property.Name)
                    .OrderBy(name =>
                        name)
                    .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(
                        IBrigadeActivity
                            .BrigadeId)
                },
                propertyNames);
        }

        [Test]
        public void ActivityBrigadeIdentityIsReadOnly()
        {
            Assert.That(
                typeof(IBrigadeActivity)
                    .GetProperty(
                        nameof(
                            IBrigadeActivity
                                .BrigadeId))
                    .CanWrite,
                Is.False);
        }

        [Test]
        public void ActivityIsNotTurnOrder()
        {
            var activity =
                new TestBrigadeActivity(
                    Guid.NewGuid());

            Assert.That(
                activity,
                Is.Not.InstanceOf<
                    ITurnOrder>());
        }

        [Test]
        public void ActivityHasNoOrderDurationContract()
        {
            Assert.That(
                typeof(IBrigadeActivity)
                    .GetProperty(
                        nameof(
                            ITurnOrder
                                .RequiredPhases)),
                Is.Null);
        }

        private sealed class TestBrigadeActivity :
            BrigadeActivity
        {
            public TestBrigadeActivity(
                Guid brigadeId)
                : base(
                    brigadeId)
            {
            }
        }
    }
}