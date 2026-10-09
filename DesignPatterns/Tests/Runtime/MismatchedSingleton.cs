namespace NG.DesignPatterns.Tests
{
    /// <summary>
    /// Deliberately passes the wrong type argument, to check that the mistake fails loudly.
    /// </summary>
    public class MismatchedSingleton : Singleton<TestManager> { }
}
