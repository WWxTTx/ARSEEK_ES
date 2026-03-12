using UnityEngine.Playables;
using UnityEngine;

public class SetParentMixerBehaviour : PlayableBehaviour
{
    public Transform defaultParent;
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount();
        Transform bindedTransform = playerData as Transform;
        bool clipFound = false;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<SetParentBehaviour> inputPlayable = (ScriptPlayable<SetParentBehaviour>)playable.GetInput(i);
            SetParentBehaviour inputBehaviour = inputPlayable.GetBehaviour();

            if (inputWeight > 0.5f)
            {
                clipFound = true;
                inputBehaviour.ProcessFrame(playable, info, bindedTransform);
            }
        }

        if (!clipFound && bindedTransform != null)
        {
            bindedTransform.SetParent(defaultParent, true);
        }
    }
}