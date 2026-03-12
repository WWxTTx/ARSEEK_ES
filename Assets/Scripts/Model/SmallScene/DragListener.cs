using System;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

public enum MathType
{
    Origin,
    OneMinus,
}

[Serializable]
public class DragListenerParams
{
    public string operationName;
    public MathType MathType;
    public float Ratio;
#if UNITY_EDITOR
    /// <summary>
    /// EDITORÏÔÒþ×´Ì¬
    /// </summary>
    public bool state = true;
#endif
}

public class DragListener : MonoBase
{
    /// <summary>
    /// ¼àÌý¶ÔÏó
    /// </summary>
    public ModelOperation target;
    protected ModelInfo targetInfo;

    protected ModelOperation modelOperation;

    /// <summary>
    /// Ó°Ïì²Ù×÷
    /// </summary>
    public List<DragListenerParams> dragListenerParams = new List<DragListenerParams>();

    private void OnEnable()
    {
        targetInfo = target.GetComponent<ModelInfo>();
        modelOperation = GetComponent<ModelOperation>();

        // Subscribe to rotation events
        ModelOperationEventManager.Subscribe<DragEvent>(OnRotationChanged);
    }

    public void SetFinalState(float openRatio)
    {
        SetMultiplier(openRatio);
    }


    private void OnDisable()
    {
        // Unsubscribe when disabled
        ModelOperationEventManager.Unsubscribe<DragEvent>(OnRotationChanged);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ModelOperationEventManager.Unsubscribe<DragEvent>(OnRotationChanged);
    }

    private void OnRotationChanged(DragEvent dragEvent)
    {
        if (targetInfo.ID == dragEvent.ID)
        {
            SetMultiplier(dragEvent.NormalizedAngleDelta);
        }
    }

    protected void SetMultiplier(float value)
    {
        foreach (var dragParams in dragListenerParams)
        {
            var op = modelOperation.operations.Find(o => dragParams.operationName.Equals(o.name));
            if (op != null && op.behaveBases != null)
            {
                foreach (BehaveBase behave in op.behaveBases)
                {
                    behave.SetMultiplier(ConvertMultiplier(value, dragParams.Ratio, dragParams.MathType));
                }
            }
        }
    }

    private float ConvertMultiplier(float value, float ratio, MathType mathType)
    {
        switch (mathType)
        {
            case MathType.OneMinus:
                return (1 - value) * ratio;
            default:
                return value * ratio;
        }
    }
}
