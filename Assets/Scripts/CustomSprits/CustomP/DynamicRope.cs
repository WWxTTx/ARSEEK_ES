using UnityEngine;

/// <summary>
/// 采用三次贝塞尔（4控制点）的动态下垂绳索
/// A -- C1 ---- C2 -- B
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DynamicRope : MonoBehaviour
{
    public Transform pointA;      // 固定点A
    public Transform pointB;      // 移动点B

    [Header("下垂控制")]
    public float sagHeight = 0.3f;      // 下垂系数（随距离放大）

    [Header("端点延伸控制")]
    public float endTangentLength = 0.5f; // 端点切线长度（沿 localY）

    public int resolution = 30;          // 分段数（建议略高一点）

    private LineRenderer lineRenderer;
    private float oldDis;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = resolution;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        float distance = Vector3.Distance(pointA.position, pointB.position);

        if (Mathf.Approximately(oldDis, distance))
            return;

        oldDis = distance;

        // ====== 1) 端点控制点（沿各自局部Y轴） ======
        Vector3 c1 = pointA.position + pointA.up * endTangentLength;
        Vector3 c2 = pointB.position + pointB.up * endTangentLength;

        // ====== 2) 下垂修正（作用在中段） ======
        Vector3 mid = (pointA.position + pointB.position) * 0.5f;
        Vector3 sag = Vector3.down * (sagHeight * distance);

        // 把中段轻微“拉下去”
        c1 += sag * 0.5f;
        c2 += sag * 0.5f;

        // ====== 3) 生成三次贝塞尔曲线 ======
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 p = CalculateCubicBezier(
                t,
                pointA.position,
                c1,
                c2,
                pointB.position
            );

            lineRenderer.SetPosition(i, p);
        }
    }

    // 三次贝塞尔公式
    Vector3 CalculateCubicBezier(
        float t,
        Vector3 p0, Vector3 p1,
        Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        return
            uuu * p0 +
            3 * uu * t * p1 +
            3 * u * tt * p2 +
            ttt * p3;
    }
}
