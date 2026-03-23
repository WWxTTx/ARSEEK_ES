using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 缩放类型
/// </summary>
public enum ZoomType
{
    /// <summary>
    /// 禁用
    /// </summary>
    None,
    /// <summary>
    /// 无限制
    /// </summary>
    Unrestrict,
    /// <summary>
    /// 有限制
    /// </summary>
    Restrict,
    /// <summary>
    /// 相机缩进
    /// </summary>
    Camera
}

/// <summary>
/// 模型缩放
/// </summary>
public class ModelZoom : MonoBase
{
    /// <summary>
    /// 缩放类型
    /// </summary>
    public ZoomType zoomType = ZoomType.Restrict;

    #region 模型缩放
    /// <summary>
    /// 最大值
    /// </summary>
    public float maxValue = 1.5f;
    /// <summary>
    /// 最小值
    /// </summary>
    public float minValue = 0.5f;
    /// <summary>
    /// 缩放差值
    /// </summary>
    public float differenceValue = 1f;
    /// <summary>
    /// 初始scale值
    /// </summary>
    Vector3 oldSize;
    /// <summary>
    /// 当前缩放值
    /// </summary>
    public float nowValue = 1;
    #endregion

    /// <summary>
    /// 上次触摸点1(手指1)  
    /// </summary>
    private Vector2 oldPos1;
    /// <summary>
    /// 上次触摸点2(手指2)  
    /// </summary>
    private Vector2 oldPos2;
    /// <summary>
    /// 触摸点距离差值
    /// </summary>
    float offest = 1;

    /// <summary>
    /// 用于鼠标滑轮是否滑动检测
    /// </summary>
    bool isMouseZoom = false;

    public class EndZoomEvent : UnityEvent { }
    public EndZoomEvent endZoomEvent = new EndZoomEvent();


    private void Awake()
    {
        AddMsg((ushort)ModelOperateEvent.Scale);
        Init();
        TapRecognizer.Instance.RegistOnLeftMouseDoubleClick(ResetValue);
    }

    protected override void Start()
    {
        base.Start();
        endZoomEvent.AddListener(EndZoomHandler);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        oldSize = transform.localScale;
        minValue = transform.localScale.x * 0.5f;
        maxValue = transform.localScale.x * 2.5f;
        nowValue = transform.localScale.x;
    }

    public void SetValue(float now, float min = -1f, float max = -1f)
    {
        nowValue = now;
        if (min < 0)
            minValue = now * 0.5f;
        else
            minValue = min;

        if (max < 0)
            maxValue = now * 2.5f;
        else
            maxValue = max;

        transform.localScale = (Vector3.one * nowValue);
    }

    /// <summary>
    /// 初始化限制参数
    /// </summary>
    /// <param name="value"></param>
    public void SetRange(float min, float max)
    {
        maxValue = max;
        minValue = min;
    }

    /// <summary>
    /// 重置缩放范围
    /// </summary>
    public void ResetRange()
    {
        minValue = oldSize.x * 0.5f;
        maxValue = oldSize.x * 2.5f;
    }

    /// <summary>
    /// 重置缩放值
    /// </summary>
    public void ResetValue()
    {
        transform.localScale = oldSize;
        //minValue = transform.localScale.x * 0.5f;
        //maxValue = transform.localScale.x * 2.5f;
        nowValue = transform.localScale.x;
    }

    void Update()
    {
        //无模型时禁止操作
        if (ModelManager.Instance.modelRoot.childCount == 0)
            return;
        //协同房间无权限用户禁止操作
        if (GlobalInfo.IsLiveMode() && !GlobalInfo.IsOperator())
            return;

        if (Input.touchCount == 2 && !GUITool.IsOverGUI(Input.GetTouch(0).position)
            && !GUITool.IsOverGUI(Input.GetTouch(1).position))
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                oldPos1 = touch1.position; oldPos2 = touch2.position;
                return;
            }

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                if (Vector3.Dot(touch1.position - oldPos1, touch2.position - oldPos2) < 0)
                {
                    //计算老的两点距离和新的两点间距离，变大要放大模型，变小要缩放模型  
                    float oldDistance = Vector2.Distance(oldPos1, oldPos2);
                    float newDistance = Vector2.Distance(touch1.position, touch2.position);

                    //两个距离之差，为正表示放大手势， 为负表示缩小手势  
                    float offset = newDistance - oldDistance;
                    offest = offset * 0.004f;
                    if (Mathf.Abs(offset) > 1)
                        ModelZoomHandler(offest);
                }

                oldPos1 = touch1.position; oldPos2 = touch2.position;
            }

            if (touch2.phase == TouchPhase.Ended)
            {
                endZoomEvent?.Invoke();
            }
        }
        else
        {
            offest = Input.GetAxis("Mouse ScrollWheel");
            if (offest != 0 && !GUITool.IsOverGUI(Input.mousePosition))
            {
                ModelZoomHandler(offest);
                isMouseZoom = true;
            }
            else if (isMouseZoom)
            {
                endZoomEvent?.Invoke();
                isMouseZoom = false;
            }
        }
    }

    private void ModelZoomHandler(float value)
    {
        nowValue = transform.localScale.x + differenceValue * value;

        if (zoomType == ZoomType.Restrict)
        {
            if (nowValue < minValue)
                nowValue = minValue;
            else if (nowValue > maxValue)
                nowValue = maxValue;
        }
        //TODO
        //else
        //{
        //    nowValue = Mathf.Max(nowValue, 0.01f);
        //}
        transform.localScale = Vector3.one * nowValue;
    }
    private void EndZoomHandler()
    {
        if (ModelManager.Instance.modelRoot.childCount == 0)
            return;

        if (GlobalInfo.IsLiveMode() && GlobalInfo.IsOperator())
        {
            MsgFloat msgF = new MsgFloat((ushort)ModelOperateEvent.Scale, nowValue);
            MsgBrodcastOperate msg = new MsgBrodcastOperate(msgF.msgId, JsonTool.Serializable(msgF));
            //IMManager.Instance.SendOperationData(msg);
            NetworkManager.Instance.SendIMMsg(msg);
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        if (msg.msgId == (ushort)ModelOperateEvent.Scale)
        {
            nowValue = Mathf.Clamp(((MsgBrodcastOperate)msg).GetData<MsgFloat>().arg, minValue, maxValue);
            transform.localScale = Vector3.one * nowValue;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        transform.localScale = oldSize;
        endZoomEvent.RemoveAllListeners();
        TapRecognizer.Instance?.UnRegistOnLeftMouseDoubleClick(ResetValue);
    }
}
