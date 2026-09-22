using System;

namespace BastionMarch.Simulation.Turns.Planning
{
    public sealed class PhaseReservationResult
    {
        public bool IsSuccess =>
            FailureReason ==
            PhaseReservationFailureReason.None;

        public PhaseReservationFailureReason
            FailureReason
        {
            get;
        }

        public PhaseReservation Reservation
        {
            get;
        }

        private PhaseReservationResult(
            PhaseReservationFailureReason
                failureReason,
            PhaseReservation reservation)
        {
            FailureReason =
                failureReason;

            Reservation =
                reservation;
        }

        public static PhaseReservationResult
            Success(
                PhaseReservation reservation)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(
                    nameof(reservation));
            }

            return new PhaseReservationResult(
                PhaseReservationFailureReason.None,
                reservation);
        }

        public static PhaseReservationResult
            Failure(
                PhaseReservationFailureReason
                    failureReason)
        {
            if (failureReason ==
                PhaseReservationFailureReason.None)
            {
                throw new ArgumentException(
                    "Failure result must contain " +
                    "a failure reason.",
                    nameof(failureReason));
            }

            return new PhaseReservationResult(
                failureReason,
                reservation: null);
        }
    }
}