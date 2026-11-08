#region Using
using UnityEngine;
using UnityEngine.Playables;
#endregion

namespace NG.Utility.Tests
{
    /// <summary>
    /// An empty playable with a set duration, so PlayableDirector can be tested without the Timeline package.
    /// </summary>
    public class TestPlayableAsset : PlayableAsset
    {
        public double length = 0.2;

        public override double duration => length;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }
    }
}
