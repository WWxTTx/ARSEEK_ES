using UnityEngine;

/// <summary>
/// 将拖拽转换成数值
/// </summary>
public class DragConvertAngle : MonoBehaviour
{
    /// <summary>
    /// 回调 物体实际角度和旋转轴向
    /// </summary>
    private UnityEngine.Events.UnityAction<float> onUpdate;
    /// <summary>
    /// 回调 物体实际角度和旋转轴向
    /// </summary>
    private UnityEngine.Events.UnityAction onMouseDown;
    /// <summary>
    /// 回调 物体实际角度和旋转轴向
    /// </summary>
    private UnityEngine.Events.UnityAction onMouseUp;

    /// <summary>
    /// 主相机
    /// </summary>
    private Camera mainCamera;
    /// <summary>
    /// 鼠标在该物体面向镜头的平面上的向量
    /// </summary>
    private Vector3 panelVector;
    /// <summary>
    /// 鼠标在该物体目标平面上的投影向量
    /// </summary>
    private Vector3 projectionVector;
    /// <summary>
    /// 法向量
    /// </summary>
    private Vector3 normalVector;
    /// <summary>
    /// 抓取点
    /// </summary>
    private Vector3 grabPoint;
    /// <summary>
    /// 鼠标的深度 参照该物体面向镜头的平面
    /// </summary>
    private float depth;
    /// <summary>
    /// 是否在拖拽中
    /// </summary>
    private bool isDragging;


    public void Init(Axis axis, UnityEngine.Events.UnityAction onMouseDown, UnityEngine.Events.UnityAction onMouseUp, UnityEngine.Events.UnityAction<float> onUpdate)
    {
        mainCamera = Camera.main;

        switch (axis)
        {
            case Axis.X:
                normalVector = transform.right;
                break;
            case Axis.Y:
                normalVector = transform.up;
                break;
            case Axis.Z:
                normalVector = transform.forward;
                break;
        }

        this.onUpdate = onUpdate;
        this.onMouseDown = onMouseDown;
        this.onMouseUp = onMouseUp;
    }

    private void OnMouseOver()
    {
        if (!enabled)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!isDragging)
            {
                isDragging = true;
                GetGrabPoint();
                onMouseDown?.Invoke();
            }
        }
    }
    public void GetGrabPoint()
    {
        var depth = Vector3.Dot(transform.position - mainCamera.transform.position, mainCamera.transform.forward);
        var panelVector = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth)) - transform.position;
        grabPoint = panelVector - Vector3.Dot(panelVector, normalVector) * normalVector;
    }
    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            onMouseUp?.Invoke();
        }

        if (isDragging)
        {
            depth = Vector3.Dot(transform.position - mainCamera.transform.position, mainCamera.transform.forward);
            panelVector = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth)) - transform.position;
            projectionVector = panelVector - Vector3.Dot(panelVector, normalVector) * normalVector;
            onUpdate?.Invoke(-Vector3.SignedAngle(projectionVector, grabPoint, normalVector));
        }
    }

    public enum Axis
    {
        X,
        Y,
        Z
    }
}
