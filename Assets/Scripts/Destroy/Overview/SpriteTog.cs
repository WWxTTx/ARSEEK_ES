using UnityEngine;
using UnityFramework.Runtime;


[RequireComponent(typeof(SpriteRenderer))]
public class SpriteTog : MonoBase
{

    //public class ToggleEvent : UnityEvent<bool> { }
    //public ToggleEvent onValueChanged = new ToggleEvent();
    private bool isOn = false;
    public bool IsOn
    {
        get
        {
            return isOn;
        }
        set
        {
            if (isOn != value)
            {
                isOn = value;
                OnValueChanged(value);
            }
        }
    }
    [HideInInspector]
    public Sprite normalSprite;
    [HideInInspector]
    public Sprite hoverSprite;
    [HideInInspector]
    public Sprite pressedSprite;
    [HideInInspector]
    public Sprite selectSprite;

    ModelInfo modelInfo;
    SpriteRenderer spriteRenderer;
    BoxCollider bc;

    void Awake()
    {
        AddMsg((ushort)SpriteTogEvent.Close);
    }

    public void SetSprite(Sprite normal, Sprite hover, Sprite pressed, Sprite selected)
    {
        normalSprite = normal;
        hoverSprite = hover;
        pressedSprite = pressed;
        selectSprite = selected;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = normal;
        spriteRenderer.flipX = true;

        modelInfo = GetComponent<ModelInfo>();
        gameObject.AddComponent<BoxCollider>();

        OnValueChanged(isOn);
    }

    void OnValueChanged(bool state)
    {
        if (state)
        {
            spriteRenderer.sprite = selectSprite;
            var detailsPos = Camera.main.WorldToScreenPoint(transform.position);
            detailsPos = new Vector2(detailsPos.x / (Screen.height / 1080f), detailsPos.y / (Screen.height / 1080f));//解决分辨率不同时的UI显示位置不正确bug
            FormMsgManager.Instance.SendMsg(new MsgStringVector2((ushort)ResourcesPanelEvent.Details, modelInfo.ID, detailsPos));
        }
        else
            spriteRenderer.sprite = normalSprite;
    }

    private void OnMouseUpAsButton()
    {
        if (!GUITool.IsOverGUI(Input.mousePosition))
            IsOn = !IsOn;
    }

    private void OnMouseEnter()
    {
        if (!IsOn)
            spriteRenderer.sprite = hoverSprite;
    }

    private void OnMouseExit()
    {
        if (!IsOn)
            spriteRenderer.sprite = normalSprite;
    }

    private void OnMouseDown()
    {
        if (!IsOn)
            spriteRenderer.sprite = pressedSprite;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SpriteTogEvent.Close:
                IsOn = false;
                break;
            default:
                break;
        }
    }
}
