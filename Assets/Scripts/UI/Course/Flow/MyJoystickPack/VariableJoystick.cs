using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    // 点击旋转相关配置
    [Header("Tap Rotation Settings")]
    [SerializeField] private bool enableTapRotation = true;

    private Vector2 fixedPosition = Vector2.zero;
    private bool tapRotationTriggered;
    private Vector2 pressStartPosition;

    /// <summary>
    /// 点击旋转是否已触发
    /// </summary>
    public bool IsTapRotation => enableTapRotation && tapRotationTriggered;

    /// <summary>
    /// 点击时的屏幕坐标位置
    /// </summary>
    public Vector2 TapScreenPos { get; private set; }

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if(joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        fixedPosition = background.anchoredPosition;
        SetMode(joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if(joystickType != JoystickType.Fixed)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);

        // 记录点击位置
        if (enableTapRotation)
        {
            tapRotationTriggered = false;
            TapScreenPos = eventData.position;
            pressStartPosition = eventData.position;
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if(joystickType != JoystickType.Fixed)
            background.gameObject.SetActive(false);

        // 点击抬起时，如果未发生拖拽，则触发点击旋转
        if (enableTapRotation)
        {
            float dragDistance = Vector2.Distance(eventData.position, pressStartPosition);
            if (dragDistance <= Screen.dpi * 0.1f)
                tapRotationTriggered = true;
        }

        base.OnPointerUp(eventData);
    }

    // 拖拽超过阈值时不触发点击旋转
    public override void OnDrag(PointerEventData eventData)
    {
        if (enableTapRotation)
        {
            float dragDistance = Vector2.Distance(eventData.position, pressStartPosition);
            if (dragDistance > Screen.dpi * 0.1f)
                tapRotationTriggered = false;
        }
        base.OnDrag(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }