using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;
using UI.Dates;

/// <summary>
/// 创建考核房间弹窗，TODO和协同分离或者抽离父类
/// </summary>
public class ExamCreateRoomModule : CreateRoomModule
{
    //房间的有效期 当前版本不用这个，全部走快捷房间的逻辑
    //private DatePicker datePicker;
    //private TimePicker timePicker;

    private DateTime startDate;
    private TimeSpan startTime;

    public override void Open(UIData uiData = null)
    {
        //设置获取的课程标签类型
        tagType = 3;

        base.Open(uiData);

        //datePicker = GetComponentInChildren<DatePicker>(true);
        //datePicker.SelectedDate = startDate = DateTime.Today;
        //datePicker.UpdateInputFieldText();
        //datePicker.OnSingleDayButtonClicked += (selectedDate) => { startDate = selectedDate; };

        //timePicker = GetComponentInChildren<TimePicker>(true);
        //timePicker.OnTimeAssert += () => { UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(1), new LocalTipModule.ModuleData("时间不能早于当前时间", 120)); };
        //timePicker.OnTimeSelected += (timespan) => { startTime = timespan; };

        //CreateNowBtn = transform.GetComponentByChildName<Button>("CreateNow");
        //CreateNowBtn.onClick.AddListener(OnCreateRoomNowBtnClicked);
    }

    /// <summary>
    /// 初始化考核列表
    /// </summary>
    protected override void InitData()
    {
        RequestManager.Instance.GetExamList(courseData =>
        {
            GlobalInfo.SaveCourseInfo(courseData);
            InitCourseList();
        }, failureMessage =>
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("获取失败", "获取考核列表失败，请重新登录", popupDic, () => ToolManager.GoToLogin()));
            }
            Log.Error($"获取考核列表失败！原因为：{failureMessage}");
        });
    }

    protected override void OnCreateRoomBtnClicked()
    {
        RequestManager.Instance.GetExamPaper(thisCourseId, exam =>
        {
            if (exam.encyclopediaList == null || exam.encyclopediaList.Count == 0)
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform, new LocalTipModule.ModuleData("该考核未添加习题"));
                return;
            }
            //todo exam.duration
            CreateRoomAndJoin(RoomName.text, RoomPassword.text, exam.duration + 1, exam.id, GetCourseTags(exam.tags), exam.iconPath);
        }, (error) =>
        {
            Log.Error($"获取考核试卷[{thisCourseId}]失败，{error}");
        });
    }

    private void OnCreateRoomNowBtnClicked()
    {
        RequestManager.Instance.GetExamPaper(thisCourseId, exam =>
        {
            if (exam.encyclopediaList == null || exam.encyclopediaList.Count == 0)
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform, new LocalTipModule.ModuleData("该考核未添加习题"));
                return;
            }
            //todo
            CreateRoomNow(RoomName.text, RoomPassword.text, exam.duration + 1, exam.id, GetCourseTags(exam.tags), exam.iconPath);
        }, (error) =>
        {
            Log.Error($"获取考核试卷[{thisCourseId}]失败，{error}");
        });
    }

    /// <summary>
    /// 创建 不直接进入房间
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="roomPassword"></param>
    /// <param name="duration"></param>
    /// <param name="courseId"></param>
    /// <param name="courseTitle"></param>
    /// <param name="courseIcon"></param>

    protected override void CreateRoomAndJoin(string roomName, string roomPassword, int duration, int courseId, string courseTitle, string courseIcon)
    {
        ExamRoomType roomType = LiveRoom.isOn ? ExamRoomType.Person : ExamRoomType.Group;
        NetworkManager.Instance.CreateExamReserveRoom(roomName, roomPassword, (startDate + startTime).ToString("yyyy-MM-dd HH:mm"), duration, roomType, courseId, courseTitle, courseIcon,
           (roomUuid) =>
           {
               FormMsgManager.Instance.SendMsg(new MsgBase((ushort)RoomChannelEvent.UpdateRoomList));
               CloseModule();
           },
           (code, failureMessage) =>
           {
               UIManager.Instance.CloseUI<TransitionPanel>();
               switch (code)
               {
                   case 0:
                       UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，创建房间失败"));
                       break;
                   default:
                       UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("创建房间失败"));
                       break;
               }
               Log.Error($"创建房间失败！原因为：{failureMessage}");
           });
    }

    /// <summary>
    /// 快速创建 测试用？
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="roomPassword"></param>
    /// <param name="duration"></param>
    /// <param name="courseId"></param>
    /// <param name="courseTitle"></param>
    /// <param name="courseIcon"></param>
    private void CreateRoomNow(string roomName, string roomPassword, int duration, int courseId, string courseTitle, string courseIcon)
    {
        ExamRoomType roomType = LiveRoom.isOn ? ExamRoomType.Person : ExamRoomType.Group;
        NetworkManager.Instance.CreateExamReserveRoom(roomName, roomPassword, (DateTime.Now + TimeSpan.FromMinutes(1)).ToString("yyyy-MM-dd HH:mm"), duration, roomType, courseId, courseTitle, courseIcon,
           (roomUuid) =>
           {
               FormMsgManager.Instance.SendMsg(new MsgBase((ushort)RoomChannelEvent.UpdateRoomList));
               CloseModule();
           },
           (code, failureMessage) =>
           {
               UIManager.Instance.CloseUI<TransitionPanel>();
               switch (code)
               {
                   case 0:
                       UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，创建房间失败"));
                       break;
                   default:
                       UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("创建房间失败"));
                       break;
               }
               Log.Error($"创建房间失败！原因为：{failureMessage}");
           });
    }

    protected override void JoinRoomCallback()
    {
        //记录当前房间信息
        GlobalInfo.SetCourseMode(CourseMode.OnlineExam);
        GlobalInfo.roomInfo = thisRoomInfo;

        GlobalInfo.currentCourseID = thisCourseId;
        UIManager.Instance.CloseUI<ExamTrainingPanel>();
        UIManager.Instance.OpenUI<ExamPanel>();

        NetworkManager.Instance.SetUserColor(GlobalInfo.account.id);
        this.WaitTime(0.5f, () => UIManager.Instance.CloseUI<TransitionPanel>());
    }

    protected override void CloseModule()
    {
        UIManager.Instance.CloseModuleUI<ExamCreateRoomModule>(ParentPanel);
    }
}