using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可全屏窗口
/// </summary>
public class FullScreenWindow : MonoBehaviour
{
    /// <summary>
    /// 全屏切换方式
    /// </summary>
    public enum FullMode
    {
        Normal,//recttransform stretch
        Rescale //scale
    }
    public FullMode _FullMode;

    public Button FullScreenBtn;
    public Transform FullView;
    public Transform List;
    public Vector2 FullOffsetMax;
    public Vector2 FullOffsetMin;
    private Transform FullButtons;

    public Button ExitFullScreenBtn;
    public Transform WindowView;
    public Vector2 WindowOffsetMax;
    public Vector2 WindowOffsetMin;
    private Transform WindowButtons;

    private RectTransform rectTransform;
    private RectTransform RectTransform => rectTransform == null ? rectTransform = GetComponent<RectTransform>() : rectTransform;

    private Vector2 _transformAnchorPos;

    private bool fullScreen = false;
    public bool FullScreen => fullScreen;

    private float buttonTimer;
    //小于双击间隔就会触发全屏或者缩小
    private float doubleClickGap = 0.3f;


    private void Awake()
    {
        //SetWindow(WindowView, FullView, FullScreenBtn, ExitFullScreenBtn);

        transform.GetComponent<Button>().onClick.AddListener(() =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount ==1 && Time.time - buttonTimer < doubleClickGap)//双击全屏
#else
            //双击
            if (Time.time - buttonTimer < doubleClickGap)//双击全屏
#endif
            {
                if (fullScreen)
                    ExitFullScreenBtn.onClick.Invoke();
                else
                    FullScreenBtn.onClick.Invoke();

               buttonTimer = 0f;
            }
            buttonTimer = Time.time;
        });
    }

    /// <summary>
    /// 初始化显示内容
    /// </summary>
    /// <param name="windowView"></param>
    /// <param name="fullView"></param>
    public void SetWindow(Transform windowView, Transform fullView, Transform list, Button fullScreenBtn, Button exitFullScreenBtn, bool fullScreen = false)
    {
        _transformAnchorPos = RectTransform.anchoredPosition;

        WindowView = windowView;
        FullView = fullView;
        List = list;
        WindowButtons = WindowView.FindChildByName("Buttons");
        FullButtons = FullView.FindChildByName("Buttons");
        FullScreenBtn = fullScreenBtn;
        ExitFullScreenBtn = exitFullScreenBtn;
        FullScreenBtn.onClick.RemoveAllListeners();
        ExitFullScreenBtn.onClick.RemoveAllListeners();

        switch (_FullMode)
        {
            case FullMode.Normal:
                FullScreenBtn.onClick.AddListener(() =>
                {
                    ChangeParentRect(FullView, true);
                    ChangeParentRect(List, FullButtons);
                });
                ExitFullScreenBtn.onClick.AddListener(() =>
                {
                    ChangeParentRect(WindowView, false);
                    ChangeParentRect(List, WindowButtons);
                });
                break;
            case FullMode.Rescale:
                FullScreenBtn.onClick.AddListener(() =>
                {
                    ChangeParentRect_Rescale(FullView, true);
                    ChangeParentRect(List, FullButtons);
                });
                ExitFullScreenBtn.onClick.AddListener(() =>
                {
                    ChangeParentRect_Rescale(WindowView, false);
                    ChangeParentRect(List, WindowButtons);
                });
                break;
        }

        if (fullScreen)
            FullScreenBtn.onClick.Invoke();
        else
            ExitFullScreenBtn.onClick.Invoke();
    }

    /// <summary>
    /// 窗口还原
    /// </summary>
    public void ResetWindow()
    {
        switch (_FullMode)
        {
            case FullMode.Normal:
                ChangeParentRect(WindowView, false);
                ChangeParentRect(List, WindowView.GetChild(0));
                break;
            case FullMode.Rescale:
                ChangeParentRect_Rescale(WindowView, false);
                ChangeParentRect(List, WindowView.GetChild(0));
                break;
        }
    }

    /// <summary>
    /// 全屏切换
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fullScreen"></param>
    private void ChangeParentRect(Transform parent, bool fullScreen)
    {
        if (parent == null)
            return;

        Transform prevParent = transform.parent;
        parent.gameObject.SetActive(true);
        transform.SetParent(parent);
        transform.SetSiblingIndex(transform.parent.childCount - 2);
        if (prevParent != parent)
            prevParent.gameObject.SetActive(false);

        if (fullScreen)
        {
            RectTransform.offsetMax = FullOffsetMax;
            RectTransform.offsetMin = FullOffsetMin;
        }
        else
        {
            RectTransform.offsetMax = WindowOffsetMax;
            RectTransform.offsetMin = WindowOffsetMin;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

        if (transform.TryGetComponent(out ImageViewer imageViewer))
        {
            imageViewer.RecoverScale();
        }

        this.fullScreen = fullScreen;
    }

    /// <summary>
    /// 全屏切换
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fullScreen"></param>
    private void ChangeParentRect(Transform trans, Transform parent)
    {
        if (trans == null)
            return;

        Transform prevParent = trans.parent;
        parent.gameObject.SetActive(true);
        trans.SetParent(parent);
        trans.SetAsLastSibling();
    }

    /// <summary>
    /// 全屏切换 整体缩放
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fullScreen"></param>
    private void ChangeParentRect_Rescale(Transform parent, bool fullScreen)
    {
        Transform prevParent = transform.parent;
        parent.gameObject.SetActive(true);
        transform.SetParent(parent);
        RectTransform.anchoredPosition = (fullScreen ? 0 : _transformAnchorPos.y) * Vector2.up;
        transform.SetSiblingIndex(transform.parent.childCount - 2);

        if (prevParent != parent)
            prevParent.gameObject.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

        var rect = RectTransform.rect;
        var viewSize = new Vector2(rect.width, rect.height);
        var viewerAspect = viewSize.x / viewSize.y;
        var parentRect = transform.parent.GetComponent<RectTransform>().rect;

        Vector2 parentSize;
        float parentAspect;

        if (fullScreen)
        {
            parentSize = new Vector2(parentRect.width, parentRect.height);
            parentAspect = parentSize.x / parentSize.y;
        }
        else
        {
            parentSize = new Vector2(parentRect.width + WindowOffsetMax.x - WindowOffsetMin.x, parentRect.height + WindowOffsetMax.y - WindowOffsetMin.y);
            parentAspect = parentSize.x / parentSize.y;
        }

        if (viewerAspect > parentAspect)
            RectTransform.localScale = (parentSize.x / viewSize.x) * Vector3.one;
        else
            RectTransform.localScale = (parentSize.y / viewSize.y) * Vector3.one;

        this.fullScreen = fullScreen;
    }
}