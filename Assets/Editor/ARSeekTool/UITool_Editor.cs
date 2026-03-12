using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;
/// <summary>
/// 工具类 调整UI时批处理用
/// </summary>
public static class UITool_Editor
{
    [MenuItem("ARSeek工具/设置UI的Anchors包裹自身", priority = 1)]
    public static void SetAnchors()
    {
        Transform[] allSelects = Selection.transforms;
        if (allSelects.Length <= 0)
            return;

        RectTransform parent = default;
        Vector2 leftBottonPoint;
        Vector2 rightTopPoint;
        Vector2 paretSize = default;

        foreach (RectTransform rectTransform in allSelects)
        {
            if (rectTransform.parent)
            {
                parent = rectTransform.parent.GetComponent<RectTransform>();

                if (!parent)
                    continue;

                paretSize = parent.rect.size;
            }

            //获取当前anchors下左下角点的坐标
            //rectTransform.offsetMin 是左下角到父级rect锚点的坐标 也就是position的x和y
            //Vector2.Scale(rectTransform.pivot, rectTransform.rect.size) 计算自身锚点到自身左下角的坐标
            //Vector2.Scale(rectTransform.anchorMin, parent.rect.size) 计算自身在父级的锚点到父级左下角的坐标
            //加起来就是自身左下角点到父级左下角点的真实坐标
            leftBottonPoint = rectTransform.offsetMin + Vector2.Scale(rectTransform.anchorMin, paretSize);
            rightTopPoint = leftBottonPoint + rectTransform.rect.size;

            rectTransform.anchorMin = leftBottonPoint / paretSize;
            rectTransform.anchorMax = rightTopPoint / paretSize;
            //rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector3.zero;
            Debug.Log($"修改了名为:{rectTransform.name}GUID为:{rectTransform.GetInstanceID()}的UI");
        }
    }
    [MenuItem("ARSeek工具/Text自动锚点和大小", priority = 2)]
    public static void SetAutoSize()
    {
        GameObject[] allSelects = Selection.gameObjects;
        if (allSelects.Length <= 0)
            return;

        ContentSizeFitter temp = default;
        Text tempText = default;
        Vector2 newPivot = Vector2.zero;
        foreach (var select in allSelects)
        {
            tempText = select.GetComponent<Text>();
            if (!tempText)
                continue;

            //根据目前文字锚点设置自身锚点供Text延展
            newPivot.x = (int)tempText.alignment % 3 * 0.5f;
            newPivot.y = 1 - (Mathf.FloorToInt((int)tempText.alignment / 3) * 0.5f);

            RectTransform rectTransform = select.GetComponent<RectTransform>();
            {
                SetPivot(rectTransform, newPivot);

                temp = select.GetComponent<ContentSizeFitter>();
                if (!temp)
                {
                    temp = select.AddComponent<ContentSizeFitter>();
                    temp.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    temp.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(select.GetComponent<RectTransform>());
                    Object.DestroyImmediate(temp);
                }

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height);
            }

            //设置字体自动大小
            tempText.resizeTextForBestFit = true;
            tempText.resizeTextMinSize = 10;
            tempText.resizeTextMaxSize = tempText.fontSize;
        }
    }

    [MenuItem("ARSeek工具/LayoutElement自动比例(选中父物体)", priority = 3)]
    public static void SetFlexible()
    {
        GameObject[] allSelects = Selection.gameObjects;
        if (allSelects.Length != 1)
            return;

        float width = 0;

        foreach (var child in allSelects[0].GetComponentsInChildren<LayoutElement>())
            width += child.GetComponent<RectTransform>().rect.width;

        foreach (var child in allSelects[0].GetComponentsInChildren<LayoutElement>())
            child.flexibleWidth = child.GetComponent<RectTransform>().rect.width / width;
    }

    [MenuItem("ARSeek工具/快捷键组/UI自动大小 %T", priority = 4)]
    public static void UIAutoSize()
    {
        ContentSizeFitter temp = null;

        foreach (var target in Selection.gameObjects.Where(target => !target.GetComponent<ContentSizeFitter>()))
        {
            temp = target.AddComponent<ContentSizeFitter>();
            temp.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            temp.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(target.GetComponent<RectTransform>());
            Object.DestroyImmediate(temp);
        }
    }

    /// <summary>
    /// 代码动态设置pivot 导致ui位移 计算偏移使其不动
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="newPivot"></param>
    private static void SetPivot(RectTransform rectTransform, Vector2 newPivot)
    {
        Vector2 paretSize = default;

        if (rectTransform.parent)
        {
            var parent = rectTransform.parent.GetComponent<RectTransform>();

            if (!parent)
                return;

            paretSize = parent.rect.size;
        }

        //获取新旧pivot的差值
        var pivotIndex = newPivot - rectTransform.pivot;
        //变更pivot后的位移量 等于 anchor MaxMin的差值乘父级size乘新旧pivot差值最后减去自身的piovt变动导致的变化量
        var index = (rectTransform.anchorMax - rectTransform.anchorMin) * paretSize * (pivotIndex) - (pivotIndex * rectTransform.rect.size);

        rectTransform.offsetMin -= index;
        rectTransform.offsetMax -= index;

        rectTransform.pivot = newPivot;
    }
}
