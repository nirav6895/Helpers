using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace NG.Utilities
{
    public static class TimelineExtensions
    {
        public static IEnumerator PlayAndWait(this PlayableDirector director)
        {
            if (director == null || director.playableAsset == null)
            {
                Debug.LogWarning("PlayableDirector or its asset is null.");
                yield break;
            }

            director.Play();

            while (director.state == PlayState.Playing)
                yield return null;
        }
    }
}

