using System;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Simulation.Bastions;
using UnityEngine;

namespace BastionMarch.Presentation.Prototype
{
    /// <summary>
    /// Входная точка технической прототипной сцены.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BastionPrototypeBootstrap
        : MonoBehaviour
    {
        [SerializeField]
        private BastionPresenter _bastionPresenter;

        public Bastion Bastion
        {
            get;
            private set;
        }

        private void Reset()
        {
            ResolvePresenter();
        }

        private void Start()
        {
            if (_bastionPresenter == null)
            {
                throw new InvalidOperationException(
                    "Prototype bootstrap requires BastionPresenter.");
            }

            Bastion =
                PrototypeBastionFactory.Create();

            _bastionPresenter.Initialize(
                Bastion);
        }

        private void ResolvePresenter()
        {
            if (_bastionPresenter != null)
            {
                return;
            }

            if (transform.parent == null)
            {
                return;
            }

            _bastionPresenter =
                transform.parent
                    .GetComponentInChildren<
                        BastionPresenter>(
                            includeInactive: true);
        }
    }
}