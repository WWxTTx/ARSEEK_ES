using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// 翻页效果
/// </summary>
public class ScrollPageView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private ScrollRect rect;
    private float targethorizontal = 0;
    private List<float> posList = new List<float>();//储存contentChild页面的位置
    private int curIndex = 0;                       //当前页码
    private bool isDrag = false;                     //是否为拖拽状态
    private float startDragHorizontal;

    public UnityAction<bool> PageAction;                  //翻页结束时回调

    public UnityAction OnBeginDragAction;

    public int CurIndex
    {
        get { return curIndex; }
        set { curIndex = value; }
    }

    private List<Toggle> toggleList = new List<Toggle>();
    public List<Toggle> ToggleList
    {
        get { return toggleList; }
        set
        {
            toggleList.Clear();
            toggleList = value;
        }
    }

    private bool isLeftDrag = true;
    public bool IsLeftDrag
    {
        set { isLeftDrag = value; }
    }

    private bool isRightDrag = true;
    public bool IsRightDrag
    {
        set { isRightDrag = value; }
    }


    void Start()
    {
        rect = GetComponent<ScrollRect>();
        ScrollChildCount();
    }

    /// <summary>
    /// 页码位置
    /// </summary>
    public void ScrollChildCount()
    {
        var _rectWidth = GetComponent<RectTransform>().rect.width;
        float horizontalLength = rect.content.rect.width - _rectWidth;
        if (posList != null)
            posList.Clear();
        for (int i = 0; i < rect.content.transform.childCount; i++)
        {
            var f = _rectWidth * i / horizontalLength;
            posList.Add(f);
        }
        //curIndex = 0;//初始页码为0（显示第一页）
        PageTo(curIndex);//设置初始页面
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDrag = true;
        startDragHorizontal = rect.horizontalNormalizedPosition;

        OnBeginDragAction?.Invoke();
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    /// <param name="eventData"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void OnDrag(PointerEventData eventData)
    {
        var offes = rect.horizontalNormalizedPosition - startDragHorizontal;

        if (!isLeftDrag)
            if (offes < 0)
                rect.horizontalNormalizedPosition = startDragHorizontal;

        if(!isRightDrag)
            if (offes > 0)
                rect.horizontalNormalizedPosition = startDragHorizontal;
    }

    /// <summary>
    /// 拖拽结束
    /// </summary>
    /// <param name="eventData"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void OnEndDrag(PointerEventData eventData)
    {
        float posX = rect.horizontalNormalizedPosition;
        int index = 0;
        float offset = Mathf.Abs(posList[index] - posX); //计算当前位置与第一页的偏移量（初始化offset）
        for (int i = 1; i < posList.Count; i++)
        {
            //遍历页签，选取当前x位置和每页偏移量最小的那个页面
            float temp = Mathf.Abs(posList[i] - posX);
            if (temp < offset)
            {
                index = i;
                offset = temp;
            }
        }

        curIndex = index;
        targethorizontal = posList[curIndex];
        isDrag = false;
        DoAnim(curIndex);
    }

    /// <summary>
    /// 翻页
    /// </summary>
    /// <param name="index"></param>
    public void PageTo(int index)
    {
        if (index < 0 || index >= posList.Count) return;

        curIndex = index;
        targethorizontal = posList[curIndex]; //设置当前坐标，更新函数进行插值
        isDrag = false;
        // DoAnim();
        rect.horizontalNormalizedPosition = targethorizontal;
    }

    /// <summary>
    /// 翻页动效
    /// </summary>
    /// <param name="index"></param>
    /// <param name="scrolled">是否通过拖动翻页</param>
    public void DoAnim(int index, bool scrolled = true)
    {
        if (isDrag) return;

        DOTween.To(() => rect.horizontalNormalizedPosition, (value) => rect.horizontalNormalizedPosition = (value), targethorizontal, 0.2f).OnComplete(() =>
        {
            if (index < toggleList.Count)
            {
                toggleList[index].SetIsOnWithoutNotify(true);
            }
            PageAction?.Invoke(scrolled);
            //Log.Debug("翻页结束" + toggleList.Count);
        });
    }
}