using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Точка Presentation-слоя для будущего
    /// отображения запланированных действий хода.
    ///
    /// Конкретные данные плана будут подключены
    /// после появления контрактов Simulation
    /// этапов 12-13.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurnPlanView : MonoBehaviour
    {
        public bool IsVisible =>
            gameObject.activeSelf;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}