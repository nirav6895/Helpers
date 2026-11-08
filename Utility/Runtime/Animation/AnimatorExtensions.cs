#region Using
using System.Collections;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="Animator"/>.
    /// </summary>
    public static class AnimatorExtensions
    {
        #region Public Methods
        /// <summary>
        /// Plays a state from its start and waits until it has played once. Use it in a coroutine:
        /// <c>yield return animator.PlayAndWait("Open");</c>
        /// <para>
        /// The wait ends when the state reaches its end, when the animator leaves the state (e.g. through a transition
        /// with an exit time), or when the animator is destroyed or disabled. A looping state ends after its first loop.
        /// It follows the animator's own clock, so Animator.speed and the Update Mode are respected: an animator that uses
        /// Unscaled Time finishes while the game is paused.
        /// </para>
        /// <para>
        /// The state is applied at once with <c>Animator.Update(0)</c>, so its first frame shows on this frame.
        /// If the animator has no such state, a warning is logged and the wait ends at once.
        /// </para>
        /// </summary>
        /// <param name="animator">The animator to play.</param>
        /// <param name="stateName">The state's name, e.g. "Open", or its full path, e.g. "Base Layer.Open".</param>
        /// <param name="layer">Index of the layer that has the state.</param>
        public static IEnumerator PlayAndWait(this Animator animator, string stateName, int layer = 0)
        {
            return PlayAndWaitRoutine(animator, Animator.StringToHash(stateName), layer, stateName);
        }

        /// <inheritdoc cref="PlayAndWait(Animator, string, int)"/>
        /// <param name="stateHash">Hash of the state's name or full path, from <see cref="Animator.StringToHash"/>.</param>
        public static IEnumerator PlayAndWait(this Animator animator, int stateHash, int layer = 0)
        {
            return PlayAndWaitRoutine(animator, stateHash, layer, stateHash.ToString());
        }
        #endregion


        #region Private Methods
        private static IEnumerator PlayAndWaitRoutine(Animator animator, int stateHash, int layer, string stateLabel)
        {
            if (animator == null)
            {
                Debug.LogWarning($"[AnimatorExtensions] Can't play '{stateLabel}': the Animator is null or destroyed.");
                yield break;
            }

            if (!animator.isActiveAndEnabled)
            {
                Debug.LogWarning($"[AnimatorExtensions] Can't play '{stateLabel}': the Animator on '{animator.name}' is disabled or its GameObject is inactive.", animator);
                yield break;
            }

            if (layer < 0 || layer >= animator.layerCount || !animator.HasState(layer, stateHash))
            {
                Debug.LogWarning($"[AnimatorExtensions] The Animator on '{animator.name}' has no state '{stateLabel}' on layer {layer}.", animator);
                yield break;
            }

            animator.Play(stateHash, layer, 0f);

            // Play() is only applied on the animator's next update. Apply it now, so the state info below is the new state's.
            animator.Update(0f);

            while (animator != null && animator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
                bool inState = state.shortNameHash == stateHash || state.fullPathHash == stateHash;
                if (!inState || state.normalizedTime >= 1f)
                    yield break;

                yield return null;
            }
        }
        #endregion
    }
}
