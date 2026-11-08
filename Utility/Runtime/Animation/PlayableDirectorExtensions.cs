#region Using
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="PlayableDirector"/>, e.g. a Timeline.
    /// </summary>
    public static class PlayableDirectorExtensions
    {
        #region Public Methods
        /// <summary>
        /// Plays the director and waits until it has finished. Use it in a coroutine: <c>yield return director.PlayAndWait();</c>
        /// <para>
        /// The wait ends when the director stops (it reaches the end with Wrap Mode None, or Stop() is called), when it
        /// reaches the end with Wrap Mode Hold, or when it is destroyed or disabled. Pausing it doesn't end the wait.
        /// With Wrap Mode Loop, or Update Method Manual, it only ends when Stop() is called.
        /// </para>
        /// If the director or its Playable asset is missing, a warning is logged and the wait ends at once.
        /// </summary>
        public static IEnumerator PlayAndWait(this PlayableDirector director)
        {
            if (director == null)
            {
                Debug.LogWarning("[PlayableDirectorExtensions] Can't play: the PlayableDirector is null or destroyed.");
                yield break;
            }

            if (director.playableAsset == null)
            {
                Debug.LogWarning($"[PlayableDirectorExtensions] Can't play the PlayableDirector on '{director.name}': it has no Playable asset.", director);
                yield break;
            }

            if (!director.isActiveAndEnabled)
            {
                Debug.LogWarning($"[PlayableDirectorExtensions] Can't play the PlayableDirector on '{director.name}': it is disabled or its GameObject is inactive.", director);
                yield break;
            }

            bool stopped = false;
            void OnStopped(PlayableDirector _) => stopped = true;

            director.stopped += OnStopped;
            try
            {
                director.Play();

                while (!stopped && director != null && director.isActiveAndEnabled)
                {
                    // With Wrap Mode Hold the director keeps playing on its last frame and never stops by itself.
                    if (director.extrapolationMode == DirectorWrapMode.Hold && director.time >= director.duration)
                        yield break;

                    yield return null;
                }
            }
            finally
            {
                if (director != null)
                    director.stopped -= OnStopped;
            }
        }
        #endregion
    }
}
