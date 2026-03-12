using UnityEngine;

public class OnRotateEvent : MonoBehaviour
{
    /// <summary>
    /// 目标物体
    /// </summary>
    public Transform targetObject;
    /// <summary>
    /// 目标轴向
    /// </summary>
    public Axis axis;
    /// <summary>
    /// 最小目标角度
    /// </summary>
    public float minTargetAngle;
    /// <summary>
    /// 最大目标角度
    /// </summary>
    public float maxTargetAngle;
    /// <summary>
    /// 最小角度
    /// </summary>
    public float minAngle = 0;
    /// <summary>
    /// 最大角度
    /// </summary>
    public float maxAngle = 170;
    /// <summary>
    /// 旋转方法
    /// </summary>
    private UnityEngine.Events.UnityAction<Vector3> Rotate;
    /// <summary>
    /// 射线
    /// </summary>
    private Ray ray;
    /// <summary>
    /// 击中点
    /// </summary>
    private RaycastHit hit;
    /// <summary>
    /// 主相机
    /// </summary>
    private Camera mainCamera;
    /// <summary>
    /// 自身collider
    /// </summary>
    private Collider selfCollider;

    public void Init(UnityEngine.Events.UnityAction callBack)
    {
        enabled = true;

        mainCamera = Camera.main;
        selfCollider = GetComponent<Collider>();

        if (mainCamera == null || selfCollider == null)
        {
            Debug.LogError("没有主相机或未找到collider", gameObject);
            enabled = false;
            return;
        }

        var direction = Vector3.zero;
        var angle = 0f;

        switch (axis)
        {
            case Axis.X:
                direction = Vector3.right;

                Rotate = point =>
                {
                    point = point - Vector3.Dot(point, direction) * direction;

                    angle = Mathf.Atan2(point.z, point.y) * Mathf.Rad2Deg;
                };
                break;
            case Axis.Y:
                direction = Vector3.up;

                Rotate = point =>
                {
                    point = point - Vector3.Dot(point, direction) * direction;

                    angle = Mathf.Atan2(point.x, point.z) * Mathf.Rad2Deg;
                };
                break;
            case Axis.Z:
                direction = Vector3.forward;

                Rotate = point =>
                {
                    point = point - Vector3.Dot(point, direction) * direction;

                    angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                };
                break;
        }

        Rotate += point =>
        {
            if (angle >= minTargetAngle && angle <= maxTargetAngle)
            {
                targetObject.localEulerAngles = maxTargetAngle * direction;
                callBack?.Invoke();
                enabled = false;
            }
            else if (angle >= minAngle && angle <= maxAngle)
            {
                targetObject.localEulerAngles = angle * direction;
            }
        };
    }
    private void OnMouseOver()
    {
        if (Input.GetMouseButton(1))
        {
            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider == selfCollider)
                {
                    Rotate(transform.InverseTransformPoint(hit.point));
                    //var D = targetObject.eulerAngles[(int)axis];

                    //if (D > 180)
                    //{
                    //    D -= 360;
                    //}

                    //D = V - D;

                    //if (D < 180 && D > -180)
                    //{
                    //    targetObject.eulerAngles = V * direction;
                    //    targetObject.DORotate(V * direction, animeTime);
                    //}
                    //else
                    //{
                    //    targetObject.DORotate((targetObject.eulerAngles[(int)axis] + (D / 2)) * direction, animeTime / 2);
                    //}
                }
            }
        }
    }

    //private float GetAngle(Vector3 hitPoint)
    //{
    //    hitPoint = hitPoint - Vector3.Dot(hitPoint, direction) * direction;
    //    //Debug.LogError(Mathf.Atan2(hitPoint.x, hitPoint.z) * Mathf.Rad2Deg);
    //    return AngleClamp(Mathf.Atan2(hitPoint.x, hitPoint.z) * Mathf.Rad2Deg, minAngle, maxAngle);
    //}
    //private float? GetAngle2(Vector3 hitPoint)
    //{
    //    hitPoint = hitPoint - Vector3.Dot(hitPoint, direction) * direction;
    //    var V = Mathf.Atan2(hitPoint.x, hitPoint.z) * Mathf.Rad2Deg;

    //    if (V >= minAngle && V <= maxAngle)
    //    {
    //        return V;
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    //private float AngleClamp(float target, float min, float max)
    //{
    //    var range = (360 - max + min) / 2;

    //    if (target < min)
    //    {
    //        if (target < min - range)
    //        {
    //            return max;
    //        }
    //        else
    //        {
    //            return min;
    //        }
    //    }
    //    else if (target > max)
    //    {
    //        if (target > max + range)
    //        {
    //            return min;
    //        }
    //        else
    //        {
    //            return max;
    //        }
    //    }
    //    else
    //    {
    //        return target;
    //    }
    //}

    public enum Axis
    {
        X,
        Y,
        Z
    }
}