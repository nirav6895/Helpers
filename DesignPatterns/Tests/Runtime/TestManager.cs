namespace NG.DesignPatterns.Tests
{
    public class TestManager : Singleton<TestManager>
    {
        public int AwakeCount;

        protected override void OnAwake()
        {
            AwakeCount++;
        }
    }
}
