using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUITool
{
    /// <summary>
    /// 是否点击的是UI
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool IsOverGUI(Vector2 pos)
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            PointerEventData ped = new PointerEventData(es);
            ped.position = pos;
            List<RaycastResult> rr = new List<RaycastResult>();
            es.RaycastAll(ped, rr);
            return rr.Count > 0;
        }
        return false;
    }

    public static bool IsOverGUI(Vector2 pos, string excludeLayer)
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            PointerEventData ped = new PointerEventData(es);
            ped.position = pos;
            List<RaycastResult> rr = new List<RaycastResult>();
            es.RaycastAll(ped, rr);
            return rr.Select(r => r).Where(r => r.gameObject.layer != LayerMask.NameToLayer(excludeLayer)).ToList().Count > 0;
        }
        return false;
    }

    public static bool IsOverGUI(Vector2 pos, out GameObject hitGo)
    {
        hitGo = null;
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            PointerEventData ped = new PointerEventData(es);
            ped.position = pos;
            List<RaycastResult> rr = new List<RaycastResult>();
            es.RaycastAll(ped, rr);

            if (rr.Count == 1)
                hitGo = rr[0].gameObject;
            return rr.Count > 0;
        }
        return false;
    }
}
