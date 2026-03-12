using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 处理空白处的点击事件
/// </summary>
public class TapRecognizer : Singleton<TapRecognizer>
{
    # region 鼠标左键/移动端单指
    public UnityEvent onLeftMouseLongClick { get; set; } = new UnityEvent();
    public UnityEvent onLeftMouseShortClick { get; set; } = new UnityEvent();
    public UnityEvent onLeftMouseEmptyShortClick { get; set; } = new UnityEvent();
    public UnityEvent onLeftMouseDoubleClick { get; set; } = new UnityEvent();
    public UnityEvent onLeftMouseEmptyDoubleClick { get; set; } = new UnityEvent();

    #endregion

    #region 鼠标右键/移动端双指
    public UnityEvent onRightMouseLongClick { get; set; } = new UnityEvent();
    public UnityEvent onRightMouseShortClick { get; set; } = new UnityEvent();
    public UnityEvent onRightMouseEmptyShortClick { get; set; } = new UnityEvent();
    public UnityEvent onRightMouseDoubleClick { get; set; } = new UnityEvent();
    public UnityEvent onRightMouseEmptyDoubleClick { get; set; } = new UnityEvent();
    #endregion

    [HideInInspector]
    public float longClickTime = 0.2f;

    [HideInInspector]
    public float doubleClickTimeDelay = 0.4f;

    [HideInInspector]
    public float dragThreshold = 10f;

    private bool isLeftMouseDown;
    private bool isLeftMouseEmptyDown;
    private bool first_l = true;
    private bool singleClick_l = false;

    private bool isRightMouseDown;
    private bool isRightMouseEmptyDown;
    private bool first_r = true;
    private bool singleClick_r = false;

    private float lastMouseDownTime;
    private Vector3 lastMoushDownPosition;

    protected override void InstanceAwake()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        longClickTime = 1f;
        doubleClickTimeDelay = 0.8f;
#endif
    }

    void Update()
    {
        if (GlobalInfo.InPaintMode)
            return;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if(Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (!GUITool.IsOverGUI(touch.position))
            {
                if (touch.phase == TouchPhase.Began)
                {
                    isLeftMouseDown = true;

                    if (GetClickGameObject() == null)
                    {
                        isLeftMouseEmptyDown = true;
                    }

                    lastMouseDownTime = Time.time;
                    lastMoushDownPosition = touch.position;
                }

                if(touch.phase == TouchPhase.Ended && (isLeftMouseDown || isLeftMouseEmptyDown))
                {
                    if (Vector3.Distance(touch.position, lastMoushDownPosition) > dragThreshold)
                    {
                        isLeftMouseEmptyDown = false;
                        isLeftMouseDown = false;
                        singleClick_l = false;
                        first_l = true;
                        return;
                    }

                    if (Time.time - lastMouseDownTime > longClickTime)
                    {
                        onLeftMouseLongClick?.Invoke();
                        isLeftMouseEmptyDown = false;
                    }
                    else
                    {
                        singleClick_l = !singleClick_l;

                        if (!first_l) return;
                        first_l = false;

                        Invoke(nameof(ClickCount_L), doubleClickTimeDelay);
                    }

                    isLeftMouseDown = false;
                }
            }
        }

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if(!GUITool.IsOverGUI(touch1.position) && !GUITool.IsOverGUI(touch2.position))
            {
                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    isRightMouseDown = true;
                    if (GetClickGameObject() == null)
                    {
                        isRightMouseEmptyDown = true;
                    }

                    lastMouseDownTime = Time.time;
                    lastMoushDownPosition = touch1.position;
                }

                if (touch1.phase == TouchPhase.Ended && touch2.phase == TouchPhase.Ended && (isRightMouseDown || isRightMouseEmptyDown))
                {
                    if (Vector3.Distance(touch1.position, lastMoushDownPosition) > dragThreshold)
                    {
                        isRightMouseEmptyDown = false;
                        isRightMouseDown = false;
                        singleClick_r = false;
                        first_r = true;
                        return;
                    }

                    if (Time.time - lastMouseDownTime > longClickTime)
                    {
                        onRightMouseLongClick?.Invoke();
                        isRightMouseEmptyDown = false;
                    }
                    else
                    {
                        singleClick_r = !singleClick_r;

                        if (!first_r) return;
                        first_r = false;

                        Invoke(nameof(ClickCount_R), doubleClickTimeDelay);
                    }

                    isRightMouseDown = false;
                }
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            if (!GUITool.IsOverGUI(Input.mousePosition, GlobalInfo.OpUILayer))
            {
                isLeftMouseDown = true;

                if (GetClickGameObject() == null)
                {
                    isLeftMouseEmptyDown = true;
                }

                lastMouseDownTime = Time.time;
                lastMoushDownPosition = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0) && (isLeftMouseDown || isLeftMouseEmptyDown))
        {
            if (Vector3.Distance(Input.mousePosition, lastMoushDownPosition) > dragThreshold)
            {
                isLeftMouseEmptyDown = false;
                isLeftMouseDown = false;
                singleClick_l = false;
                first_l = true;
                return;
            }

            if (Time.time - lastMouseDownTime > longClickTime)
            {
                onLeftMouseLongClick?.Invoke();
                isLeftMouseEmptyDown = false;
            }
            else
            {
                singleClick_l = !singleClick_l;

                if (!first_l) return;
                first_l = false;

                Invoke(nameof(ClickCount_L), doubleClickTimeDelay);
            }

            isLeftMouseDown = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!GUITool.IsOverGUI(Input.mousePosition, GlobalInfo.OpUILayer))
            {
                isRightMouseDown = true;
                if (GetClickGameObject() == null)
                {
                    isRightMouseEmptyDown = true;
                }

                lastMouseDownTime = Time.time;
                lastMoushDownPosition = Input.mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(1) && (isRightMouseDown || isRightMouseEmptyDown))
        {
            if (Vector3.Distance(Input.mousePosition, lastMoushDownPosition) > dragThreshold)
            {
                isRightMouseEmptyDown = false;
                isRightMouseDown = false;
                singleClick_r = false;
                first_r = true;
                return;
            }

            if (Time.time - lastMouseDownTime > longClickTime)
            {
                onRightMouseLongClick?.Invoke();
                isRightMouseEmptyDown = false;
            }
            else
            {
                singleClick_r = !singleClick_r;

                if (!first_r) return;
                first_r = false;

                Invoke(nameof(ClickCount_R), doubleClickTimeDelay);
            }

            isRightMouseDown = false;
        }
#endif
    }

    void ClickCount_L()
    {
        if (singleClick_l)
        {
            if (isLeftMouseEmptyDown)
                onLeftMouseEmptyShortClick?.Invoke();
            else
                onLeftMouseShortClick?.Invoke();            
        }
        else
        {
            if (isLeftMouseEmptyDown)
                onLeftMouseEmptyDoubleClick?.Invoke();

            onLeftMouseDoubleClick?.Invoke();
        }

        singleClick_l = false;
        first_l = true;
        isLeftMouseEmptyDown = false;
    }

    void ClickCount_R()
    {
        if (singleClick_r)
        {
            if (isRightMouseEmptyDown)
                onRightMouseEmptyShortClick?.Invoke();

            onRightMouseShortClick?.Invoke();
        }
        else
        {
            if (isRightMouseEmptyDown)
                onRightMouseEmptyDoubleClick?.Invoke();

            onRightMouseDoubleClick?.Invoke();
        }

        singleClick_r = false;
        first_r = true;
        isRightMouseEmptyDown = false;
    }

    /// <summary>
    /// 射线射击到的对象
    /// </summary>
    /// <returns></returns>
    GameObject GetClickGameObject()
    {
        GameObject clickobj = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo))
        {
            clickobj = hitInfo.collider.gameObject;
        }
        return clickobj;
    }

    #region 单击
    public void RegistOnLeftMouseClick(UnityAction mouseClick)
    {
        onLeftMouseShortClick.AddListener(mouseClick);
    }

    public void UnRegistOnLeftMouseClick(UnityAction mouseClick)
    {
        onLeftMouseShortClick.RemoveListener(mouseClick);
    }

    public void RegistOnRightMouseClick(UnityAction mouseClick)
    {
        onRightMouseShortClick.AddListener(mouseClick);
    }

    public void UnRegistOnRightMouseClick(UnityAction mouseClick)
    {
        onRightMouseShortClick.RemoveListener(mouseClick);
    }
    #endregion

    #region 空白处单击
    public void RegistOnLeftMouseEmptyClick(UnityAction leftMouseEmptyClick)
    {
        onLeftMouseEmptyShortClick.AddListener(leftMouseEmptyClick);
    }

    public void UnRegistOnLeftMouseEmptyClick(UnityAction leftMouseEmptyClick)
    {
        onLeftMouseEmptyShortClick.RemoveListener(leftMouseEmptyClick);
    }
    public void RegistOnRightMouseEmptyClick(UnityAction rightMouseEmptyClick)
    {
        onRightMouseEmptyShortClick.AddListener(rightMouseEmptyClick);
    }

    public void UnRegistOnRightMouseEmptyClick(UnityAction rightMouseEmptyClick)
    {
        onRightMouseEmptyShortClick.RemoveListener(rightMouseEmptyClick);
    }
    #endregion


    #region 双击
    /// <summary>
    /// 注册鼠标左键双击事件
    /// </summary>
    public void RegistOnLeftMouseDoubleClick(UnityAction leftMouseDoubleClick)
    {
        onLeftMouseDoubleClick.AddListener(leftMouseDoubleClick);
    }
    /// <summary>
    /// 注销鼠标左键双击事件
    /// </summary>
    public void UnRegistOnLeftMouseDoubleClick(UnityAction leftMouseDoubleClick)
    {
        onLeftMouseDoubleClick.RemoveListener(leftMouseDoubleClick);
    }
    /// <summary>
    /// 注册鼠标右键双击事件
    /// </summary>
    public void RegistOnRightMouseDoubleClick(UnityAction rightMouseDoubleClick)
    {
        onRightMouseDoubleClick.AddListener(rightMouseDoubleClick);
    }
    /// <summary>
    /// 注销鼠标右键双击事件
    /// </summary>
    public void UnRegistOnRightMouseDoubleClick(UnityAction rightMouseDoubleClick)
    {
        onRightMouseDoubleClick.RemoveListener(rightMouseDoubleClick);
    }
    #endregion

    #region 空白处双击
    public void RegistOnLeftMouseEmptyDoubleClick(UnityAction leftMouseDoubleClick)
    {
        onLeftMouseEmptyDoubleClick.AddListener(leftMouseDoubleClick);
    }

    public void UnRegistOnLeftMouseEmptyDoubleClick(UnityAction leftMouseDoubleClick)
    {
        onLeftMouseEmptyDoubleClick.RemoveListener(leftMouseDoubleClick);
    }
    public void RegistOnRightMouseEmptyDoubleClick(UnityAction rightMouseDoubleClick)
    {
        onRightMouseEmptyDoubleClick.AddListener(rightMouseDoubleClick);
    }

    public void UnRegistOnRightMouseEmptyDoubleClick(UnityAction rightMouseDoubleClick)
    {
        onRightMouseEmptyDoubleClick.RemoveListener(rightMouseDoubleClick);
    }
    #endregion

    #region 长按
    public void RegistOnLeftMouseLongClick(UnityAction leftMouseLongClick)
    {
        onLeftMouseLongClick.AddListener(leftMouseLongClick);
    }

    public void UnRegistOnLeftMouseLongClick(UnityAction leftMouseLongClick)
    {
        onLeftMouseLongClick.RemoveListener(leftMouseLongClick);
    }
    public void RegistOnRightMouseLongClick(UnityAction rightMouseLongClick)
    {
        onRightMouseLongClick.AddListener(rightMouseLongClick);
    }

    public void UnRegistOnRightMouseLongClick(UnityAction rightMouseLongClick)
    {
        onRightMouseLongClick.RemoveListener(rightMouseLongClick);
    }
    #endregion
}