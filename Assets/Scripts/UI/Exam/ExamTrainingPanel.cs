using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;
using DG.Tweening;

/// <summary>
/// 考核房间列表界面
/// </summary>
public class ExamTrainingPanel : UIPanelBase
{
    protected override bool CanLogout { get { return true; } }
    public override bool canOpenOption => true;
    public static string searchKeyword;

    /// <summary>
    /// 不存在房间时显示图标
    /// </summary>
    private GameObject Empty;
    /// <summary>
    ///  不存在房间时显示提示文字
    /// </summary>
    private Text EmptyHint;
    /// <summary>
    /// 房间列表
    /// </summary>
    private ScrollRect LiveScrollView;
    /// <summary>
    /// 创建房间按钮(预约)
    /// </summary>
    private RectTransform StartLiveBtn;
    /// <summary>
    /// 返回按钮
    /// </summary>
    private Button ExitBtn;
    /// <summary>
    /// 刷新按钮
    /// </summary>
    private Button RefreshBtn;
    /// <summary>
    /// 搜索输入框
    /// </summary>
    private SearchModule SearchModule;
    /// <summary>
    /// 房间默认封面图
    /// </summary>
    private Texture Default_Icon;
    /// <summary>
    /// 房间刷新计时器
    /// </summary>
    private float RoomTabTime;
    /// <summary>
    /// 是否正在请求列表
    /// </summary>
    private bool requestingList;
    /// <summary>
    /// 是否自动刷新
    /// </summary>
    private bool isAutoRefresh;
    /// <summary>
    /// 房间集合<item,RoomInfoModel>
    /// </summary>
    private Dictionary<Transform, RoomInfoModel> currentItems = new Dictionary<Transform, RoomInfoModel>();
    /// <summary>
    /// 房间列表数据
    /// </summary>
    public Dictionary<string, RoomInfoModel> roomInfos = new Dictionary<string, RoomInfoModel>();
    /// <summary>
    /// 快速加入房间uuid，todo是否弃用功能
    /// </summary>
    private string quickJoinRoomID;
    /// <summary>
    /// 是否正在进入房间
    /// </summary>
    private bool joiningRoom;
    /// <summary>
    /// 是否创建房间
    /// </summary>
    public static bool creatingRoom;
    /// <summary>
    /// 是否首次成功获取到房间列表
    /// </summary>
    private bool first = true;

    private const string ellipsisTextMask = "...";
    private string[] examRoomType = new string[]
    {
        "未知",
        "个人",
        "小组"
    };
    private const string defaultCourseType = "未选择课程";

    private Dictionary<string, int> cachedRoomUUidUserId;

    public const string flag = "ExamTrainingPanel";

    protected ResourcesDownloader downloader;

    public override void Open(UIData uiData = null)
    {
        Cursor.lockState = CursorLockMode.None;
        GlobalInfo.SetCourseMode(CourseMode.Exam);
        GlobalInfo.CursorLockMode = CursorLockMode.None;
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.UpdateRoomList,
            (ushort)RoomChannelEvent.JoinRoomSuccess,
            (ushort)RoomChannelEvent.JoinRoomFail
        });

        downloader = transform.AutoComponent<ResourcesDownloader>();

        Empty = transform.FindChildByName("Empty").gameObject;
        EmptyHint = Empty.GetComponentInChildren<Text>();
        LiveScrollView = transform.GetComponentByChildName<ScrollRect>("LiveScrollView");
        StartLiveBtn = transform.GetComponentByChildName<RectTransform>("StartLiveBtn");
        ExitBtn = transform.GetComponentByChildName<Button>("ExitBtn");
        RefreshBtn = transform.GetComponentByChildName<Button>("Refresh");
        SearchModule = transform.GetComponentByChildName<SearchModule>("SearchModule");
        if (!string.IsNullOrEmpty(searchKeyword))
            SearchModule.Text = searchKeyword;

        if (LiveScrollView.content.childCount > 0)
        {
            Default_Icon = LiveScrollView.content.GetChild(0).GetComponentByChildName<RawImage>("CourseImage").texture;
        }

        StartLiveBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            creatingRoom = true;
            UIManager.Instance.OpenModuleUI<ExamCreateRoomModule>(this, transform);
        });

        //考核只有管理员才能创建房间
        if (GlobalInfo.IsExamMode() && GlobalInfo.account.roleType != 1)
            StartLiveBtn.GetComponent<Button>().interactable = false;

        ExitBtn.onClick.AddListener(Exit);

        RefreshBtn.onClick.AddListener(() =>
        {
            if (requestingList)
                return;
            RoomTabTime = 0;
            RefreshRoomList();
        });

        SearchModule.OnSearch.AddListener((value) =>
        {
            searchKeyword = SearchModule.Text;
            RefreshRoomList();
        });

        InitJoinRoomPanel();

        this.GetComponentByChildName<Button>("Setting")?.onClick.AddListener(() => UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed));

        this.GetComponentByChildName<Button>("Record").onClick.AddListener(() => { UIManager.Instance.OpenUI<RecordPanel>(); UIManager.Instance.CloseUI<ExamTrainingPanel>(); });

        if (PlayerPrefs.HasKey(flag))
        {
            try
            {
                cachedRoomUUidUserId = JsonTool.DeSerializable<Dictionary<string, int>>(PlayerPrefs.GetString(flag));
            }
            catch
            {
                cachedRoomUUidUserId = null;
            }
        }
    }

    /// <summary>
    /// 显示当前窗体
    /// </summary>
    public override void Show(UIData uiData = null)
    {
        RequestManager.Instance.GetExamABPackageList((courseABData) =>
        {
            GlobalInfo.SaveExamABInfo(courseABData);

            base.Show();
            RefreshRoomList();
            RoomTabTime = 0;
            isAutoRefresh = true;
        }, (msg) =>
        {
            Log.Error($"获取考核AB包失败！原因为：{msg}");

            base.Show();
            RefreshRoomList();
            RoomTabTime = 0;
            isAutoRefresh = true;
        });
    }

    public override void Previous()
    {
        base.Previous();
        ForceAbortConnection();
        Exit();
    }

    public override void GotoLogout()
    {
        searchKeyword = string.Empty;
        ForceAbortConnection();
        base.GotoLogout();
    }

    /// <summary>
    /// 确保加入房间过程中退出登录或返回上一页时断开连接
    /// </summary>
    private void ForceAbortConnection()
    {
        if (joiningRoom)
        {
            BestHTTP.HTTPManager.OnQuit();
            NetworkManager.Instance.StopAllCoroutines();
        }
    }

    private void Exit()
    {
        if (ResourcesDownloader.DownloadingCount > 0)
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("否", new PopupButtonData(null));
                popupDic.Add("是", new PopupButtonData(() =>
                {
                    searchKeyword = string.Empty;
                    UIManager.Instance.CloseUI<ExamTrainingPanel>();
                    UIManager.Instance.OpenUI<HomePagePanel>();
                    GlobalInfo.SetCourseMode(CourseMode.Training);
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "离开会中断资源下载，确认要离开吗？", popupDic, null, false));
            }
        }
        else
        {
            searchKeyword = string.Empty;
            UIManager.Instance.CloseUI<ExamTrainingPanel>();
            UIManager.Instance.OpenUI<HomePagePanel>();
            GlobalInfo.SetCourseMode(CourseMode.Training);
        }
    }

    /// <summary>
    /// 异常退出检测
    /// </summary>
    private void LastRoomCheck()
    {
        //之前这里是检测是否有该账号创建的房间，如果有就必须选择进入房间和删除
        //if (roomInfos.TryGetValue(PlayerPrefs.GetInt(GlobalInfo.lastSynergiaRoomId), out RoomInfoModel roomInfo))
        //{
        //    if (GlobalInfo.isExam ^ roomInfo.examType != null)
        //        return;

        //    if (roomInfo.hostId == GlobalInfo.account.id)
        //    {
        //        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        //        popupDic.Add("是", new PopupButtonData(() =>
        //        {
        //            joiningRoom = true;
        //            JoinRoom(roomInfo.roomId, roomInfo.roomPassword);
        //        }, true));
        //        popupDic.Add("否", new PopupButtonData(() =>
        //        {
        //            NetworkManager.Instance.DeleteRoom(roomInfo.roomId,
        //                () =>
        //                {
        //                    RefreshRoomList();
        //                },
        //                (failureMsg) =>
        //                {
        //                    Log.Error($"删除房间失败 {failureMsg}");
        //                });
        //        }));
        //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "检测到您上次异常退出，是否要进入房间？\n （若不进入，房间将被删除）", popupDic, null, false));
        //        return;
        //    }
        //}
    }

    /// <summary>
    /// 请求房间列表
    /// </summary>
    private void RefreshRoomList()
    {
        if (requestingList)
            return;

        requestingList = true;

        NetworkManager.Instance.GetRoomList(
         (rooms) =>
         {
             requestingList = false;
             UpdateRoomItems(rooms);

             if (first)
             {
                 first = false;
                 LastRoomCheck();
             }
         },
                 (code, msg) =>
                 {
                     requestingList = false;
                     if (!IsFiltering())
                     {
                         switch (code)
                         {
                             case 0:
                                 UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("网络异常，房间列表加载失败"));
                                 break;
                             default:
                                 UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("房间列表加载失败"));
                                 break;
                         }
                     }
                     else
                         UpdateRoomItems(roomInfos.Values.ToList());
                 });
    }

    /// <summary>
    /// 更新房间列表
    /// </summary>
    /// <param name="rooms"></param>
    private void UpdateRoomItems(List<RoomInfoModel> rooms)
    {
        if (ResourcesDownloader.DownloadingCount > 0)
            return;

        roomInfos.Clear();

        if (rooms != null)
        {
            foreach (var item in rooms)
            {
                roomInfos.Add(item.Uuid, item);
            }
            // 筛选未结束房间
            rooms = rooms.Where(room => room.ExamType != 0 && room.Status != 3).ToList();
        }

        if (rooms == null || rooms.Count == 0)
        {
            EmptyHint.text = string.Format("当前不存在{0}房间", "考核");
            Empty.SetActive(true);
            LiveScrollView.content.RefreshItemsView(new List<RoomInfoModel>(), null);
            return;
        }

        Empty.SetActive(false);
        currentItems.Clear();

        rooms = UpdateRoomListOrder(rooms);
        rooms = FilterRoomList(rooms);

        LiveScrollView.content.RefreshItemsView(rooms, SetLiveItemInfo);

        #region 资源预加载
        downloader.UpdateExamResourcesState(currentItems, OnItemStateUpdate, OnItemActive);
        #endregion
    }

    /// <summary>
    /// 排序房间列表
    /// </summary>
    /// <param name="rooms"></param>
    private List<RoomInfoModel> UpdateRoomListOrder(List<RoomInfoModel> rooms)
    {
        rooms = rooms.OrderBy(r => r.StartTime).ThenBy(r => r.CreateTime).ThenBy(r => r.creatorId).ToList();
        return rooms;
    }
    /// <summary>
    /// 设置直播房间信息
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void SetLiveItemInfo(Transform tf, RoomInfoModel info)
    {
        tf.name = info.Uuid;

        #region 基础信息
        tf.GetComponentByChildName<Text>("RoomName").EllipsisText(info.RoomName, ellipsisTextMask);
        if (info.ExamType != 0)
            tf.GetComponentByChildName<Text>("RoomType").text = examRoomType[(int)info.ExamType];
        tf.GetComponentByChildName<Text>("HostName").text = info.CreatorName;
        //需要密码的协同房间或是未开始考核的考核房间显示密码图标
        tf.FindChildByName("NeedPwd").gameObject.SetActive(info.NeedPwd);
        if (info.CourseId > 0)
        {
            tf.GetComponentByChildName<Text>("CourseType").EllipsisText(info.CourseTitle, 3, ellipsisTextMask);
            if (!string.IsNullOrEmpty(info.CourseIcon))
            {
                ResManager.Instance.LoadCoverImage(info.CourseId.ToString(), ResManager.Instance.OSSDownLoadPath + info.CourseIcon, false,
                    (arg) =>
                    {
                        if (tf && arg)
                        {
                            RawImage rawImage = tf.GetComponentByChildName<RawImage>("CourseImage");
                            rawImage.texture = arg;
                            rawImage.SetAlpha(1);
                        }
                    });
            }
        }
        else
        {
            tf.GetComponentByChildName<Text>("CourseType").text = defaultCourseType;
            tf.GetComponentInChildren<RawImage>().texture = Default_Icon;
        }
        //预约考核显示开始时间
        tf.GetComponentByChildName<Text>("CourseType").text = $"开放时间：{DateTime.Parse(info.StartTime):MM/dd HH:mm}";//yyyy/
        #endregion

        var inExam = tf.GetComponentByChildName<Button>("inExam");
        inExam.gameObject.SetActive(false);

        //当前版本都走快捷房间 没有等待开放
        //var inReserve = tf.GetComponentByChildName<Button>("inReserve");
        //inReserve.onClick.RemoveAllListeners();
        //inReserve.onClick.AddListener(() =>
        //{
        //    if (info.creatorId != GlobalInfo.account.id)
        //        UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("未到房间开放时间，不能进入"));
        //});

        //inReserve.gameObject.SetActive(!info.AllowIn);
        ////避免重叠显示
        //if (inReserve.gameObject.activeSelf)
        //    tf.FindChildByName("NeedPwd").gameObject.SetActive(false);
        //tf.FindChildByName("CourseImage").gameObject.SetActive(!inReserve.gameObject.activeSelf);

        if (info.AllowIn)
        {
            //非房主不能加入考核中的房间
            if (info.creatorId != GlobalInfo.account.id && !IsCachedRoom(info.Uuid))
            {
                inExam.onClick.RemoveAllListeners();
                inExam.onClick.AddListener(() => UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("正在考核中，不能进入")));
                inExam.gameObject.SetActive(info.Status == 2);
                //避免重叠显示
                if (info.Status == 2)
                    tf.FindChildByName("NeedPwd").gameObject.SetActive(false);
            }
        }

        Button btn = tf.GetComponentInChildren<Button>();
        currentItems.Add(tf, info);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (ResourcesDownloader.DownloadingCount > 0)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("否", new PopupButtonData(null));
                    popupDic.Add("是", new PopupButtonData(() => RoomItemBtnClick(info.Uuid), true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "进入房间会中断资源下载，确认要进入吗？", popupDic, null, false));
                }
            }
            else
            {
                RoomItemBtnClick(info.Uuid);
            }
        });

        #region 房主可取消未开放的考核 现在进入或解散在弹窗中弃用
        //GameObject deletePanel = tf.FindChildByName("Delete").gameObject;
        //deletePanel.GetComponentInChildren<Button>().onClick.AddListener(() =>
        //{
        //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        //    popupDic.Add("取消", new PopupButtonData(() => deletePanel.gameObject.SetActive(false)));
        //    popupDic.Add("确定", new PopupButtonData(() =>
        //    {
        //        deletePanel.gameObject.SetActive(false);
        //        NetworkManager.Instance.DeleteRoom(info.Uuid, () =>
        //        {
        //            RefreshRoomList();
        //            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核已取消"));
        //        }, (errorCode, errorMsg) =>
        //        {
        //            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("取消考核失败"));
        //        });
        //    }, true));
        //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定要取消考核吗?", popupDic));
        //});
        //EventTrigger eventTrigger = tf.AutoComponent<EventTrigger>();
        //eventTrigger.AddEvent(EventTriggerType.PointerEnter, (ed) =>
        //{
        //    if (info.creatorId == GlobalInfo.account.id)// && inReserve.gameObject.activeSelf)
        //    {
        //        deletePanel.gameObject.SetActive(true);
        //    }
        //});
        //eventTrigger.AddEvent(EventTriggerType.PointerExit, (ed) =>
        //{
        //    deletePanel.gameObject.SetActive(false);
        //});
        #endregion
    }
    /// <summary>
    /// 房间按钮事件
    /// </summary>
    /// <param name="uuid">房间uuid</param>
    private void RoomItemBtnClick(string uuid)
    {
        if (joiningRoom) return;
        if (!roomInfos.ContainsKey(uuid)) return;

        creatingRoom = false;

        //加入前获取最新房间信息
        NetworkManager.Instance.GetRoomInfo(uuid, (room) =>
        {
            if (room == null)
            {
                JoinRoomFailed("考核房间已解散，加入失败");
                RefreshRoomList();
                return;
            }

            roomInfos[uuid] = room;

            //判断房间满员
            if (room.ExamType == (int)ExamRoomType.Group && room.MemberCount >= 7)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("房间已满员"));
                return;
            }

            if (room.Status == 2 && room.creatorId != GlobalInfo.account.id && !IsCachedRoom(room.Uuid))
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("正在考核中，不能进入"));
                return;
            }

            //如果是该账号创建的房间，可解散或重新进入
            if (room.creatorId == GlobalInfo.account.id)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("解散", new PopupButtonData(() =>
                {
                    NetworkManager.Instance.DeleteRoom(room.Uuid, () => RefreshRoomList(), (code, msg) =>
                    {
                        Log.Error($"删除房间失败 {msg}");
                    });
                }, false));
                popupDic.Add("进入", new PopupButtonData(() =>
                {
                    joiningRoom = true;
                    JoinRoom(uuid, room.Password);
                    GlobalInfo.currentCourseID = room.CourseId;
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "进入或者解散房间？", popupDic));
            }
            //无需密码的房间 直接加入
            else if (!roomInfos[uuid].NeedPwd)
            {
                ConfirmAndJoinRoom(uuid, () =>
                {
                    joiningRoom = true;
                    JoinRoom(uuid, roomInfos[uuid].Password);
                });
            }
            else
            {
                joiningRoom = true;
                quickJoinRoomID = uuid;
                //输入密码弹窗
                JoinRoomPanel.SetActive(true);
                SoundManager.Instance.PlayEffect("Popup");
                Sequence sequence = DOTween.Sequence();
                sequence.Join(DOTween.To(() => 0f, (value) => Mask.alpha = value, 1f, 0.5f));
                sequence.Join(DOTween.To(() => 0.001f * Vector3.one, (value) => Background.localScale = value, Vector3.one, 0.5f));
            }
        }, (code, msg) =>
        {
            switch (code)
            {
                case 0:
                    JoinRoomFailed("网络异常，加入房间失败");
                    RefreshRoomList();
                    break;
            }
        });
    }

    /// <summary>
    /// 更新课程item状态显示
    /// </summary>
    /// <param name="item"></param>
    /// <param name="result"></param>
    /// <param name="roomInfo"></param>
    /// <param name="data"></param>
    private void OnItemStateUpdate(Transform item, int result, RoomInfoModel roomInfo, List<CourseABPackage> data)
    {
        Button UpdateBtn = item.GetComponentByChildName<Button>("Update");
        Transform DownloadTrans = item.FindChildByName("Download");
        Text DownloadText = DownloadTrans.GetComponentInChildren<Text>();

        if (result == 0 || GlobalInfo.account.id == roomInfo.creatorId)
        {
            UpdateBtn.gameObject.SetActive(false);
            DownloadTrans.gameObject.SetActive(false);
        }
        else
        {
            //设置初始状态
            switch (result)
            {
                case 1:
                    UpdateBtn.GetComponentInChildren<Text>().text = "下载";
                    break;
                case 2:
                    UpdateBtn.GetComponentInChildren<Text>().text = "更新";
                    break;
                case 3:
                    UpdateBtn.GetComponentInChildren<Text>().text = "继续下载";
                    break;
            }

            UpdateBtn.onClick.RemoveAllListeners();
            UpdateBtn.onClick.AddListener(() =>
            {
                if (GlobalInfo.isOffLine)
                {
                    ToolManager.PleaseOnline();
                    return;
                }

                downloader.UpdateDownloadingCount(roomInfo.CourseId);

                DownloadText.text = "下载中 0%";
                DownloadTrans.gameObject.SetActive(true);
                UpdateBtn.gameObject.SetActive(false);

                var downloadBackGround = DownloadTrans.GetChild(0);
                {
                    downloadBackGround.localScale = Vector3.zero;
                    DownloadText.SetAlpha(0);

                    DOTween.Sequence()
                    .Append(downloadBackGround.DOScale(Vector3.one, 0.2f))
                    .AppendInterval(0.1f)
                    .Append(DownloadText.DOFade(1, 0.3f));
                }

                downloader.AddABPackTask(roomInfo.CourseId, data, DownloadText, DownloadTrans, UpdateBtn, null/*(count) => Finished.text = count.ToString()*/);
            });

            DownloadTrans.gameObject.SetActive(false);
            UpdateBtn.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 修改item可交互状态
    /// </summary>
    /// <param name="item"></param>
    private void OnItemActive(Transform item)
    {
        Button Resources = item.GetComponentByChildName<Button>("Resources");
        Resources.GetComponent<Image>().raycastTarget = true;
    }

    private void ConfirmAndJoinRoom(string roomUuid, UnityAction onConfirmed)
    {
        if (IsCachedRoom(roomUuid))
        {
            onConfirmed?.Invoke();
        }
        else
        {
            UIManager.Instance.OpenUI<ExamInfoConfirmPanel>(UILevel.Normal, new ExamInfoConfirmPanel.ConfirmData()
            {
                onConfirmed = onConfirmed
            });
        }
    }

    /// <summary>
    /// 是否是已参加考核的房间
    /// </summary>
    /// <returns></returns>
    private bool IsCachedRoom(string roomUuid)
    {
        return cachedRoomUUidUserId != null && cachedRoomUUidUserId.ContainsKey(roomUuid) && cachedRoomUUidUserId[roomUuid] == GlobalInfo.account.id;
    }

    /// <summary>
    /// 是否正在搜索
    /// </summary>
    /// <returns></returns>
    private bool IsFiltering()
    {
        return SearchModule != null && !string.IsNullOrEmpty(SearchModule.Text);
    }

    /// <summary>
    /// 根据搜索关键词筛选房间列表
    /// </summary>
    /// <param name="rooms"></param>
    /// <returns></returns>
    private List<RoomInfoModel> FilterRoomList(List<RoomInfoModel> rooms)
    {
        if (!IsFiltering())
            return rooms;

        if (!string.IsNullOrEmpty(searchKeyword))
        {
            rooms = rooms.Select(item => item).Where(
                room => room.RoomName.Replace(" ", "").Contains(searchKeyword.Replace(" ", ""))
                || room.CreatorName.Replace(" ", "").Contains(searchKeyword.Replace(" ", ""))).ToList();
        }

        if (rooms.Count == 0)
        {
            EmptyHint.text = "未搜索到房间";
            Empty.SetActive(true);
        }

        return rooms;
    }

    #region 输入密码弹窗
    private GameObject JoinRoomPanel;
    private CanvasGroup Mask;
    private Transform Background;
    private InputField_LinkMode RoomPassword;
    private Text Tip;
    private Button CancelBtn;
    private Button EnterBtn;
    private void InitJoinRoomPanel()
    {
        JoinRoomPanel = transform.FindChildByName("JoinRoom").gameObject;
        Mask = JoinRoomPanel.transform.GetComponentByChildName<CanvasGroup>("Mask");
        Background = JoinRoomPanel.transform.FindChildByName("BackGround");
        RoomPassword = transform.GetComponentByChildName<InputField_LinkMode>("RoomPassword");
        Tip = RoomPassword.transform.GetComponentByChildName<Text>("Tip");
        CancelBtn = transform.GetComponentByChildName<Button>("Cancel");
        EnterBtn = transform.GetComponentByChildName<Button>("Enter");

        RoomPassword.onValueChanged.AddListener((value) =>
        {
            Tip.text = string.Empty;
            EnterBtn.interactable = value.Length == 6;
        });
        CancelBtn.onClick.AddListener(() =>
        {
            RoomPassword.text = string.Empty;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(() => 1f, (value) => Mask.alpha = value, 0f, 0.2f));
            sequence.Join(DOTween.To(() => Vector3.one, (value) => Background.localScale = value, 0.001f * Vector3.one, 0.2f));
            sequence.OnComplete(() =>
            {
                JoinRoomPanel.SetActive(false);
                joiningRoom = false;
            });
        });
        EnterBtn.onClick.AddListener(() =>
        {
            if (!roomInfos.ContainsKey(quickJoinRoomID))
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("房间已解散，加入失败", 5f));
                return;
            }
            if (roomInfos.TryGetValue(quickJoinRoomID, out RoomInfoModel roomInfo) && !RoomPassword.text.Equals(roomInfo.Password))
            {
                Tip.text = "房间密码错误";
                return;
            }
            ConfirmAndJoinRoom(quickJoinRoomID, () =>
            {
                joiningRoom = true;
                JoinRoom(quickJoinRoomID, RoomPassword.text);
            });
        });
    }
    #endregion

    /// <summary>
    /// 加入房间
    /// </summary>
    /// <param name="uuid">房间uuid</param>
    /// <param name="password">房间密码</param>
    private void JoinRoom(string uuid, string password)
    {
        UIManager.Instance.OpenUI<TransitionPanel>(UILevel.Loading, new TransitionPanel.TransitionData(string.Format("正在进入{0}...", "考核"), true));

        GlobalInfo.roomInfo = roomInfos[uuid];
        roomInfos[uuid].Password = password;

        SendMsg(new MsgStringFloat((ushort)CoursePanelEvent.Transition, string.Empty, 1f));
        NetworkManager.Instance.JoinRoom(roomInfos[uuid]);
    }

    /// <summary>
    /// 加入房间回调
    /// </summary>
    /// <param name="info"></param>
    private void JoinRoomCallback()
    {
        joiningRoom = false;
        RoomPassword.text = string.Empty;
        JoinRoomPanel.SetActive(false);

        GlobalInfo.SetCourseMode(CourseMode.OnlineExam);
        UIManager.Instance.CloseUI<ExamTrainingPanel>();

        if (GlobalInfo.IsHomeowner())
            UIManager.Instance.OpenUI<ExamPanel>();
        else
            UIManager.Instance.OpenUI<ExamCoursePanel>();

        this.WaitTime(0.5f, () => UIManager.Instance.CloseUI<TransitionPanel>());
    }
    /// <summary>
    /// 加入房间失败回调
    /// </summary>
    /// <param name="info"></param>
    public void JoinRoomFailed(string msg, float toastShowTime = 1.5f)
    {
        GlobalInfo.roomInfo = null;
        UIManager.Instance.CloseUI<TransitionPanel>();
        UIManager.Instance.CloseUI<LoadingPanel>();
        joiningRoom = false;
        RoomPassword.text = string.Empty;
        JoinRoomPanel.SetActive(false);
        if (!string.IsNullOrEmpty(msg))
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo(msg, toastShowTime));
        }
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        isAutoRefresh = false;
        RoomTabTime = 0;

        base.Hide(uiData, callback);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        ResManager.Instance.StopAllDownLoad();
        base.Close(uiData, callback);
    }


    private void Update()
    {
        if (isAutoRefresh)
        {
            RoomTabTime += Time.deltaTime;
            if (RoomTabTime >= GlobalInfo.roomListRefreshTime && !joiningRoom && !IsFiltering())
            {
                RoomTabTime = 0;
                RefreshRoomList();
            }
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.UpdateRoomList:
                RefreshRoomList();
                break;
            case (ushort)RoomChannelEvent.JoinRoomSuccess:
                //若通过点击创建房间按钮加入房间，通过CreateRoomModule处理
                if (!creatingRoom)
                    JoinRoomCallback();
                break;
            case (ushort)RoomChannelEvent.JoinRoomFail:
                //if (!creatingRoom)
                    JoinRoomFailed(((MsgString)msg).arg);
                break;
        }
    }

    public override void JoinAnim(UnityAction callback)
    {
        var exit = ExitBtn.GetComponent<RectTransform>();
        float offset = 16f;
#if UNITY_ANDROID || UNITY_IOS
        offset = 112f;
#endif
        var posX = exit.rect.width + offset;
        exit.gameObject.SetActive(true);
        exit.anchoredPosition = new Vector2(-posX, exit.anchoredPosition.y);
        JoinSequence.Append(exit.DOAnchorPosX(offset, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ResourcesDownloader.DownloadingCount = 0;
        //释放不再引用的资源
        Resources.UnloadUnusedAssets();
        StopAllCoroutines();
    }
}