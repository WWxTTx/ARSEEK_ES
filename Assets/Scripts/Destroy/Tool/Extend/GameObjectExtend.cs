using UnityEngine;

public static class GameObjectExtend
{
    /// <summary>
    /// 设定同级值只显隐某物体
    /// </summary>
    /// <param name="Self">自身</param>
    /// <param name="isShow">显隐</param>
    public static void OnlyShowOrHide(this GameObject Self, bool isShow)
    {
        Transform parent = Self.transform.parent;
        if (parent != null)
            foreach (Transform child in parent)
                child.gameObject.SetActive(!isShow);
        Self.SetActive(isShow);
    }

    /// <summary>
    /// 计算模型包围盒
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="localSpace"></param>
    /// <returns></returns>
    public static Bounds CalculateBounds(this GameObject gameObject, bool localSpace = false)
    {
        Vector3 position = gameObject.transform.position;
        Quaternion rotation = gameObject.transform.rotation;
        Vector3 localScale = gameObject.transform.localScale;
        if (localSpace)
        {
            gameObject.transform.position = Vector3.zero;
            //gameObject.transform.rotation = Quaternion.identity;
            //gameObject.transform.localScale = Vector3.one;
        }

        Bounds bounds = new Bounds();
        Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
        if (componentsInChildren.Length != 0)
        {
            Bounds bounds2 = componentsInChildren[0].bounds;
            bounds.center = bounds2.center;
            bounds.extents = bounds2.extents;
            for (int index = 1; index < componentsInChildren.Length; ++index)
            {
                Bounds bounds3 = componentsInChildren[index].bounds;
                bounds.Encapsulate(bounds3);
            }
        }
        else
        {
            Collider[] collidersInChildren = gameObject.GetComponentsInChildren<Collider>();
            if (collidersInChildren.Length != 0)
            {
                Bounds bounds4 = collidersInChildren[0].bounds;
                bounds.center = bounds4.center;
                bounds.extents = bounds4.extents;
                for (int index = 1; index < collidersInChildren.Length; ++index)
                {
                    Bounds bounds5 = collidersInChildren[index].bounds;
                    bounds.Encapsulate(bounds5);
                }
            }
        }

        if (localSpace)
        {
            gameObject.transform.position = position;
            //gameObject.transform.rotation = rotation;
            //gameObject.transform.localScale = localScale;
        }

        return bounds;
    }
}
