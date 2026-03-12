using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
/// <summary>
/// 自定义的编辑器窗口，脚本需放置在Editor文件夹下
/// </summary>
public class ScenePackWindow : EditorWindow
{
    public static string titleName = "MarsDT 场景打包工具V 1.1";
    string ToolsUseDescribe = "本工具用于对总览和百科进行打包,使用方法为:\r\n选中模型--->勾选需要打包的平台--->选择资源类型--->点击 开始打包 ";

    bool IsSelectGo = false;

    bool IsCreateAndroidAB = false;
    bool IsCreateIosAB = false;
    bool IsCreatePCAB = false;
    bool IsCreateUWPAB = false;
    bool isCheckPlatform = false;
    bool IsPack = false;

    string[] funcTypes = new string[] { "总览", "拆解组装", "分段演示A", "运行演示", "分段演示B", "模拟操作_练习", "封面", "模拟操作_考核" };
    /// <summary>
    /// "0-总览", "1-拆解组装", "2-运行演示", "3-分段演示", "4-分段演示B", "5-练习", "6-封面","7-考核"
    /// </summary>
    int abIndex;

    private static ScenePackWindow spw;
    #region 样式
    private static bool isInit = false;
    private static GUIStyle style_label;
    private static GUIStyle style_margin;
    private static GUIStyle style_18font;
    private static GUIStyle style_16font;
    #endregion
    /// <summary>
    /// ab包保存文件夹
    /// </summary>
    public static string assetsBundlePath;
    /// <summary>
    /// ab包名称，默认为选择中的第一个GameObject的名字
    /// </summary>
    public string abName = "";

    [MenuItem("Tools/打包工具")]
    public static void ShowWindow()
    {
        isInit = false;

        assetsBundlePath = Application.dataPath;

        spw = (ScenePackWindow)GetWindow(typeof(ScenePackWindow), true, titleName);
        spw.Show();
        spw.Focus();
    }

    private void InitStyle()
    {
        //按需设置样式
        if (isInit)
            return;

        isInit = true;
        style_label = new GUIStyle(GUI.skin.label);
        style_label.margin.top = 5;
        style_label.margin.left = 5;
        style_label.fontSize = 16;

        style_margin = new GUIStyle(GUI.skin.label);
        style_margin.margin.left = 50;

        style_18font = new GUIStyle(GUI.skin.label);
        style_18font.fontSize = 18;

        style_16font = new GUIStyle(GUI.skin.label);
        style_16font.fontSize = 16;
    }

    void OnGUI()
    {
        InitStyle();
        EditorGUILayout.Space();

        // 文本
        GUILayout.Label(ToolsUseDescribe, style_label, GUILayout.Width(600));
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("支持的设备包括:", style_18font, GUILayout.Width(200f));
        GUILayout.BeginArea(new Rect(150, 100, 200, 120));
        IsCreateAndroidAB = EditorGUILayout.ToggleLeft("Android(安卓设备)", IsCreateAndroidAB, style_16font, GUILayout.Width(180), GUILayout.Height(20));
        IsCreateIosAB = EditorGUILayout.ToggleLeft("IOS(苹果设备)", IsCreateIosAB, style_16font, GUILayout.Width(180), GUILayout.Height(20));
        IsCreatePCAB = EditorGUILayout.ToggleLeft("PC(电脑设备)", IsCreatePCAB, style_16font, GUILayout.Width(180), GUILayout.Height(20));
        IsCreateUWPAB = EditorGUILayout.ToggleLeft("UWP(Hololens设备)", IsCreateUWPAB, style_16font, GUILayout.Width(180), GUILayout.Height(20));
        GUILayout.EndArea();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        GUILayout.Label("", GUILayout.Height(100));
        EditorGUILayout.Space();
        abIndex = EditorGUILayout.Popup("选择资源类型:", abIndex, funcTypes);
        GUILayout.Space(10);
        abName = EditorGUILayout.TextField("输入ab包名称： ", abName, GUILayout.Width(400), GUILayout.Height(30));
        EditorGUILayout.EndVertical();

        CheckPlatformAndGameObject();

        EditorGUILayout.BeginVertical();
        EditorGUI.BeginDisabledGroup(!IsPack);
        if (GUILayout.Button("开始打包", GUILayout.Width(150f), GUILayout.Height(40f)))
        {
            ClearConsole();
            Debug.Log("assetsBundlePath:" + assetsBundlePath);
            FormTool.OpenFolderDialog("请选择保存路径", assetsBundlePath, (path) =>
            {
                assetsBundlePath = path;
                string savePath = path + "/" + abName + "/";

                Debug.Log("当前打包的场景路径:" + savePath);

                //创建保存地址文件夹
                string saveDir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                AssetDatabase.Refresh();

                PackScenes(saveDir, abName);

                ExportABJsonParams(saveDir, abName);
            });
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        if (!isCheckPlatform)
            EditorGUILayout.HelpBox("至少选择一个平台", MessageType.Warning);

        if (!IsSelectGo)
            EditorGUILayout.HelpBox("请至少选择一个需要打包的模型", MessageType.Warning);  //显示帮助框，类型为 警告  

        EditorGUILayout.EndVertical();
    }

    void CheckPlatformAndGameObject()
    {
        IsPack = false;
        if (IsCreateAndroidAB || IsCreateIosAB || IsCreatePCAB || IsCreateUWPAB)
            isCheckPlatform = true;
        else
            isCheckPlatform = false;

        if (isCheckPlatform && IsSelectGo)
        {
            IsPack = true;
        }
    }

    /// <summary>
    /// 清空控制台Log信息
    /// </summary>
    public static void ClearConsole()
    {
        var assembly = Assembly.GetAssembly(typeof(Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

    public void PackScenes(string saveDir, string abName)
    {
        UnityEngine.Object[] goList = Selection.objects;
        string[] path = new string[goList.Length];
        for (int i = 0; i < goList.Length; i++)
        {
            path[i] = AssetDatabase.GetAssetPath(goList[i]);
        }

        DateTime startTime = DateTime.Now;
        if (IsCreatePCAB)
            PackAB(saveDir, abName, path, BuildTarget.StandaloneWindows);
        if (IsCreateAndroidAB)
            PackAB(saveDir, abName, path, BuildTarget.Android);
        if (IsCreateIosAB)
            PackAB(saveDir, abName, path, BuildTarget.iOS);
        if (IsCreateUWPAB)
            PackAB(saveDir, abName, path, BuildTarget.WSAPlayer);
        AssetDatabase.Refresh();
        //打开一个通知栏
        string showText = "场景：" + abName + " 打包完成,总共耗时:" + (DateTime.Now - startTime).Seconds + " 秒";
        ShowNotification(new GUIContent(showText));
        Debug.Log(showText);

        try
        {
            System.Diagnostics.Process.Start(saveDir);//打开文件夹
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "  " + saveDir);
        }
    }

    /// <summary>
    /// 打ab包
    /// </summary>
    /// <param name="abDir">ab包保存文件夹</param>
    /// <param name="abName">ab包名称</param>
    /// <param name="assetPaths">需要打包资源路径集合</param>
    /// <param name="abPlatform">打包平台</param>
    void PackAB(string abDir, string abName, string[] assetPaths, BuildTarget abPlatform)
    {
        DateTime beginTime = DateTime.Now;
        Debug.Log(abPlatform + "开始打包,Time:" + beginTime);

        switch (abPlatform)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                abName += ".abw";
                break;
            case BuildTarget.iOS:
                abName += ".abi";
                break;
            case BuildTarget.Android:
                abName += ".aba";
                break;
            case BuildTarget.WSAPlayer:
                abName += ".abu";
                break;
            default:
                break;
        }

        AssetBundleBuild[] abbi = new AssetBundleBuild[1];
        abbi[0].assetBundleName = abName;
        abbi[0].assetNames = assetPaths;
        BuildPipeline.BuildAssetBundles(abDir, abbi, BuildAssetBundleOptions.ChunkBasedCompression, abPlatform);

        Debug.Log(abPlatform + "打包完成,耗时: " + (DateTime.Now - beginTime).Seconds + " 秒");
    }

    private void ExportABJsonParams(string dirPath, string abName)
    {
        string filePath = Path.Combine(dirPath, "ABJson.json");

        switch (abIndex)
        {
            case 0:
                break;
            default:
                JObject jd = new JObject();
                jd.Add(new JProperty("resname", abName));
                jd.Add(new JProperty("type", abIndex));
                jd.Add(new JProperty("json", string.Empty));
                //jd["resname"] = abName;
                //jd["type"] = abIndex;
                //jd["json"] = string.Empty;
                File.WriteAllText(filePath, jd.ToString());
                break;
        }
    }

    void OnInspectorUpdate()//实时刷新面板
    {
        //这里开启窗口的重绘，不然窗口信息不会刷新
        Repaint();
    }
    void OnFocus()
    {
        UpdateCheckObject();
    }
    void OnSelectionChange()
    {
        UpdateCheckObject();
    }
    void UpdateCheckObject()
    {
        //当窗口出去开启状态，并且在Hierarchy视图中选择某游戏对象时调用
        if (Selection.objects.Length > 0)
        {
            abName = Selection.objects[0].name;
            IsSelectGo = true;
        }
        else
        {
            IsSelectGo = false;
        }
    }

  
}