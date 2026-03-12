using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityFramework.Runtime;

/// <summary>
/// 悬浮提示界面
/// 子类须实现虚方法InitHoverHint,对需要有悬浮提示的按钮调用AddHoverHint
/// </summary>
public abstract class HoverHintPanel : UIPanelBase
{
    /// <summary>
    /// 悬浮提示出现方位
    /// </summary>
    public enum HoverOrientation
    {
        Right,
        Bottom
    }

    #region 不同方位的悬浮提示的预制体
    public GameObject RightHoverPrefab;
    public GameObject BottomHoverPrefab;
    #endregion

    protected RectTransform _rightHoverHint;
    protected RectTransform RightHoverHint
    {
        get
        {
            if(_rightHoverHint == null)
            {
                if (RightHoverPrefab == null)
                    Debug.LogError("未设置悬浮提示预制体");
                _rightHoverHint = Instantiate(RightHoverPrefab, transform).GetComponent<RectTransform>();
            }
            return _rightHoverHint;
        }
    }

    protected RectTransform _bottomHoverHint;
    protected RectTransform BottomHoverHint
    {
        get
        {
            if (_bottomHoverHint == null)
            {
                if (BottomHoverPrefab == null)
                    Debug.LogError("未设置悬浮提示预制体");
                _bottomHoverHint = Instantiate(BottomHoverPrefab, transform).GetComponent<RectTransform>();
            }
            return _bottomHoverHint;
        }
    }

    private RectTransform ActiveHoverHint;
    private bool flag;
    private float hoverDeltaTime;
    private float HoverThreshold = 0f;// 0.6f;

    protected abstract void InitHoverHint();

    public override void Show(UIData uiData = null)
    {
#if UNITY_STANDALONE
        InitHoverHint();
#endif
        base.Show(uiData);
    }

    protected void AddHoverHint(Component btn, string info, HoverOrientation hoverOrientation = HoverOrientation.Right)
    {
#if UNITY_STANDALONE
        if (btn == null)
            return;

        RectTransform targetHover = null;
        Vector2 anchorPos = Vector2.zero;
        switch (hoverOrientation)
        {
            case HoverOrientation.Right:
                targetHover = RightHoverHint;
                anchorPos = new Vector2(btn.GetComponent<RectTransform>().sizeDelta.x / 2 + 2, 0);
                break;
            case HoverOrientation.Bottom:
                targetHover = BottomHoverHint;
                anchorPos = new Vector2(0, -btn.GetComponent<RectTransform>().sizeDelta.y - 8);
                break;
            default:
                break;
        }
        if (targetHover == null)
            return;

        EventTrigger eventTrigger = btn.AutoComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerEnter, (_) =>
        {
            if (targetHover)
            {
                targetHover.GetComponentInChildren<Text>().text = info;
                targetHover.transform.SetParent(btn.transform);
                targetHover.anchoredPosition = anchorPos;
                ActiveHoverHint = targetHover;
                flag = true;
            }
        });
        eventTrigger.AddEvent(EventTriggerType.PointerExit, (_) =>
        {
            flag = false;
            targetHover.gameObject.SetActive(false);
            ActiveHoverHint = null;
        });
#endif
    }

    protected void AddHoverHint(Toggle toggle, string infoOff, string infoOn, HoverOrientation hoverOrientation = HoverOrientation.Right)
    {
#if UNITY_STANDALONE
        if (toggle == null)
            return;

        RectTransform targetHover = null;
        Vector2 anchorPos = Vector2.zero;
        switch (hoverOrientation)
        {
            case HoverOrientation.Right:
                targetHover = RightHoverHint;
                anchorPos = new Vector2(toggle.GetComponent<RectTransform>().sizeDelta.x / 2 + 2, 0);
                break;
            case HoverOrientation.Bottom:
                targetHover = BottomHoverHint;
                anchorPos = new Vector2(0, -toggle.GetComponent<RectTransform>().sizeDelta.y - 8);
                break;
            default:
                break;
        }
        if (targetHover == null)
            return;

        EventTrigger eventTrigger = toggle.AutoComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerEnter, (_) =>
        {
            if (targetHover)
            {
                targetHover.GetComponentInChildren<Text>().text = toggle.isOn ? infoOn : infoOff;
                targetHover.transform.SetParent(toggle.transform);
                targetHover.anchoredPosition = anchorPos;
                ActiveHoverHint = targetHover;
                flag = true;
            }
        });
        eventTrigger.AddEvent(EventTriggerType.PointerExit, (_) =>
        {
            flag = false;
            targetHover.gameObject.SetActive(false);
            ActiveHoverHint = null;
        });
#endif
    }


    private void Update()
    {
#if UNITY_STANDALONE
        OnUpdate();
#endif
    }

    protected virtual void OnUpdate()
    {
        OnHoverCheck();
    }

    protected void OnHoverCheck()
    {
        if (flag)
        {
            hoverDeltaTime += Time.deltaTime;
            if (hoverDeltaTime > HoverThreshold && ActiveHoverHint != null)
            {
                ActiveHoverHint.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(ActiveHoverHint);
            }
        }
        else
        {
            hoverDeltaTime = 0;
        }
    }
}