using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class CollisionBoxMouseEvent : MonoBehaviour
{
    public class OnCollisionMouseDown : UnityEvent { }
    public OnCollisionMouseDown onMouseDown { get; set; } = new OnCollisionMouseDown();

    public class OnCollisionMouse : UnityEvent { }
    public OnCollisionMouse onMouse { get; set; } = new OnCollisionMouse();

    public class OnCollisionMouseUp : UnityEvent { }
    public OnCollisionMouseUp onMouseUp { get; set; } = new OnCollisionMouseUp();

    public class OnMouseNotCollisionUp : UnityEvent { }
    /// <summary>
    /// 鼠标抬起 鼠标指针不在碰撞盒子上执行
    /// </summary>
    public OnMouseNotCollisionUp onMouseNotCollisionUp { get; set; } = new OnMouseNotCollisionUp();

    public class OnClickAsButton : UnityEvent { }
    public OnClickAsButton onClick { get; set; } = new OnClickAsButton();

    public class OnClickAsButton1 : UnityEvent<GameObject> { }
    public OnClickAsButton1 onClick1 { get; set; } = new OnClickAsButton1();

    public class OnCollisionMouseDrag : UnityEvent { }
    public OnCollisionMouseDrag onMouseDrag { get; set; } = new OnCollisionMouseDrag();

    public class OnCollisionMouseEnter : UnityEvent { }
    public OnCollisionMouseEnter onMouseEnter { get; set; } = new OnCollisionMouseEnter();

    public class OnCollisionMouseExit : UnityEvent { }
    public OnCollisionMouseExit onMouseExit { get; set; } = new OnCollisionMouseExit();

    [HideInInspector]
    public float lengthClickTime = 0.2f;

    public class OnCollisionMouseLengthClick : UnityEvent { }
    public OnCollisionMouseLengthClick onMouseLengthClick { get; set; } = new OnCollisionMouseLengthClick();

    public class OnCollisionMouseShortClick : UnityEvent { }
    public OnCollisionMouseShortClick onMouseShortClick { get; set; } = new OnCollisionMouseShortClick();

    public class OnCollisionMouseDoubleClick : UnityEvent { }
    public OnCollisionMouseDoubleClick onMouseDoubleClick { get; set; } = new OnCollisionMouseDoubleClick();

    bool isenter = false;

    bool first = true;
    bool singleClick = false;

    [HideInInspector]
    public float doubleClickTimeDelay = 0.4f;

    private Vector3 mouseDownPosition;

    private void OnMouseEnter()
    {
        if (!isActiveAndEnabled)
            return;
        isenter = true;
        if (!GUITool.IsOverGUI(Input.mousePosition))
            onMouseEnter?.Invoke();
    }

    private void OnMouseExit()
    {
        if (!isActiveAndEnabled)
            return;
        isenter = false;
        if (!GUITool.IsOverGUI(Input.mousePosition))
            onMouseExit?.Invoke();
    }

    float _mouseDownTime;
    float _lastClickTime;

    private void OnMouseDown()
    {
        if (!isActiveAndEnabled)
            return;
        mouseDownPosition = Input.mousePosition;
        if (isenter && !GUITool.IsOverGUI(Input.mousePosition))
        {
            _mouseDownTime = Time.time;
            onMouseDown?.Invoke();
        }
    }

    private void OnMouseOver()
    {
        if (!isActiveAndEnabled)
            return;
        if (isenter && !GUITool.IsOverGUI(Input.mousePosition))
            onMouse?.Invoke();
    }

    private void OnMouseUp()
    {
        if (!isActiveAndEnabled)
            return;
        if (isenter && !GUITool.IsOverGUI(Input.mousePosition))
        {
            onMouseUp?.Invoke();

            float tempTime = lengthClickTime;
            if(Input.touchCount > 0)
            {
                tempTime = lengthClickTime * 5f;
            }

            if (Time.time - _mouseDownTime > tempTime)
                onMouseLengthClick?.Invoke();
            else
            {
                singleClick = !singleClick;

                if (!first) return;
                first = false;

                Invoke(nameof(ClickCount), /*lengthClickTime + */doubleClickTimeDelay);
            }
        }
        onMouseNotCollisionUp?.Invoke();
    }

    void ClickCount()
    {
        if (singleClick)
            onMouseShortClick?.Invoke();
        else
            onMouseDoubleClick?.Invoke();

        singleClick = false;
        first = true;
    }

    private void OnMouseUpAsButton()
    {
        if (!isActiveAndEnabled)
            return;
        //避免滑动时触发点击事件
        if (!GUITool.IsOverGUI(Input.mousePosition) && Vector3.Distance(Input.mousePosition, mouseDownPosition) < 10f)
        {
            onClick?.Invoke();
            onClick1?.Invoke(gameObject);
        }
    }

    private void OnMouseDrag()
    {
        if (!isActiveAndEnabled)
            return;
        if (!GUITool.IsOverGUI(Input.mousePosition))
        {
            onMouseDrag?.Invoke();
        }
    }

    public void SetState(bool state)
    {
        Collider[] Colliders = GetComponents<Collider>();
        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i].enabled = state;
        }
    }
}
