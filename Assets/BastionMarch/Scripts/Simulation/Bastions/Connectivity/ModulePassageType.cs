namespace BastionMarch.Simulation.Bastions
{
    /// <summary>
    /// Физическая конструкция, позволяющая перемещаться
    /// между двумя соседними отсеками.
    /// </summary>
    public enum ModulePassageType
    {
        /// <summary>
        /// Горизонтальный проход через общую стену.
        /// </summary>
        Door,

        /// <summary>
        /// Вертикальный проход через пол или потолок.
        /// </summary>
        Hatch,

        /// <summary>
        /// Вертикальная подъёмная лестница.
        /// </summary>
        Ladder,

        /// <summary>
        /// Вертикальный лестничный переход.
        /// </summary>
        Stairway,

        /// <summary>
        /// Вертикальный механизированный переход.
        /// </summary>
        Elevator
    }
}