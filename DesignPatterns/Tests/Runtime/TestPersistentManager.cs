namespace NG.DesignPatterns.Tests
{
    public class TestPersistentManager : Singleton<TestPersistentManager>
    {
        protected override bool PersistAcrossScenes => true;
    }
}
