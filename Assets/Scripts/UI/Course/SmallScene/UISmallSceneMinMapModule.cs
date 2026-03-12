using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using UnityEngine.Rendering.Universal;
using System;

public class MinMapData : UIData
{
    /// <summary>
    /// 小地图相机正交视口大小
    /// </summary>
    public int orthographicSize;
    /// <summary>
    /// 导航点
    /// </summary>
    public List<NavigationPoint> naviPoints;
    /// <summary>
    /// 角色控制器-自身
    /// </summary>
    public PlayerController playerController;
    /// <summary>
    /// 小地图初始化所需数据
    /// </summary>
    /// <param name="orthographicSize">小地图相机正交视口大小</param>
    /// <param name="naviPoints">导航点</param>
    public MinMapData(int orthographicSize = 10, List<NavigationPoint> naviPoints = null, PlayerController playerController = null)
    {
        this.orthographicSize = orthographicSize;
        this.naviPoints = naviPoints;
        this.playerController = playerController;
    }
}

/// <summary>
/// 模拟操作小地图模块
/// </summary>
public class UISmallSceneMinMapModule : UIModuleBase
{
    /// <summary>
    /// 小地图相机渲染RenderTexture
    /// </summary>
    public RenderTexture MapRenderTexture;
    /// <summary>
    /// 是否打开大地图
    /// </summary>
    public bool inMap;

    /// <summary>
    /// 小地图相机
    /// </summary>
    private Camera mapCamera;
    /// <summary>
    /// 小地图
    /// </summary>
    private RectTransform MiniMapRect;
    /// <summary>
    /// 大地图
    /// </summary>
    private Transform mapUI;
    /// <summary>
    /// 导航点Item
    /// </summary>
    private Transform mapIcon;
    /// <summary>
    /// 小地图ScrollRect,控制角色始终在画面中
    /// </summary>
    private ScrollRect miniMapScrollRect;
    /// <summary>
    /// 角色控制器-自身
    /// </summary>
    private PlayerController playerController;
    private Vector3 tempVec3;

    public override void Open(UIData uiData = null)
    {
        base.Open();
        AddMsg(new ushort[]{
            (ushort)SmallFlowModuleEvent.RightFlex
        });

        MinMapData data = uiData as MinMapData;
        playerController = data.playerController;
        InitMapCamera(data.orthographicSize);
        InitMap();
        InitNaviPointList(data.naviPoints);
    }
    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        if (mapUI)
            mapUI.parent = transform;

        if (mapCamera)
            Destroy(mapCamera.gameObject);

        base.Close(uiData, callback);
    }
    private void InitMapCamera(int orthographicSize)
    {
        // 创建小地图相机
        mapCamera = new GameObject("MapCamera").AutoComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.targetTexture = MapRenderTexture;
        UniversalAdditionalCameraData uacm = mapCamera.AutoComponent<UniversalAdditionalCameraData>();
        if (uacm) uacm.renderShadows = false;
        mapCamera.cullingMask = (1 << LayerMask.NameToLayer("Default")) + (1 << LayerMask.NameToLayer("Map"));
        mapCamera.transform.parent = ModelManager.Instance.modelRoot;
        mapCamera.transform.localPosition = 6.2f * Vector3.up;
        mapCamera.transform.localEulerAngles = 90f * Vector3.right;
        mapCamera.orthographicSize = orthographicSize;
    }

    /// <summary>
    /// 初始化地图
    /// </summary>
    private void InitMap()
    {
        MiniMapRect = transform.GetComponentByChildName<RectTransform>("MiniMap");
        mapUI = this.FindChildByName("Map");
        mapIcon = this.FindChildByName("MapItem");

        mapUI.GetComponentInChildren<Button>(true).onClick.AddListener(() => MapControl(false));

        miniMapScrollRect = this.GetComponentByChildName<ScrollRect>("MiniMapScrollRect");

        MiniMapRect.GetComponent<Button>().onClick.AddListener(() =>
        {
            MapControl(true);
        });

        mapIcon.gameObject.SetActive(false);
        MapControl(false);
    }

    /// <summary>
    /// 初始化地图导航点列表
    /// </summary>
    private void InitNaviPointList(List<NavigationPoint> points)
    {
        if (points == null || points.Count <= 0)
            return;

        var MapContent = this.FindChildByName("MapContent");
        var MiniMapContent = this.FindChildByName("MiniMapContent");

        while (MapContent.childCount > 0)
        {
            DestroyImmediate(MapContent.GetChild(0).gameObject);
        }

        while (MiniMapContent.childCount > 0)
        {
            DestroyImmediate(MiniMapContent.GetChild(0).gameObject);
        }

        foreach (var pointInfo in points)
        {
            tempVec3 = ((pointInfo.Point.localPosition / mapCamera.orthographicSize) + Vector3.one) / 2;

            var mapItem = Instantiate(mapIcon, MapContent) as RectTransform;
            {
                mapItem.name = pointInfo.Name;
                mapItem.anchorMin = mapItem.anchorMax = new Vector2(tempVec3.x, tempVec3.z);
                mapItem.anchoredPosition = Vector2.zero;

                mapItem.GetComponentInChildren<Text>(true).text = pointInfo.Name;
                mapItem.GetComponentInChildren<Button>(true).onClick.AddListener(() =>
                {
                    playerController.StartNavigation(pointInfo.Point);
                    MapControl(false);
                });

                mapItem.gameObject.SetActive(true);
            }

            var miniMapItem = Instantiate(mapIcon, MiniMapContent) as RectTransform;
            {
                miniMapItem.name = pointInfo.Name;
                miniMapItem.anchorMin = miniMapItem.anchorMax = new Vector2(tempVec3.x, tempVec3.z);
                miniMapItem.anchoredPosition = Vector2.zero;

                miniMapItem.GetComponentInChildren<Text>(true).text = pointInfo.Name;
                miniMapItem.GetComponentInChildren<Button>(true).image.raycastTarget = false;
                miniMapItem.gameObject.SetActive(true);
            }
        }
    }

    private void Update()
    {
        //小地图角色始终在画面中
        if (mapCamera == null || playerController == null)
            return;
        tempVec3 = ((playerController.transform.localPosition / mapCamera.orthographicSize) + Vector3.one) / 2;
        miniMapScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(tempVec3.x);
        miniMapScrollRect.verticalNormalizedPosition = Mathf.Clamp01(tempVec3.z);
    }

    /// <summary>
    /// 控制地图开关
    /// </summary>
    private void MapControl(bool isShow)
    {
        inMap = isShow;
        if (isShow)
        {
            if (GlobalInfo.isExam)
                mapUI.parent = ((ExamCoursePanel)ParentPanel).ShowModulePoint;
            else
                mapUI.parent = ((OPLCoursePanel)ParentPanel).ShowModulePoint;
        }
        else
        {
            mapUI.parent = transform;
            mapUI.SetAsLastSibling();
        }
        mapUI.gameObject.SetActive(isShow);
        SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.MaxMap, isShow));
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.RightFlex:
                bool rightFlex = ((MsgBool)msg).arg1;
                float RightAnchorPos = rightFlex ? -324f : -24f;
                MiniMapRect.DOAnchorPos3DX(RightAnchorPos, 0.3f);
                break;
            case (ushort)ShortcutEvent.PressAnyKey:
                ShortcutManager.Instance.CheckShortcutKey(msg, new Dictionary<string, Action>()
                {
                    {ShortcutManager.SmallScene_OpenMap, ()=>MapControl(!inMap)}
                });
                break;
            default:
                break;
        }
    }
}