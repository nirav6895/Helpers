namespace NG.DesignPatterns.DataBase
{
    /// <summary>
    /// ISavable Interface: Implement to make a class storable in the SessionStateManager.
    /// </summary>
    public interface IData
    {
        public static void Save(IData savable)
        {
            DataBase.Save(savable);
        }
    }
}