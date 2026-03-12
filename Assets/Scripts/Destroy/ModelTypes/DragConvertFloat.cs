using UnityEngine;

/// <summary>
/// 将拖拽转换成数值
/// </summary>
public class DragConvertFloat : MonoBehaviour
{
    /// <summary>
    /// 主相机
    /// </summary>
    private Camera mainCamera;
    /// <summary>
    /// 向量开始点
    /// </summary>
    private Transform startPoint;
    /// <summary>
    /// 向量结束点
    /// </summary>
    private Transform endPoint;
    /// <summary>
    /// 开始拖拽点
    /// </summary>
    private Vector3 startDragPoint;
    /// <summary>
    /// 目标向量
    /// </summary>
    private Vector3 targetVector;
    /// <summary>
    /// 点积缓存
    /// </summary>
    private float dotProduct;
    /// <summary>
    /// 目标模长缓存
    /// </summary>
    private float targetMagnitude;
    /// <summary>
    /// 当前slider值
    /// </summary>
    private float currentValue;
    /// <summary>
    /// 缓存slider值
    /// </summary>
    private float cacheValue;
    /// <summary>
    /// 是否在拖拽中
    /// </summary>
    private bool isDragging;
    /// <summary>
    /// 数值回调
    /// </summary>
    private UnityEngine.Events.UnityAction<float> callBack;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="callBack"></param>
    public void Init(Transform startPoint,Transform endPoint,UnityEngine.Events.UnityAction<float> callBack)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.callBack = callBack;

        mainCamera = Camera.main;
        enabled = true;
    }

    /// <summary>
    /// 刷新组件
    /// </summary>
    public void Refresh()
    {
        enabled = true;
        cacheValue = 0;
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseOver()
    {
        if(!enabled)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            startDragPoint = Input.mousePosition;
            targetVector = mainCamera.WorldToScreenPoint(endPoint.position) - mainCamera.WorldToScreenPoint(startPoint.position);
        }
    }

    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            cacheValue = currentValue;
        }

        if (isDragging)
        {
            dotProduct = Vector3.Dot(Input.mousePosition - startDragPoint, targetVector);
            targetMagnitude = targetVector.magnitude;

            currentValue = (dotProduct > 0 ? 1 : -1) * (dotProduct / (targetMagnitude * targetMagnitude) * targetVector).magnitude / targetMagnitude;
            currentValue = Mathf.Clamp01(currentValue + cacheValue);

            callBack?.Invoke(currentValue);
        }
    }
}
