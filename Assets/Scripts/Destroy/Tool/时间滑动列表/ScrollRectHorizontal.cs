using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

public class ScrollRectHorizontal : MonoBehaviour
{
    /// <summary>
    /// 更新单个元素信息事件
    /// </summary>
    public TypeTransform UpdataItemEvenet;
    /// <summary>
    /// 选中单个元素信息事件
    /// </summary>
    public TypeTransformIntAndTransform SelectItemEvent;
    /// <summary>
    /// 是否启用定位归位功能
    /// </summary>
    public bool UseLocation = true;
    /// <summary>
    /// 子物体是否携带可交互UI 暂无 但之后有可能开发
    /// </summary>
    //private bool HasInteractiveUI = false;
    /// <summary>
    /// 额外元素数量 用于更完美的全局覆盖 
    /// 数量越多 高速拉动时出现的显示问题越少 但占用就越高 
    /// 默认 3
    /// </summary>
    public int ExtendIndex = 3;
    /// <summary>
    /// 选中标位
    /// </summary>
    [Range(0, 1F)]
    public float Target;
    #region private变量
    /// <summary>
    /// 本体携带的组件
    /// </summary>
    private ScrollRect scrollRect;
    /// <summary>
    /// 元素Y轴坐标
    /// </summary>
    private float itemYPostion;
    /// <summary>
    /// 元素最大宽度
    /// </summary>
    private float itemMaxWidth;
    /// <summary>
    /// 实际元素数量
    /// </summary>
    private int itemNum;
    /// <summary>
    /// 每次移动的宽度
    /// </summary>
    private float MoveWidth;
    /// <summary>
    /// 额外事件
    /// </summary>
    private EventTrigger ExtendEvent;
    /// <summary>
    /// 物体占比
    /// </summary>
    private float itemProportion;
    /// <summary>
    /// 标位占比
    /// </summary>
    private float targetProportion;
    /// <summary>
    /// 当前选中值
    /// </summary>
    private int Index = 0;
    /// <summary>
    /// 当前进度整数部分
    /// </summary>
    private int nowInt;
    /// <summary>
    /// 当前进度小数部分
    /// </summary>
    private float nowFloat;
    /// <summary>
    /// 需要额外移动的item数量
    /// </summary>
    private int extendNum;
    /// <summary>
    /// 当前进度
    /// </summary>
    private float Value;
    /// <summary>
    /// Update控制
    /// </summary>
    private bool StopUpdate = true;
    /// <summary>
    /// item列表
    /// </summary>
    private List<RectTransform> ItemRectTransformList;

    public delegate void TypeTransform(Transform item);
    public delegate void TypeTransformIntAndTransform(int index, Transform item);
    #endregion
    private void Awake()
    {
        scrollRect = gameObject.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Log.Error("未携带ScrollRect请检查脚本位置！");
            return;
        }

        scrollRect.horizontal = true;
        scrollRect.vertical = false;

        InitItem();

        if (UseLocation)
        {
            ExtendEvent.AddEvent(EventTriggerType.EndDrag, (arg) =>
            {
                scrollRect.enabled = false;

                float temp = (Index * itemProportion) + (itemProportion * 0.5F) - targetProportion;
                DOTween.To(() => scrollRect.horizontalNormalizedPosition, (x) => scrollRect.horizontalNormalizedPosition = x, temp, 0.5f).onComplete =
                () =>
                {
                    scrollRect.enabled = true;
                    StopUpdate = true;
                };
            });
        }
    }
    /// <summary>
    /// 初始化物体组和参数
    /// </summary>
    private void InitItem()
    {
        ItemRectTransformList = new List<RectTransform>();

        GameObject Item = scrollRect.content.GetChild(0).gameObject;
        itemYPostion = Item.GetComponent<RectTransform>().anchoredPosition.y;
        itemMaxWidth = Item.GetComponent<RectTransform>().rect.width;

        itemNum = Mathf.CeilToInt(scrollRect.GetComponent<RectTransform>().rect.width / itemMaxWidth) + ExtendIndex;
        itemProportion = 1f / itemNum;
        MoveWidth = itemNum * itemMaxWidth;
        targetProportion = (scrollRect.GetComponent<RectTransform>().rect.width * Target) / (itemNum * itemMaxWidth);
       
        for (int i = 0; i < itemNum; i++)
        {
            GameObject item = Instantiate(Item, scrollRect.content);
            ItemRectTransformList.Add(item.GetComponent<RectTransform>());
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * itemMaxWidth, itemYPostion);
            item.SetActive(true);
        }

        scrollRect.content.sizeDelta += new Vector2(itemMaxWidth * itemNum, 0);
        scrollRect.onValueChanged.AddListener((arg) => StopUpdate = false);

        ExtendEvent = gameObject.GetComponent<EventTrigger>();
        if (!ExtendEvent)
            ExtendEvent = gameObject.AddComponent<EventTrigger>();
        ExtendEvent.AddEvent(EventTriggerType.EndDrag, (arg) =>
        {
            if (!UseLocation)
                StopUpdate = true;
        });
    }
    private void Update()
    {
        if (StopUpdate) return;

        Value = scrollRect.horizontalNormalizedPosition;

        nowInt = Mathf.FloorToInt(Value);
        nowFloat = Value - nowInt;
        extendNum = Mathf.FloorToInt(itemNum * nowFloat) - 2;
        Index = Mathf.RoundToInt((Value + targetProportion) / itemProportion);

        for (int i = 0; i < itemNum; i++)
        {
            ItemRectTransformList[i].anchoredPosition = new Vector2(MoveWidth * (nowInt + (extendNum > 0 ? 1 : 0)), itemYPostion) + new Vector2(i * itemMaxWidth, 0);
            UpdataItemEvenet?.Invoke(ItemRectTransformList[i]);
            extendNum--;
        }

        SelectItemEvent?.Invoke(Index, ItemRectTransformList[Index % ItemRectTransformList.Count]);
    }
}
