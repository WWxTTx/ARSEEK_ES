using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityFramework.Runtime;

/// <summary>
/// UI拖拽组件
/// </summary>
public class DraggableComponent : MonoBehaviour
{
    private bool isDrag;
    private Vector2 dragOffset;

    [SerializeField] private Image trigger;
    private RectTransform rect;

    private Canvas canvas;

    /// <summary>
    /// 初始位置
    /// </summary>
    private Vector2 anchorPosition;

    private void Awake()
    {
        rect = transform.GetComponent<RectTransform>();
        anchorPosition = rect.anchoredPosition;
        canvas = UIManager.Instance.canvas;

        EventTrigger eventTrigger2;
        if (trigger)
            eventTrigger2 = trigger.gameObject.AddComponent<EventTrigger>();
        else
            eventTrigger2 = gameObject.AddComponent<EventTrigger>();

        eventTrigger2.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            isDrag = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
               canvas.transform as RectTransform,
               Input.mousePosition,
               canvas.worldCamera,
               out dragOffset);

            dragOffset = (Vector2)rect.anchoredPosition - dragOffset;
            //dragOffset = (Vector2)Input.mousePosition - (Vector2)rect.anchoredPosition;

        });
        eventTrigger2.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            isDrag = false;
        });
    }
    private void OnEnable()
    {
        rect.anchoredPosition = anchorPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDrag)
        {
            //rect.anchoredPosition = (Vector2)Input.mousePosition - dragOffset;
            Vector2 localPointerPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out localPointerPos))
            {
                rect.anchoredPosition = localPointerPos + dragOffset;
            }
        }
    }
}