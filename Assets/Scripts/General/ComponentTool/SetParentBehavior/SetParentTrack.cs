using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.855f, 0.8623f, 0.87f)]
[TrackClipType(typeof(SetParentClip))]
[TrackBindingType(typeof(Transform))]
public class SetParentTrack : TrackAsset
{
    public ExposedReference<Transform> defaultParent;

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var mixerPlayable = ScriptPlayable<SetParentMixerBehaviour>.Create(graph, inputCount);
        var mixerBehaviour = mixerPlayable.GetBehaviour();
        mixerBehaviour.defaultParent = defaultParent.Resolve(graph.GetResolver());
        return mixerPlayable;
    }
}