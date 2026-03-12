using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class TextExtend
{
    /// <summary>
    /// 超出text长度的字符串用mask代替
    /// </summary>
    /// <param name="target">目标Text</param>
    /// <param name="mask">掩码</param>
    public static void EllipsisText(this Text target, string mask)
    {
        Font font = target.font;
        string content = target.text;
        CharacterInfo charInfo;

        float maxWidth = target.GetComponent<RectTransform>().rect.width;
        float lineWidth = target.GetComponent<RectTransform>().rect.width;

        //tdptd 临时这么写 因为无法解决空格智能换行问题之后可能会改
        font.RequestCharactersInTexture("我", target.fontSize, target.fontStyle);
        font.GetCharacterInfo('我', out charInfo, target.fontSize, target.fontStyle);

        var line = Mathf.FloorToInt(target.GetComponent<RectTransform>().rect.height / (charInfo.glyphHeight + 2));
        //{
        //    if (line <= 0)
        //    {
        //        line = 1;
        //    }
        //    else if (line > 2)
        //    {
        //        //有空格将影响计算所以少一行
        //        line -= 1;
        //    }

        //    maxWidth *= line;
        //}

        for (int i = 0; i < mask.Length; i++)
        {
            font.RequestCharactersInTexture(mask, target.fontSize, target.fontStyle);
            font.GetCharacterInfo(mask[i], out charInfo, target.fontSize, target.fontStyle);
            maxWidth -= charInfo.advance;
        }

        if (line > 1)
        {
            float currentWidth = 0;
            int index = 1;
            for (int i = 0; i < content.Length; i++)
            {
                font.RequestCharactersInTexture(content, target.fontSize, target.fontStyle);
                font.GetCharacterInfo(content[i], out charInfo, target.fontSize, target.fontStyle);
                currentWidth += charInfo.advance;

                if (index < line)
                {
                    if (currentWidth > lineWidth)
                    {
                        currentWidth = charInfo.advance;
                        index++;
                    }
                }
                else if (currentWidth > maxWidth)
                {
                    target.text = $"{content.Substring(0, i - 1)}{mask}";
                    break;
                }
            }
        }
        else
        {
            float currentWidth = 0;
            for (int i = 0; i < content.Length; i++)
            {
                font.RequestCharactersInTexture(content, target.fontSize, target.fontStyle);
                font.GetCharacterInfo(content[i], out charInfo, target.fontSize, target.fontStyle);
                currentWidth += charInfo.advance;
                if (currentWidth > maxWidth)
                {
                    target.text = $"{content.Substring(0, i - 1)}{mask}";
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 超出文本框的部分用mask代替
    /// </summary>
    /// <param name="target">目标Text</param>
    /// <param name="content">原文本内容</param>
    /// <param name="mask">如果超出文本框 代替剩余文本的字符串</param>
    /// <param name="maxLine">限定行数</param>
    public static void EllipsisText(this Text target, string content, string mask, int maxLine = int.MaxValue - 1)
    {
        if (string.IsNullOrEmpty(content))
        {
            target.text = string.Empty;
            return;
        }

        var textGenerator = new TextGenerator(content.Length);
        {
            var setting = target.GetGenerationSettings(target.rectTransform.rect.size);
            if (textGenerator.PopulateWithErrors(content, setting, target.gameObject))
            {
                var endIndex = GetLineEndPosition(textGenerator, maxLine - 1);
                {
                    if (endIndex < content.Length)
                    {
                        do
                        {
                            if (endIndex <= 0)
                            {
                                content = "";
                                Debug.LogError(target.gameObject + "放不下任何一个字，检查UI大小");
                                break;
                            }

                            content = content.Substring(0, endIndex--);
                            textGenerator.PopulateWithErrors($"{content}{mask}", setting, target.gameObject);
                        }
                        while ((content.Length + mask.Length) != GetLineEndPosition(textGenerator, maxLine - 1));

                        content = $"{content}{mask}";
                    }
                }
            }
            else
            {
               Debug.LogError("错误", target.gameObject);
            }

            target.text = content;
        }
    }

    /// <summary>
    /// 超出文本框的部分用mask代替
    /// </summary>
    /// <param name="target">目标Text</param>
    /// <param name="content">原文本内容</param>
    /// <param name="suffixLength">尾缀长度</param>
    /// <param name="mask">如果超出文本框 代替剩余文本的字符串</param>
    /// <param name="maxLine">限定行数</param>
    public static void EllipsisText(this Text target, string content, int suffixLength, string mask, int maxLine = int.MaxValue - 1)
    {
        if (string.IsNullOrEmpty(content))
        {
            target.text = string.Empty;
            return;
        }
        if (content.Length <= suffixLength)
        {
            target.text = content;
            return;
        }

        string pendix = content.Substring(content.Length - suffixLength);

        var textGenerator = new TextGenerator(content.Length);
        {
            var setting = target.GetGenerationSettings(target.rectTransform.rect.size);

            if (textGenerator.PopulateWithErrors(content, setting, target.gameObject))
            {
                var endIndex = GetLineEndPosition(textGenerator, maxLine - 1);
                {
                    if (endIndex < content.Length)
                    {
                        do
                        {
                            content = content.Substring(0, endIndex--);
                            textGenerator.PopulateWithErrors($"{content}{mask}{pendix}", setting, target.gameObject);
                        }
                        while ((content.Length + mask.Length + pendix.Length) != GetLineEndPosition(textGenerator, maxLine - 1));

                        content = $"{content}{mask}{pendix}";
                    }
                }
            }
            else
            {
                Debug.LogError("错误", target.gameObject);
            }
            target.text = content;
        }
    }

    /// <summary>
    /// 获取Text能显示出的最后一个字符的index
    /// </summary>
    /// <param name="generator">目标text</param>
    /// <param name="line">可以指定行数</param>
    /// <returns></returns>
    private static int GetLineEndPosition(TextGenerator generator, int line = 0)
    {
        line =Mathf.Max(line, 0);

        //当指定行数小于最大行数时 返回行尾字符的index
        if (line + 1 < generator.lines.Count)
        {
            return generator.lines[line + 1].startCharIdx - 1;
        }

        return generator.characterCountVisible;
    }
}