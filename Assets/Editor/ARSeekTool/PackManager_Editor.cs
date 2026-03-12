using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;
using MenuItem = UnityEditor.MenuItem;

[InitializeOnLoad]
public class PackManager_Editor
{
    /// <summary>
    /// PC打包路径
    /// </summary>
    private static string PCBuildPath;
    /// <summary>
    /// 安卓打包路径
    /// </summary>
    private static string AndroidBuildPath;
    static PackManager_Editor()
    {
        //设置默认路径
        if (!PlayerPrefs.HasKey("ARSeekPCBuildPath"))
            PlayerPrefs.SetString("ARSeekPCBuildPath", @"D:\Pack\WindowsBulid\");
        if (!PlayerPrefs.HasKey("ARSeekAndroidBuildPath"))
            PlayerPrefs.SetString("ARSeekAndroidBuildPath", @"D:\Pack\AndroidBuild\");

        PCBuildPath = PlayerPrefs.GetString("ARSeekPCBuildPath");
        AndroidBuildPath = PlayerPrefs.GetString("ARSeekAndroidBuildPath");
    }

    [MenuItem("ARSeek工具/快捷打包/Windows打包", priority = 95)]
    public static void WindowsBuild()
    {
        string Path = PCBuildPath;
        Path += DateTime.Now.ToString("ARSeek yyyy_MM_dd HH_mm_ss");
        Directory.CreateDirectory(Path);
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, $"{Path}\\{PlayerSettings.productName}.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Process.Start(Path);
    }
    [MenuItem("ARSeek工具/快捷打包/Android打包", priority = 96)]
    public static void AndroidBuild()
    {
        PlayerSettings.keystorePass = "123456";
        PlayerSettings.keyaliasPass = "123456";

        string Path = AndroidBuildPath;
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, $"{Path}{ DateTime.Now.ToString("ARSeek yyyy_MM_dd HH_mm_ss")}.apk", BuildTarget.Android, BuildOptions.None);
        Process.Start(Path);
    }
    [MenuItem("ARSeek工具/快捷打包/Android打包并安装", priority = 97)]
    public static void AndroidPackInstall()
    {
        PlayerSettings.keystorePass = "123456";
        PlayerSettings.keyaliasPass = "123456";

        string Path = AndroidBuildPath;
        Path += DateTime.Now.ToString("ARSeek yyyy_MM_dd HH_mm_ss") + ".apk";
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, Path, BuildTarget.Android, BuildOptions.None);

        var Command = $"adb install \"{Path}\"";
        Command = "/c chcp 437&&" + Command.Trim().TrimEnd('&') + "&exit";

        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = Command;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data.Equals("Success"))
                Debug.Log("安装成功！");
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.LogError($"安装失败CMD错误:{e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close();

        EditorUtility.DisplayDialog("提示", "完成！", "确定");
    }
    [MenuItem("ARSeek工具/快捷打包/AB包_win", priority = 98)]
    public static void ABPackageBuild()
    {
        UnityEngine.Object[] selections = Selection.objects;
        if (selections.Length == 0)
            return;

        string abName = "Lit.abw";
        string strABOutPAthDir = @"D:\Pack\ResourcesBuild";

        string[] selectionPackagePaths = new string[selections.Length];
        for (int i = 0; i < selections.Length; i++)
            selectionPackagePaths[i] = AssetDatabase.GetAssetPath(selections[i]);

        AssetBundleBuild[] packageResources = new AssetBundleBuild[1];
        packageResources[0].assetBundleName = abName;
        packageResources[0].assetNames = new string[selectionPackagePaths.Length];

        for (int i = 0; i < selectionPackagePaths.Length; i++)
            packageResources[0].assetNames[i] = selectionPackagePaths[i];

        BuildPipeline.BuildAssetBundles(strABOutPAthDir, packageResources, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);

        using (Process.Start(strABOutPAthDir)) { }
    }
    [MenuItem("ARSeek工具/快捷打包/配置", priority = 99)]
    public static void SetSavePath()
    {
        switch (EditorUtility.DisplayDialogComplex("配置存储路径", "请选择要配置的平台...", "PC", "取消", "Android"))
        {
            case 0:
                FormTool.OpenFolderDialog("选择PC打包路径...", string.Empty, path =>
                {
                    if (string.IsNullOrEmpty(path))
                        EditorUtility.DisplayDialog("错误", "选择路径为空！", "确定");
                    else
                    {
                        if (!path.EndsWith("\\"))
                            path += "\\";
                        Debug.Log($"新路径已保存，路径为:{path}");
                        PlayerPrefs.SetString("ARSeekPCBuildPath", path);
                    }
                });
                break;
            case 2:
                FormTool.OpenFolderDialog("选择安卓打包路径...", string.Empty, path =>
                {
                    if (string.IsNullOrEmpty(path))
                        EditorUtility.DisplayDialog("错误", "选择路径为空！", "确定");
                    else
                    {
                        if (!path.EndsWith("\\"))
                            path += "\\";
                        Debug.Log($"新路径已保存，路径为:{path}");
                        PlayerPrefs.SetString("AndroidBuildPath", path);
                    }
                });
                break;
            case 1:
                Debug.Log($"取消存储路径更改");
                break;
        }
    }
}
