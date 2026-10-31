using System;
using System.Collections.Generic;

namespace BastionMarch.Simulation.Turns.Resolution
{
    /// <summary>
    /// Runtime-владелец execution state Orders.
    ///
    /// Один OrderId может иметь не более
    /// одного OrderExecutionState.
    ///
    /// Registry не привязан к одному Turn
    /// и поэтому может переживать TurnEnd.
    /// </summary>
    public sealed class OrderExecutionRegistry
    {
        private readonly Dictionary<
            Guid,
            OrderExecutionState>
                _statesByOrderId;

        public int Count =>
            _statesByOrderId.Count;

        public OrderExecutionRegistry()
        {
            _statesByOrderId =
                new Dictionary<
                    Guid,
                    OrderExecutionState>();
        }

        public bool TryGet(
            Guid orderId,
            out OrderExecutionState state)
        {
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Order id cannot be empty.",
                    nameof(orderId));
            }

            return _statesByOrderId.TryGetValue(
                orderId,
                out state);
        }

        public void Add(
            OrderExecutionState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            if (_statesByOrderId.ContainsKey(
                    state.OrderId))
            {
                throw new InvalidOperationException(
                    "Execution state for this " +
                    "order already exists.");
            }

            _statesByOrderId.Add(
                state.OrderId,
                state);
        }
    }
}