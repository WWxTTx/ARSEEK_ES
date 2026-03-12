using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


public class SetParentClip : PlayableAsset, ITimelineClipAsset
{
    public ExposedReference<Transform> parentObject;
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var playable = ScriptPlayable<SetParentBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.parentObject = parentObject.Resolve(graph.GetResolver());
        return playable;
    }
}