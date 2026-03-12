using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;

using UnityFramework.Runtime;
public class ScrollRectVertical : MonoBehaviour
{
    #region Public变量
    /// <summary>
    /// 更新单个元素信息事件
    /// </summary>
    public TypeTransformIntAndTransform UpdataItemEvent;
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
    /// 元素默认值
    /// </summary>
    public int DefaultValue = 0;
    /// <summary>
    /// 选中标位
    /// </summary>
    [Range(0, 1F)]
    public float Target;
    #endregion
    #region Private变量
    /// <summary>
    /// 本体携带的组件
    /// </summary>
    private ScrollRect scrollRect;
    /// <summary>
    /// 元素X轴坐标
    /// </summary>
    private float itemXPostion;
    /// <summary>
    /// 元素最大高度
    /// </summary>
    private float itemMaxHeight;
    /// <summary>
    /// 实际元素数量
    /// </summary>
    private int itemNum;
    /// <summary>
    /// 每次移动的高度
    /// </summary>
    private float MoveHeight;
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
    /// 表示标定点所占用的增量
    /// 以标定点（float 0-1）为基础 判断是否通过当前位置 也代表选中中心
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

    public delegate void TypeTransformIntAndTransform(int index, Transform item);
    #endregion

    private void Start()
    {
        //检测组件 并设置为纵向
        scrollRect = gameObject.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Log.Error("未携带ScrollRect请检查脚本位置！");
            return;
        }
        scrollRect.horizontal = false;
        scrollRect.vertical = true;


        InitItem();

        if (UseLocation)
        {
            ExtendEvent.AddEvent(EventTriggerType.EndDrag, (arg) =>
            {
                scrollRect.enabled = false;

                float temp = (Index * itemProportion) + (itemProportion * 0.5F) - targetProportion;
                DOTween.To(() => scrollRect.verticalNormalizedPosition, (x) => scrollRect.verticalNormalizedPosition = x, temp, 0.5f).onComplete =
                () =>
                {
                    scrollRect.enabled = true;
                    StopUpdate = true;
                };
            });
        }

        if (DefaultValue != 0 || Target != 0)
            scrollRect.verticalNormalizedPosition = (DefaultValue * itemProportion) + (itemProportion * 0.5F) - targetProportion;
    }

    /// <summary>
    /// 初始化物体组和参数
    /// </summary>
    private void InitItem()
    {
        ItemRectTransformList = new List<RectTransform>();

        GameObject Item = scrollRect.content.GetChild(0).gameObject;
        itemXPostion = Item.GetComponent<RectTransform>().anchoredPosition.x;
        itemMaxHeight = Item.GetComponent<RectTransform>().rect.height;

        //计算多少物体可以覆盖整个Content 计算后向上取整并加上额外数量（ExtendIndex）
        itemNum = Mathf.CeilToInt(scrollRect.GetComponent<RectTransform>().rect.height / itemMaxHeight) + ExtendIndex;
        //获取物体占总元素的实际百分比 也就是说 slider移动这个值 对应的距离正好是这个物体的长度
        itemProportion = 1f / itemNum;
        MoveHeight = itemNum * itemMaxHeight;
        //标志位所占总元素的实际百分比 例如0.5的中间线 targetProportion并不等于0.5
        targetProportion = (scrollRect.GetComponent<RectTransform>().rect.height * Target) / (itemNum * itemMaxHeight);

        //初始化所有元素
        for (int i = 0; i < itemNum; i++)
        {
            GameObject item = Instantiate(Item, scrollRect.content);
            ItemRectTransformList.Add(item.GetComponent<RectTransform>());
            UpdataItemEvent?.Invoke(i, item.transform);
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(itemXPostion, itemMaxHeight * i);
            item.SetActive(true);
        }

        scrollRect.content.sizeDelta += new Vector2(0, itemMaxHeight * itemNum);
        scrollRect.onValueChanged.AddListener((arg) => StopUpdate = false);

        //是否启用拖拽结束的定位 控制方式有待商榷 可能会优化
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
        //刷新开关
        if (StopUpdate)
            return;

        Value = scrollRect.verticalNormalizedPosition;

        //获取当前整数部分 小数部分 需要移动的超过1圈的物体数量 以及当前选中的item序号
        nowInt = Mathf.FloorToInt(Value);
        nowFloat = Value - nowInt;
        extendNum = Mathf.FloorToInt(itemNum * nowFloat) - 2;
        Index = Mathf.RoundToInt(((Value + targetProportion) / itemProportion) - 0.5f);

        //强行设置每个元素物体的位置 百分百正确 但是消耗很高 等待优化
        for (int i = 0; i < itemNum; i++)
        {
            ItemRectTransformList[i].anchoredPosition = new Vector2(0, MoveHeight * (nowInt + (extendNum > 0 ? 1 : 0))) + new Vector2(itemXPostion, itemMaxHeight * i);
            UpdataItemEvent?.Invoke(((nowInt + (extendNum > 0 ? 1 : 0)) * itemNum) + i, ItemRectTransformList[i]);
            extendNum--;
        }

        //触发事件
        if (Index >= 0)
            SelectItemEvent?.Invoke(Index, ItemRectTransformList[Index % ItemRectTransformList.Count]);
        else
            SelectItemEvent?.Invoke(Index, ItemRectTransformList[(Index % ItemRectTransformList.Count) + ItemRectTransformList.Count - 1]);
    }
}
