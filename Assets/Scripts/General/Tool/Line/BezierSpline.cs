using UnityEngine;

public class BezierSpline : MonoBehaviour
{
    /// <summary>
    /// 获取一条贝塞尔曲线
    /// </summary>
    /// <param name="Num">曲线内点的数量</param>
    /// <param name="start">曲线起点</param>
    /// <param name="ctrl">曲线控制点</param>
    /// <param name="end">曲线终点</param>
    /// <returns>返回Vector3数组</returns>
    public static Vector3[] EditerSpline_OneContrlPoint(int Num, Vector3 start, Vector3 ctrl, Vector3 end)
    {     
        return Spline(SetPoint(Num), start, ctrl, end);
    }
    /// <summary>
    /// 获取一条贝塞尔曲线
    /// </summary>
    /// <param name="Num">曲线内点的数量</param>
    /// <param name="start">曲线起点</param>
    /// <param name="ctrl">曲线控制点</param>
    /// <param name="end">曲线终点</param>
    /// <returns>返回Vector3数组</returns>
    public static Vector3[] EditerSpline_OneContrlPoint(int Num, Vector3 start, Vector3 ctrl1, Vector3 ctrl2, Vector3 end)
    {
        return Spline(SetPoint(Num), start, ctrl1, ctrl2, end);
    }
    /// <summary>
    /// 根据点的数量计算点的位置组
    /// </summary>
    /// <param name="pointNum">点的数量</param>
    /// <returns>返回点的位置组float</returns>
    private static float[] SetPoint(int pointNum)
    {
        if (pointNum <= 2) return new float[] { 0, 1 };
        float interval = 1f / (pointNum - 2);
        float[] pointArray = new float[pointNum];
        pointArray[0] = 0;
        pointArray[pointNum - 1] = 1;
        for(int i = 1; i < pointNum-1 ; i++)
        {
            pointArray[i] = i * interval;
        }
        return pointArray;
    }
    /// <summary>
    /// 三次贝塞尔曲线
    /// </summary>
    /// <param name="point">百分比进度 0-1 float 例0f 出发点 1f 终点</param>
    /// <param name="start">出发点</param>
    /// <param name="ctrl">控制点</param>
    /// <param name="end">终点</param>
    /// <returns></returns>
    private static Vector3[] Spline(float[] point, Vector3 start, Vector3 ctrl, Vector3 end)
    {
        //B(t) = (1-t)2P0+ 2 (1-t) tP1 + t2P2,   0 <= t <= 1
        float u;
        float uu;
        float tt;
        Vector3[] pointArray = new Vector3[point.Length];
        pointArray[0] = start;
        pointArray[point.Length - 1] = end;
        for (int i = 1; i < point.Length-1; i++)
        {
            u = 1 - point[i];
            uu = u * u;
            tt = point[i] * point[i];
            pointArray[i] = uu * start;
            pointArray[i] += 2 * u * point[i] * ctrl;
            pointArray[i] += tt * end;
        }
        return pointArray;
    }
    /// <summary>
    /// 四次贝塞尔曲线
    /// </summary>
    /// <param name="point">百分比进度 0-1 float 例0f 出发点 1f 终点</param>
    /// <param name="start">出发点</param>
    /// <param name="ctrl1">控制点1</param>
    /// <param name="ctrl2">控制点2</param>
    /// <param name="end">终点</param>
    /// <returns></returns>
    public static Vector3[] Spline(float[] point, Vector3 start, Vector3 ctrl1, Vector3 ctrl2, Vector3 end)
    {
        //B(t) = (1 - t)^3 * P0 + 3 * (1 - t)^2 * t * P1 + 3 * (1 - t) * t^2 * P2 + t^3 * P3
        float u;
        float t;
        float uu;
        float uuu;
        float tt;
        Vector3[] pointArray = new Vector3[point.Length];
        pointArray[0] = start;
        pointArray[point.Length - 1] = end;
        for (int i = 1; i < point.Length - 1; i++)
        {
            u = 1 - point[i];
            t = point[i] * point[i];
            uu = u * u;
            uuu = uu * u;
            tt = t * point[i];

            Vector3 outPut = uuu * start;
            outPut += 3 * uu * point[i] * ctrl1;
            outPut += 3 * u * t * ctrl2;
            outPut += tt * end;
            pointArray[i] = outPut;
        }
        return pointArray;
    }
}
