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
        private BastionView _bastionView;

        public Bastion Bastion { get; private set; }

        private void Reset()
        {
            if (_bastionView == null &&
                transform.parent != null)
            {
                _bastionView =
                    transform.parent
                        .GetComponentInChildren<BastionView>(
                            includeInactive: true);
            }
        }

        private void Start()
        {
            if (_bastionView == null)
            {
                throw new InvalidOperationException(
                    "Prototype bootstrap requires BastionView.");
            }

            Bastion =
                PrototypeBastionFactory.Create();

            _bastionView.Bind(
                Bastion);
        }
    }
}