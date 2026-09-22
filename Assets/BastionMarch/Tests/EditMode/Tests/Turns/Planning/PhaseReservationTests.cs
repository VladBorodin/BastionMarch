using System;
using BastionMarch.Simulation.Turns.Planning;
using NUnit.Framework;

namespace BastionMarch.Simulation.EditModeTests
    .Turns.Planning
{
    [TestFixture]
    public sealed class PhaseReservationTests
    {
        [Test]
        public void ReservationStoresIdentity()
        {
            Guid orderId =
                Guid.NewGuid();

            Guid brigadeId =
                Guid.NewGuid();

            var reservation =
                new PhaseReservation(
                    orderId,
                    actionPhase: 2,
                    brigadeId);

            Assert.That(
                reservation.OrderId,
                Is.EqualTo(orderId));

            Assert.That(
                reservation.ActionPhase,
                Is.EqualTo(2));

            Assert.That(
                reservation.BrigadeId,
                Is.EqualTo(brigadeId));
        }

        [Test]
        public void ReservationRejectsEmptyOrderId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new PhaseReservation(
                        Guid.Empty,
                        actionPhase: 1,
                        brigadeId:
                            Guid.NewGuid()));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ReservationRejectsInvalidPhase(
            int invalidPhase)
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new PhaseReservation(
                        Guid.NewGuid(),
                        invalidPhase,
                        Guid.NewGuid()));
        }

        [Test]
        public void ReservationRejectsEmptyBrigadeId()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new PhaseReservation(
                        Guid.NewGuid(),
                        actionPhase: 1,
                        brigadeId:
                            Guid.Empty));
        }
    }
}