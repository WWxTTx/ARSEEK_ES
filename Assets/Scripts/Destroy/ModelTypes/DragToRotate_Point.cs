using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DragConvertFloat))]
public class DragToRotate_Point : MonoBehaviour
{
    public Transform targetObject;
    public Transform startPoint;
    public Transform endPoint;
    public Vector3 startAngle;
    public Vector3 endAngle;
    public int gear;
    public List<string> operations;

    private DragConvertFloat dragConvertFloat;
    private Vector3 angleValue;
    private float gearValue;
    private int currentGear;


    public void Init(UnityEngine.Events.UnityAction<string> callBack)
    {
        gear -= 1;
        gearValue = 1f / gear;
        angleValue = endAngle - startAngle;

        dragConvertFloat = GetComponent<DragConvertFloat>();
        {
            dragConvertFloat.Init(startPoint, endPoint, value =>
            {
                currentGear = Mathf.CeilToInt(value / gearValue);
                targetObject.localEulerAngles = startAngle + (currentGear * gearValue * angleValue);

                if (operations.Count > currentGear)
                {
                    callBack?.Invoke(operations[currentGear]);
                }
            });
        }
    }

    public void Refresh()
    {
        dragConvertFloat.Refresh();
    }
}