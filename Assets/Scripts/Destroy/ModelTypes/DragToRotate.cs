using UnityEngine;

[RequireComponent(typeof(DragConvertFloat))]
public class DragToRotate : MonoBehaviour
{
    public Transform targetObject;
    public Transform startPoint;
    public Transform endPoint;
    public Vector3 startAngle;
    public Vector3 endAngle;
    private Vector3 angleValue;
    private DragConvertFloat dragConvertFloat;

    public void Init(UnityEngine.Events.UnityAction callBack)
    {
        angleValue = endAngle - startAngle;

        dragConvertFloat = GetComponent<DragConvertFloat>();
        {
            dragConvertFloat.Init(startPoint, endPoint, value =>
            {
                targetObject.localEulerAngles = startAngle + (value * angleValue);

                if (value == 1)
                {
                    callBack?.Invoke();
                    //dragConvertFloat.enabled = false;
                }
            });
        }
    }

    public void Refresh()
    {
        dragConvertFloat.Refresh();
    }
}