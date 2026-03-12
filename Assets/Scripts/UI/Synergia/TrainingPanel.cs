using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 协同房间列表界面
/// </summary>
public class TrainingPanel : UIPanelBase
{
    protected override bool CanLogout { get { return true; } }
    public override bool canOpenOption => true;
    public static string searchKeyword;

    /// <summary>
    /// 不存在房间时显示提示图标
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
    /// 创建房间按钮
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
    /// 房间集合items
    /// </summary>
    private List<RectTransform> currentItems = new List<RectTransform>();
    /// <summary>
    /// 房间列表数据
    /// </summary>
    public Dictionary<string, RoomInfoModel> roomInfos = new Dictionary<string, RoomInfoModel>();
    /// <summary>
    /// 快速加入房间id，todo是否弃用功能
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
    private string[] roomType = new string[]
    {
        "未知",
        "直播",
        "协同",
    };
    private const string defaultCourseType = "未选择课程";


    public override void Open(UIData uiData = null)
    {
        Cursor.lockState = CursorLockMode.None;
        GlobalInfo.CursorLockMode = CursorLockMode.None;

        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.UpdateRoomList,
            (ushort)RoomChannelEvent.JoinRoomSuccess,
            (ushort)RoomChannelEvent.JoinRoomFail
        });

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

            UIManager.Instance.OpenModuleUI<CreateRoomModule>(this, transform);
        });

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
    }

    /// <summary>
    /// 显示当前窗体
    /// </summary>
    public override void Show(UIData uiData = null)
    {
        base.Show();

        RefreshRoomList();
        RoomTabTime = 0;
        isAutoRefresh = true;
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
        searchKeyword = string.Empty;
        UIManager.Instance.CloseUI<TrainingPanel>();
        UIManager.Instance.OpenUI<HomePagePanel>();
    }

    /// <summary>
    /// 异常退出检测
    /// </summary>
    private void LastRoomCheck()
    {
        if (roomInfos.TryGetValue(PlayerPrefs.GetString(GlobalInfo.lastSynergiaRoomId), out RoomInfoModel roomInfo))
        {
            if (roomInfo.creatorId == GlobalInfo.account.id)
            {
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("是", new PopupButtonData(() =>
                {
                    joiningRoom = true;
                    JoinRoom(roomInfo.Uuid, roomInfo.Password);
                }, true));
                popupDic.Add("否", new PopupButtonData(() =>
                {
                    NetworkManager.Instance.DeleteRoom(roomInfo.Uuid, () => RefreshRoomList(), (code, msg) =>
                    {
                        Log.Error($"删除房间失败 {msg}");
                    });
                }));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "检测到您上次异常退出，是否要进入房间？\n （若不进入，房间将被删除）", popupDic, null, false));
                return;
            }
        }
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
        roomInfos.Clear();

        if (rooms != null)
        {
            rooms = rooms.FindAll(r => r.CourseId != 0);
            foreach (var item in rooms)
            {
                roomInfos.Add(item.Uuid, item);
            }
            rooms = rooms.Where(room => room.RoomType > 0).ToList();
        }

        if (rooms == null || rooms.Count == 0)
        {
            EmptyHint.text = string.Format("当前不存在{0}房间", "协同");
            Empty.SetActive(true);
            LiveScrollView.content.RefreshItemsView(new List<RoomInfoModel>(), null);
            return;
        }

        Empty.SetActive(false);
        currentItems.Clear();

        rooms = UpdateRoomListOrder(rooms);
        rooms = FilterRoomList(rooms);

        LiveScrollView.content.RefreshItemsView(rooms, SetLiveItemInfo);
    }

    /// <summary>
    /// 排序房间列表
    /// </summary>
    /// <param name="rooms"></param>
    private List<RoomInfoModel> UpdateRoomListOrder(List<RoomInfoModel> rooms)
    {
        List<RoomInfoModel> priorityOne = new List<RoomInfoModel>();
        List<RoomInfoModel> priorityTwo = new List<RoomInfoModel>();
        List<RoomInfoModel> priorityThree = new List<RoomInfoModel>();
        RoomInfoModel roomInfoModel = null;

        foreach (var item in rooms)
        {
            if (item.creatorId == GlobalInfo.account.id)
            {
                if (roomInfoModel != null)
                {
                    priorityOne.Add(item);
                }
                else
                {
                    roomInfoModel = item;
                }
            }
            else if (item.Status == 2)
            {
                priorityOne.Add(item);
            }
            else if (item.Status == 1)
            {
                priorityTwo.Add(item);
            }
            else if (item.Status == 0)
            {
                priorityThree.Add(item);
            }
        }
        rooms.Clear();
        if (roomInfoModel != null)
        {
            rooms.Add(roomInfoModel);
        }
        for (int i = 0; i < priorityOne.Count; i++)
        {
            rooms.Add(priorityOne[i]);
        }
        for (int i = 0; i < priorityTwo.Count; i++)
        {
            rooms.Add(priorityTwo[i]);
        }
        for (int i = 0; i < priorityThree.Count; i++)
        {
            rooms.Add(priorityThree[i]);
        }
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

        tf.GetComponentByChildName<Text>("RoomName").EllipsisText(info.RoomName, ellipsisTextMask);

        tf.GetComponentByChildName<Text>("RoomType").text = roomType[info.RoomType];

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

        Button btn = tf.GetComponentInChildren<Button>();
        RectTransform btnTf = btn.GetComponent<RectTransform>();
        currentItems.Add(btnTf);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => RoomItemBtnClick(info.Uuid));
    }

    /// <summary>
    /// 房间按钮事件
    /// </summary>
    /// <param name="uuid">房间uuid/param>
    private void RoomItemBtnClick(string uuid)
    {
        if (joiningRoom) return;
        if (!roomInfos.ContainsKey(uuid)) return;

        joiningRoom = true;
        //加入前获取最新房间信息
        NetworkManager.Instance.GetRoomInfo(/*GlobalInfo.roomInfo.uuid*/uuid, (roomInfo) =>
        {
            if (roomInfo == null)
            {
                JoinRoomFailed("房间已解散，加入失败");
                RefreshRoomList();
                return;
            }

            roomInfos[uuid] = roomInfo;

            //判断房间满员
            if (roomInfo.RoomType == (int)RoomType.Synergia && roomInfo.MemberCount >= 6)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("房间已满员"));
                return;
            }

            //本人创建的或无需密码的房间 直接加入
            if (roomInfos[uuid].creatorId == GlobalInfo.account.id || !roomInfos[uuid].NeedPwd)
            {
                JoinRoom(uuid, roomInfos[uuid].Password);
            }
            else
            {
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
                //|| room.roomNumber.Replace(" ", "").Contains(searchKeyword.Replace(" ", ""))
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
            JoinRoom(quickJoinRoomID, RoomPassword.text);
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
        if (!roomInfos.ContainsKey(uuid))
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("房间已解散，加入失败", 5f));
            return;
        }

        if (roomInfos.TryGetValue(uuid, out RoomInfoModel roomInfo))
        {
            if ((string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(roomInfo.Password)) || !password.Equals(roomInfo.Password))
            {
                Tip.text = "房间密码错误";
                return;
            }
        }

        UIManager.Instance.OpenUI<TransitionPanel>(UILevel.Loading, new TransitionPanel.TransitionData(string.Format("正在进入{0}...", "协同"), false));

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
        JoinRoomPanel.SetActive(false);

        GlobalInfo.isLive = true;

        UIManager.Instance.CloseUI<TrainingPanel>();
        UIManager.Instance.OpenUI<OPLSynCoursePanel>();

        this.WaitTime(0.5f, () => UIManager.Instance.CloseUI<TransitionPanel>());
    }

    /// <summary>
    /// 加入房间失败回调
    /// </summary>
    /// <param name="info"></param>
    public void JoinRoomFailed(string msg, float toastShowTime = 1.5f)
    {
        UIManager.Instance.CloseUI<LoadingPanel>();
        joiningRoom = false;
        GlobalInfo.roomInfo = null;

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
                if (!creatingRoom)
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
        //释放不再引用的资源
        Resources.UnloadUnusedAssets();
    }
}