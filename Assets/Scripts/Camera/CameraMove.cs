using DG.Tweening;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 相机移动
/// </summary>
public class CameraMove : MonoBehaviour
{
    /// <summary>
    /// 移动类型
    /// </summary>
    public CameraMoveType moveType;

    private Camera mainCam;

    /// <summary>
    /// 移动速度
    /// </summary>
    public float moveSpeed = 0.01f;

    /// <summary>
    /// 上次触摸点1(手指1)  
    /// </summary>
    private Vector2 oldPos1;
    /// <summary>
    /// 上次触摸点2(手指2)  
    /// </summary>
    private Vector2 oldPos2;

    /// <summary>
    /// 上次鼠标位置
    /// </summary>
    private Vector3 previousMousePosition;
    /// <summary>
    /// 移动方向
    /// </summary>
    private Vector2 direction;

    /// <summary>
    /// 移动方向系数
    /// </summary>
    private int directionRatio = -1;

    #region 范围限制
    /// <summary>
    /// 向左移动最大距离
    /// </summary>
    private float maxMoveLeft = 0;
    /// <summary>
    /// 向右移动最大距离
    /// </summary>
    private float maxMoveRight = 0;
    /// <summary>
    /// 向上移动最大距离
    /// </summary>
    private float maxMoveUp = 0;
    /// <summary>
    /// 向下移动最大距离
    /// </summary>
    private float maxMoveDown = 0;
    #endregion

    private Vector3 targetPosition;

    private float distance;
    private float viewportHeight;
    public float speedRatio = 1;

    private bool isMouseDown = false;
    private bool isTouchBegin = false;

    private void Awake()
    {
        mainCam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        isMouseDown = false;
        isTouchBegin = false;
    }

    private void LateUpdate()
    {
        if (mainCam == null)
            return;

        if (GlobalInfo.InPaintMode)
            return;

        if (GlobalInfo.isLive && !GlobalInfo.IsOperator())
            return;

        if (moveType == CameraMoveType.None || !ModelManager.Instance.CameraControl || ModelManager.Instance.CameraDotween)
        {
            isMouseDown = false;
            isTouchBegin = false;
            return;
        }

        if (Input.touchCount == 2 && !GUITool.IsOverGUI(Input.GetTouch(0).position)
          && !GUITool.IsOverGUI(Input.GetTouch(1).position))
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                PrepareMove(touch1.position);
                oldPos1 = touch1.position; oldPos2 = touch2.position;
                isTouchBegin = true;
                return;
            }

            if (isTouchBegin && touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                if (Vector3.Dot(touch1.position - oldPos1, touch2.position - oldPos2) > 0)
                {
                    BeginMove(touch1.position);
                }
                else
                {
                    PrepareMove(touch1.position);
                }
                oldPos1 = touch1.position; oldPos2 = touch2.position;
            }
        }
        else
        {
            if (!GUITool.IsOverGUI(Input.mousePosition))
            {
                if (Input.GetMouseButtonDown(2))
                {
                    PrepareMove(Input.mousePosition);
                    isMouseDown = true;
                }
                if (isMouseDown && Input.GetMouseButton(2))
                {
                    BeginMove(Input.mousePosition);
                }
            }
        }
    }

    /// <summary>
    /// 开始移动
    /// </summary>
    /// <param name="screenPos">屏幕坐标</param>
    void PrepareMove(Vector3 screenPos)
    {
        previousMousePosition = screenPos;
    }

    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="screenPos">屏幕坐标</param>
    void BeginMove(Vector3 screenPos)
    {
        direction = screenPos - previousMousePosition;
        Move(directionRatio * direction);
        previousMousePosition = screenPos;
    }

    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="delta"></param>
    public void Move(Vector2 delta)
    {
        #region 根据距模型中心点距离计算移动速度
        targetPosition = ModelManager.Instance.modelBoundsCenter;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPosition = hit.point;
        }

        distance = Vector3.Distance(transform.position, targetPosition);
        viewportHeight = Mathf.Tan(mainCam.fieldOfView * Mathf.Deg2Rad * 0.5f) * (mainCam.nearClipPlane + distance) * 2;

        speedRatio = viewportHeight * 100 / Screen.height;
        #endregion

        if (moveType != CameraMoveType.Vertical)
            transform.Translate(delta.x * moveSpeed * speedRatio * Vector3.right);
        if (moveType != CameraMoveType.Horizontal)
            transform.Translate(delta.y * moveSpeed * speedRatio * Vector3.up);

        //// large world coordinates
        //Vector3 translation = Vector3.zero;
        //if (moveType != CameraMoveType.Vertical)
        //    translation += delta.x * moveSpeed * speedRatio * Vector3.right;
        //if (moveType != CameraMoveType.Horizontal)
        //    translation += delta.y * moveSpeed * speedRatio * Vector3.up;
        //// Apply translation with clamping
        //transform.position = ClampPosition(transform.position + transform.TransformDirection(translation));
    }

    public float clampRadius = 500f; // Max distance from origin
    Vector3 ClampPosition(Vector3 position)
    {
        if (position.magnitude > clampRadius)
        {
            return position.normalized * clampRadius;
        }
        return position;
    }

    /// <summary>
    /// 设置移动范围
    /// </summary>
    /// <param name="inverse"></param>
    /// <param name="max_l"></param>
    /// <param name="max_r"></param>
    /// <param name="max_u"></param>
    /// <param name="max_d"></param>
    public void SetRange(bool inverse, float max_l, float max_r, float max_u, float max_d)
    {
        directionRatio = inverse ? -1 : 1;
        maxMoveLeft = max_l;
        maxMoveRight = max_r;
        maxMoveUp = max_u;
        maxMoveDown = max_d;
    }

    /// <summary>
    /// 重置移动范围(无限制)
    /// </summary>
    public void ResetRange()
    {
        directionRatio = -1;
        maxMoveLeft = 0;
        maxMoveRight = 0;
        maxMoveUp = 0;
        maxMoveDown = 0;
    }

    private void OnDestroy()
    {
        DOTween.Kill("BehaveMoveCamera");
        DOTween.Kill("BehaveZoomCamera");
        if (ModelManager.Instance != null)
            transform.position = ModelManager.Instance.initCamPos;
    }
}