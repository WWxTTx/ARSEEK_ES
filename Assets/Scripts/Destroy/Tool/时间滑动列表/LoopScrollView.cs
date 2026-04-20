using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityFramework.Runtime;

/// <summary>
/// 滚动列表，类似时间表盘
/// </summary>
public class LoopScrollView : MonoBehaviour
{
    #region 声明
    [Range(0, 1)]
    [Tooltip("识别点，此点重合的元素为当前选中元素,默认0.5F")]
    public float CheckPoint = 0.5f;
    /// <summary>
    /// 是否执行Update 有需要可暂停
    /// </summary>
    private bool runUpdate = true;
    /// <summary>
    /// 数组序号记录
    /// </summary>
    public int NowIndex;
    /// <summary>
    /// 最小值侧 数组序号记录
    /// </summary>
    public int minIndex;
    /// <summary>
    /// 最大值侧 数组序号记录
    /// </summary>
    public int maxIndex;
    /// <summary>
    /// 当前选中物体
    /// </summary>
    public Transform selectItem;
    /// <summary>
    /// 数组大小
    /// </summary>
    public int arraySize;
    /// <summary>
    /// item换位极限值
    /// </summary>
    private float moveLimitValue;
    /// <summary>
    /// item对应轴长度
    /// </summary>
    private float itemLenght;
    /// <summary>
    /// 组件整体对应轴长度
    /// </summary>
    private float componentLenght;
    /// <summary>
    /// 选中差值 第一元素到选中元素的差值
    /// </summary>
    private int selectDValue;
    /// <summary>
    /// 移动参数
    /// </summary>
    private float moveIndex;
    /// <summary>
    /// 组件移动的位移量
    /// </summary>
    private Vector2 moveValue;
    /// <summary>
    /// 最小值侧物体跟踪
    /// </summary>
    public Transform minPoint;
    /// <summary>
    /// 最大值侧物体跟踪
    /// </summary>
    public Transform maxPoint;
    /// <summary>
    /// 组件
    /// </summary>
    private ScrollRect component;
    /// <summary>
    /// 组件
    /// </summary>
    private RectTransform Item;
    /// <summary>
    /// item链表
    /// </summary>
    private LinkedList<Transform> items;
    /// <summary>
    /// 实例化时调用的事件
    /// </summary>
    private UnityAction<int, Transform> instantiate;
    /// <summary>
    /// 更换选中时的当前item事件
    /// </summary>
    private UnityAction<int, Transform> nowSelectedItem;
    /// <summary>
    /// 更换选中时的上一个item事件
    /// </summary>
    private UnityAction<int, Transform> previousSelectedItem;
    /// <summary>
    /// 更换端点item时更改前item事件
    /// </summary>
    private UnityAction<int, Transform> afterExtremeValueChange;
    /// <summary>
    /// 更换端点item时更改后item事件
    /// </summary>
    private UnityAction<int, Transform> beforeExtremeValueChange;
    /// <summary>
    /// Disable时触发
    /// </summary>
    private UnityAction<int[], Transform[]> onDisableEvent;
    #endregion
    private void Awake()
    {
        if (!component) init();
    }
    private void init()
    {
        
        component = GetComponent<ScrollRect>();

        if (!component)
        {
            Log.Error("缺少ScrollRect组件");
            enabled = false;
            return;
        }
        if (component.content.childCount < 1)
        {
            Log.Error("content缺少单例");
            enabled = false;
            return;
        }
        if (component.horizontal && component.vertical)
        {
            Log.Error("不支持双向");
            enabled = false;
            return;
        }
        component.movementType = ScrollRect.MovementType.Unrestricted;
        if (!component.GetComponent<EventTrigger>())
            component.gameObject.AddComponent<EventTrigger>();
        if (!component.GetComponent<CanvasGroup>())
            component.gameObject.AddComponent<CanvasGroup>();

        component.GetComponent<EventTrigger>().AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            component.GetComponent<CanvasGroup>().blocksRaycasts = false;
            DOTween.To(() =>
            component.verticalNormalizedPosition,
            x => component.verticalNormalizedPosition = x,
            selectItem.position.y / scaleIndex / componentLenght - moveIndex, 0.25f).SetRelative().SetEase(Ease.Linear).onComplete =
            () => component.GetComponent<CanvasGroup>().blocksRaycasts = true;
        });
        Item = component.content.GetChild(0).GetComponent<RectTransform>();
        InitItemInfo();
    }
    private Vector2 itemVectorLenght;
    private RectTransform defaultItem;
    private System.Threading.CancellationTokenSource loopCts;
    private void InitItemInfo()
    {
        defaultItem = component.content.GetChild(0).GetComponent<RectTransform>();
        if (component.horizontal)
        {
            itemLenght = Item.sizeDelta.x;
            itemVectorLenght = new Vector2(itemLenght, 0);
            componentLenght = component.GetComponent<RectTransform>().sizeDelta.x;
        }
        else
        {
            itemLenght = Item.sizeDelta.y;
            componentLenght = component.GetComponent<RectTransform>().sizeDelta.y;
            itemVectorLenght = new Vector2(0, -itemLenght);
        }
    }
    /// <summary>
    /// 加载数组
    /// </summary>
    /// <param name="ArratSize">数组长度</param>
    /// <param name="Init">初始化事件</param>
    /// <param name="NowSelectedItem">更换选中时的当前item事件</param>
    /// <param name="PreviousSelectedItem">更换选中时的上一个item事件</param>
    /// <param name="AfterExtremeValueChange">更换端点item时更改前item事件</param>
    /// <param name="BeforeExtremeValueChange">更换端点item时更改后item事件</param>
    public void Load(int ArratSize, UnityAction<int, Transform> Init = null, UnityAction<int, Transform> NowSelectedItem = null, UnityAction<int, Transform> PreviousSelectedItem = null, UnityAction<int, Transform> AfterExtremeValueChange = null, UnityAction<int, Transform> BeforeExtremeValueChange = null, UnityAction<int[], Transform[]> OnDisableEvent = null)
    {
        if (!component) init();
        arraySize = ArratSize;
        instantiate = Init;
        nowSelectedItem = NowSelectedItem;
        previousSelectedItem = PreviousSelectedItem;
        afterExtremeValueChange = AfterExtremeValueChange;
        beforeExtremeValueChange = BeforeExtremeValueChange;
        onDisableEvent = OnDisableEvent;
        InitItem(instantiate);
    }
    public void ResetLoad(int index = -1)
    {
        Vector2 reset;
        if (component.horizontal)
        {
            reset = new Vector2(0, defaultItem.anchoredPosition.y);
        }
        else
        {
            reset = new Vector2(defaultItem.anchoredPosition.x, 0);
        }
        if (index == -1)
        {
            StartLoop();
        }
        else
        {
            if (index > arraySize - 1)
            {
                Log.Error("调用超界");
                return;
            }
            previousSelectedItem?.Invoke(NowIndex, selectItem);
            int move = index - NowIndex;
            NowIndex = index;
            minIndex = index - minDValue;
            maxIndex = maxDValue + index;
            if (minIndex < 0) minIndex = arraySize + minIndex;
            else if (minIndex > arraySize - 1) minIndex = minIndex - arraySize;
            if (maxIndex < 0) maxIndex = arraySize + maxIndex;
            else if (maxIndex > arraySize - 1) maxIndex = maxIndex - arraySize;
            component.content.anchoredPosition = Vector2.zero;
            int tempInt = minIndex;
            for (int i = 1, size = component.content.childCount; i < size; i++)
            {
                RectTransform item = component.content.GetChild(i).GetComponent<RectTransform>();
                if (tempInt == NowIndex) selectItem = item;
                item.anchoredPosition = reset + ((i - 1) * itemVectorLenght);
                instantiate?.Invoke(tempInt, item);
                tempInt++;
                if (tempInt < 0) tempInt = arraySize + tempInt;
                else if (tempInt > arraySize - 1) tempInt = tempInt - arraySize;
            }

            minPoint = items.First.Value;
            maxPoint = items.Last.Value;

            nowSelectedItem?.Invoke(NowIndex, selectItem);
            StartLoop();
        }
    }
    private void OnDisable()
    {
        nowSelectedItem?.Invoke(NowIndex, selectItem);
        onDisableEvent?.Invoke(new int[] { minIndex, NowIndex, maxIndex }, new Transform[] { minPoint, selectItem, maxPoint });
    }
    public int minDValue;
    public int maxDValue;
    public float scaleIndex = 1;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="insAction">初始化事件</param>
    private void InitItem(UnityAction<int, Transform> insAction)
    {
        Vector2 tempVector;
        int max;
        if (component.horizontal)
        {
            itemLenght = Item.sizeDelta.x;
            componentLenght = component.GetComponent<RectTransform>().sizeDelta.x;
            tempVector = new Vector2(0, 0.5f);
            Item.anchorMin = tempVector;
            Item.anchorMax = tempVector;
            Item.pivot = tempVector;
            Item.anchoredPosition = new Vector2(0, Item.anchoredPosition.y);
            tempVector = new Vector2(Item.sizeDelta.x, 0);
        }
        else
        {
            foreach (RectTransform scale in transform.GetComponentsInParent<RectTransform>())
            {
                scaleIndex *= scale.localScale.y;
            }
            itemLenght = Item.sizeDelta.y;
            componentLenght = component.GetComponent<RectTransform>().sizeDelta.y;
            tempVector = new Vector2(0.5f, 1f);
            Item.anchorMin = tempVector;
            Item.anchorMax = tempVector;
            Item.pivot = tempVector;
            Item.anchoredPosition = new Vector2(Item.anchoredPosition.x, 0);
            tempVector = new Vector2(0, -Item.sizeDelta.y);
        }

        max = Mathf.CeilToInt(componentLenght / itemLenght) + (componentLenght % itemLenght == 0 ? 2 : 1);
        component.content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, componentLenght * 2);
        moveValue = component.horizontal ? new Vector2(max * itemLenght, 0) : new Vector2(0, max * itemLenght);
        NowIndex = Mathf.CeilToInt(componentLenght * CheckPoint / itemLenght) - 1;
        selectDValue = NowIndex;
        items = new LinkedList<Transform>();
        minIndex = 0;
        maxIndex = max - 1;
        minDValue = NowIndex - minIndex;
        maxDValue = maxIndex - NowIndex;
        for (int i = 0; i < max; i++)
        {
            RectTransform item = Instantiate(Item.gameObject, component.content).GetComponent<RectTransform>();
            item.name = i.ToString();
            items.AddLast(item);
            if (i == NowIndex) selectItem = item;
            item.anchoredPosition += i * tempVector;
            item.gameObject.SetActive(true);
            insAction?.Invoke(i, item);
        }
        nowSelectedItem?.Invoke(NowIndex, selectItem);
        moveLimitValue = ((max * itemLenght) - (componentLenght * 0.5f)) * 0.9f;

        minPoint = items.First.Value;
        maxPoint = items.Last.Value;

        moveIndex = (itemLenght * 0.5f + component.transform.position.y) / componentLenght;
        if (!gameObject.activeInHierarchy) return;

        StartLoop();
    }
    private void StartLoop()
    {
        loopCts?.Cancel();
        loopCts = new System.Threading.CancellationTokenSource();
        if (component.horizontal) horizontalUpdate(loopCts.Token).Forget();
        else verticalUpdate(loopCts.Token).Forget();
    }
    /// <summary>
    /// 横轴更新
    /// </summary>
    async UniTaskVoid horizontalUpdate(System.Threading.CancellationToken ct)
    {
        float x;
        while (runUpdate)
        {
            x = component.transform.position.x;
            if (x - minPoint.position.x > moveLimitValue)
            {
                minPoint.GetComponent<RectTransform>().anchoredPosition += moveValue;
                minPoint = NextValue(items, minPoint);
            }
            if (maxPoint.position.x - x > moveLimitValue - itemLenght)
            {
                maxPoint.GetComponent<RectTransform>().anchoredPosition -= moveValue;
                maxPoint = PreviousValue(items, maxPoint);
            }
            await UniTask.Yield(ct);
        }
    }
    /// <summary>
    /// 纵轴更新
    /// </summary>
    async UniTaskVoid verticalUpdate(System.Threading.CancellationToken ct)
    {
        //新标志位
        bool? flag = false;
        //旧标志位
        bool? oldFlag = false;
        //旧坐标
        float oldPoint = 0;
        //min侧的bool值
        bool minBool;
        //max侧的bool值
        bool maxBool;
        float y = component.transform.position.y;
        //选中物体的检测范围
        float CheckRange = ((CheckPoint - 0.5f) * y) + y;
        while (runUpdate)
        {
            if (minPoint.position.y - y > moveLimitValue * scaleIndex)
            {
                beforeExtremeValueChange?.Invoke(maxIndex, maxPoint);
                beforeExtremeValueChange?.Invoke(maxIndex, maxPoint);
                minPoint.GetComponent<RectTransform>().anchoredPosition -= moveValue;
                maxPoint = minPoint;
                minPoint = NextValue(items, minPoint);
                ExtremeValueChange(true);
            }
            if (y - maxPoint.position.y > (moveLimitValue - itemLenght) * scaleIndex)
            {
                beforeExtremeValueChange?.Invoke(maxIndex, maxPoint);
                beforeExtremeValueChange?.Invoke(maxIndex, maxPoint);
                maxPoint.GetComponent<RectTransform>().anchoredPosition += moveValue;
                minPoint = maxPoint;
                maxPoint = PreviousValue(items, maxPoint);
                ExtremeValueChange(false);
            }
            oldFlag = (oldPoint > (itemLenght + CheckRange) * scaleIndex) || (oldPoint < CheckRange * scaleIndex);
            oldPoint = selectItem.position.y;
            minBool = (oldPoint > (itemLenght + CheckRange) * scaleIndex);
            maxBool = (oldPoint < CheckRange * scaleIndex);
            flag = maxBool || minBool;
            if (oldFlag == false && flag == true)
            {
                if (!minBool)
                {
                    previousSelectedItem?.Invoke(NowIndex, selectItem);
                    selectItem = PreviousValue(items, selectItem);
                    NowIndex--;
                    if (NowIndex < 0) NowIndex = arraySize - 1;
                    nowSelectedItem?.Invoke(NowIndex, selectItem);
                }
                else
                {
                    previousSelectedItem?.Invoke(NowIndex, selectItem);
                    selectItem = NextValue(items, selectItem);
                    NowIndex++;
                    if (NowIndex >= arraySize) NowIndex = 0;
                    nowSelectedItem?.Invoke(NowIndex, selectItem);
                }
            }
            await UniTask.Yield(ct);
        }
    }
    /// <summary>
    /// 极值更换时调用
    /// </summary>
    /// <param name="choice"></param>
    private void ExtremeValueChange(bool choice)
    {
        if (choice)
        {
            minIndex++;
            maxIndex++;
        }
        else
        {
            minIndex--;
            maxIndex--;
        }
        if (minIndex < 0) minIndex = arraySize - 1;
        else if (minIndex > arraySize - 1) minIndex = 0;
        if (maxIndex < 0) maxIndex = arraySize - 1;
        else if (maxIndex > arraySize - 1) maxIndex = 0;
        afterExtremeValueChange?.Invoke(minIndex, minPoint);
        afterExtremeValueChange?.Invoke(maxIndex, maxPoint);
    }
    /// <summary>
    /// 获取上一链表值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private T PreviousValue<T>(LinkedList<T> self, T value)
    {
        LinkedListNode<T> node = self.Find(value);
        if (node.Previous == null) return self.Last.Value;
        return node.Previous.Value;
    }
    /// <summary>
    /// 获取下一链表值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private T NextValue<T>(LinkedList<T> self, T value)
    {
        LinkedListNode<T> node = self.Find(value);
        if (node.Next == null) return self.First.Value;
        return node.Next.Value;
    }
}
