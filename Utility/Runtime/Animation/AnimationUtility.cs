namespace NG.Utility.Animation
{
    #region Using
    using System;
    using System.Collections;
    using UnityEngine;
    #endregion

    /// <summary>
    /// Utility used for basic type of animation using script instead of Animator
    /// For Example, Move/Rotate/Scale GameObject, Fade In/Out Effect
    /// </summary>
    public class AnimationUtility : MonoBehaviour
    {
        /// <summary>
        /// Play Animation with given Total Time
        /// </summary>
        /// <param name="Output">Callback of updated delta value</param>
        /// <param name="totalTime">Total Time of Animation</param>
        /// <param name="startValue">Start Value</param>
        /// <param name="endValue">End Value</param>
        /// <returns>IEnumerator</returns>
        public static IEnumerator PlayAnimationByTime(Action<float> Output, float totalTime = 1f, float startValue = 0f, float endValue = 1f)
        {
            float diff = endValue - startValue;
            float elapsedTime = 0f;
            float value;

            while (elapsedTime < totalTime)
            {
                // Set Value
                value = startValue + (elapsedTime / totalTime * diff);
                Output(value);

                // Update Elapsed Time
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Output(endValue);
        }
    }
}