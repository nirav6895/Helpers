using UnityEngine;

namespace NG.DesignPatterns.Tests
{
    public interface ITestMonoService : IService { }

    /// <summary>
    /// MonoBehaviour service, used to check that destroyed services count as missing.
    /// </summary>
    public class TestService : MonoBehaviour, ITestMonoService { }
}
