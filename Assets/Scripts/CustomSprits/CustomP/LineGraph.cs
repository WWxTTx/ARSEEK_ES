using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineGraph : Graphic
{
    [Header("Data")]
    public List<float> points = new List<float>();

    [Header("Data Range")]
    public float Xmin = 0f;
    public float Xmax = 1f;

    [Header("Style")]
    public float lineWidth = 2f;

    [Header("Progress")]
    [Range(0f, 1f)]
    public float progress = 1f;

    public void Clear()
    {
        points.Clear();
        progress = 0f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null || points.Count < 2 || progress <= 0f)
            return;

        Rect rect = GetPixelAdjustedRect();

        float xMin = rect.xMin + raycastPadding.x;
        float yMin = rect.yMin + raycastPadding.y;
        float xMax = rect.xMax - raycastPadding.z;
        float yMax = rect.yMax - raycastPadding.w;

        float width = xMax - xMin;
        float height = yMax - yMin;

        int n = points.Count;

        // ===== 进度映射 =====
        float maxPointF = Mathf.Lerp(0, n - 1, progress);
        int maxPointIndex = Mathf.FloorToInt(maxPointF);
        float lastPointT = maxPointF - maxPointIndex;

        // ==================================================
        // 关键点：基于「已绘制内容」计算 min / max
        // ==================================================
        bool hasEnoughData = maxPointIndex >= 0;

        float minY, maxY;

        if (!hasEnoughData)
        {
            // 没有点 → 水平线在中间
            minY = maxY = (Xmin + Xmax) * 0.5f;
        }
        else
        {
            minY = float.MaxValue;
            maxY = float.MinValue;

            // 已完成的整点
            for (int i = 0; i <= maxPointIndex; i++)
            {
                minY = Mathf.Min(minY, points[i]);
                maxY = Mathf.Max(maxY, points[i]);
            }

            // 最后一段插值（让参考线更“贴合”）
            if (maxPointIndex < n - 1 && lastPointT > 0f)
            {
                float yInterp = Mathf.Lerp(points[maxPointIndex], points[maxPointIndex + 1], lastPointT);
                minY = Mathf.Min(minY, yInterp);
                maxY = Mathf.Max(maxY, yInterp);
            }
        }

        float minLineY = yMin + Mathf.InverseLerp(Xmin, Xmax, minY) * height;
        float maxLineY = yMin + Mathf.InverseLerp(Xmin, Xmax, maxY) * height;

        // ===== 水平虚线（随进度 & 数据）=====
        float xProgressMax = Mathf.Lerp(xMin, xMax, progress);
        DrawHorizontalDashedLine(vh, xMin, xProgressMax, minLineY, color);
        DrawHorizontalDashedLine(vh, xMin, xProgressMax, maxLineY, color);

        // ===== 曲线 =====
        for (int i = 0; i < maxPointIndex; i++)
        {
            DrawCurveSegment(vh, i, xMin, yMin, width, height);
        }

        // ===== 最后一段（部分）=====
        if (maxPointIndex < n - 1 && lastPointT > 0f)
        {
            DrawCurveSegmentPartial(vh, maxPointIndex, lastPointT, xMin, yMin, width, height);
        }

        // ===== 点 =====
        for (int i = 0; i <= maxPointIndex; i++)
        {
            Vector2 p = DataToUIPoint(i, points[i], xMin, yMin, width, height);
            DrawPoint(vh, p, color);
        }
    }

    void DrawCurveSegment(VertexHelper vh, int i, float xMin, float yMin, float w, float h)
    {
        int n = points.Count;

        int i0 = Mathf.Clamp(i - 1, 0, n - 1);
        int i1 = i;
        int i2 = i + 1;
        int i3 = Mathf.Clamp(i + 2, 0, n - 1);

        Vector2 p0 = DataToUIPoint(i0, points[i0], xMin, yMin, w, h);
        Vector2 p1 = DataToUIPoint(i1, points[i1], xMin, yMin, w, h);
        Vector2 p2 = DataToUIPoint(i2, points[i2], xMin, yMin, w, h);
        Vector2 p3 = DataToUIPoint(i3, points[i3], xMin, yMin, w, h);

        DrawCatmullRomCurve(vh, p0, p1, p2, p3, color, lineWidth);
    }

    void DrawCurveSegmentPartial(VertexHelper vh, int i, float tMax, float xMin, float yMin, float w, float h)
    {
        int n = points.Count;

        int i0 = Mathf.Clamp(i - 1, 0, n - 1);
        int i1 = i;
        int i2 = i + 1;
        int i3 = Mathf.Clamp(i + 2, 0, n - 1);

        Vector2 p0 = DataToUIPoint(i0, points[i0], xMin, yMin, w, h);
        Vector2 p1 = DataToUIPoint(i1, points[i1], xMin, yMin, w, h);
        Vector2 p2 = DataToUIPoint(i2, points[i2], xMin, yMin, w, h);
        Vector2 p3 = DataToUIPoint(i3, points[i3], xMin, yMin, w, h);

        DrawCatmullRomCurvePartial(vh, p0, p1, p2, p3, tMax, color, lineWidth);
    }

    Vector2 DataToUIPoint(int index, float y, float xMin, float yMin, float w, float h)
    {
        float nx = (float)index / (points.Count - 1);
        float ny = Mathf.InverseLerp(Xmin, Xmax, y);
        return new Vector2(xMin + nx * w, yMin + ny * h);
    }

    void DrawHorizontalDashedLine(VertexHelper vh, float xMin, float xMax, float y, Color color)
    {
        float dash = lineWidth * 2f;
        float gap = lineWidth * 2f;
        float x = xMin;

        while (x < xMax)
        {
            float x2 = Mathf.Min(x + dash, xMax);
            DrawLine(vh, new Vector2(x, y), new Vector2(x2, y), color * 0.6f, lineWidth * 0.7f);
            x += dash + gap;
        }
    }

    void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, Color c, float w)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x) * (w * 0.5f);

        int i = vh.currentVertCount;
        vh.AddVert(a - n, c, Vector2.zero);
        vh.AddVert(a + n, c, Vector2.zero);
        vh.AddVert(b + n, c, Vector2.zero);
        vh.AddVert(b - n, c, Vector2.zero);

        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }

    void DrawCatmullRomCurve(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color c, float w)
    {
        const int seg = 12;
        Vector2 prev = p1;
        for (int i = 1; i <= seg; i++)
        {
            float t = i / (float)seg;
            Vector2 cur = Catmull(p0, p1, p2, p3, t);
            DrawLine(vh, prev, cur, c, w);
            prev = cur;
        }
    }

    void DrawCatmullRomCurvePartial(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float tMax, Color c, float w)
    {
        const int seg = 12;
        int count = Mathf.Max(1, Mathf.RoundToInt(seg * tMax));

        Vector2 prev = p1;
        for (int i = 1; i <= count; i++)
        {
            float t = i / (float)seg;
            Vector2 cur = Catmull(p0, p1, p2, p3, t);
            DrawLine(vh, prev, cur, c, w);
            prev = cur;
        }
    }

    Vector2 Catmull(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    const int circleSegments = 20;
    void DrawPoint(VertexHelper vh, Vector2 center, Color c)
    {
        Rect r = GetPixelAdjustedRect();
        float radius = lineWidth * 2f;

        int ci = vh.currentVertCount;
        vh.AddVert(center, c, Vector2.zero);

        for (int i = 0; i <= circleSegments; i++)
        {
            float a = Mathf.Deg2Rad * (i * 360f / circleSegments);
            vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, c, Vector2.zero);
        }

        for (int i = 1; i <= circleSegments; i++)
            vh.AddTriangle(ci, ci + i, ci + i + 1);
    }
}
