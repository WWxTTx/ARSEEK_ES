using UnityEngine;
using UnityEngine.Playables;

public class SetParentBehaviour : PlayableBehaviour
{
    public Transform parentObject;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (parentObject == null) return;

        var transform = playerData as Transform;
        if (transform != null)
        {
            transform.SetParent(parentObject, true);
        }
    }
}