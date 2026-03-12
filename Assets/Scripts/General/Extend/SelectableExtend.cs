using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public static class SelectableExtend
{
    /// <summary>
    /// 遮罩UI 在UI表面覆盖一层Button来拦截事件
    /// </summary>
    /// <param name="target"></param>
    /// <param name="callBack"></param>
    public static void MaskSelectable(this Selectable target, UnityAction callBack = null)
    {
        var mask = new GameObject("MaskImage");
        {
            mask.transform.parent = target.transform;
            mask.transform.localScale = Vector3.one;
            mask.transform.localEulerAngles = Vector3.zero;
            mask.AddComponent<LayoutElement>().ignoreLayout = true;
            mask.AddComponent<Image>().color = Color.clear;
            mask.AddComponent<Button>().onClick.AddListener(callBack);

            var rectTransform = mask.AutoComponent<RectTransform>();
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
        }
    }
}
