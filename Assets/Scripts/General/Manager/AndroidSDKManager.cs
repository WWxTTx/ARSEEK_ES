using UnityFramework.Runtime;
using UnityEngine;

public class AndroidSDKManager : Singleton<AndroidSDKManager>
{
    public static AndroidJavaClass androidClass;
    public static AndroidJavaObject androidInstance;
    public delegate void MessageCallBack(string message);
    public MessageCallBack GetPhotoPath;
    public MessageCallBack ErrorEvent;

    private string Name;

    protected override void InstanceAwake()
    {
        base.InstanceAwake();
        if (Application.platform == RuntimePlatform.Android)
        {
            androidClass = new AndroidJavaClass("com.KHTF.ARIM.MainActivity");
            androidInstance = androidClass.GetStatic<AndroidJavaObject>("instance");
            Name = name;
            Debug.Log(Name + "初始化完毕！");
        }
    }

    public void AndroidOpenAlbum(MessageCallBack callBack)
    {
        Debug.Log("开启相册！");
        androidInstance.Call("SetCallBack", Name, "GetCallBack");
        androidInstance.Call("OpenAlbum");

        callBack += (path) =>
        {
            GetPhotoPath -= callBack;
        };

        ErrorEvent += (error) =>
        {
            GetPhotoPath -= callBack;
        };

        GetPhotoPath += callBack;
    }

    public void AndroidInstallAPK(string path)
    {
        Debug.Log("开始安装！");
        androidInstance.Call("SetCallBack", Name, "GetCallBack");
        androidInstance.Call("InstallAPK", path);
    }

    public float AndroidGetMemory()
    {
        int memory = -1;
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            float tempMemory = androidInstance.CallStatic<float>("GetMemory", currentActivity);
            memory = (int)tempMemory;
        }
        catch (System.Exception e)
        {
            Log.Error(e);
            return -1;
        }
        return memory;
    }

    public void GetCallBack(string callBack)
    {
        Debug.Log("Android端回传:" + callBack);
        GetPhotoPath?.Invoke(callBack);
    }

    /// <summary>
    /// 安卓返回的错误代码
    /// 0 权限不足
    /// </summary>
    /// <param name="error"></param>
    public void ErrorCallBack(string error)
    {
        if (error.Length > 2)
        {
            Log.Error(error);
            return;
        }
        switch (int.Parse(error))
        {
            case 0:
                UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("没有对应权限，请同意给予权限或者手动开启权限！"));
                ErrorEvent.Invoke(error);
                break;
            default:
                break;
        }
    }
}

