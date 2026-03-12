using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;
/// <summary>
/// 可指定行数并自动缩放的Text
/// </summary>
public class Text_LockLine : Text
{
    /// <summary>
    /// 多少行
    /// </summary>
    public int Line;
    /// <summary>
    /// 临时计数用 防止多次创建
    /// </summary>
    private readonly UIVertex[] _tmpVerts = new UIVertex[4];
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        if (font == null)
            return;

        //好像是个外部监控 先开启再修改 改完了关闭
        m_DisableFontTextureRebuiltCallback = true;

        SetTextGeneration();

        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
        float unitsPerPixel = 1 / pixelsPerUnit;
        int vertCount = verts.Count;

        // We have no verts to process just return (case 1037923)
        if (vertCount <= 0)
        {
            toFill.Clear();
            return;
        }

        Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
        roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
        toFill.Clear();
        if (roundingOffset != Vector2.zero)
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                _tmpVerts[tempVertsIndex] = verts[i];
                _tmpVerts[tempVertsIndex].position *= unitsPerPixel;
                _tmpVerts[tempVertsIndex].position.x += roundingOffset.x;
                _tmpVerts[tempVertsIndex].position.y += roundingOffset.y;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(_tmpVerts);
            }
        }
        else
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                _tmpVerts[tempVertsIndex] = verts[i];
                _tmpVerts[tempVertsIndex].position *= unitsPerPixel;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(_tmpVerts);
            }
        }

        m_DisableFontTextureRebuiltCallback = false;
    }

    /// <summary>
    /// 设置字体的体积
    /// </summary>
    /// <param name="content"></param>
    private void SetTextGeneration(string content = "")
    {
        if (horizontalOverflow == HorizontalWrapMode.Overflow)
        {
            Log.Error("Text设置为HorizontalWrapMode.Overflow会导致无限运算！", gameObject);
            return;
        }

        if (string.IsNullOrEmpty(content))
            content = text;

        if (Line < 1)
            Line = 1;

        TextGenerationSettings settings = GetGenerationSettings(rectTransform.rect.size);
        settings.resizeTextForBestFit = false;

        //不缩放模式
        if (!resizeTextForBestFit)
        {
            cachedTextGenerator.PopulateWithErrors(content, settings, gameObject);
            return;
        }

        //记录测试text 最大行数(测算达到目标行数最优字号) 安全系数(防止无法预料的意外发生)
        string temp;
        int maxLine = 0;
        int safeIndex = 0;

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("A");

        while (true)
        {
            safeIndex++;
            if (safeIndex > 10)
            {
                Log.Error("超出10个轮回 回弹！请检查该物体！", gameObject);
                return;
            }

            temp = stringBuilder.ToString();

            for (int i = resizeTextMaxSize; i >= resizeTextMinSize; --i)
            {
                settings.fontSize = i;
                cachedTextGenerator.PopulateWithErrors(temp.ToString(), settings, gameObject);

                if (cachedTextGenerator.lineCount >= Line)
                {
                    cachedTextGenerator.PopulateWithErrors(content, settings, gameObject);
                    return;
                }
                else if (cachedTextGenerator.lineCount > maxLine)
                    maxLine = cachedTextGenerator.lineCount;

                if (cachedTextGenerator.characterCount == temp.Length)
                    break;
            }

            for (int i = temp.Length; i <= Mathf.CeilToInt(((float)Line / maxLine) * temp.Length); i++)
                stringBuilder.Append("A");

            maxLine = 0;
        }
    }
    /// <summary>
    /// 返回当前显示的最大字符数量
    /// </summary>
    /// <param name="content">想显示的内容</param>
    /// <returns>最大字符数量</returns>
    public int GetMaxFontNum(string content)
    {
        SetTextGeneration(content);
        return cachedTextGenerator.characterCount;
    }
}