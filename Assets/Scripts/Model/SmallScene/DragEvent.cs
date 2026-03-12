using UnityEngine;

/// <summary>
/// 模型拖拽事件
/// </summary>
public class DragEvent
{
    /// <summary>
    /// ModelInfo ID
    /// </summary>
    public string ID { get; }

    public GameObject Target { get; }
    //public Vector3 CurrentEulerAngles { get; }
    //public Vector3 RotationDelta { get; }
    //public float CurrentAngle { get; } // For single axis mode
    public float AngleDelta { get; } // For single axis mode

    public float NormalizedAngleDelta { get; }

    public DragEvent(string id, GameObject target, /*Vector3 currentEuler, Vector3 delta, float currentAngle = 0,*/ float angleDelta = 0, float normalizedAngleDelta = 0)
    {
        ID = id;
        Target = target;
        //CurrentEulerAngles = currentEuler;
        //RotationDelta = delta;
        //CurrentAngle = currentAngle;
        AngleDelta = angleDelta;
        NormalizedAngleDelta = normalizedAngleDelta;
    }
}

public class ModelStateEvent
{
    public string ID { get; }
    public string State { get; }

    public ModelStateEvent(string id, string state)
    {
        ID = id;
        State = state;
    }
}