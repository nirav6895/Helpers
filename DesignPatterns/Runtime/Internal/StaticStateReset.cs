#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Clears the package's static registries (services, session data, event listeners) every time Play Mode starts.
    /// With Domain Reload disabled in Enter Play Mode Settings, static fields survive between Play sessions;
    /// without this, a session would start with the previous session's services and listeners.
    /// </summary>
    internal static class StaticStateReset
    {
        #region Variables
        private static readonly List<Action> resetActions = new();
        #endregion


        #region Methods
        /// <summary>
        /// Registers a reset callback. Call it from a static constructor so each class registers once.
        /// Generic static classes (e.g. EventBus&lt;TEnum&gt;) can't use RuntimeInitializeOnLoadMethod
        /// themselves, because Unity can't know which type arguments to call it with.
        /// </summary>
        internal static void Register(Action reset)
        {
            resetActions.Add(reset);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetAll()
        {
            foreach (Action reset in resetActions)
                reset();
        }
        #endregion
    }
}
