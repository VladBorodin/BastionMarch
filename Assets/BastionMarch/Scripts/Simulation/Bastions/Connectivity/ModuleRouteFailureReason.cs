namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Причина, по которой маршрут не был построен.
    ///
    /// Пользовательские формулировки должны находиться
    /// в локализации Presentation-слоя.
    /// </summary>
    public enum ModuleRouteFailureReason
    {
        None,

        SourceModuleNotFound,

        TargetModuleNotFound,

        /// <summary>
        /// Между модулями вообще не существует
        /// непрерывной цепочки физических переходов.
        /// </summary>
        NoStructuralConnection,

        /// <summary>
        /// Физическая цепочка существует, но текущие
        /// состояния или направления переходов
        /// не позволяют пройти по ней.
        /// </summary>
        TraversalBlocked
    }
}