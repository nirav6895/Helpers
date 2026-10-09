namespace NG.DesignPatterns
{
    /// <summary>
    /// Marks a class or struct as an event for <see cref="EventBus"/>. The event's type is the key,
    /// and the instance is the payload passed to every listener.
    /// </summary>
    public interface IEvent { }
}
