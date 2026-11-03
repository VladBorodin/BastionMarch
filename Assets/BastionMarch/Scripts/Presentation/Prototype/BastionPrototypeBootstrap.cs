using System;
using BastionMarch.Presentation.Bastions;
using BastionMarch.Simulation.Bastions;
using BastionMarch.Simulation.Crew;
using BastionMarch.Simulation.Turns;
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

        public BastionTurnSession TurnSession
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

            Bastion = PrototypeBastionFactory.Create();

            TurnSession = new BastionTurnSession(Bastion, InitialBrigadeWorkProfiles.CreateCatalog());

            _bastionPresenter.Initialize(Bastion);

            ModuleSelectionController moduleSelection = FindAnyObjectByType<ModuleSelectionController>();

            if (moduleSelection == null)
            {
                throw new InvalidOperationException("Prototype scene requires ModuleSelectionController.");
            }

            PrototypeMovePlanningController planning = GetComponent<PrototypeMovePlanningController>();

            if (planning == null)
            {
                planning = gameObject.AddComponent<PrototypeMovePlanningController>();
            }

            planning.Initialize(TurnSession, moduleSelection);
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