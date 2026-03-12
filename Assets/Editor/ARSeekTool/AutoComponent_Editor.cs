#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 自动设置组件 关闭射线检测等
/// </summary>
[InitializeOnLoad]
public class AutoComponent_Editor
{
    static AutoComponent_Editor()
    {
        ObjectFactory.componentWasAdded -= SetComponent;

        if (PlayerPrefs.GetInt("AutoComponent") > 0)
        {
            ObjectFactory.componentWasAdded += SetComponent;
            Menu.SetChecked("ARSeek工具/组件自动设置", true);
        }
        else
            Menu.SetChecked("ARSeek工具/组件自动设置", false);
    }
    [MenuItem("ARSeek工具/组件自动设置", priority = 4)]
    private static void SetEnable()
    {
        if (PlayerPrefs.GetInt("AutoComponent") > 0)
        {
            ObjectFactory.componentWasAdded -= SetComponent;
            PlayerPrefs.SetInt("AutoComponent", 0);
            Menu.SetChecked("ARSeek工具/组件自动设置", false);
        }
        else
        {
            ObjectFactory.componentWasAdded += SetComponent;
            PlayerPrefs.SetInt("AutoComponent", 1);
            Menu.SetChecked("ARSeek工具/组件自动设置", true);
        }
    }

    /// <summary>
    /// 设置组件参数
    /// </summary>
    /// <param name="component">添加的组件</param>
    private static void SetComponent(Component component)
    {
        SetImage(component);
        SetText(component);
        SetSelectable(component);
    }
    /// <summary>
    /// 关闭Image的默认射线检测和遮罩 需要检测的手动开
    /// </summary>
    private static void SetImage(Component component)
    {
        Image image = component as Image;
        if (image == null)
            return;

        image.raycastTarget = false;
        //image.maskable = false;
    }
    /// <summary>
    /// 关闭Text的默认射线检测和遮罩 需要检测的手动开
    /// </summary>
    private static void SetText(Component component)
    {
        Text text = component as Text;
        if (text == null)
            return;

        text.raycastTarget = false;
        text.maskable = false;
    }
    /// <summary>
    /// 设置所有可交互组件 使其高亮值为1:1:1:0.8
    /// </summary>
    private static void SetSelectable(Component component)
    {
        Selectable selectable = component as Selectable;
        if (selectable == null)
            return;

        var colors = selectable.colors;
        colors.highlightedColor = new Color(1, 1, 1, 0.8f);
        selectable.colors = colors;
    }
}
#endif
