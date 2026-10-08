namespace NG.DesignPatterns.DataBase
{
    /// <summary>
    /// Extension methods for the ISavable interface.
    /// Provides a convenient way to save an ISavable object to the session state.
    /// </summary>
    public static class IDataExtensions
    {
        /// <summary>
        /// Saves the current ISavable object into the SessionStateManager using its runtime type.
        /// Can be called on any instance of a class implementing ISavable.
        /// </summary>
        /// <param name="savable">The ISavable object to save.</param>
        public static void SaveState(this IData savable)
        {
            DataBase.Save(savable);
        }
    }
}