using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class ButtonEffectEditorWindow : EditorWindow
{
    [MenuItem("ARSeek工具/资源/替换Button\\Toggle\\Dropdown组件")]
    static void ReplaceComponent()
    {
        ButtonEffectEditorWindow window = (ButtonEffectEditorWindow)EditorWindow.GetWindow(typeof(ButtonEffectEditorWindow), false, "替换", true);
        window.Show();
    }

    private DefaultAsset targetFolder = null;
    private List<Object> result = new List<Object>();

    private Vector2 scrollPos;

    private void OnGUI()
    {
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Select Folder", targetFolder, typeof(DefaultAsset), false);

        if (targetFolder != null)
        {
            if(GUILayout.Button("查找", GUILayout.Width(100)))
            {
                EditorGUILayout.BeginHorizontal();
                GetAtPath(AssetDatabase.GetAssetPath(targetFolder), ref result);    
                EditorGUILayout.EndHorizontal();
            }     

            if(result.Count > 0)
            {
                EditorGUILayout.HelpBox($"查找到{result.Count}个文件", MessageType.Info);

                //显示结果
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(600));
                for (int i = 0; i < result.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(result[i], typeof(Texture2D), true, GUILayout.Width(300));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                if (GUILayout.Button("替换", GUILayout.Width(100)))
                {
                    foreach(Object o in result)
                        Replace(o);
                }
            }
        }
        else
        {
            result.Clear();
        }
    }

    /// <summary>
    /// 获取路径下的指定类型资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    static void GetAtPath(string path, ref List<Object> result)
    {
        result.Clear();
        ArrayList al = new ArrayList();

        string[] fileEntries = Directory.GetFiles(Application.dataPath.Replace("Assets", string.Empty) + path, "*.*", SearchOption.AllDirectories);
        foreach (string fileName in fileEntries)
        {
            string localPath = fileName.Replace(Application.dataPath, "Assets");
            
            Object t = AssetDatabase.LoadAssetAtPath(localPath, typeof(Object));

            if (t != null)
                al.Add(t);
        }

        for (int i = 0; i < al.Count; i++)
        {
            result.Add((Object)al[i]);
        }
    }

    private void Replace(Object prefabObject)
    {
        AssetDatabase.Refresh();
        //获取模型资源
        string assetPath = AssetDatabase.GetAssetPath(prefabObject);
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
        if (obj != null)
        {
            //实例化模型
            GameObject go = Instantiate(obj) as GameObject;

            StringBuilder sb = new StringBuilder();
            sb.Append($"{obj.name}替换完成：");

            ReplaceButtons2(go, ref sb);
            ReplaceToggles2(go, ref sb);
            //ReplaceDropdown(go, ref sb);

            //保存预制体
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            //销毁模型实例化对象
            DestroyImmediate(go);

            Debug.Log(sb.ToString());
        }
    }

    private void ReplaceButtons(GameObject go, ref StringBuilder sb)
    {
        ButtonWithSoundEffect buttonWithSoundEffect;
        GameObject componentGO;

        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;

        sb.Append("[Button]:");

        foreach (Button button in go.GetComponentsInChildren<Button>(true))
        {
            if (button is Button_LinkMode)
                continue;

            componentGO = button.gameObject;
            interactable = button.interactable;
            transition = button.transition;
            targetGraphic = button.targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    colorBlock = button.colors;
                    break;
                case Selectable.Transition.SpriteSwap:
                    spriteState = button.spriteState;
                    break;
                case Selectable.Transition.Animation:
                    animationTriggers = button.animationTriggers;
                    break;
            }
            navigation = button.navigation;
            componentGO = button.gameObject;

            DestroyImmediate(button, true);

            buttonWithSoundEffect = componentGO.AddComponent<ButtonWithSoundEffect>();
            buttonWithSoundEffect.interactable = interactable;
            buttonWithSoundEffect.transition = transition;
            buttonWithSoundEffect.targetGraphic = targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    buttonWithSoundEffect.colors = colorBlock;
                    break;
                case Selectable.Transition.SpriteSwap:
                    buttonWithSoundEffect.spriteState = spriteState;
                    break;
                case Selectable.Transition.Animation:
                    buttonWithSoundEffect.animationTriggers = animationTriggers;
                    break;
            }
            buttonWithSoundEffect.navigation = navigation;

            sb.Append($"{componentGO.name};");
        }
    }

    /// <summary>
    /// withsoundeffect 替换为 linkmode
    /// </summary>
    /// <param name="go"></param>
    /// <param name="sb"></param>
    private void ReplaceButtons2(GameObject go, ref StringBuilder sb)
    {
        Button_LinkMode button_LinkMode;
        GameObject componentGO;

        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;

        sb.Append("[Button]:");

        foreach (ButtonWithSoundEffect button in go.GetComponentsInChildren<ButtonWithSoundEffect>(true))
        {
            componentGO = button.gameObject;
            interactable = button.interactable;
            transition = button.transition;
            targetGraphic = button.targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    colorBlock = button.colors;
                    break;
                case Selectable.Transition.SpriteSwap:
                    spriteState = button.spriteState;
                    break;
                case Selectable.Transition.Animation:
                    animationTriggers = button.animationTriggers;
                    break;
            }
            navigation = button.navigation;
            componentGO = button.gameObject;

            DestroyImmediate(button, true);

            button_LinkMode = componentGO.AddComponent<Button_LinkMode>();
            button_LinkMode.interactable = interactable;
            button_LinkMode.transition = transition;
            button_LinkMode.targetGraphic = targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    button_LinkMode.colors = colorBlock;
                    break;
                case Selectable.Transition.SpriteSwap:
                    button_LinkMode.spriteState = spriteState;
                    break;
                case Selectable.Transition.Animation:
                    button_LinkMode.animationTriggers = animationTriggers;
                    break;
            }
            button_LinkMode.navigation = navigation;

            sb.Append($"{componentGO.name};");
        }
    }
    private void ReplaceToggles(GameObject go, ref StringBuilder sb)
    {
        ToggleWithSoundEffect toggleWithSoundEffect;
        GameObject componentGO;

        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        bool isOn;
        Toggle.ToggleTransition toggleTransition;
        Graphic graphic;
        ToggleGroup toggleGroup;

        sb.Append("[Toggle]:");

        foreach (Toggle toggle in go.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle is Toggle_LinkMode)
                continue;

            componentGO = toggle.gameObject;
            interactable = toggle.interactable;
            transition = toggle.transition;
            targetGraphic = toggle.targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    colorBlock = toggle.colors;
                    break;
                case Selectable.Transition.SpriteSwap:
                    spriteState = toggle.spriteState;
                    break;
                case Selectable.Transition.Animation:
                    animationTriggers = toggle.animationTriggers;
                    break;
            }
            navigation = toggle.navigation;
            isOn = toggle.isOn;
            toggleTransition = toggle.toggleTransition;
            graphic = toggle.graphic;
            toggleGroup = toggle.group;

            DestroyImmediate(toggle, true);

            toggleWithSoundEffect = componentGO.AddComponent<ToggleWithSoundEffect>();
            toggleWithSoundEffect.interactable = interactable;
            toggleWithSoundEffect.targetGraphic = targetGraphic;
            toggleWithSoundEffect.transition = transition;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    toggleWithSoundEffect.colors = colorBlock;
                    break;
                case Selectable.Transition.SpriteSwap:
                    toggleWithSoundEffect.spriteState = spriteState;
                    break;
                case Selectable.Transition.Animation:
                    toggleWithSoundEffect.animationTriggers = animationTriggers;
                    break;
            }
            toggleWithSoundEffect.navigation = navigation;
            toggleWithSoundEffect.isOn = isOn;
            toggleWithSoundEffect.toggleTransition = toggleTransition;
            toggleWithSoundEffect.graphic = graphic;
            toggleWithSoundEffect.group = toggleGroup;

            sb.Append($"{componentGO.name};");
        }
    }

    /// <summary>
    /// withsoundeffects 替换为 toggle_linkmode
    /// </summary>
    /// <param name="go"></param>
    /// <param name="sb"></param>
    private void ReplaceToggles2(GameObject go, ref StringBuilder sb)
    {
        Toggle_LinkMode toggle_LinkMode;
        GameObject componentGO;

        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        bool isOn;
        Toggle.ToggleTransition toggleTransition;
        Graphic graphic;
        ToggleGroup toggleGroup;

        sb.Append("[Toggle]:");

        foreach (ToggleWithSoundEffect toggle in go.GetComponentsInChildren<ToggleWithSoundEffect>(true))
        {
            componentGO = toggle.gameObject;
            interactable = toggle.interactable;
            transition = toggle.transition;
            targetGraphic = toggle.targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    colorBlock = toggle.colors;
                    break;
                case Selectable.Transition.SpriteSwap:
                    spriteState = toggle.spriteState;
                    break;
                case Selectable.Transition.Animation:
                    animationTriggers = toggle.animationTriggers;
                    break;
            }
            navigation = toggle.navigation;
            isOn = toggle.isOn;
            toggleTransition = toggle.toggleTransition;
            graphic = toggle.graphic;
            toggleGroup = toggle.group;

            DestroyImmediate(toggle, true);

            toggle_LinkMode = componentGO.AddComponent<Toggle_LinkMode>();
            toggle_LinkMode.interactable = interactable;
            toggle_LinkMode.targetGraphic = targetGraphic;
            toggle_LinkMode.transition = transition;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    toggle_LinkMode.colors = colorBlock;
                    break;
                case Selectable.Transition.SpriteSwap:
                    toggle_LinkMode.spriteState = spriteState;
                    break;
                case Selectable.Transition.Animation:
                    toggle_LinkMode.animationTriggers = animationTriggers;
                    break;
            }
            toggle_LinkMode.navigation = navigation;
            toggle_LinkMode.isOn = isOn;
            toggle_LinkMode.toggleTransition = toggleTransition;
            toggle_LinkMode.graphic = graphic;
            toggle_LinkMode.group = toggleGroup;

            sb.Append($"{componentGO.name};");
        }
    }

    private void ReplaceDropdown(GameObject go, ref StringBuilder sb)
    {
        Dropdown_LinkMode dropdownWithSoundEffect;
        GameObject componentGO;

        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        RectTransform template;
        Text captionText;
        Image captionImage;
        Text itemText;
        Image itemImage;
        int value;
        float alphaFadeSpeed;
        List<Dropdown.OptionData> options;

        sb.Append("[Dropdown]:");

        foreach (Dropdown dropdown in go.GetComponentsInChildren<Dropdown>(true))
        {
            componentGO = dropdown.gameObject;
            interactable = dropdown.interactable;
            transition = dropdown.transition;
            targetGraphic = dropdown.targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    colorBlock = dropdown.colors;
                    break;
                case Selectable.Transition.SpriteSwap:
                    spriteState = dropdown.spriteState;
                    break;
                case Selectable.Transition.Animation:
                    animationTriggers = dropdown.animationTriggers;
                    break;
            }
            navigation = dropdown.navigation;
            template = dropdown.template;
            captionText = dropdown.captionText;
            captionImage = dropdown.captionImage;
            itemText = dropdown.itemText;
            itemImage = dropdown.itemImage;
            value = dropdown.value;
            alphaFadeSpeed = dropdown.alphaFadeSpeed;
            options = dropdown.options;

            DestroyImmediate(dropdown, true);

            dropdownWithSoundEffect = componentGO.AddComponent<Dropdown_LinkMode>();
            dropdownWithSoundEffect.interactable = interactable;
            dropdownWithSoundEffect.transition = transition;
            dropdownWithSoundEffect.targetGraphic = targetGraphic;
            switch (transition)
            {
                case Selectable.Transition.ColorTint:
                    dropdownWithSoundEffect.colors = colorBlock;
                    break;
                case Selectable.Transition.SpriteSwap:
                    dropdownWithSoundEffect.spriteState = spriteState;
                    break;
                case Selectable.Transition.Animation:
                    dropdownWithSoundEffect.animationTriggers = animationTriggers;
                    break;
            }
            dropdownWithSoundEffect.navigation = navigation;
            dropdownWithSoundEffect.template = template;
            dropdownWithSoundEffect.captionText = captionText;
            dropdownWithSoundEffect.captionImage = captionImage;
            dropdownWithSoundEffect.itemText = itemText;
            dropdownWithSoundEffect.itemImage = itemImage;
            dropdownWithSoundEffect.value = value;
            dropdownWithSoundEffect.alphaFadeSpeed = alphaFadeSpeed;
            dropdownWithSoundEffect.options = options;

            sb.Append($"{componentGO.name};");
        }
    }
}