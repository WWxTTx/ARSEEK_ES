using UnityFramework.Runtime;

/// <summary>
/// 模型操作
/// </summary>
public enum ModelOperateEvent
{
    Min = UIAnimEvent.Max + 1,
    Click,
    Scale,
    Rotate,
    Max,
}

/// <summary>
/// 登录界面
/// </summary>
public enum LoginEvent
{
    Min = ModelOperateEvent.Max + 1,
    /// <summary>
    /// 版本检测页面
    /// </summary>
    CheckVersion,
    /// <summary>
    /// 登录页面
    /// </summary>
    Login,
    /// <summary>
    /// 注册页面
    /// </summary>
    Register,
    /// <summary>
    /// 忘记密码页面
    /// </summary>
    Forget,
    Max,
}

/// <summary>
/// 设置界面
/// </summary>
public enum OptionPanelEvent
{
    Min = LoginEvent.Max + 1,
    /// <summary>
    /// 修改昵称
    /// </summary>
    Name,
    /// <summary>
    /// 修改单位
    /// </summary>
    Org,
    /// <summary>
    /// 退出登录
    /// </summary>
    Logout,
    Max,
}

public enum SpriteTogEvent
{
    Min = OptionPanelEvent.Max + 1,
    Close,
    Max,
}

public enum ResourcesPanelEvent
{
    Min = SpriteTogEvent.Max + 1,
    /// <summary>
    /// 选择课程
    /// </summary>
    SelectCourse,
    /// <summary>
    /// 课程详情
    /// </summary>
    Details,
    Max
}

public enum BaikeSelectModuleEvent
{
    Min = ResourcesPanelEvent.Max + 1,
    BaikeSelect,
    Hide,
    Max
}

public enum CoursePanelEvent
{
    Min = BaikeSelectModuleEvent.Max + 1,
    /// <summary>
    /// 用于协同权限切换时修改UI可交互性
    /// </summary>
    OpenMask,
    CloseMask,
    /// <summary>
    /// 标注模式
    /// </summary>
    EditMode,
    /// <summary>
    /// 模型层级
    /// </summary>
    HierarchyBtn,
    /// <summary>
    /// 操作列表
    /// </summary>
    OperationListBtn,
    /// <summary>
    /// 动画列表
    /// </summary>
    AnimListBtn,
    /// <summary>
    /// 选择课程
    /// </summary>
    SwitchResource,
    /// <summary>
    /// 百科模型加载完成
    /// </summary>
    ChangeModel,
    /// <summary>
    /// AR界面 追踪位置
    /// </summary>
    ModelLocate,
    /// <summary>
    /// 转场
    /// </summary>
    Transition,
    /// <summary>
    /// 设置
    /// </summary>
    Option,
    /// <summary>
    /// 退出课程
    /// </summary>
    Quit,
    Max
}

public enum ARModuleEvent
{
    Min = CoursePanelEvent.Max + 1,
    Open,
    Tracking,
    Close,
    Support,
    Unsupport,
    ExitCourse,
    Max
}

public enum PaintEvent
{
    Min = ARModuleEvent.Max + 1,
    PaintArea,
    SyncPaint,
    SyncUndo,
    SyncReset,
    ExitPaint,
    Max
}

public enum KnowledgeModuleEvent
{
    Min = PaintEvent.Max + 1,
    Show,
    Hide,
    Max
}

/// <summary>
/// 图片、文档、音视频
/// </summary>
public enum HyperLinkEvent
{
    Min = KnowledgeModuleEvent.Max + 1,
    HyperlinkImage,
    HyperlinkVideo,
    HyperlinkDOC,
    HyperlinkAudio,
    HyperlinkClose,
    HyperImgClose,
    HyperVideoClose,
    HyperAudioClose,
    VideoCtrl,
    VideoValue,
    VideoSync,
    AudioCtrl,
    AudioValue,
    AudioSync,
    Max
}

/// <summary>
/// 拆解、动画百科
/// </summary>
public enum IntegrationModuleEvent
{
    Min = HyperLinkEvent.Max + 1,
    /// <summary>
    /// 拆分
    /// </summary>
    Split,
    /// <summary>
    /// 组合
    /// </summary>
    Comb,
    /// <summary>
    /// 单独显示
    /// </summary>
    Check,
    /// <summary>
    /// 全部显示
    /// </summary>
    UnCheck,
    /// <summary>
    /// 跳级选中
    /// </summary>
    JumpToSelect,
    /// <summary>
    /// 还原
    /// </summary>
    CombAll,
    /// <summary>
    /// 动画切换
    /// </summary>
    AnimSelect,
    /// <summary>
    /// 动画播放暂停
    /// </summary>
    AnimPlay,
    /// <summary>
    /// 动画进度
    /// </summary>
    AnimValue,
    /// <summary>
    /// 动画结束
    /// </summary>
    AnimFinish,
    /// <summary>
    /// 控制透明度
    /// </summary>
    AlphaValue,
    Max
}

/// <summary>
/// 模拟操作百科
/// </summary>
public enum SmallFlowModuleEvent
{
    Min = IntegrationModuleEvent.Max + 1,
    /// <summary>
    /// 左侧ui活动
    /// </summary>
    LeftFlex,
    /// <summary>
    /// 右侧ui活动
    /// </summary>
    RightFlex,
    /// <summary>
    /// 选择任务
    /// </summary>
    SelectFlow,
    /// <summary>
    /// 选择步骤
    /// </summary>
    SelectStep,
    /// <summary>
    /// 选择下一步骤
    /// </summary>
    NextStep,
    /// <summary>
    /// 选择工具
    /// </summary>
    SelectTool,
    /// <summary>
    /// 切换工具箱道具
    /// </summary>
    ChangeProp,
    /// <summary>
    /// 聚焦对象变化 修改操作权限占用
    /// </summary>
    FocusChanged,
    /// <summary>
    /// 观察
    /// </summary>
    Look,
    Look2D,
    /// <summary>
    /// 操作
    /// </summary>
    Operate,
    /// <summary>
    /// 上位机选中
    /// </summary>
    MasterComputerSelect,
    /// <summary>
    /// 上位机操作
    /// </summary>
    MasterComputerOperate,
    /// <summary>
    /// 输入操作
    /// </summary>
    Input,
    /// <summary>
    /// 联系操作
    /// </summary>
    Contact,
    /// <summary>
    /// 操作开始执行
    /// </summary>
    StartExecute,
    /// <summary>
    /// 操作表现、联动执行完成
    /// 自动跳步
    /// </summary>
    StepEnd,
    /// <summary>
    /// 操作执行完成
    /// </summary>
    CompleteExecute,
    /// <summary>
    /// 步骤完成
    /// </summary>
    CompleteStep,
    /// <summary>
    /// 当前任务全部步骤完成
    /// </summary>
    CompleteAll,
    /// <summary>
    /// 关闭监控
    /// </summary>
    HideMonitor,
    UIHighlight,
    /// <summary>
    /// 操作记录
    /// </summary>
    OperatingRecord,
    /// <summary>
    /// 输入操作记录
    /// </summary>
    OperatingRecordInput,
    /// <summary>
    /// 修改输入操作记录
    /// </summary>
    OperatingRecordChange,
    /// <summary>
    /// 清除记录
    /// </summary>
    OperatingRecordClear,
    /// <summary>
    /// 小地图最大化
    /// </summary>
    MaxMap,
    /// <summary>
    /// 输入操作记录
    /// </summary>
    SelectInput,
    /// <summary>
    /// 联系
    /// </summary>
    SelectContact,
    /// <summary>
    /// 执行2D操作
    /// </summary>
    Operate2D,
    /// <summary>
    /// 显示操作UI
    /// </summary>
    ShowUIOperation,
    /// <summary>
    /// 关闭相机操作
    /// </summary>
    CloseCameraOperation,
    /// <summary>
    /// 打开相机操作
    /// </summary>
    OpenCameraOperation,
    /// <summary>
    /// 视角引导
    /// </summary>
    Guide,
    /// <summary>
    /// 系统记录
    /// </summary>
    SystemRecord,
    /// <summary>
    /// 工具栏显示
    /// </summary>
    ShowTool,
    Max
}

/// <summary>
/// 习题百科
/// </summary>
public enum ExercisesModuleEvent
{
    Min = SmallFlowModuleEvent.Max + 1,
    /// <summary>
    /// 选择答案
    /// </summary>
    ChooseAnswer,
    /// <summary>
    /// 查看答案
    /// </summary>
    ConfirmAnswer,
    /// <summary>
    /// 查看图片
    /// </summary>
    OpenAnswerImg,
    CloseAnswerImg,
    /// <summary>
    /// 查看视频
    /// </summary>
    OpenAnswerVideo,
    CloseAnswerVideo,
    Max
}

/// <summary>
/// 角色同步
/// </summary>
public enum GazeEvent
{
    Min = ExercisesModuleEvent.Max + 1,
    SyncCamera,
    UserPose,
    Max,
}

/// <summary>
/// 模型层级面板
/// </summary>
public enum HierarchyEvent
{
    Min = GazeEvent.Max + 1,
    Hide,
    /// <summary>
    /// 展开节点
    /// </summary>
    Expand,
    /// <summary>
    /// 收起节点
    /// </summary>
    Collapse,
    /// <summary>
    /// 点击节点
    /// </summary>
    Click,
    /// <summary>
    /// 更新节点课件资料提示
    /// </summary>
    UpdateAttachment,
    Interactable,
    Max
}

/// <summary>
/// 动画列表面板
/// </summary>
public enum AdaptiveListEvent
{
    Min = HierarchyEvent.Max + 1,
    Hide,
    Select,
    SelectWithoutNotify,
    Max
}


/// <summary>
/// 操作列表面板
/// </summary>
public enum OperationListEvent
{
    Min = AdaptiveListEvent.Max + 1,
    Open,
    Show,
    Hide,
    Max
}

/// <summary>
/// 协同状态同步
/// </summary>
public enum StateEvent
{
    Min = OperationListEvent.Max + 1,
    /// <summary>
    /// 同步版本准备操作
    /// </summary>
    PreSyncVersion,
    Max
}

/// <summary>
/// 网络通道
/// </summary>
public enum NetworkChannelEvent
{
    Min = StateEvent.Max + 1,
    Open,
    Closed,
    Error,
    HeartMiss,
    Max
}

/// <summary>
/// 房间通道
/// </summary>
public enum RoomChannelEvent
{
    Min = NetworkChannelEvent.Max + 1,
    UpdateRoomList,
    JoinRoomSuccess,
    JoinRoomFail,
    LeaveRoom,
    OtherJoin,
    OtherLeave,
    UpdateMainScreen,
    UpdateControl,
    TalkState,
    UpdateMemberList,
    LiveRoomMemberModuleClose,
    LiveRoomMemberModuleShow,
    LiveRoomSettingModuleClose,
    /// <summary>
    /// 更新房间信息
    /// </summary>
    RoomInfo,
    /// <summary>
    /// 房间解散
    /// </summary>
    RoomClose,
    Max
}

/// <summary>
/// 音视频通道
/// </summary>
public enum MediaChannelEvent
{
    Min = RoomChannelEvent.Max + 1,
    AddView,
    RemoveView,
    ClearView,
    MicError,
    MicOnAir,
    Max
}

public enum ExamPanelEvent
{
    Min = MediaChannelEvent.Max + 1,
    ExerciseScore,
    /// <summary>
    /// 考核状态 
    /// </summary>
    Start,
    //Pause,
    /// <summary>
    /// 同步计时
    /// </summary>
    Resume,
    Stop,
    /// <summary>
    /// 房主计时结束
    /// </summary>
    Timeout,
    /// <summary>
    /// 本地计时结束
    /// </summary>
    LocalTimeout,
    /// <summary>
    /// 确保结束考核后清空状态消息
    /// </summary>
    Flush,
    /// <summary>
    /// 提交成绩
    /// </summary>
    Submit,
    /// <summary>
    /// 退出房间
    /// </summary>
    Quit,
    Max
}

/// <summary>
/// 直播答题消息
/// </summary>
public enum JudgeOnlineEvent
{
    Min = ExamPanelEvent.Max + 1,
    Start,
    Answer,
    Complete,
    End,
    Max
}

public enum ShortcutEvent
{
    Min = JudgeOnlineEvent.Max + 1,
    /// <summary>
    /// 按下任意键 在uibase中已经添加
    /// </summary>
    PressAnyKey,
    Max
}


/// <summary>
/// 操作记录列表面板
/// </summary>
public enum HistoryEvent
{
    Min = ShortcutEvent.Max + 1,
    Open,
    Show,
    Hide,
    Max
}