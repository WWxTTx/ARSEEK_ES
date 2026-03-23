using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 相机缩放
/// </summary>
public class CameraZoom : MonoBehaviour
{
    /// <summary>
    /// 缩放类型
    /// </summary>
    public CameraZoomType zoomType;

    private Camera mainCam;

    /// <summary>
    /// 缩放灵敏度
    /// </summary>
    public float zoomSensitivity = 10f;

    public float minDistance { get; private set; }

    public float maxDistance { get; private set; }

    public float nowDistance
    {
        get
        {
            return Vector3.Distance(transform.position, targetPosition);
        }
    }

    /// <summary>
    /// 期望位置
    /// </summary>
    private Vector3 desiredPosition;
    /// <summary>
    /// 缩放方向
    /// </summary>
    private Vector3 zoomDirection;
    /// <summary>
    /// 缩放目标位置
    /// </summary>
    private Vector3 targetPosition;

    private bool hasHit = false;

    /// <summary>
    /// 上次触摸点1(手指1)  
    /// </summary>
    private Vector2 oldPos1;
    /// <summary>
    /// 上次触摸点2(手指2)  
    /// </summary>
    private Vector2 oldPos2;
    /// <summary>
    /// 缩放偏移值
    /// </summary>
    float offest = 1;

    /// <summary>
    /// 鼠标滚轮是否滑动过
    /// </summary>
    bool isMouseZoom = false;

    bool isTouchBegin = false;

    public class EndZoomEvent : UnityEvent { }
    public EndZoomEvent endZoomEvent = new EndZoomEvent();

    private float viewportHeight;
    private float speedRatio = 1;

    private Plane infiniteHorizontalPlane = new Plane(Vector3.up, 0);
    private Plane infiniteVerticalPlane = new Plane(Vector3.forward, 0);

    private void Awake()
    {
        mainCam = GetComponent<Camera>();

        minDistance = 1;
        maxDistance = 20;
    }

    private void OnEnable()
    {
        isMouseZoom = false;
        isTouchBegin = false;
    }

    private void LateUpdate()
    {
        if (mainCam == null)
            return;

        if (GlobalInfo.InPaintMode)
            return;

        if (GlobalInfo.IsLiveMode() && !GlobalInfo.IsUserOperator())
            return;

        if (zoomType == CameraZoomType.None || !ModelManager.Instance.CameraControl || ModelManager.Instance.CameraDotween)
        {
            isMouseZoom = false;
            isTouchBegin = false;
            return;
        }

        if (Input.touchCount == 2 && !GUITool.IsOverGUI(Input.GetTouch(0).position, GlobalInfo.OpUILayer)
            && !GUITool.IsOverGUI(Input.GetTouch(1).position))
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                oldPos1 = touch1.position; oldPos2 = touch2.position;
                isTouchBegin = true;
                return;
            }

            if (isTouchBegin && touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                if (Vector3.Dot(touch1.position - oldPos1, touch2.position - oldPos2) < 0)
                {
                    //两指触摸时的距离变化，变大要放大模型，变小要缩小模型  
                    float oldDistance = Vector2.Distance(oldPos1, oldPos2);
                    float newDistance = Vector2.Distance(touch1.position, touch2.position);

                    //计算距离之差，为正表示放大操作， 为负表示缩小操作  
                    float offset = newDistance - oldDistance;
                    offest = offset * 0.004f;
                    if (Mathf.Abs(offset) > 1)
                        Zoom(offest);
                }

                oldPos1 = touch1.position;
                oldPos2 = touch2.position;
            }

            if (touch2.phase == TouchPhase.Ended)
            {
                endZoomEvent?.Invoke();
            }
        }
        else
        {
            var x = Input.mousePosition.x;
            var y = Input.mousePosition.y;

            if (x < 0 || x > Screen.width || y < 0 || y > Screen.height)
            {
                return;
            }

            offest = Input.GetAxis("Mouse ScrollWheel");

            if (offest != 0 && !GUITool.IsOverGUI(Input.mousePosition, GlobalInfo.OpUILayer))
            {
                Zoom(offest);
                isMouseZoom = true;
            }
            else if (isMouseZoom)
            {
                endZoomEvent?.Invoke();
                isMouseZoom = false;
            }
        }
    }

    /// <summary>
    /// 执行缩放
    /// </summary>
    /// <param name="value"></param>
    public void Zoom(float value)
    {
        Ray ray;
        RaycastHit hit;
        hasHit = false;

        switch (zoomType)
        {
            case CameraZoomType.None:
                return;
            case CameraZoomType.Forward:
                zoomDirection = transform.forward;
                ray = new Ray(transform.position, transform.forward);
                if (Physics.Raycast(ray, out hit))
                {
                    targetPosition = hit.point;
                    hasHit = true;
                }
                break;
            case CameraZoomType.Mouse:
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                ray = mainCam.ScreenPointToRay((oldPos1 + oldPos2) / 2);
#else
                ray = mainCam.ScreenPointToRay(Input.mousePosition);
#endif
                if (Physics.Raycast(ray, out hit))
                {
                    zoomDirection = ray.direction;
                    targetPosition = hit.point;
                    hasHit = true;
                }
                // Raycast against an infinite plane, in case no colliders are present
                else if (infiniteHorizontalPlane.Raycast(ray, out float dist))
                {
                    zoomDirection = ray.direction;
                    hasHit = false;
                }
                else if (infiniteVerticalPlane.Raycast(ray, out float distV))
                {
                    zoomDirection = ray.direction;
                    hasHit = false;
                }
                break;
            case CameraZoomType.Pivot:
                targetPosition = ModelManager.Instance.modelBoundsCenter;
                zoomDirection = (targetPosition - transform.position).normalized;
                hasHit = true;
                break;
        }

        if (hasHit)
        {
            viewportHeight = Mathf.Tan(mainCam.fieldOfView * Mathf.Deg2Rad * 0.5f) * (mainCam.nearClipPlane + Vector3.Distance(transform.position, targetPosition)) * 2;
            speedRatio = viewportHeight * 100 / Screen.height;
        }

        desiredPosition = transform.position + zoomDirection * value * zoomSensitivity * speedRatio;

        //限制缩放距离范围
        if (hasHit)
        {
            if (Vector3.Dot(desiredPosition - targetPosition, zoomDirection) > 0)
            {
                transform.position = targetPosition - zoomDirection.normalized * minDistance;
            }
            else
            {
                var d = Vector3.Distance(desiredPosition, targetPosition);
                if (d > maxDistance)
                {
                    //transform.position = targetPosition - zoomDirection.normalized * maxDistance;
                    transform.position = desiredPosition + zoomDirection.normalized * (d - maxDistance);
                }
                else if (d < minDistance)
                {
                    //transform.position = targetPosition - zoomDirection.normalized * minDistance;
                    transform.position = desiredPosition - zoomDirection.normalized * (minDistance - d);
                }
                else
                {
                    transform.position = desiredPosition;
                }
            }
        }
        else
        {
            transform.position = desiredPosition;
        }
    }

    /// <summary>
    /// 设置缩放距离范围
    /// </summary>
    /// <param name="minDistance"></param>
    /// <param name="maxDistance"></param>
    public void SetRange(float minDistance, float maxDistance)
    {
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
    }

    /// <summary>
    /// 重置缩放距离范围
    /// </summary>
    public void ResetRange()
    {
        this.minDistance = 1f;
        this.maxDistance = 20f;
    }

    /// <summary>
    /// 更新缩放距离范围
    /// </summary>
    public void UpdateRange()
    {
        this.minDistance = Mathf.Min(minDistance, 1f);
        this.maxDistance = 20f;
    }

    void OnDestroy()
    {
        DOTween.Kill("BehaveMoveCamera");
        DOTween.Kill("BehaveZoomCamera");
        if (ModelManager.Instance != null)
            transform.position = ModelManager.Instance.initCamPos;
        endZoomEvent.RemoveAllListeners();
    }
}
