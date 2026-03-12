using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 可拖拽排序列表
/// </summary>
public class ReorderableList : MonoBehaviour
{

    /// <summary>
    /// Viewport左下角\右上角
    /// </summary>
    public Transform viewportMinMark;
    public Transform viewportMaxMark;
    /// <summary>
    /// Content左下角\右上角
    /// </summary>
    public Transform contentMinMark;
    public Transform contentMaxMark;

    private ScrollRect scrollRect;

    public GameObject Viewport { get { return scrollRect.viewport.gameObject; } }

    public GameObject Content { get { return scrollRect.content.gameObject; } }

    private VerticalLayoutGroup contentLayoutGroup;
    private Rect contentRect { get { return new Rect(contentMinMark.position, contentMaxMark.position - contentMinMark.position); } }
    private Rect viewportRect { get { return new Rect(viewportMinMark.position, viewportMaxMark.position - viewportMinMark.position); } }

    /// <summary>
    /// 长按秒数
    /// </summary>
    public float longPressThreshold = 0.4f;

    /// <summary>
    /// 超出边界时ScrollView自动滚动速度
    /// </summary>
#if UNITY_ANDROID || UNITY_IOS
    private Vector2 autoScrollSpeed = new Vector2(600f, 600f);
#else
    private Vector2 autoScrollSpeed = new Vector2(300f, 300f);
#endif

    /// <summary>
    /// 拖动时间隙
    /// </summary>
    private GameObject dummy;
    public GameObject Dummy
    {
        get { return dummy; }
        set
        {
            if (dummy != null)
            {
                DestroyDummy(element);
                dummy = null;
            }
            if (value != null)
            {
                dummy = CreateDummy(Content, element = value);
            }
        }
    }
    /// <summary>
    /// 当前拖拽的元素
    /// </summary>
    private GameObject element;

    public int ElementsCount { get { return Content.transform.childCount - ReferencePointsCount; } }

    /// <summary>
    /// 参照点数目(content的左下、右上角)
    /// </summary>
    public int ReferencePointsCount { get; private set; }


    public class OnChangeModeCallback : UnityEvent<bool> { }
    public OnChangeModeCallback onChangeMode = default;
    public class OnReorderCallback : UnityEvent<int> { }
    public OnReorderCallback onSelect = default;
    public OnReorderCallback onBeginOrder = default;
    public OnReorderCallback onUpdateOrder = default;
    public OnReorderCallback onEndOrder = default;

    private bool interactable = true;
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            interactable = value;
            foreach (var element in GetComponentsInChildren<ReorderableListElement>())
            {
                element.Interactable = value;
            }
        }
    }

    private bool orderable;
    public bool Orderable
    {
        get { return orderable; }
        set
        {
            orderable = value;
            if (!value)
                ReorderableListElement.ActiveElement = null;

            if (onChangeMode != null)
                onChangeMode.Invoke(value);
        }
    }

    private List<int> indexes;
    public List<int> Indexes
    {
        get
        {
            if (indexes == null)
                indexes = new List<ReorderableListElement>(Content.GetComponentsInChildren<ReorderableListElement>()).ConvertAll(element => element.Index);
            return indexes;
        }
    }

    private List<GameObject> elementGOs;
    public List<GameObject> ElementGameObjects
    {
        get
        {
            if (elementGOs == null)
                elementGOs = new List<ReorderableListElement>(Content.GetComponentsInChildren<ReorderableListElement>()).ConvertAll(element => element.gameObject);
            return elementGOs;
        }
    }

    /// <summary>
    /// 获取指定索引的元素
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject this[int index]
    {
        get
        {
            return (index >= 0 && index < ElementsCount) ? Content.transform.GetChild(ReferencePointsCount + index).gameObject : null;
        }
    }

    public float TopPadding;
    public float BottomPadding;

    private void Awake()
    {
        if (!scrollRect)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
            contentLayoutGroup = Content.GetComponent<VerticalLayoutGroup>();
            ReferencePointsCount = Content.transform.childCount - Content.GetComponentsInChildren<ReorderableListElement>().Length;
            Orderable = false;
        }
    }

    /// <summary>
    /// 添加元素
    /// </summary>
    /// <param name="element"></param>
    public void AddElement(GameObject element)
    {
        if (!element)
            return;

        var le = element.GetComponentInChildren<ReorderableListElement>();
        if (le == null)
            return;

        element.transform.SetParent(Content.transform);
        element.transform.SetAsLastSibling();

        le.Index = Content.GetComponentsInChildren<ReorderableListElement>().Length - 1;
        le.Init(this, element.GetComponent<RectTransform>());
    }

    /// <summary>
    /// 添加多个元素
    /// </summary>
    /// <param name="elements"></param>
    public void AddElements(IEnumerable<GameObject> elements)
    {
        if (!element)
            return;

        foreach (var element in elements)
            AddElement(element);
    }

    /// <summary>
    /// 清除元素
    /// </summary>
    public void ClearElement(bool immediate = false)
    {
        if (immediate)
        {
            //foreach (var element in Content.GetComponentsInChildren<ReorderableListElement>())
            //    DestroyImmediate(element.ElementRectTransform.gameObject);

            for (int i = Content.transform.childCount - 1; i >= 2; i--)
            {
                DestroyImmediate(Content.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            foreach (var element in Content.GetComponentsInChildren<ReorderableListElement>())
                Destroy(element.ElementRectTransform.gameObject);
        }
    }

    /// <summary>
    /// 选中
    /// </summary>
    /// <param name="index"></param>
    public void OnSelect(int index = -1)
    {
        if (onSelect != null)
        {
            onSelect.Invoke(index);
        }
    }

    private bool useNormalizedPosition = false;

    /// <summary>
    /// 更新间隙对象的位置
    /// </summary>
    /// <param name="pos"></param> 
    public void UpdateDraggingPosition(Vector2 pos)
    {
        if (useNormalizedPosition)
        {
            var normarizedContentPosition = Vector2.Max(Vector2.Min((pos - contentRect.position) / contentRect.size, Vector2.one), Vector2.zero);
            //Item长度不均时，位置计算有问题
            var index = Mathf.Clamp((ElementsCount - 1) - Mathf.FloorToInt(normarizedContentPosition.y / (1f / ElementsCount)), 0, ElementsCount - 1);

            if (dummy.transform.GetSiblingIndex() != ReferencePointsCount + index)
            {
                dummy.transform.SetSiblingIndex(ReferencePointsCount + index);
                if (onUpdateOrder != null)
                    onUpdateOrder.Invoke(index);
            }
        }
        else
        {
            var prevIndex = dummy.transform.GetSiblingIndex();

            Vector2 prevItem = Vector2.positiveInfinity;
            if (prevIndex > ReferencePointsCount)
                prevItem = scrollRect.content.GetChild(prevIndex - 1).GetComponent<RectTransform>().position;

            Vector2 nextItem = Vector2.negativeInfinity;
            if (prevIndex < scrollRect.content.childCount - 1)
                nextItem = scrollRect.content.GetChild(prevIndex + 1).GetComponent<RectTransform>().position;

            var index = prevIndex;
            if (pos.y > prevItem.y)
                index = prevIndex - 1;
            if (pos.y < nextItem.y)
                index = prevIndex + 1;

            if (prevIndex != index)
            {
                dummy.transform.SetSiblingIndex(index);
                if (onUpdateOrder != null)
                    onUpdateOrder.Invoke(index);
            }
        }

        var delta = pos.OutArea(viewportRect);
        if ((delta.y < 0f/* && scrollRect.verticalNormalizedPosition >= 0f*/) || (delta.y > 0f && scrollRect.verticalNormalizedPosition <= 1f))
        {
            if (delta.y > 0f && scrollRect.verticalNormalizedPosition < 0f)
            {
                scrollRect.verticalNormalizedPosition = 1e-5f;
            }
            scrollRect.velocity = -delta * autoScrollSpeed;
        }
    }

    public void UpdateIndex()
    {
        foreach (var element in Content.GetComponentsInChildren<ReorderableListElement>())
            element.Index = element.SiblingIndex;
    }

    /// <summary>
    /// 生成间隙占位物体
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    private GameObject CreateDummy(GameObject parent, GameObject element)
    {
        var obj = new GameObject("DummyElement", new Type[] { typeof(RectTransform), typeof(ReorderableListElement) });
        obj.transform.SetParent(parent.transform);
        obj.transform.SetSiblingIndex(element.transform.GetSiblingIndex());
        element.transform.SetParent(transform);
        var rect = obj.GetComponent<RectTransform>();
        var sourceRect = element.GetComponent<RectTransform>();
        rect.sizeDelta = sourceRect.sizeDelta;
        //rect.sizeDelta = new Vector2(sourceRect.sizeDelta.x, 100f);
        rect.pivot = sourceRect.pivot;
        rect.localRotation = sourceRect.localRotation;
        rect.localScale = sourceRect.localScale;
        //占位高度
        var layoutElement = obj.AutoComponent<LayoutElement>();
        if (element.TryGetComponent(out LayoutElement le))
            layoutElement.minHeight = le.minHeight;
        else
            layoutElement.minHeight = rect.sizeDelta.y;

        var index = element.GetComponentInChildren<ReorderableListElement>().Index;
        obj.GetComponent<ReorderableListElement>().Index = index;

        if (onBeginOrder != null)
            onBeginOrder.Invoke(index);
        return obj;
    }


    /// <summary>
    /// 销毁间隙占位物体
    /// </summary>
    /// <param name="element"></param>
    private void DestroyDummy(GameObject element)
    {
        if (element != null)
        {
            element.transform.SetParent(Content.transform);
            var index = dummy.transform.GetSiblingIndex();
            element.transform.SetSiblingIndex(index);
            element = null;

            DestroyImmediate(dummy.gameObject);

            if (onEndOrder != null)
                onEndOrder.Invoke(index);
        }
        else
        {
            Destroy(dummy.gameObject);
        }
    }

    #region 事件注册
    public void AddOnChangeModeListener(UnityAction<bool> onChangeModeAction)
    {
        if (onChangeModeAction != null)
        {
            if (onChangeMode == null) { onChangeMode = new OnChangeModeCallback(); }
            onChangeMode.AddListener(onChangeModeAction);
        }
    }

    public void RemoveOnChangeModeListener(UnityAction<bool> onChangeModeAction)
    {
        if (onChangeModeAction != null)
        {
            if (onChangeMode != null)
            {
                onChangeMode.RemoveListener(onChangeModeAction);
            }
            else
            {
                onChangeMode.RemoveAllListeners();
            }
        }
    }
    public void AddOnSelectListener(UnityAction<int> onSelectAction)
    {
        if (onSelectAction != null)
        {
            if (onSelect == null) { onSelect = new OnReorderCallback(); }
            onSelect.AddListener(onSelectAction);
        }
    }
    public void RemoveOnSelectListener(UnityAction<int> onSelectAction)
    {
        if (onSelectAction != null)
        {
            if (onSelect != null)
            {
                onSelect.RemoveListener(onSelectAction);
            }
            else
            {
                onSelect.RemoveAllListeners();
            }
        }
    }
    public void AddOnBeginOrderListener(UnityAction<int> onBeginOrderAction)
    {
        if (onBeginOrderAction != null)
        {
            if (onBeginOrder == null) { onBeginOrder = new OnReorderCallback(); }
            onBeginOrder.AddListener(onBeginOrderAction);
        }
    }

    public void RemoveOnBeginOrderListener(UnityAction<int> onBeginOrderAction)
    {
        if (onBeginOrderAction != null)
        {
            if (onBeginOrder != null)
            {
                onBeginOrder.RemoveListener(onBeginOrderAction);
            }
            else
            {
                onBeginOrder.RemoveAllListeners();
            }
        }
    }

    public void AddOnUpdateOrderListener(UnityAction<int> onUpdateOrderAction)
    {
        if (onUpdateOrderAction != null)
        {
            if (onUpdateOrder == null) { onUpdateOrder = new OnReorderCallback(); }
            onUpdateOrder.AddListener(onUpdateOrderAction);
        }
    }

    public void RemoveOnUpdateOrderListener(UnityAction<int> onUpdateOrderAction)
    {
        if (onUpdateOrderAction != null)
        {
            if (onUpdateOrder != null)
            {
                onUpdateOrder.RemoveListener(onUpdateOrderAction);
            }
            else
            {
                onUpdateOrder.RemoveAllListeners();
            }
        }
    }
    public void AddOnEndOrderListener(UnityAction<int> onEndOrderAction)
    {
        if (onEndOrderAction != null)
        {
            if (onEndOrder == null) { onEndOrder = new OnReorderCallback(); }
            onEndOrder.AddListener(onEndOrderAction);
        }
    }

    public void RemoveOnEndOrderListener(UnityAction<int> onEndOrderAction)
    {
        if (onEndOrderAction != null)
        {
            if (onEndOrder != null)
            {
                onEndOrder.RemoveListener(onEndOrderAction);
            }
            else
            {
                onEndOrder.RemoveAllListeners();
            }
        }
    }
    #endregion

    private void OnDestroy()
    {
        ///移除事件
        if (onChangeMode != null)
        {
            onChangeMode.RemoveAllListeners();
            onChangeMode = null;
        }
        if (onSelect != null)
        {
            onSelect.RemoveAllListeners();
            onSelect = null;
        }
        if (onBeginOrder != null)
        {
            onBeginOrder.RemoveAllListeners();
            onBeginOrder = null;
        }
        if (onUpdateOrder != null)
        {
            onUpdateOrder.RemoveAllListeners();
            onUpdateOrder = null;
        }
        if (onEndOrder != null)
        {
            onEndOrder.RemoveAllListeners();
            onEndOrder = null;
        }
    }
}