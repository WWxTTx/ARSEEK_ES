using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 可拖拽排序列表元素
/// </summary>
public class ReorderableListElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    #region Static
    /// <summary>
    /// 当前操作元素
    /// </summary>
    private static ReorderableListElement activeElement;
    public static ReorderableListElement ActiveElement
    {
        get { return activeElement; }
        set
        {
            if (activeElement != null && activeElement != value)
            {
                activeElement.Release();
            }
            activeElement = value;
        }
    }
    #endregion

    private ReorderableList orderableList;
    private ScrollRect scrollRect;
    /// <summary>
    /// 列表的RectTransform
    /// </summary>
    private RectTransform listRectTransform;
    /// <summary>
    /// 拖拽元素整体的RectTransform
    /// </summary>
    public RectTransform ElementRectTransform;
    private Button[] buttons;

    /// <summary>
    /// 索引
    /// </summary>  
    public int Index { get; set; }

    public int SiblingIndex
    {
        get
        {
            return ElementRectTransform.GetSiblingIndex() - orderableList.ReferencePointsCount;
        }
    }
    private bool isPointerDown;
    private int pointerId = int.MinValue;
    private float pointerDownTime;
    private bool isDragging;

    /// <summary>
    /// 是否可点击
    /// </summary>
    private bool interactable = true;
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            interactable = value;
            isPointerDown = isDragging = false;
            if (buttons != null)
            {
                foreach (var button in buttons)
                {
                    button.interactable = value;
                }
            }
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init(ReorderableList reorderableList, RectTransform draggableTrans)
    {
        if (!orderableList)
        {
            orderableList = reorderableList;
            scrollRect = GetComponentInParent<ScrollRect>();
            listRectTransform = orderableList.transform.GetComponent<RectTransform>();
            ElementRectTransform = draggableTrans;
            buttons = GetComponentsInChildren<Button>();
        }
    }

    private void Update()
    {
        if (!interactable)
            return;

        if (isPointerDown && !orderableList.Orderable)
        {
            if (pointerDownTime > orderableList.longPressThreshold)
            {
                orderableList.Orderable = true;
                isPointerDown = false;
            }
            pointerDownTime += Time.deltaTime;
        }
        if (isDragging && orderableList.Orderable)
        {
            orderableList.UpdateDraggingPosition(ElementRectTransform.transform.position);
        }
    }

    /// <summary>
    /// 释放
    /// </summary>
    private void Release()
    {
        if (isPointerDown)
        {
            OnPointerUp(lastEventData);
        }
        if (isDragging)
        {
            OnEndDrag(lastEventData);
        }
        activeElement = null;
    }

    #region 事件监听

    /// <summary>
    /// 最后一次事件数据
    /// </summary>
    private PointerEventData lastEventData;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
            return;

        if (!isPointerDown && !isDragging && (eventData.pointerId == 0 || eventData.pointerId == -1))
        {
            lastEventData = eventData;
            ActiveElement = this;
            pointerId = eventData.pointerId;
            isPointerDown = true;
            pointerDownTime = 0f;
            isDragging = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable || ActiveElement != this)
            return;

        isPointerDown = false;
        if (!isDragging && !eventData.hovered.Contains(eventData.pointerPress))
        {
            activeElement = null;
            lastEventData = eventData;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable || ActiveElement != this)
            return;

        if (!orderableList.Orderable && !isDragging)
        {
            orderableList.OnSelect(Index);
            activeElement = null;
            lastEventData = eventData;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable || ActiveElement != this)
            return;

        if (!orderableList.Orderable)
        {
            if (scrollRect != null)
            {
                scrollRect.OnBeginDrag(eventData);
                lastEventData = eventData;
            }
            isPointerDown = false;
        }
        else
        {
            ElementRectTransform.gameObject.AutoComponent<CanvasGroup>().alpha = 0.3f;
            orderableList.Dummy = ElementRectTransform.gameObject;
            lastEventData = eventData;
        }
        isDragging = true;
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || ActiveElement != this)
            return;

        if (!orderableList.Orderable)
        {
            if (scrollRect != null)
            {
                scrollRect.OnDrag(eventData);
                lastEventData = eventData;
            }
        }
        else
        {
            var pos = ElementRectTransform.position;
            eventData.position = new Vector2(eventData.position.x, Mathf.Clamp(eventData.position.y, orderableList.BottomPadding, Screen.height - orderableList.TopPadding));

            RectTransformUtility.ScreenPointToWorldPointInRectangle(listRectTransform, eventData.position, eventData.enterEventCamera, out var worldPos);
            if (worldPos.y == eventData.position.y)
                return;
            pos.y = worldPos.y;
            ElementRectTransform.position = pos;
            lastEventData = eventData;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactable || ActiveElement != this)
            return;

        if (!orderableList.Orderable)
        {
            if (scrollRect != null)
            {
                scrollRect.OnEndDrag(eventData);
                lastEventData = eventData;
            }
        }
        else
        {
            DestroyImmediate(ElementRectTransform.gameObject.GetComponent<CanvasGroup>());
            orderableList.Dummy = null;
            lastEventData = eventData;
        }
        isDragging = false;
        activeElement = null;
    }
    #endregion
}