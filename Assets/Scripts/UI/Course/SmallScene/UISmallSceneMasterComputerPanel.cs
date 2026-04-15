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

    /// <summary>
    /// 缓存的 ImageViewer 组件，用于复用
    /// </summary>
    private ImageViewer imageViewer;

    private void Awake()
    {
        // 监听步骤选择事件
        AddMsg(new ushort[]{
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.CompleteStep
        });

        // 从 WindowView 下获取唯一的 ImageViewer
        imageViewer = WindowView.GetComponentInChildren<ImageViewer>();

        // 绑定放大缩小按钮
        if (imageViewer != null)
        {
            ZoomInBtn.onClick.AddListener(() => imageViewer.ZoomIn());
            ZoomOutBtn.onClick.AddListener(() => imageViewer.ZoomOut());
        }
        ListToggle.onClick.AddListener(() => {
            ListContent.gameObject.SetActive(!ListContent.gameObject.activeSelf);
        });
        Over.onClick.AddListener(OnOverButtonClick);
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
        // 隐藏图纸面板
        HideView();
        Over.gameObject.SetActive(false);
        ShowTool();

        // 结束当前步骤进入下一步
        SmallFlowCtrl flowCtrl = ModelManager.Instance?.modelGo?.GetComponent<SmallFlowCtrl>();
        if (flowCtrl != null)
        {
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
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectStep:
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                // 步骤切换时关闭工具栏子界面，隐藏Over按钮
                Over.gameObject.SetActive(false);
                ShowTool();
                HideView();
                break;
        }
    }
    void ShowTool()
    {
        // 取消选中图纸按钮并显示工具栏
        UISmallSceneToolModule toolModule = FindObjectOfType<UISmallSceneToolModule>();
        if (toolModule != null)
        {
            toolModule.CancelDrawingToggle();
            toolModule.ShowTool(true);  // 显示工具栏
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