using UnityEngine;

public static class ColorExtend
{
    //颜色暂时没固定下来 之后用这种方式 不用多次创建color 节约资源
    //public static Color Hex_FFAABB
    //{
    //    get
    //    {
    //        if (_FFAABB.Equals(default))
    //            _FFAABB = "#FFAABB".HexToColor();

    //        return _FFAABB;
    //    }
    //}
    //private static Color _FFAABB;


    public static string ColorToHex(this Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(c);
    }

    public static string ColorToHexRGB(this Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static Color HexToColor(this string Self)
    {
        if (ColorUtility.TryParseHtmlString(Self, out Color color))
        {
            return color;
        }
        return Color.white;
    }

    /// <summary>
    /// 设置透明度 从根源下手 有color的都能设置
    /// </summary>
    /// <param name="component"></param>
    /// <param name="alpha"></param>
    public static void SetAlpha(this UnityEngine.UI.Graphic component, float alpha)
    {
        Color temp = component.color;
        component.color = new Color(temp.r, temp.g, temp.b, alpha);
    }
}
