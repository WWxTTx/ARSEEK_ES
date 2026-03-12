using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 无限滚动列表
/// </summary>
public class InfiniteListScrollRect : ScrollRect
{
    /// <summary>
    /// 元素模板
    /// </summary>
    public GameObject ElementTemplate;
    /// <summary>
    /// 元素模板
    /// </summary>
    public GameObject SelfElementTemplate;

    /// <summary>
    /// 元素排列方向
    /// </summary>
    public Direction ListingDirection = Direction.Vertical;
    /// <summary>
    /// 元素高度
    /// </summary>
    public int Height = 20;
    /// <summary>
    /// 元素之间的间隔
    /// </summary>

    public int Interval = 5;
    private List<InfiniteListData> _datas = new List<InfiniteListData>();
    private HashSet<InfiniteListData> _dataIndexs = new HashSet<InfiniteListData>();
    private Dictionary<InfiniteListData, InfiniteListElement> _displayElements = new Dictionary<InfiniteListData, InfiniteListElement>();
    private HashSet<InfiniteListData> _invisibleList = new HashSet<InfiniteListData>();

    private Dictionary<bool, Queue<InfiniteListElement>> _elementsPoolByType = new Dictionary<bool, Queue<InfiniteListElement>>();
    private RectTransform _uiTransform;

    /// <summary>
    /// UGUI变换组件
    /// </summary>
    public RectTransform UITransform
    {
        get
        {
            if (_uiTransform == null)
            {
                _uiTransform = GetComponent<RectTransform>();
            }
            return _uiTransform;
        }
    }
    /// <summary>
    /// 当前数据数量
    /// </summary>
    public int DataCount
    {
        get
        {
            return _datas.Count;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        onValueChanged.AddListener((value) => { RefreshScrollView(); });
    }

    /// <summary>
    /// 添加一条新的数据到无限列表尾部
    /// </summary>
    /// <param name="data">无限列表数据</param>
    public virtual void AddData(InfiniteListData data)
    {
        if (_dataIndexs.Contains(data))
        {
            Debug.LogWarning("添加数据至无限列表失败：列表中已存在该数据 " + data.ToString());
            return;
        }

        _datas.Add(data);
        _dataIndexs.Add(data);

        RefreshScrollContent();

        listDirty = true;
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="data">无限列表数据</param>
    public virtual InfiniteListData GetData(int index)
    {
        if (index < 0 || index >= _datas.Count)
        {
            return null;
        }

        return _datas[index];
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="data">无限列表数据</param>
    public virtual InfiniteListData GetData(string uid)
    {
        if(_datas == null || _datas.Count == 0)
        {
            return null;
        }

        return _datas.Find(d => d.UID.Equals(uid));
    }

    /// <summary>
    /// 添加多条新的数据到无限列表尾部
    /// </summary>
    /// <typeparam name="T">无限列表数据类型</typeparam>
    /// <param name="datas">无限列表数据</param>
    public void AddDatas<T>(List<T> datas) where T : InfiniteListData
    {
        for (int i = 0; i < datas.Count; i++)
        {
            if (_dataIndexs.Contains(datas[i]))
            {
                Debug.LogWarning("添加数据至无限列表失败：列表中已存在该数据 " + datas[i].ToString());
                continue;
            }

            _datas.Add(datas[i]);
            _dataIndexs.Add(datas[i]);
        }

        RefreshScrollContent();

        listDirty = true;
    }

    /// <summary>
    /// 刷新滚动列表内容
    /// </summary>
    protected void RefreshScrollContent()
    {
        if (ListingDirection == Direction.Vertical)
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, _datas.Count * (Height + Interval));
        }
        else
        {
            content.sizeDelta = new Vector2(_datas.Count * (Height + Interval), content.sizeDelta.y);
        }

        RefreshScrollView();
    }

    /// <summary>
    /// 刷新滚动视图
    /// </summary>
    protected void RefreshScrollView()
    {
        if (ListingDirection == Direction.Vertical)
        {
            float contentY = content.anchoredPosition.y;
            float viewHeight = UITransform.sizeDelta.y;

            ClearInvisibleVerticalElement(contentY, viewHeight);

            int originIndex = (int)(contentY / (Height + Interval));
            if (originIndex < 0) originIndex = 0;
            for (int i = originIndex; i < _datas.Count; i++)
            {
                InfiniteListData data = _datas[i];
                float viewY = -(i * Height + (i + 1) * Interval);
                float realY = viewY + contentY;
                if (realY > -viewHeight)
                {
                    if (_displayElements.ContainsKey(data))
                    {
                        _displayElements[data].UITransform.anchoredPosition = new Vector2(0, viewY);
                        continue;
                    }

                    InfiniteListElement element = ExtractIdleElement(data.Self);
                    //element.index = i;
                    //element.uid = data.UID;
                    element.transform.name = data.UID;
                    element.UITransform.anchoredPosition = new Vector2(0, viewY);
                    element.OnUpdateData(this, data);
                    _displayElements.Add(data, element);
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            float contentX = content.anchoredPosition.x;
            float viewWidth = UITransform.sizeDelta.x;

            ClearInvisibleHorizontalElement(contentX, viewWidth);

            int originIndex = (int)(-contentX / (Height + Interval));
            if (originIndex < 0) originIndex = 0;
            for (int i = originIndex; i < _datas.Count; i++)
            {
                InfiniteListData data = _datas[i];
                float viewX = i * Height + (i + 1) * Interval;
                float realX = viewX + contentX;
                if (realX < viewWidth)
                {
                    if (_displayElements.ContainsKey(data))
                    {
                        _displayElements[data].UITransform.anchoredPosition = new Vector2(viewX, 0);
                        continue;
                    }

                    InfiniteListElement element = ExtractIdleElement(data.Self);
                    element.UITransform.anchoredPosition = new Vector2(viewX, 0);
                    element.OnUpdateData(this, data);
                    _displayElements.Add(data, element);
                }
                else
                {
                    break;
                }
            }
        }
    }

    bool listDirty = false;

    private void Update()
    {
        if (listDirty)
        {
            verticalNormalizedPosition = 0f;
            listDirty = false;
        }
    }

    /// <summary>
    /// 清理并回收看不见的元素（垂直模式）
    /// </summary>
    /// <param name="contentY">滚动视图内容位置y</param>
    /// <param name="viewHeight">滚动视图高度</param>
    private void ClearInvisibleVerticalElement(float contentY, float viewHeight)
    {
        foreach (var element in _displayElements)
        {
            float realY = element.Value.UITransform.anchoredPosition.y + contentY;
            if (realY < Height && realY > -viewHeight)
            {
                continue;
            }
            else
            {
                _invisibleList.Add(element.Key);
            }
        }
        foreach (var item in _invisibleList)
        {
            RecycleElement(_displayElements[item]);
            _displayElements.Remove(item);
        }
        _invisibleList.Clear();
    }
    /// <summary>
    /// 清理并回收看不见的元素（水平模式）
    /// </summary>
    /// <param name="contentX">滚动视图内容位置x</param>
    /// <param name="viewWidth">滚动视图宽度</param>
    private void ClearInvisibleHorizontalElement(float contentX, float viewWidth)
    {
        foreach (var element in _displayElements)
        {
            float realX = element.Value.UITransform.anchoredPosition.x + contentX;
            if (realX > -Height && realX < viewWidth)
            {
                continue;
            }
            else
            {
                _invisibleList.Add(element.Key);
            }
        }
        foreach (var item in _invisibleList)
        {
            RecycleElement(_displayElements[item]);
            _displayElements.Remove(item);
        }
        _invisibleList.Clear();
    }

    private InfiniteListElement ExtractIdleElement(bool self)
    {
        if(_elementsPoolByType.TryGetValue(self, out Queue<InfiniteListElement> pool))
        {
            if(pool.Count > 0)
            {
                InfiniteListElement element = pool.Dequeue();
                element.gameObject.SetActive(true);
                return element;
            }
        }

        GameObject elementGo = Instantiate(self ? SelfElementTemplate : ElementTemplate, content);
        InfiniteListElement elementComponent = elementGo.GetComponent<InfiniteListElement>();
        elementComponent.OnInitData(this);
        elementGo.SetActive(true);
        return elementComponent;
    }

    /// <summary>
    /// 回收一个无用的无限列表元素
    /// </summary>
    /// <param name="element">无限列表元素</param>
    private void RecycleElement(InfiniteListElement element)
    {
        element.OnClearData();
        element.gameObject.SetActive(false);
        if (_elementsPoolByType.TryGetValue(element.Self, out Queue<InfiniteListElement> pool))
        {
            pool.Enqueue(element);
        }
        else
        {
            Queue<InfiniteListElement> _elementPool = new Queue<InfiniteListElement>();
            _elementPool.Enqueue(element);
            _elementsPoolByType.Add(element.Self, _elementPool);
        }
    }
    /// <summary>
    /// 方向
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// 水平
        /// </summary>
        Horizontal,
        /// <summary>
        /// 垂直
        /// </summary>
        Vertical
    }
}