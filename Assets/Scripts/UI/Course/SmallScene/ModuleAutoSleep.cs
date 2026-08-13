using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用空闲计时组件：激活后监听 watchArea 区域内的点击/触摸，
/// 超过 idleSeconds 无活动则触发 onIdle 一次并自动失活。
/// 用于 UISmallScene 系列模块的"展开后无操作自动收缩"逻辑。
/// </summary>
public class ModuleAutoSleep : MonoBehaviour
{
    public float idleSeconds = 5f;
    public RectTransform watchArea;
    public UnityEvent onIdle = new UnityEvent();

    private float lastActivityTime;
    private bool active;

    public void Activate()
    {
        active = true;
        lastActivityTime = Time.time;
    }

    public void Deactivate()
    {
        active = false;
    }

    private void Update()
    {
        if (!active) return;

        if (Input.GetMouseButtonUp(0) && watchArea != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(watchArea, Input.mousePosition))
            {
                lastActivityTime = Time.time;
            }
        }

        if (Time.time - lastActivityTime >= idleSeconds)
        {
            active = false;
            onIdle?.Invoke();
        }
    }
}

/// <summary>
/// 计算侧栏模块 Background 的"收缩后 anchoredPosition.x"，
/// 使 Close 按钮落在屏幕边缘且完全可见。
/// </summary>
public static class ModuleSlideUtility
{
    public enum SlideEdge { Left, Right, Bottom, Top }

    /// <summary>
    /// 计算 Background 收缩后的目标 anchoredPosition.x。
    /// toRight=true：Background 向右滑出（安卓），Close 落到屏幕右边缘。
    /// toRight=false：Background 向左滑出（PC），Close 落到屏幕左边缘 + edgeOffset（避开常驻 SideBar）。
    /// </summary>
    public static float GetCollapsedBackgroundX(RectTransform background, RectTransform closeArrow, bool toRight, float edgeOffset = 0f)
    {
        if (background == null)
            return 0f;

        if (closeArrow == null)
            return toRight ? background.sizeDelta.x : -background.sizeDelta.x;

        return GetCollapsedTargetAxis(background, closeArrow, toRight ? SlideEdge.Right : SlideEdge.Left, edgeOffset).x;
    }

    /// <summary>
    /// 计算 mover 收缩后的目标 anchoredPosition，使 closeArrow 落到指定屏幕边缘且完全可见。
    /// edgeOffset 定义"有效边界"的内缩量（如 PC 左侧避开常驻 SideBar 的 44 宽，则有效左边界 = 真实左边界 + 44）。
    /// 返回的是 anchoredPosition（Vector2），调用方按需取 .x 或 .y。
    /// </summary>
    public static Vector2 GetCollapsedTargetAxis(RectTransform mover, RectTransform closeArrow, SlideEdge edge, float edgeOffset = 0f)
    {
        if (mover == null || closeArrow == null)
            return mover != null ? mover.anchoredPosition : Vector2.zero;

        Canvas canvas = mover.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.rootCanvas == null)
            return mover.anchoredPosition;

        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        // canvasCorners: [0]=BL, [1]=TL, [2]=TR, [3]=BR
        float canvasLeftWorld = canvasCorners[0].x;
        float canvasRightWorld = canvasCorners[2].x;
        float canvasBottomWorld = canvasCorners[0].y;
        float canvasTopWorld = canvasCorners[2].y;

        Vector3 closeCenterWorld = closeArrow.position;
        Vector3 closeWorldSize = new Vector3(
            closeArrow.rect.width * Mathf.Abs(closeArrow.lossyScale.x),
            closeArrow.rect.height * Mathf.Abs(closeArrow.lossyScale.y),
            0f);

        Vector3 parentScale = mover.parent != null ? mover.parent.lossyScale : Vector3.one;
        if (parentScale.x == 0f) parentScale.x = 1f;
        if (parentScale.y == 0f) parentScale.y = 1f;

        // edgeOffset 定义有效边界的内缩量，先将其换算到 world 单位，然后调整 canvas 边界
        float offsetWorldX = edgeOffset * canvasRect.lossyScale.x;
        float offsetWorldY = edgeOffset * canvasRect.lossyScale.y;

        float effectiveLeftWorld = canvasLeftWorld + offsetWorldX;
        float effectiveRightWorld = canvasRightWorld - offsetWorldX;
        float effectiveBottomWorld = canvasBottomWorld + offsetWorldY;
        float effectiveTopWorld = canvasTopWorld - offsetWorldY;

        float targetWorldX = closeCenterWorld.x;
        float targetWorldY = closeCenterWorld.y;

        switch (edge)
        {
            case SlideEdge.Left:
                targetWorldX = effectiveLeftWorld + closeWorldSize.x / 2f;
                break;
            case SlideEdge.Right:
                targetWorldX = effectiveRightWorld - closeWorldSize.x / 2f;
                break;
            case SlideEdge.Bottom:
                targetWorldY = effectiveBottomWorld + closeWorldSize.y / 2f;
                break;
            case SlideEdge.Top:
                targetWorldY = effectiveTopWorld - closeWorldSize.y / 2f;
                break;
        }

        Vector3 deltaWorld = new Vector3(targetWorldX - closeCenterWorld.x, targetWorldY - closeCenterWorld.y, 0f);

        Vector2 localDelta = new Vector2(deltaWorld.x / parentScale.x, deltaWorld.y / parentScale.y);

        return new Vector2(mover.anchoredPosition.x + localDelta.x, mover.anchoredPosition.y + localDelta.y);
    }
}
