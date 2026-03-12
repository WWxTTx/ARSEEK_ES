using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityFramework.Runtime;

public class ARModuleData : UIData
{
    public Toggle toggle;
    public ARModuleData(Toggle toggle)
    {
        this.toggle = toggle;
    }
}

public class ARModule : UIModuleBase
{
    /// <summary>
    /// 删除事件 动态添加 防止出现因为不支持而弹出后 额外调用删除事件的问题
    /// </summary>
    private Action destroyEvent;
    /// <summary>
    ///  模型父节点
    /// </summary>
    private Transform modelRoot;
    /// <summary>
    ///  模型
    /// </summary>
    //private Transform model;

    /// <summary>
    /// 记录原父节点位置
    /// </summary>
    private Vector3 startPoint;
    /// <summary>
    /// 记录追踪平面点击位置
    /// </summary>
    private Vector3 latestTouchPosition;
    /// <summary>
    /// 是否点击定位
    /// </summary>
    private bool hasTouchPos;

    /// <summary>
    /// 加载模型回调
    /// </summary>
    private UnityAction onModelLocate;

    private bool initialized = false;
    private ARSession ARSession;
    private ARPlaneManager planeManager;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private bool exitingCourse = false;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        Action GoBack = () => (uiData as ARModuleData).toggle.isOn = false;

        AddMsg(new ushort[]
        {
            (ushort)CoursePanelEvent.ModelLocate,
            (ushort)CoursePanelEvent.ChangeModel,
            (ushort)ARModuleEvent.ExitCourse
        });

#if UNITY_ANDROID
        this.GetPermission(PermissionManager.Request.相机, arg =>
        {
            switch (arg)
            {
                case PermissionManager.Result.已授权:
                    CheckAndInit(GoBack);
                    break;
                case PermissionManager.Result.未授权:
                case PermissionManager.Result.未授权且不再询问:
                    GoBack.Invoke();
                    break;
            }
        });
#else
        CheckAndInit(GoBack);
#endif 
    }
    private void CheckAndInit(Action GoBack)
    {
        if (ARManager.Instance.OpenARSession())
        {
            SendMsg(new MsgBase((ushort)ARModuleEvent.Open));
            InitARSession();
        }
        else
            GoBack?.Invoke();
    }

    /// <summary>
    /// 初始化变量及定位按钮事件
    /// </summary>
    private void InitARSession()
    {
        ARSession = ARManager.Instance.ARCamera.GetComponentInChildren<ARSession>();
        planeManager = ARManager.Instance.ARCamera.GetComponentInChildren<ARPlaneManager>();
        raycastManager = ARManager.Instance.ARCamera.GetComponentInChildren<ARRaycastManager>();

        modelRoot = ModelManager.Instance.modelRoot;

        startPoint = modelRoot.position;
        if (ModelManager.Instance.modelGo)
        {
            //model = ModelManager.Instance.modelGo.transform;
            OpenCtrl(true);

            modelRoot.position = new Vector3(0, 5000, 0);//不显示模型
            //model.localPosition = Vector3.zero;
            //model.localEulerAngles = Vector3.zero;
        }

        Button ModelResetBtn = this.GetComponentByChildName<Button>("ModelReset");
        ModelResetBtn.onClick.AddListener(() =>
        {
            //开启位置选择
            modelRoot.position = new Vector3(0, 5000, 0);//不显示模型
            OpenCtrl(true);
        });
        ModelResetBtn.gameObject.SetActive(true);

        //动态添加事件 防止 直接弹出后额外运行删除事件
        destroyEvent = () =>
        {
            ARManager.Instance.CloseARSession();

            //if (arLoaded)
            //    startPoint = ModelManager.Instance.modelRoot.GetModelCenterVector(ModelManager.Instance.centerDis);
            modelRoot.position = startPoint;
            if (!exitingCourse)
            {
                if (onModelLocate != null)
                {
                    onModelLocate.Invoke();
                    onModelLocate = null;
                }
                else
                {
                    SendMsg(new MsgBase((ushort)ARModuleEvent.Close));
                }
            }

            OpenCtrl(false);
            //NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)GazeEvent.SyncCamera));
        };
    }


    private void Update()
    {
        if (!initialized)
            return;

        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (GUITool.IsOverGUI(touchPosition, out GameObject go) && (go == null || !go.name.Equals("ModelCtrl")))
            return;

        if (raycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            if (onModelLocate != null)
            {
                onModelLocate.Invoke();
                onModelLocate = null;
            }
            modelRoot.position = s_Hits[0].pose.position; //跟踪位置
            latestTouchPosition = modelRoot.position;
            hasTouchPos = true;

            //关闭位置选择
            OpenCtrl(false);

            NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)GazeEvent.SyncCamera));
        }
    }

    /// <summary>
    /// 开启平面检测
    /// </summary>
    /// <param name="open"></param>
    public void OpenCtrl(bool open)
    {
        GlobalInfo.isARTracking = open;
        SendMsg(new MsgBool((ushort)ARModuleEvent.Tracking, open));

        //清空上一次检测的平面
        if (open && ARSession)
        {
            ARSession.Reset();
        }

        initialized = open;
        if (planeManager)
        {
            planeManager.enabled = open;
            planeManager.SetTrackablesActive(open);
        }
        if(raycastManager)
            raycastManager.enabled = open;
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)CoursePanelEvent.ModelLocate:
                MsgUnityAction msgAction = (MsgUnityAction)msg;
                onModelLocate = msgAction.arg;
                UIManager.Instance.CloseUI<LoadingPanel>();
                if (!initialized)
                {
                    modelRoot.position = new Vector3(0, 5000, 0);//不显示模型                                                                
                    OpenCtrl(true);//开启位置选择
                }
                break;
            case (ushort)CoursePanelEvent.ChangeModel:
                if (hasTouchPos)
                {
                    modelRoot.position = latestTouchPosition;
                }
                break;
            case (ushort)ARModuleEvent.ExitCourse:
                exitingCourse = true;
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        destroyEvent?.Invoke();
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }
}