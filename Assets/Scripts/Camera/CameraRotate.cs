using UnityEngine;
using UnityFramework.Runtime;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// 相机旋转
/// </summary>
public class CameraRotate : MonoBehaviour
{
    /// <summary>
    /// 旋转类型
    /// </summary>
    public CameraRotateType rotateType;

    private Camera mainCam;

    /// <summary>
    /// 旋转速度
    /// </summary>
    [Header("旋转速度")]
    public float rotateSensitivity = 5;

    /// <summary>
    /// 是否可俯仰
    /// </summary>
    private bool allowPitch = true;
    /// <summary>
    /// 最小俯仰角度
    /// </summary>
    private float minAnglePitch = -90;
    /// <summary>
    /// 最大俯仰角度
    /// </summary>
    private float maxAnglePitch = 90;
    /// <summary>
    /// 是否可偏航
    /// </summary>
    private bool allowYaw = true;
    /// <summary>
    /// 最小偏航角度
    /// </summary>
    private float minAngleYaw = -180;
    /// <summary>
    /// 最大偏航角度
    /// </summary>
    private float maxAngleYaw = 180;

    /// <summary>
    /// 记录初始旋转
    /// </summary>
    private Vector3 oldEulerAngle;
    /// <summary>
    /// 记录锚点距离
    /// </summary>
    private float distance;

    /// <summary>
    /// 旋转锚点
    /// </summary>
    private Vector3 pivot;

    private Vector2 mouseDelta;

    private float angleX;
    private float angleY;
    private Quaternion rotation;
    private Vector3 dir;

    private bool isMouseDown = false;

    private Plane infiniteHorizontalPlane = new Plane(Vector3.up, 0);
    private Plane infiniteVerticalPlane = new Plane(Vector3.forward, 0);

    bool reset = false;
    /// <summary>
    /// 是否正在重置回位
    /// </summary>
    public bool Resetting
    {
        get { return reset; }
        set { reset = value; }
    }

    #region 计算位置和dotween时间
    private const float refPosDelta = 5f;
    private const float refAngleDelta = 360f;
    #endregion

    private void Awake()
    {
        mainCam = GetComponent<Camera>();

        oldEulerAngle = transform.eulerAngles;

#if UNITY_EDITOR || UNITY_STANDALONE
        rotateSensitivity = 5f;
#else
        rotateSensitivity = 0.5f;
#endif

        dir = transform.position - ModelManager.Instance.modelRoot.position;
        UpdateState();
    }

    private void OnEnable()
    {
        isMouseDown = false;
    }

    private void LateUpdate()
    {
        if (mainCam == null)
            return;

        if (GlobalInfo.InPaintMode)
            return;

        if (Resetting)
        {
            transform.position = pivot + transform.rotation * Quaternion.Inverse(rotation) * dir;
        }

        if (wanderFlowMode)
        {
            if (Input.GetMouseButtonDown(1))
            {
                isMouseDown = true;
                rotation = transform.rotation;
                dir = transform.position - pivot;
            }

            if (Input.GetMouseButton(1) && isMouseDown && ModelManager.Instance.CameraControl)
            {
                BeginRotate();
            }

            if (Input.GetMouseButtonUp(1))
            {
                isMouseDown = false;
            }

            return;
        }

        if (rotateType == CameraRotateType.None || !ModelManager.Instance.CameraControl || ModelManager.Instance.CameraDotween)
        {
            isMouseDown = false;
            return;
        }

        if (!GlobalInfo.IsOperator())
            return;

        if (Input.touchCount == 0 || Input.touchCount == 1)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!GUITool.IsOverGUI(Input.mousePosition))
                {
                    isMouseDown = true;

                    switch (rotateType)
                    {
                        case CameraRotateType.RotateAround:
                            pivot = ModelManager.Instance.modelBoundsCenter;
                            break;
                        case CameraRotateType.RotateAroundMouse:
                            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out RaycastHit hit))
                            {
                                pivot = hit.point;
                            }
                            // Raycast against an infinite plane, in case no colliders are present
                            else if (infiniteHorizontalPlane.Raycast(ray, out float dist))
                            {
                                pivot = ray.GetPoint(dist);
                            }
                            else if (infiniteVerticalPlane.Raycast(ray, out float distV))
                            {
                                pivot = ray.GetPoint(distV);
                            }
                            break;
                        case CameraRotateType.RotateAroundScreen:
                            //pivot = Camera.main.ViewportToWorldPoint(Camera.main.ScreenToViewportPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, Vector3.Distance(transform.position, ModelManager.Instance.modelRoot.position))));
                            Ray ray2 = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
                            if (Physics.Raycast(ray2, out RaycastHit hit2))
                            {
                                pivot = hit2.point;
                            }
                            // Raycast against an infinite plane, in case no colliders are present
                            else if (infiniteHorizontalPlane.Raycast(ray2, out float dist))
                            {
                                pivot = ray2.GetPoint(Mathf.Clamp(dist, 0, Vector3.Distance(transform.position, ModelManager.Instance.modelRoot.position)));
                            }
                            else if (infiniteVerticalPlane.Raycast(ray2, out float distV))
                            {
                                pivot = ray2.GetPoint(Mathf.Clamp(distV, 0, Vector3.Distance(transform.position, ModelManager.Instance.modelRoot.position)));
                            }
                            break;
                        default:
                            break;
                    }
                    rotation = transform.rotation;
                    dir = transform.position - pivot;
                }
            }

            if (Input.GetMouseButton(0) && isMouseDown && ModelManager.Instance.CameraControl)
            {
                BeginRotate();
            }

            if (Input.GetMouseButtonUp(0))
            {
                isMouseDown = false;
            }
        }
    }

    public bool wanderFlowMode = false;

    public void SetAngle(Vector2 angle)
    {
        angleX = angle.x;
        angleY = angle.y;
        transform.eulerAngles = angle;
    }

    private void BeginRotate()
    {
        mouseDelta = GetMouseDelta();
        Rotate(mouseDelta * rotateSensitivity);
    }

    /// <summary>
    /// 旋转
    /// </summary>
    /// <param name="delta"></param>
    public void Rotate(Vector2 delta)
    {
        if (Vector2.SqrMagnitude(delta) == 0)
            return;

        if ((angleX > 90f && angleX < 270f) || (angleX < -90 && angleX > -270f))
            angleY -= delta.x;
        else
            angleY += delta.x;

        angleX -= delta.y;

        angleX = ClampAngle(angleX, minAnglePitch, maxAnglePitch);
        if (minAngleYaw != -180 || maxAngleYaw != 180)
            angleY = ClampAngle(angleY, minAngleYaw, maxAngleYaw);

        if (!allowPitch)
            angleX = oldEulerAngle.x;
        if (!allowYaw)
            angleY = oldEulerAngle.y;

        switch (rotateType)
        {
            case CameraRotateType.None:
            case CameraRotateType.LookAround:
                LookAround();
                break;
            default:
                RotateAround();
                break;
        }
    }

    /// <summary>
    /// 以自己为中心旋转
    /// </summary>
    private void LookAround()
    {
        transform.eulerAngles = new Vector3(angleX, angleY);
    }

    /// <summary>
    /// 围绕旋转
    /// </summary>
    private void RotateAround()
    {
        Quaternion rot = Quaternion.Euler(angleX, angleY, 0);
        transform.rotation = rot;
        //transform.position = pivot + rot * Vector3.back * distance;
        transform.position = pivot + transform.rotation * Quaternion.Inverse(rotation) * dir;
    }

    /// <summary>
    /// 设置旋转限制参数
    /// </summary>
    /// <param name="min_p"></param>
    /// <param name="max_p"></param>
    /// <param name="min_y"></param>
    /// <param name="max_y"></param>
    public void SetRange(bool allowPitch, float min_p, float max_p, bool allowYaw, float min_y, float max_y)
    {
        this.allowPitch = allowPitch;
        minAnglePitch = min_p;
        maxAnglePitch = max_p;
        this.allowYaw = allowYaw;
        minAngleYaw = min_y;
        maxAngleYaw = max_y;
    }

    /// <summary>
    /// 重置旋转角度范围
    /// </summary>
    public void ResetRange()
    {
        allowPitch = true;
        minAnglePitch = -90;
        maxAnglePitch = 90;
        allowYaw = true;
        minAngleYaw = -180;
        maxAngleYaw = 180;
    }

    /// <summary>
    /// 重置相机旋转
    /// </summary>
    public void ResetRotate(Vector3 position, bool usePosition, float angleX, float angleY, float playTime, UnityAction callback = null)
    {
        this.angleX = angleX == -1 ? transform.eulerAngles.x : angleX;
        this.angleY = angleY == -1 ? transform.eulerAngles.y : angleY;

        switch (rotateType)
        {
            case CameraRotateType.LookAround:
                Resetting = true;
                transform.DORotate(new Vector3(angleX, angleY), playTime).OnComplete(() =>
                {
                    Resetting = false;
                    callback?.Invoke();
                });
                break;
            default:
                pivot = ModelManager.Instance.modelBoundsCenter;
                rotation = transform.rotation;
                dir = transform.position - pivot;

                if (angleX == -1 && angleY == -1)
                {
                    Vector3 targetPosition = pivot + rotation * Vector3.back * distance;
                    float posDelta = Vector3.Distance(transform.position, targetPosition);
                    transform.DOMove(targetPosition, Mathf.Clamp(posDelta / refPosDelta, 0, playTime)).OnComplete(() => callback?.Invoke()).SetEase(Ease.InOutQuad);
                }
                else
                {
                    Quaternion rot = Quaternion.Euler(this.angleX, this.angleY, 0);
                    if (usePosition)
                    {
                        Resetting = true;
                        float rotDelta = Quaternion.Angle(transform.rotation, rot);
                        transform.DORotateQuaternion(rot, Mathf.Clamp(rotDelta / refAngleDelta, 0, playTime)).OnComplete(() =>
                        {
                            float posDelta = Vector3.Distance(transform.position, position);
                            transform.DOMove(position, Mathf.Clamp(posDelta / refPosDelta, 0, playTime)).OnComplete(() =>
                            {
                                Resetting = false;
                                callback?.Invoke();
                            }).SetEase(Ease.InOutQuad);
                        }).SetEase(Ease.InOutQuad);
                    }
                    else
                    {
                        Vector3 targetPosition = pivot + transform.rotation * Vector3.back * distance;
                        float posDelta = Vector3.Distance(transform.position, targetPosition);
                        transform.DOMove(targetPosition, Mathf.Clamp(posDelta / refPosDelta, 0, playTime)).OnComplete(() =>
                        {
                            dir = transform.position - pivot;
                            Resetting = true;
                            float rotDelta = Quaternion.Angle(transform.rotation, rot);
                            transform.DORotateQuaternion(rot, Mathf.Clamp(rotDelta / refAngleDelta, 0, playTime)).OnComplete(() =>
                            {
                                //确保最终站到位
                                targetPosition = pivot + transform.rotation * Vector3.back * distance;
                                posDelta = Vector3.Distance(transform.position, targetPosition);
                                transform.DOMove(targetPosition, Mathf.Clamp(posDelta / refPosDelta, 0, playTime)).OnComplete(() =>
                                {
                                    Resetting = false;
                                    callback?.Invoke();
                                }).SetEase(Ease.InOutQuad);
                            }).SetEase(Ease.InOutQuad);
                        }).SetEase(Ease.InOutQuad);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 更新当前位状态
    /// </summary>
    public void UpdateState()
    {
        oldEulerAngle = transform.eulerAngles;
        angleX = oldEulerAngle.x;
        angleY = oldEulerAngle.y;
        pivot = ModelManager.Instance.modelBoundsCenter;
        distance = Vector3.Distance(transform.position, pivot);
    }

    /// <summary>
    /// 还原初始位状态
    /// </summary>
    public void RevertState()
    {
        oldEulerAngle = transform.eulerAngles;
        angleX = oldEulerAngle.x;
        angleY = oldEulerAngle.y;
        pivot = ModelManager.Instance.modelBoundsCenter;
        distance = ModelManager.Instance.centerDis;
    }

    /// <summary>
    /// 获取鼠标位置差值
    /// </summary>
    /// <returns></returns>
    private Vector2 GetMouseDelta()
    {
        if (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Moved)
        {
            return Input.GetTouch(0).deltaPosition;
        }
        else
        {
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 270)
            angle -= 360;
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    private void OnDestroy()
    {
        DOTween.Kill("BehaveMoveCamera");
        DOTween.Kill("BehaveZoomCamera");
        if (ModelManager.Instance != null)
        {
            transform.position = ModelManager.Instance.initCamPos;
            transform.eulerAngles = ModelManager.Instance.initCamEuler;
        }
    }

    public void SetEnable(bool enabled)
    {
        if (enabled || !isMouseDown)
        {
            this.enabled = enabled;
        }
    }
}
