using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUITool
{
    // 缓存字段，避免每帧分配
    private static PointerEventData cachedPointerEventData;
    private static List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();

    /// <summary>
    /// 是否在UI上
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool IsOverGUI(Vector2 pos)
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            if (cachedPointerEventData == null)
                cachedPointerEventData = new PointerEventData(es);
            else
                cachedPointerEventData.Reset();

            cachedPointerEventData.position = pos;
            cachedRaycastResults.Clear();
            es.RaycastAll(cachedPointerEventData, cachedRaycastResults);
            return cachedRaycastResults.Count > 0;
        }
        return false;
    }

    public static bool IsOverGUI(Vector2 pos, string excludeLayer)
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            if (cachedPointerEventData == null)
                cachedPointerEventData = new PointerEventData(es);
            else
                cachedPointerEventData.Reset();

            cachedPointerEventData.position = pos;
            cachedRaycastResults.Clear();
            es.RaycastAll(cachedPointerEventData, cachedRaycastResults);

            int excludeLayerIndex = LayerMask.NameToLayer(excludeLayer);
            for (int i = 0; i < cachedRaycastResults.Count; i++)
            {
                if (cachedRaycastResults[i].gameObject.layer != excludeLayerIndex)
                    return true;
            }
            return false;
        }
        return false;
    }

    public static bool IsOverGUI(Vector2 pos, out GameObject hitGo)
    {
        hitGo = null;
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            if (cachedPointerEventData == null)
                cachedPointerEventData = new PointerEventData(es);
            else
                cachedPointerEventData.Reset();

            cachedPointerEventData.position = pos;
            cachedRaycastResults.Clear();
            es.RaycastAll(cachedPointerEventData, cachedRaycastResults);

            if (cachedRaycastResults.Count == 1)
                hitGo = cachedRaycastResults[0].gameObject;
            return cachedRaycastResults.Count > 0;
        }
        return false;
    }
}
