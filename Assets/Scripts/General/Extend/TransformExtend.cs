using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

public static class TransformExtend
{
    public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }
    public static Tween DoGroupAlpha(this Transform tf, float endValue, float time)
    {
        CanvasGroup cg = tf.GetComponent<CanvasGroup>();
        if (cg == null) throw new System.Exception(string.Format("当前物体{0}未找到CanvasGroup组件", tf.name));
        return cg.DOFade(endValue, time);
    }

    /// <summary>
    /// 获取目标偏移屏幕中心的目标位置
    /// </summary>
    /// <param name="moveTf">目标对象</param>
    /// <param name="xOffset">偏移屏幕中心的水平偏移量</param>
    /// <param name="yOffset">偏移屏幕中心的垂直偏移量</param>
    /// <returns>目标位置</returns>
    public static Vector3 GetModelCenterVector(this Transform moveTf, float dis = 0f, float xOffset = 0f, float yOffset = 0f)
    {
        Log.Debug("Model name：" + moveTf.name);
        Camera mainCamera = Camera.main;
        Vector2 screenPoint = new Vector2(Screen.width * 0.5f + xOffset, Screen.height * 0.5f + yOffset);
        Vector3 tempVec = mainCamera.WorldToScreenPoint(moveTf.position);
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, tempVec.z));

        Vector3 cameraPos = mainCamera.transform.position;
        if (dis <= 0)
            dis = Vector3.Distance(cameraPos, moveTf.position);

        Vector3 direction;
        if (Vector3.Distance(worldPoint, cameraPos) <= 0.001f)
            direction = mainCamera.transform.forward;
        else
        {
            if (Vector3.Dot(mainCamera.transform.forward, (worldPoint - cameraPos).normalized) > 0)
                direction = (worldPoint - cameraPos).normalized;
            else
                direction = (cameraPos - worldPoint).normalized;
        }
        Vector3 targetPoint = cameraPos + direction * dis;
        return targetPoint;
    }

    /// <summary>
    /// 附加实例列表
    /// </summary>
    /// <typeparam name="T">实例所需的信息对象类型</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="infos">信息列表</param>
    /// <param name="childCallback">实例化每个子对象后回调</param>
    public static void AddItemsView<T>(this Transform Self, List<T> infos, UnityAction<Transform, T> childCallback)
    {
        if (infos == null || infos.Count < 0) return;

        Transform defaultItem = Self.GetChild(0);
        int count = infos.Count;
        for (int i = 0; i < count; i++)
        {
            T t = infos[i];
            Transform tmp = Object.Instantiate(defaultItem, Self);
            childCallback?.Invoke(tmp, t);
            tmp.gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// 刷新实例列表
    /// </summary>
    /// <typeparam name="T">实例所需的信息对象类型</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="infos">信息列表</param>
    /// <param name="childCallback">实例化每个子对象后回调</param>
    public static void RefreshItemsView<T>(this Transform Self, List<T> infos, UnityAction<Transform, T> childCallback, bool isShow = true)
    {
        if (infos == null || infos.Count < 0) return;

        int childCount = Self.childCount;
        List<Transform> oldChildren = new List<Transform>();
        for (int i = 1; i < childCount; i++)
        {
            Transform tmp = Self.GetChild(i);
            tmp.gameObject.SetActive(false);
            oldChildren.Add(tmp);
        }

        Transform defaultItem = Self.GetChild(0);
        int count = infos.Count;
        for (int i = 0; i < count; i++)
        {
            T t = infos[i];

            Transform tmp = null;
            if (i >= oldChildren.Count)
                tmp = Object.Instantiate(defaultItem, Self);
            else
                tmp = oldChildren[i];

            childCallback?.Invoke(tmp, t);
            tmp.gameObject.SetActive(isShow);
        }
    }

    /// <summary>
    /// 刷新实例列表 用于同一个列表下多种实例的情况
    /// </summary>
    /// <typeparam name="T">实例所需的信息对象类型</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="item">原实例</param>
    /// <param name="infos">信息列表</param>
    /// <param name="childCallback">实例化每个子对象后回调</param>
    public static void RefreshItemsView<T>(this Transform Self, GameObject item, List<T> infos, UnityAction<Transform, T> childCallback, int startIndex = 0)
    {
        if (infos == null || infos.Count < 0) return;

        if (Self.childCount > 0)
        {
            ItemViewAnim(Self, () =>
            {
                while (Self.childCount > startIndex)
                {
                    Object.DestroyImmediate(Self.GetChild(startIndex).gameObject);
                }

                InstanItem();
            });
        }
        else
        {
            InstanItem();
        }

        void InstanItem()
        {
            int count = infos.Count;
            for (int i = 0; i < count; i++)
            {
                T t = infos[i];

                Transform tmp = Object.Instantiate(item, Self).transform;
                tmp.gameObject.SetActive(true);
                childCallback?.Invoke(tmp, t);
            }
        }
    }


    private static void ItemViewAnim(Transform item, UnityAction callback)
    {
        //var x = item.localPosition.x + 10f;
        //item.DOLocalMoveX(1f, 0.5f).OnComplete(() => callback());
        callback?.Invoke();
    }


    /// <summary>
    /// 刷新实例列表 用于同一个列表下多种实例的情况
    /// </summary>
    /// <typeparam name="T">实例所需的信息对象类型</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="item">原实例</param>
    /// <param name="infos">信息列表</param>
    /// <param name="childCallback">实例化每个子对象后回调</param>
    public static void RefreshMultipleItemsView<T>(this Transform Self, List<GameObject> items, List<T> infos, UnityAction<Transform, T> childCallback, int startIndex = 0)
    {
        if (Self.childCount > 0)
        {
            ItemViewAnim(Self, () =>
            {
                while (Self.childCount > startIndex)
                {
                    Object.DestroyImmediate(Self.GetChild(startIndex).gameObject);
                }

                InstanItem();
            });
        }
        else
        {
            InstanItem();
        }

        void InstanItem()
        {
            if (infos == null || infos.Count < 0) return;

            int count = infos.Count;
            int index;
            for (int i = 0; i < count; i++)
            {
                T t = infos[i];
                index = t.GetHashCode();
                if (index == -1)
                    continue;

                Transform tmp = Object.Instantiate(items[index], Self).transform;
                tmp.gameObject.SetActive(true);
                childCallback?.Invoke(tmp, t);
            }
        }
    }


    /// <summary>
    /// 放弃重复利用资源 减少出错概率
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Self"></param>
    /// <param name="infos"></param>
    /// <param name="childCallback"></param>
    public static void UpdateItemsView<T>(this Transform Self, List<T> infos, UnityAction<Transform, T> childCallback)
    {
        if (infos == null || infos.Count < 0) return;

        for (int i = Self.childCount - 1; i > 0; i--)
        {
            if (Self.GetChild(i).gameObject.activeSelf)
            {
                Object.DestroyImmediate(Self.GetChild(i).gameObject);
            }
        }
        GameObject prefab = Self.GetChild(0).gameObject;
        int count = infos.Count;
        for (int i = 0; i < count; i++)
        {
            T t = infos[i];

            Transform tmp = Object.Instantiate(prefab, Self).transform;
            childCallback?.Invoke(tmp, t);
            tmp.gameObject.SetActive(true);
        }
    }

    public delegate string GetInstanceName<T>(T t);
    /// <summary>
    /// 刷新实例列表
    /// </summary>
    /// <typeparam name="T">>实例所需的信息组 用于一一对应</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="infos">信息列表</param>
    /// <param name="getNameCallback">获取已有物体名称</param>
    /// <param name="newChildCallback">用信息对新添加的物体进行处理</param>
    /// <param name="oldChildCallback">用信息对已有的物体进行处理</param>
    public static void UpdateItemsView<T>(this Transform Self, List<T> infos, GetInstanceName<T> getNameCallback, UnityAction<Transform, T> newChildCallback, UnityAction<Transform, T> oldChildCallback = null)
    {
        int childCount = Self.childCount;
        int count = infos == null ? 0 : infos.Count;
        Dictionary<string, Transform> oldChildren = new Dictionary<string, Transform>();
        Transform defaultTf = Self.GetChild(0);
        if (childCount > 1)
        {
            for (int i = 1; i < childCount; i++)
            {
                Transform tmp = Self.GetChild(i);
                tmp.gameObject.SetActive(false);
                if (!oldChildren.ContainsKey(tmp.name))
                {
                    oldChildren.Add(tmp.name, tmp);
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            T t = infos[i];
            string tmpName = string.Empty;
            if (getNameCallback != null)
            {
                tmpName = getNameCallback(t);
            }

            Transform tmp = null;
            if (!oldChildren.ContainsKey(tmpName))
            {
                tmp = Object.Instantiate(defaultTf, Self);
                if (!string.IsNullOrEmpty(tmpName))
                {
                    tmp.name = tmpName;
                }
                else
                {
                    tmp.name += Self.childCount;
                }
                newChildCallback?.Invoke(tmp, t);
            }
            else
            {
                tmp = oldChildren[tmpName];
                oldChildCallback?.Invoke(tmp, t);
            }
            tmp.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 追加实例列表
    /// </summary>
    /// <typeparam name="T">实例所需的信息对象类型</typeparam>
    /// <param name="Self">实例组的父对象</param>
    /// <param name="infos">信息列表</param>
    /// <param name="childCallback">实例化每个子对象后回调</param>
    public static void AppendItemsView<T>(this Transform Self, List<T> infos, UnityAction<Transform, T> childCallback, bool isShow = true)
    {
        if (infos == null || infos.Count < 0) return;

        Transform defaultItem = Self.GetChild(0);
        int count = infos.Count;
        for (int i = 0; i < count; i++)
        {
            T t = infos[i];

            Transform tmp = Object.Instantiate(defaultItem, Self);
            childCallback?.Invoke(tmp, t);
            tmp.gameObject.SetActive(isShow);
        }
    }
}
