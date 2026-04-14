using UnityEngine;
using UnityEngine.EventSystems;

public class UITrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    UIOperation uiOperation;

    private void Start()
    {
        uiOperation = GetComponentInParent<UIOperation>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (uiOperation && uiOperation.isSelect)
            uiOperation.OnTrigger(collision.transform.parent.gameObject);
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (uiOperation)
                uiOperation.isSelect = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            //Log.Debug("鼠标左键按下");
            if (uiOperation)
                uiOperation.isSelect = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //Log.Debug("鼠标抬起");
    }
}
