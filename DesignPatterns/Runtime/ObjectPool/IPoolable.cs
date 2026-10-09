namespace NG.DesignPatterns
{
    /// <summary>
    /// Implement on items of an <see cref="ObjectPool{T}"/>, or on the pooled component of a
    /// <see cref="ComponentPool{T}"/>, to reset state between uses.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called after the item has been taken from the pool and activated.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when the item is released, before it is deactivated.
        /// </summary>
        void OnDespawn();
    }
}
