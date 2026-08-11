using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 图纸数据结构
/// </summary>
public struct DrawingData
{
    public string name;
    public Sprite sprite;
}

public class UISmallSceneMasterComputerPanel : MonoBase
{
    public Text Title;
    public Transform WindowView;
    public Button ZoomInBtn;
    public Button ZoomOutBtn;
    public Button Over;

    public Button ListToggle;
    public ToggleGroup ListContent;

    private List<DrawingData> data;
    private Color textSelectColor;

    private ImageViewer imageViewer;
    private RectTransform windowRect;
    private Canvas windowCanvas;
    private bool isZoomed;

    // margins: (left, bottom, right, top)
#if UNITY_ANDROID || UNITY_IOS
    private static readonly Vector4 defaultMargins = new Vector4(300, 200, 300, 200);
    private static readonly Vector4 zoomedMargins = new Vector4(0, 200, 0, 100);
#else
    private static readonly Vector4 defaultMargins = new Vector4(300, 150, 300, 200);
    private static readonly Vector4 zoomedMargins = new Vector4(50, 120, 0, 50);
#endif

    private void Awake()
    {
        AddMsg(new ushort[]{
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.ClousePop
        });

        imageViewer = WindowView.GetComponentInChildren<ImageViewer>();
        windowRect = WindowView as RectTransform;
        windowCanvas = WindowView.GetComponent<Canvas>();

        ZoomInBtn.onClick.AddListener(() => ToggleZoom(true));
        ZoomOutBtn.onClick.AddListener(() => ToggleZoom(false));

        ListToggle.onClick.AddListener(() => {
            ListContent.gameObject.SetActive(!ListContent.gameObject.activeSelf);
        });
        Over.onClick.AddListener(OnOverButtonClick);

        // 默认显示放大按钮，WindowView 使用默认边距
        ApplyMargins(defaultMargins);
        ZoomInBtn.gameObject.SetActive(true);
        ZoomOutBtn.gameObject.SetActive(false);
        if (windowCanvas != null)
            windowCanvas.sortingOrder = 0;
    }

    private void ApplyMargins(Vector4 margins)
    {
        if (windowRect == null) return;
        windowRect.offsetMin = new Vector2(margins.x, margins.y);
        windowRect.offsetMax = new Vector2(-margins.z, -margins.w);
    }

    private void ToggleZoom(bool zoomIn)
    {
        if (zoomIn)
        {
            ApplyMargins(zoomedMargins);
            ZoomInBtn.gameObject.SetActive(false);
            ZoomOutBtn.gameObject.SetActive(true);
            if (windowCanvas != null)
                windowCanvas.sortingOrder = 1;
        }
        else
        {
            ApplyMargins(defaultMargins);
            ZoomInBtn.gameObject.SetActive(true);
            ZoomOutBtn.gameObject.SetActive(false);
            if (windowCanvas != null)
                windowCanvas.sortingOrder = 0;
        }
        isZoomed = zoomIn;
    }

    /// <summary>
    /// 设置图纸列表
    /// </summary>
    /// <param name="source">图纸数据列表</param>
    /// <param name="selectedColor">选中颜色</param>
    public void SetViews(List<DrawingData> source, Color selectedColor)
    {
        data = source;
        textSelectColor = selectedColor;

        if (data == null || data.Count == 0)
            return;

        gameObject.SetActive(true);

        // 使用 RefreshItemsView 复用列表项
        ListContent.transform.RefreshItemsView(data, (item, drawingData) =>
        {
            Text text = item.GetComponentInChildren<Text>();
            text.text = drawingData.name;
            text.color = Color.white;
            Toggle toggle = item.GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                text.color = isOn ? textSelectColor : Color.white;
                if (isOn)
                {
                    int index = item.GetSiblingIndex() - 1;
                    if (index >= 0 && index < data.Count)
                        ShowDrawing($"{data[index].name}({index + 1}/{data.Count})", data[index].sprite);
                }
            });
        });

        ListContent.gameObject.SetActive(data.Count > 1);

        foreach (var component in ListContent.GetComponentsInChildren<LayoutGroup>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(component.GetComponent<RectTransform>());
        }

        Toggle[] toggles = ListContent.GetComponentsInChildren<Toggle>();
        toggles[toggles.Length - 1].SetIsOnWithoutNotify(true);
        toggles[toggles.Length - 1].onValueChanged.Invoke(true);
        ListContent.allowSwitchOff = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowView()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 完成按钮点击回调
    /// </summary>
    private void OnOverButtonClick()
    {
        // 隐藏本地面板
        HideView();
        Over.gameObject.SetActive(false);

        // 广播关闭图纸（arg1=false 区别于 BehavePopup 的关闭弹窗 arg1=true）
        ToolManager.SendBroadcastMsg(new MsgBool((ushort)SmallFlowModuleEvent.ClousePop, false));

        // 谁点击谁推进步骤（不限定房主，房主可能不在房间）
        SmallFlowCtrl flowCtrl = ModelManager.Instance?.modelGo?.GetComponent<SmallFlowCtrl>();
        if (flowCtrl != null)
        {
            GlobalInfo.WaitUiOq = false;
            SpeechManager.Instance.PlayImmediate(flowCtrl.CurrentStep().ID, 0, TipType.StepComplete);
            flowCtrl.RecordCurrentStepOperations();
            flowCtrl.Next();
        }
    }

    /// <summary>
    /// 显示图纸（复用 ImageViewer，只更新图片）
    /// </summary>
    /// <param name="drawingName">图纸名称</param>
    /// <param name="sprite">图纸图片</param>
    public void ShowDrawing(string drawingName, Sprite sprite)
    {
        gameObject.SetActive(true);
        Title.text = drawingName;

        if (imageViewer != null && sprite != null)
        {
            imageViewer.SetImage(sprite);
        }
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HideView()
    {
        gameObject.SetActive(false);
        Over.gameObject.SetActive(false);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectStep:
                HideView();
                break;
            case (ushort)SmallFlowModuleEvent.ClousePop:
                // arg1=false=关闭图纸(arg1=true=关闭弹窗，由UISmallSceneModule处理)
                if (msg is MsgBrodcastOperate brodcast
                    && brodcast.senderId != GlobalInfo.account.id
                    && !brodcast.GetData<MsgBool>().arg1)
                    HideView();
                break;
        }
    }
}


public static class RectTransformExtension
{
    public static void SetParentAndKeepPosition(this RectTransform rectTransform, Transform parent)
    {
        var originAnchorPosition = rectTransform.anchoredPosition;
        var originOffsetMin = rectTransform.offsetMin;
        var originOffsetMax = rectTransform.offsetMax;

        rectTransform.SetParent(parent);

        rectTransform.localPosition = Vector3.zero;
        rectTransform.anchoredPosition = originAnchorPosition;
        rectTransform.offsetMin = originOffsetMin;
        rectTransform.offsetMax = originOffsetMax;
        rectTransform.localScale = Vector3.one;
    }
}