using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FindMissingScripts : EditorWindow
{
    private static List<string> missingAssets = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("ARSeek工具/资源/查找预制体丢失组件")]
    public static void ShowWindow()
    {
        FindMissingScripts window = (FindMissingScripts)EditorWindow.GetWindow(typeof(FindMissingScripts), false, "查找预制体丢失组件", true);
        window.minSize = new Vector2(540, 480);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            if (GUILayout.Button("查找(Assets/)"))
            {
                if (!FindMissingScriptsInAssets())
                {
                    this.ShowNotification(new GUIContent("未查找到丢失组件的预制体"));
                }
            }
            GUILayout.Space(10);

            if (missingAssets.Count > 0)
            {
                if (GUILayout.Button("移除全部丢失脚本"))
                {
                    GameObject prefabObject;
                    foreach (string path in missingAssets)
                    {
                        prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabObject);
                    }
                }
                GUILayout.Space(20);
            }

            GUILayout.Label("查找结果:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                foreach (string path in missingAssets)
                {
                    GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.ObjectField(prefabObject, typeof(GameObject), true);
                        if (GUILayout.Button("移除丢失脚本", GUILayout.Width(120)))
                        {
                            //移除对象上丢失脚本
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabObject);
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                }
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndVertical();
    }

    private static bool FindMissingScriptsInAssets()
    {
        missingAssets.Clear();
        string[] allAssetsId = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        string assetPath;
        foreach (string assetGuid in allAssetsId)
        {
            assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var components = assetRoot.GetComponentsInChildren<Component>(true);
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                missingAssets.Add(assetPath);
            }
        }
        return missingAssets.Count > 0;
    }
}