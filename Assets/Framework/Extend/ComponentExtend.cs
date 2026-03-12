using UnityEngine;

public static class ComponentExtend
{
    /// <summary>
    /// 获取某对象下的子物体，适用于多层父子级关系的查找
    /// </summary>
    /// <param name="objName">子物体名称</param>
    /// <returns></returns>
    public static Transform FindChildByName(this Component self, string objName)
    {
        if (self == null || string.IsNullOrEmpty(objName))
            return null;

        Transform[] children = self.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Equals(objName))
                return children[i];
        }
        
        return null;
    }
    /// <summary>
    /// 获取某对象下的子物体上的某个组件，适用于多层父子级关系的查找
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="objName">子物体名称</param>
    /// <returns></returns>
    public static T GetComponentByChildName<T>(this Component self, string objName) where T : Component
    {
        Transform tf = FindChildByName(self, objName);
        if (tf != null)
            return tf.GetComponent<T>();
        else
            return null;
    }
    /// <summary>
    /// 自动检查并添加组件
    /// </summary>
    /// <typeparam name="T">Component</typeparam>
    /// <param name="Self">添加组件的Component</param>
    /// <param name="CoverModel">是否删除已有并重新添加组件</param>
    /// <returns>组件</returns>
    public static T AutoComponent<T>(this Component Self, bool CoverModel = false) where T : Component
    {
        if(CoverModel)
        {
            if (Self.TryGetComponent(out T destroy))
                Object.Destroy(destroy);

            return Self.gameObject.AddComponent<T>();
        }
        else
        {
            if (Self.TryGetComponent(out T component))
                return component;
            else
                return Self.gameObject.AddComponent<T>();
        }
    }
    /// <summary>
    /// 自动检查并添加组件
    /// </summary>
    /// <typeparam name="T">GameObject</typeparam>
    /// <param name="Self">添加组件的GameObject</param>
    /// <param name="CoverModel">是否删除已有并重新添加组件</param>
    /// <returns>组件</returns>
    public static T AutoComponent<T>(this GameObject Self, bool CoverModel = false) where T : Component
    {
        if (CoverModel)
        {
            if (Self.TryGetComponent(out T destroy))
                Object.Destroy(destroy);

            return Self.AddComponent<T>();
        }
        else
        {
            if (Self.TryGetComponent(out T component))
                return component;
            else
                return Self.AddComponent<T>();
        }
    }
}
