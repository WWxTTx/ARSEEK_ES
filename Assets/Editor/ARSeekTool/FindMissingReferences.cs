using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FindMissingReferences : EditorWindow
{
    internal class ComponentProperty
    {
        public Component Component;
        public string PropertyName;

        public ComponentProperty(Component component, SerializedProperty serializedProperty)
        {
            Component = component;
            PropertyName = ObjectNames.NicifyVariableName(serializedProperty.name);
        }
    }

    private GameObject selectObject;
    private bool valid = false;
    private int missingScripts = 0;
    private Dictionary<GameObject, List<ComponentProperty>> missingReferences = new Dictionary<GameObject, List<ComponentProperty>>();
    private Vector2 scrollPosition;


    [MenuItem("ARSeek工具/资源/查找预制体丢失引用")]
    public static void ShowWindow()
    {
        FindMissingReferences window = (FindMissingReferences)GetWindow(typeof(FindMissingReferences), false, "查找预制体丢失引用", true);
        window.minSize = new Vector2(540, 480);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            selectObject = (GameObject)EditorGUILayout.ObjectField(selectObject, typeof(GameObject), true, GUILayout.Width(200));
            if (selectObject == null)
                return;

            if (GUILayout.Button("查找"))
            {
                if (!FindMissingReferences_Selected())
                {
                    this.ShowNotification(new GUIContent("未查找到丢失引用"));
                }
            }
            GUILayout.Space(10);

            if (valid)
            {
                GUILayout.Label("查找结果:");
                GUILayout.Space(10);

                GUILayout.Label($"丢失脚本：{missingScripts}");
                GUILayout.Space(8);

                GUILayout.Label($"丢失引用：{missingReferences.Count}");
                GUILayout.Space(4);

                if (missingReferences.Count > 0)
                {
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("对象", GUILayout.Width(150f));
                            GUILayout.Label("组件", GUILayout.Width(150f));
                            GUILayout.Label("属性", GUILayout.Width(150f));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(4);

                        foreach (var kvp in missingReferences)
                        {
                            foreach (var property in kvp.Value)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true, GUILayout.Width(150f));
                                    GUILayout.Label(property.Component.GetType().Name, GUILayout.Width(150f));
                                    GUILayout.Label(property.PropertyName, GUILayout.Width(150f));
                                }
                                GUILayout.EndHorizontal();
                                GUILayout.Space(4);
                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }
            }
            else
            {
                StageUtility.GoBackToPreviousStage();
            }
        }
        GUILayout.EndVertical();
    }

    private bool FindMissingReferences_Selected()
    {
        valid = false;
        missingScripts = 0;
        missingReferences.Clear();

        AssetDatabase.OpenAsset(selectObject.GetInstanceID());
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        valid = findMissingReferences(stage.prefabContentsRoot, ref missingReferences);

        return valid;
    }

    private bool findMissingReferences(GameObject go, ref Dictionary<GameObject, List<ComponentProperty>> missingReferences)
    {
        bool valid = false;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            var component = components[i];
            if (!component)
            {
                missingScripts++;
                continue;
            }

            var serializedObject = new SerializedObject(component);
            var serializedProperty = serializedObject.GetIterator();
            while (serializedProperty.NextVisible(true))
            {
                if (serializedProperty.propertyType == SerializedPropertyType.ObjectReference &&
                    serializedProperty.objectReferenceValue == null && serializedProperty.objectReferenceInstanceIDValue != 0)
                {
                    var missingRef = new ComponentProperty(component, serializedProperty);
                    if (!missingReferences.ContainsKey(go))
                    {
                        missingReferences.Add(go, new List<ComponentProperty>());
                    }
                    missingReferences[go].Add(missingRef);
                }
            }
        }
        valid = missingScripts > 0 || missingReferences.Count > 0;

        foreach (Transform child in go.transform)
        {
            valid |= findMissingReferences(child.gameObject, ref missingReferences);
        }
        return valid;
    }
}