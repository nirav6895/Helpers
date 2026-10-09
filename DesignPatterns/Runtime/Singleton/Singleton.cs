#region Using
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Base class for a MonoBehaviour that should exist only once, e.g.
    /// <c>public class AudioManager : Singleton&lt;AudioManager&gt;</c>.
    /// <para>
    /// When a second instance wakes up, its whole GameObject is destroyed and <see cref="Instance"/> keeps the first.
    /// Put setup code in <see cref="OnAwake"/>, which only runs on the instance that is kept; don't declare your
    /// own Awake (the compiler warns with CS0108 if you do). If you override <see cref="OnDestroy"/>, call base.OnDestroy().
    /// </para>
    /// </summary>
    /// <typeparam name="T">The deriving class itself.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        #region Inspector Variables
        [Header("Singleton")]
        [Tooltip("Keep this object alive across scene loads (DontDestroyOnLoad). It is moved to the root of the hierarchy.")]
        [SerializeField] private bool persistAcrossScenes = false;
        #endregion


        #region Properties
        /// <summary>
        /// The active instance, or null if there is none or it has been destroyed.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Whether this object survives scene loads. Defaults to the Inspector setting; override to force it in code.
        /// </summary>
        protected virtual bool PersistAcrossScenes => persistAcrossScenes;
        #endregion


        #region Unity Methods
        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    Debug.LogWarning($"[Singleton] A second {typeof(T).Name} exists on '{name}'. It is only destroyed automatically in Play Mode.", this);

                return;
            }

            // A hard cast: a class that passes the wrong T (class A : Singleton<B>) fails here instead of leaving Instance null.
            Instance = (T)this;

            if (PersistAcrossScenes && Application.isPlaying)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            OnAwake();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion


        #region Virtual Methods
        /// <summary>
        /// Called from Awake on the instance that is kept. Put your setup code here.
        /// </summary>
        protected virtual void OnAwake() { }
        #endregion
    }
}
