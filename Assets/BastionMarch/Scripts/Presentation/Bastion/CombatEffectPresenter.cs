using UnityEngine;

namespace BastionMarch.Presentation.Bastions
{
    /// <summary>
    /// Точка расширения Presentation для будущего
    /// воспроизведения результатов боя:
    /// попаданий, пробитий, взрывов, пожаров
    /// и других визуальных эффектов.
    ///
    /// Не содержит боевой логики.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatEffectPresenter : MonoBehaviour
    {
        public void Clear()
        {
            // Пустая точка расширения P2.
            // Реальные эффекты появятся после
            // реализации боевой Simulation.
        }
    }
}