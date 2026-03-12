using UnityEngine;

[ExecuteAlways]
public class Line : MonoBehaviour
{
    /// <summary>
    /// 起点
    /// </summary>
    public Transform start;
    /// <summary>
    /// 终点
    /// </summary>
    public Transform end;
    /// <summary>
    /// 材质
    /// </summary>
    public Material mat;
    /// <summary>
    /// 宽度
    /// </summary>
    public float with = 0.1f;
    /// <summary>
    /// 控制点所在位置 0（起点）- 1（终点
    /// </summary>
    [Range(0, 1)]
    public float contrlPoint = 0.5f;
    /// <summary>
    /// 控制点的高度
    /// </summary>
    [Range(0, 10)]
    public float contrlHeight = 1f;
    /// <summary>
    /// 控制点的方向
    /// </summary>
    [Range(0, 360)]
    public float contrlAngle = 0;
    /// <summary>
    /// 有多少个点 越多弧线越精确 但消耗越大
    /// </summary>
    [Range(2, 1000)]
    public int pointNum = 3;
    /// <summary>
    /// 刷新时间间隔
    /// </summary>
    public float time_Update = 0.05f;

    private LineRenderer lineRenderer;
    private float timer;
    /// <summary>
    /// 起点坐标记录 用于不动时停止刷新 减少消耗
    /// </summary>
    private Vector3 startPoint;
    /// <summary>
    /// 终点坐标记录 用于不动时停止刷新 减少消耗
    /// </summary>
    private Vector3 endPoint;
    private string oldScale;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        timer = time_Update;
        oldScale = transform.lossyScale.x.ToString("0.00");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= time_Update && start != null && end != null)
        {
            timer = 0;

            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                if (startPoint == start.position && endPoint == end.position)
                    return;
            }
            lineRenderer.material = mat;
            lineRenderer.startWidth = with;
            lineRenderer.endWidth = with;
            pointNum = pointNum <= 2 ? 2 : pointNum;

            if (!oldScale.Equals(transform.lossyScale.x.ToString("0.00")))
            {
                oldScale = transform.lossyScale.x.ToString("0.00");
                contrlHeight *= System.Convert.ToSingle(oldScale);
            }

            startPoint = start.position;
            endPoint = end.position;

            Vector3 contrlpoint = startPoint + (endPoint - startPoint) * contrlPoint + RotationMatrix(Vector3.up, contrlAngle).normalized * contrlHeight;
            Vector3[] LineArray = BezierSpline.EditerSpline_OneContrlPoint(pointNum, startPoint, contrlpoint, endPoint);

            lineRenderer.positionCount = pointNum;
            for (int i = 0; i < pointNum; i++)
                lineRenderer.SetPosition(i, LineArray[i]);
        }
    }

    /// <summary>
    /// 旋转向量，使其方向改变，大小不变
    /// </summary>
    /// <param name="v">需要旋转的向量</param>
    /// <param name="angle">旋转的角度</param>
    /// <returns>旋转后的向量</returns>
    private Vector3 RotationMatrix(Vector3 v, float angle)
    {
        var x = v.x;
        var y = v.y;
        var sin = System.Math.Sin(System.Math.PI * angle / 180);
        var cos = System.Math.Cos(System.Math.PI * angle / 180);
        var newX = x * cos + y * sin;
        var newY = x * -sin + y * cos;
        return new Vector3((float)newX, (float)newY);
    }
}
