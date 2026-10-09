namespace NG.DesignPatterns
{
    /// <summary>
    /// Marks a type that can be registered in the <see cref="ServiceLocator"/>.
    /// Have your service interfaces extend it, e.g. <c>public interface IAudioService : IService</c>.
    /// </summary>
    public interface IService { }
}
