#if UNITY_ANDROID
using UnityEngine;
using UnityEngine.Android;

public static class  PermissionManager
{
    /// <summary>
    /// 同意授权的通用回调
    /// </summary>
    public static System.Action<string> granted;
    /// <summary>
    /// 拒绝授权的通用回调
    /// </summary>
    public static System.Action<string> denied;
    /// <summary>
    /// 拒绝授权并不在询问的通用回调
    /// </summary>
    public static System.Action<string> deniedAndDontAskAgain;

    /// <summary>
    /// 权限实际名称
    /// </summary>
    private static string[] permissionRequest = new string[]
    {
        "android.permission.CAMERA",
        "android.permission.RECORD_AUDIO" ,
        "android.permission.READ_EXTERNAL_STORAGE",
        "android.permission.WRITE_EXTERNAL_STORAGE",
        "android.permission.ACCESS_COARSE_LOCATION",
        "android.permission.ACCESS_FINE_LOCATION"
    };
    private static string[] permissionName = new string[]
    {
        "相机",
        "麦克风" ,
        "SD卡写入",
        "SD卡读取",
        "定位",
        "精准定位"
    };

    public static void GetPermission(this MonoBehaviour component, Request permission, System.Action<Result> callBack = null, bool cover = false)
    {
        component.StartCoroutine(StartGetPermission(permission, callBack, cover));
    }
    /// <summary>
    /// 开始获取权限 写在协程中是因为回调时是子线程 无法操作部分UnityAPI 用携程拉回主线程
    /// </summary>
    /// <param name="permission">需获取的权限</param>
    /// <param name="callBack">回调</param>
    /// <param name="cover">是否覆盖默认回调</param>
    /// <returns></returns>
    private static System.Collections.IEnumerator StartGetPermission(Request permission, System.Action<Result> callBack, bool cover)
    {
        int choise = (int)permission;
        System.Collections.Generic.List<string> permissions = new System.Collections.Generic.List<string>();
        for (int i = 0; i < permissionRequest.Length; i++)
            if ((choise & 1 << i) > 0)
                if (!Permission.HasUserAuthorizedPermission(permissionRequest[i]))
                    permissions.Add(permissionRequest[i]);

        if (permissions.Count > 0)
        {
            int callBackID = -1;
            string content = string.Empty;

            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (callback) =>
            {
                callBackID = 0;
                content = callback;
            };
            callbacks.PermissionDenied += (callback) =>
            {
                callBackID = 1;
                content = callback;
            };
            callbacks.PermissionDeniedAndDontAskAgain += (callback) =>
            {
                callBackID = 2;
                content = callback;
            };

            if (permissions.Count > 1)
                Permission.RequestUserPermissions(permissions.ToArray(), callbacks);
            else
                Permission.RequestUserPermission(permissions[0], callbacks);

            WaitForSeconds waitTime = new WaitForSeconds(0.5f);
            while (callBackID < 0)
                yield return waitTime;

            if (!cover)
            {
                for (int i = 0; i < permissionRequest.Length; i++)
                    if (permissionRequest[i].Equals(content))
                    {
                        content = permissionName[i];
                        break;
                    }

                switch (callBackID)
                {
                    case 0:
                        granted?.Invoke(content);
                        break;
                    case 1:
                        denied?.Invoke(content);
                        break;
                    case 2:
                        deniedAndDontAskAgain?.Invoke(content);
                        break;
                }
            }

            callBack?.Invoke((Result)callBackID);
        }
        else
            callBack?.Invoke(Result.已授权);
    }

    /// <summary>
    /// 可获取的权限
    /// </summary>
    public enum Request
    {
        相机 = 1,
        麦克风 = 2,
        读取 = 4,
        写入 = 8,
        粗略定位 = 16,
        精确定位 = 32
    }
    /// <summary>
    /// 权限获取结果
    /// </summary>
    public enum Result
    {
        已授权,
        未授权,
        未授权且不再询问
    }
}
#endif
