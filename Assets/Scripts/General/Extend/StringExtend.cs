using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StringExtend
{
    /// <summary>
    /// 移除特殊字符 仅留下中文英文数字
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string RemoveSpecialSymbols(this string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        var stringBuilder = new System.Text.StringBuilder();
        {
            System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(target);
            {
                int stringLength;
                string content;
                while (enumerator.MoveNext())
                {
                    stringLength = System.Text.Encoding.Default.GetBytes(enumerator.Current.ToString()).Length;

                    //排除表情
                    if (stringLength > 0 && stringLength < 4)
                    {
                        content = enumerator.Current.ToString();

                        if (stringLength == 1)
                        {
                            if ((content[0] >= '0' && content[0] <= '9') ||
                                (content[0] >= 'A' && content[0] <= 'Z') ||
                                (content[0] >= 'a' && content[0] <= 'z'))
                            {
                                stringBuilder.Append(content);
                            }
                        }
                        else if (new System.Text.RegularExpressions.Regex("^[\u4e00-\u9fa5]$").IsMatch(content))
                        {
                            stringBuilder.Append(content);
                        }
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 仅留下英文和数字
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string RemoveSpecialSymbols_RemoveChinese(this string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        var stringBuilder = new System.Text.StringBuilder();
        {
            System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(target);
            {
                int stringLength;
                string content;
                while (enumerator.MoveNext())
                {
                    stringLength = System.Text.Encoding.Default.GetBytes(enumerator.Current.ToString()).Length;

                    content = enumerator.Current.ToString();

                    if (stringLength == 1)
                    {
                        if ((content[0] >= '0' && content[0] <= '9') ||
                            (content[0] >= 'A' && content[0] <= 'Z') ||
                            (content[0] >= 'a' && content[0] <= 'z'))
                        {
                            stringBuilder.Append(content);
                        }
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 移除特殊字符 仅留下中文
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string RemoveSpecialSymbols_Chinese(this string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        var stringBuilder = new System.Text.StringBuilder();
        {
            System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(target);
            {
                int stringLength;
                string content;
                while (enumerator.MoveNext())
                {
                    stringLength = System.Text.Encoding.Default.GetBytes(enumerator.Current.ToString()).Length;

                    //排除表情 英文 数字
                    if (stringLength > 1 && stringLength < 4)
                    {
                        content = enumerator.Current.ToString();

                        if (new System.Text.RegularExpressions.Regex("^[\u4e00-\u9fa5]$").IsMatch(content))
                        {
                            stringBuilder.Append(content);
                        }
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 移除表情符号
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string RemoveEmojiSymbols(this string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        var stringBuilder = new System.Text.StringBuilder();
        {
            System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(target);
            {
                int stringLength;
                string content;
                while (enumerator.MoveNext())
                {
                    stringLength = System.Text.Encoding.Default.GetBytes(enumerator.Current.ToString()).Length;
                    if (stringLength > 0 && stringLength < 4)
                    {
                        content = enumerator.Current.ToString();
                        stringBuilder.Append(content);
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }

    public static string EllipsisText(this string target, int maxLength, string mask)
    {
        if (string.IsNullOrEmpty(target) || target.Length <= maxLength)
            return target;
        return $"{target.Substring(0, maxLength)}{mask}";
    }
}