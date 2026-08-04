using System;
using BastionMarch.Presentation.Bastions.State;
using BastionMarch.Simulation.Bastions;
using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Соединяет изменяемую модель Simulation
    /// с неизменяемыми снимками Presentation.
    ///
    /// View не получает прямую ссылку на Bastion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BastionPresenter : MonoBehaviour
    {
        [SerializeField]
        private BastionView _view;

        public Bastion SourceBastion
        {
            get;
            private set;
        }

        public BastionPresentationState CurrentState
        {
            get;
            private set;
        }

        public bool IsInitialized =>
            SourceBastion != null;

        public event Action<BastionPresentationState>
            StatePresented;

        private void Reset()
        {
            ResolveView();
        }

        private void Awake()
        {
            ResolveView();
        }

        public void Initialize(
            Bastion bastion)
        {
            SourceBastion = bastion ??
                throw new ArgumentNullException(
                    nameof(bastion));

            RefreshPresentation();
        }

        /// <summary>
        /// Создаёт новый снимок текущей Simulation
        /// и передаёт его во View.
        /// </summary>
        public void RefreshPresentation()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "BastionPresenter must be initialized before refresh.");
            }

            BastionPresentationState state =
                BastionPresentationStateFactory.Capture(
                    SourceBastion);

            CurrentState = state;

            _view.Render(state);

            StatePresented?.Invoke(state);
        }

        public void Clear()
        {
            _view.Clear();

            SourceBastion = null;
            CurrentState = null;
        }

        private void ResolveView()
        {
            if (_view == null)
            {
                _view =
                    GetComponent<BastionView>();
            }

            if (_view == null)
            {
                throw new InvalidOperationException(
                    "BastionPresenter requires BastionView " +
                    "on the same GameObject.");
            }
        }
    }
}