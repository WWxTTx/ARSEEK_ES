using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 移动类型
/// </summary>
public enum MoveType
{
    /// <summary>
    /// 禁用移动
    /// </summary>
    None,
    /// <summary>
    /// 无限制移动
    /// </summary>
    Unrestrict,
    /// <summary>
    /// 有限制移动
    /// </summary>
    Restrict,
    /// <summary>
    /// 相机
    /// </summary>
    Camera
}
/// <summary>
/// 模型移动
/// </summary>
public class ModelMove : MonoBase
{
    /// <summary>
    /// 移动类型
    /// </summary>
    public MoveType moveType;

    private Camera mainCam;

    /// <summary>
    /// 物体屏幕坐标
    /// </summary>
    private Vector3 screenPoint;
    /// <summary>
    /// 触发移动点与物体世界坐标偏移量
    /// </summary>
    private Vector3 offset;
    /// <summary>
    /// 触发移动点屏幕坐标
    /// </summary>
    private Vector3 currScreenPoint;
    /// <summary>
    /// 物体目标坐标（世界坐标）
    /// </summary>
    private Vector3 targetPoint;

    private Vector2 oldPos1;
    private Vector2 oldPos2;

    /// <summary>
    /// 物体视口坐标
    /// </summary>
    private Vector3 viewPoint;

    /// <summary>
    /// 水平方向移动最小值
    /// </summary>
    [Range(0f, 1f)]
    public float minMove_H = 0f;
    /// <summary>
    /// 水平方向移动最大值
    /// </summary>
    [Range(0f, 1f)]
    public float maxMove_H = 1f;
    /// <summary>
    /// 垂直方向移动最小值
    /// </summary>
    [Range(0f, 1f)]
    public float minMove_V = 0f;
    /// <summary>
    /// 垂直方向移动最大值
    /// </summary>
    [Range(0f, 1f)]
    public float maxMove_V = 1f;

    private Vector3 oldPosition;

    private void Awake()
    {
        mainCam = Camera.main;
        oldPosition = transform.position;

        TapRecognizer.Instance.RegistOnLeftMouseDoubleClick(ResetMove);
    }

    private void Update()
    {
        if (moveType == MoveType.None)
            return;

        if (Input.touchCount == 2 && !GUITool.IsOverGUI(Input.GetTouch(0).position)
          && !GUITool.IsOverGUI(Input.GetTouch(1).position))
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began && touch2.phase == TouchPhase.Began)
            {
                MoveBegin(touch1.position);
                oldPos1 = touch1.position; oldPos2 = touch2.position;
                return;
            }

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                if (Vector3.Dot(touch1.position - oldPos1, touch2.position - oldPos2) > 0)
                {
                    Move(touch1.position);
                }
                oldPos1 = touch1.position; oldPos2 = touch2.position;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(2))
            {
                MoveBegin(Input.mousePosition);
            }

            if (Input.GetMouseButton(2))
            {
                Move(Input.mousePosition);
            }
        }
    }
    /// <summary>
    /// 开始移动
    /// </summary>
    /// <param name="pos">屏幕坐标</param>
    void MoveBegin(Vector3 pos)
    {
        screenPoint = mainCam.WorldToScreenPoint(transform.position);
        offset = transform.position - mainCam.ScreenToWorldPoint(new Vector3(pos.x, pos.y, screenPoint.z));
    }
    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="pos">屏幕坐标</param>
    void Move(Vector3 pos)
    {
        currScreenPoint = new Vector3(pos.x, pos.y, screenPoint.z);
        targetPoint = mainCam.ScreenToWorldPoint(currScreenPoint) + offset;

        if (moveType == MoveType.Restrict)
        {
            viewPoint = mainCam.WorldToViewportPoint(targetPoint);
            if (viewPoint.x < minMove_H)
            {
                viewPoint.x = minMove_H;
            }
            else if (viewPoint.x > maxMove_H)
            {
                viewPoint.x = maxMove_H;
            }
            if (viewPoint.y < minMove_V)
            {
                viewPoint.y = minMove_V;
            }
            else if (viewPoint.y > maxMove_V)
            {
                viewPoint.y = maxMove_V;
            }

            targetPoint = mainCam.ViewportToWorldPoint(viewPoint);
        }

        transform.position = targetPoint;
    }

    /// <summary>
    /// 初始化限制参数
    /// </summary>
    public void SetRange(float _minMove_H, float _maxMove_H, float _minMove_V, float _maxMove_V)
    {
        if (_minMove_H > 0 || _maxMove_H > 0 || _minMove_V > 0 || _maxMove_V > 0)
        {
            moveType = MoveType.Restrict;
            minMove_H = _minMove_H;
            maxMove_H = _maxMove_H;
            minMove_V = _minMove_V;
            maxMove_V = _maxMove_V;
        }
        else
            moveType = MoveType.Unrestrict;
    }
    public void ResetRange()
    {
        minMove_H = 0;
        maxMove_H = 1;
        minMove_V = 0;
        maxMove_V = 1;
    }
    
    public void ResetMove()
    {
        transform.position = oldPosition;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TapRecognizer.Instance?.UnRegistOnLeftMouseDoubleClick(ResetMove);
    }
}
