using System.Collections;
using UnityEngine;

namespace NG.Utilities
{
    public static class AnimatorExtensions
    {
        public static IEnumerator PlayAndWait(this Animator animator, string stateName, int layer = 0)
        {

            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("Animator is null or inactive.");
                yield break;
            }

            animator.Play(stateName, layer);

            while (!animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                yield return null;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            float length = stateInfo.length;

            yield return new WaitForSeconds(length);
        }
    }
}

