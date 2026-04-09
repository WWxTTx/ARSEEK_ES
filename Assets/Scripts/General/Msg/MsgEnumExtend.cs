using System;
using System.Collections.Generic;
using UnityFramework.Runtime;

/// <summary>
/// 模型操作
/// </summary>
public enum ModelOperateEvent
{
    Min = UIAnimEvent.Max + 1,        // ID:12
    Click,                            // ID:13
    Scale,                            // ID:14
    Rotate,                           // ID:15
    Max,                              // ID:16
}

/// <summary>
/// 登录界面
/// </summary>
public enum LoginEvent
{
    Min = ModelOperateEvent.Max + 1,  // ID:17
    /// <summary>
    /// 版本检测页面
    /// </summary>
    CheckVersion,                     // ID:18
    /// <summary>
    /// 登录页面
    /// </summary>
    Login,                            // ID:19
    /// <summary>
    /// 注册页面
    /// </summary>
    Register,                         // ID:20
    /// <summary>
    /// 忘记密码页面
    /// </summary>
    Forget,                           // ID:21
    Max,                              // ID:22
}

/// <summary>
/// 设置界面
/// </summary>
public enum OptionPanelEvent
{
    Min = LoginEvent.Max + 1,         // ID:23
    /// <summary>
    /// 修改昵称
    /// </summary>
    Name,                             // ID:24
    /// <summary>
    /// 修改单位
    /// </summary>
    Org,                              // ID:25
    /// <summary>
    /// 退出登录
    /// </summary>
    Logout,                           // ID:26
    Max,                              // ID:27
}

public enum SpriteTogEvent
{
    Min = OptionPanelEvent.Max + 1,   // ID:28
    Close,                            // ID:29
    Max,                              // ID:30
}

public enum ResourcesPanelEvent
{
    Min = SpriteTogEvent.Max + 1,     // ID:31
    /// <summary>
    /// 选择课程
    /// </summary>
    SelectCourse,                     // ID:32
    /// <summary>
    /// 课程详情
    /// </summary>
    Details,                          // ID:33
    Max                               // ID:34
}

public enum BaikeSelectModuleEvent
{
    Min = ResourcesPanelEvent.Max + 1,// ID:35
    BaikeSelect,                      // ID:36
    Hide,                             // ID:37
    Max,                              // ID:38
}

public enum CoursePanelEvent
{
    Min = BaikeSelectModuleEvent.Max + 1,    // ID:39
    /// <summary>
    /// 用于协同权限切换时修改UI可交互性
    /// </summary>
    OpenMask,                                // ID:40
    CloseMask,                               // ID:41
    /// <summary>
    /// 标注模式
    /// </summary>
    EditMode,                                // ID:42
    /// <summary>
    /// 模型层级
    /// </summary>
    HierarchyBtn,                            // ID:43
    /// <summary>
    /// 操作列表
    /// </summary>
    OperationListBtn,                        // ID:44
    /// <summary>
    /// 动画列表
    /// </summary>
    AnimListBtn,                             // ID:45
    /// <summary>
    /// 选择课程
    /// </summary>
    SwitchResource,                          // ID:46
    /// <summary>
    /// 百科模型加载完成
    /// </summary>
    ChangeModel,                             // ID:47
    /// <summary>
    /// AR界面 追踪位置
    /// </summary>
    ModelLocate,                             // ID:48
    /// <summary>
    /// 转场
    /// </summary>
    Transition,                              // ID:49
    /// <summary>
    /// 设置
    /// </summary>
    Option,                                  // ID:50
    /// <summary>
    /// 退出课程
    /// </summary>
    Quit,                                    // ID:51
    Max,                                     // ID:52
}

public enum ARModuleEvent
{
    Min = CoursePanelEvent.Max + 1,   // ID:53
    Open,                             // ID:54
    Tracking,                         // ID:55
    Close,                            // ID:56
    Support,                          // ID:57
    Unsupport,                        // ID:58
    ExitCourse,                       // ID:59
    Max,                              // ID:60
}

public enum PaintEvent
{
    Min = ARModuleEvent.Max + 1,      // ID:61
    PaintArea,                        // ID:62
    SyncPaint,                        // ID:63
    SyncUndo,                         // ID:64
    SyncReset,                        // ID:65
    ExitPaint,                        // ID:66
    Max,                              // ID:67
}

public enum KnowledgeModuleEvent
{
    Min = PaintEvent.Max + 1,         // ID:68
    Show,                             // ID:69
    Hide,                             // ID:70
    Max,                              // ID:71
}

/// <summary>
/// 图片、文档、音视频
/// </summary>
public enum HyperLinkEvent
{
    Min = KnowledgeModuleEvent.Max + 1,  // ID:72
    HyperlinkImage,                      // ID:73
    HyperlinkVideo,                      // ID:74
    HyperlinkDOC,                       // ID:75
    HyperlinkAudio,                      // ID:76
    HyperlinkClose,                      // ID:77
    HyperImgClose,                       // ID:78
    HyperVideoClose,                     // ID:79
    HyperAudioClose,                     // ID:80
    VideoCtrl,                           // ID:81
    VideoValue,                          // ID:82
    VideoSync,                           // ID:83
    AudioCtrl,                           // ID:84
    AudioValue,                          // ID:85
    AudioSync,                           // ID:86
    Max,                                 // ID:87
}

/// <summary>
/// 拆解、动画百科
/// </summary>
public enum IntegrationModuleEvent
{
    Min = HyperLinkEvent.Max + 1,    // ID:88
    /// <summary>
    /// 拆分
    /// </summary>
    Split,                           // ID:89
    /// <summary>
    /// 组合
    /// </summary>
    Comb,                            // ID:90
    /// <summary>
    /// 单独显示
    /// </summary>
    Check,                           // ID:91
    /// <summary>
    /// 全部显示
    /// </summary>
    UnCheck,                         // ID:92
    /// <summary>
    /// 跳级选中
    /// </summary>
    JumpToSelect,                    // ID:93
    /// <summary>
    /// 还原
    /// </summary>
    CombAll,                         // ID:94
    /// <summary>
    /// 动画切换
    /// </summary>
    AnimSelect,                      // ID:95
    /// <summary>
    /// 动画播放暂停
    /// </summary>
    AnimPlay,                        // ID:96
    /// <summary>
    /// 动画进度
    /// </summary>
    AnimValue,                       // ID:97
    /// <summary>
    /// 动画结束
    /// </summary>
    AnimFinish,                      // ID:98
    /// <summary>
    /// 控制透明度
    /// </summary>
    AlphaValue,                      // ID:99
    Max,                             // ID:100
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
    SelectStep,                      // ID:105
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
    ClickObj,
    Look2D,
    /// <summary>
    /// 操作
    /// </summary>
    Operate,                     // ID:112
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
    ShowUIOperation,                        // ID:129
    /// <summary>
    /// 道具状态
    /// </summary>
    OpState,                                // ID:130
    /// <summary>
    /// 关闭相机操作
    /// </summary>
    CloseCameraOperation,                   // ID:131
    /// <summary>
    /// 打开相机操作
    /// </summary>
    OpenCameraOperation,                    // ID:132
    /// <summary>
    /// 视角引导
    /// </summary>
    Guide,                                  // ID:133
    /// <summary>
    /// 系统记录
    /// </summary>
    SystemRecord,                           // ID:134
    /// <summary>
    /// 工具栏显示
    /// </summary>
    ShowTool,                               // ID:135
    /// <summary>
    /// 释放操作权限
    /// </summary>
    ReleasePermission,                      // ID:136
    /// <summary>
    /// TSQ_TsqXsp 按钮事件同步
    /// </summary>
    SynchronizationTsq,                    // ID:137
    /// <summary>
    /// LCU_mlfsjs 按钮事件同步
    /// </summary>
    SynchronizationLcu,                    // ID:141
    /// <summary>
    /// LC_Zlqzz 按钮事件同步
    /// </summary>
    SynchronizationZlqzz,                  // ID:142
    Max,                                   // ID:143
}

/// <summary>
/// 习题百科
/// </summary>
public enum ExercisesModuleEvent
{
    Min = SmallFlowModuleEvent.Max + 1,  // ID:141
    /// <summary>
    /// 选择答案
    /// </summary>
    ChooseAnswer,                        // ID:138
    /// <summary>
    /// 查看答案
    /// </summary>
    ConfirmAnswer,                       // ID:139
    /// <summary>
    /// 查看图片
    /// </summary>
    OpenAnswerImg,                       // ID:140
    CloseAnswerImg,                      // ID:141
    /// <summary>
    /// 查看视频
    /// </summary>
    OpenAnswerVideo,                     // ID:142
    CloseAnswerVideo,                    // ID:143
    Max,                                 // ID:144
}

/// <summary>
/// 角色同步
/// </summary>
public enum GazeEvent
{
    Min = ExercisesModuleEvent.Max + 1,  // ID:145
    SyncCamera,                          // ID:146
    UserPose,                            // ID:147
    Max,                                 // ID:148
}

/// <summary>
/// 模型层级面板
/// </summary>
public enum HierarchyEvent
{
    Min = GazeEvent.Max + 1,             // ID:149
    Hide,                               // ID:150
    /// <summary>
    /// 展开节点
    /// </summary>
    Expand,                             // ID:151
    /// <summary>
    /// 收起节点
    /// </summary>
    Collapse,                           // ID:152
    /// <summary>
    /// 点击节点
    /// </summary>
    Click,                              // ID:153
    /// <summary>
    /// 更新节点课件资料提示
    /// </summary>
    UpdateAttachment,                   // ID:154
    Interactable,                       // ID:155
    Max,                                // ID:156
}

/// <summary>
/// 动画列表面板
/// </summary>
public enum AdaptiveListEvent
{
    Min = HierarchyEvent.Max + 1,        // ID:157
    Hide,                                // ID:158
    Select,                              // ID:159
    SelectWithoutNotify,                 // ID:160
    Max,                                 // ID:161
}

/// <summary>
/// 操作列表面板
/// </summary>
public enum OperationListEvent
{
    Min = AdaptiveListEvent.Max + 1,     // ID:162
    Open,                                // ID:163
    Show,                                // ID:164
    Hide,                                // ID:165
    Max,                                 // ID:166
}

/// <summary>
/// 协同状态同步
/// </summary>
public enum StateEvent
{
    Min = OperationListEvent.Max + 1,    // ID:167
    /// <summary>
    /// 同步版本准备操作
    /// </summary>
    PreSyncVersion,                     // ID:168
    Max,                                // ID:169
}

/// <summary>
/// 网络通道
/// </summary>
public enum NetworkChannelEvent
{
    Min = StateEvent.Max + 1,            // ID:170
    Open,                                // ID:171
    Closed,                              // ID:172
    Error,                               // ID:173
    HeartMiss,                           // ID:174
    Max,                                 // ID:175
}

/// <summary>
/// 房间通道
/// </summary>
public enum RoomChannelEvent
{
    Min = NetworkChannelEvent.Max + 1,     // ID:176
    UpdateRoomList,                       // ID:177
    JoinRoomSuccess,                      // ID:178
    JoinRoomFail,                         // ID:179
    LeaveRoom,                            // ID:180
    OtherJoin,                            // ID:181
    OtherLeave,                           // ID:182
    StartMainScreen,                      // ID:183
    UpdateMainScreen,                     // ID:184
    UpdateControl,                        // ID:185
    TalkState,                            // ID:186
    UpdateMemberList,                     // ID:187
    LiveRoomMemberModuleClose,            // ID:188
    LiveRoomMemberModuleShow,             // ID:189
    LiveRoomSettingModuleClose,           // ID:190
    /// <summary>
    /// 更新房间信息
    /// </summary>
    RoomInfo,                             // ID:191
    /// <summary>
    /// 房间解散
    /// </summary>
    RoomClose,                            // ID:192
    Max,                                  // ID:193
}

/// <summary>
/// 音视频通道
/// </summary>
public enum MediaChannelEvent
{
    Min = RoomChannelEvent.Max + 1,    // ID:194
    AddView,                           // ID:195
    RemoveView,                        // ID:196
    ClearView,                         // ID:197
    MicError,                          // ID:198
    MicOnAir,                          // ID:199
    Max,                               // ID:200
}

public enum ExamPanelEvent
{
    Min = MediaChannelEvent.Max + 1,    // ID:201
    ExerciseScore,                      // ID:202
    /// <summary>
    /// 考核状态 
    /// </summary>
    Start,                              // ID:203
    Resume,                             // ID:204
    Stop,                               // ID:205
    /// <summary>
    /// 房主计时结束
    /// </summary>
    Timeout,                            // ID:206
    /// <summary>
    /// 本地计时结束
    /// </summary>
    LocalTimeout,                       // ID:207
    /// <summary>
    /// 确保结束考核后清空状态消息
    /// </summary>
    Flush,                              // ID:208
    /// <summary>
    /// 提交成绩
    /// </summary>
    Submit,                             // ID:209
    /// <summary>
    /// 退出房间
    /// </summary>
    Quit,                               // ID:210
    Max,                                // ID:211
}

/// <summary>
/// 直播答题消息
/// </summary>
public enum JudgeOnlineEvent
{
    Min = ExamPanelEvent.Max + 1,     // ID:212
    Start,                            // ID:213
    Answer,                           // ID:214
    Complete,                         // ID:215
    End,                              // ID:216
    Max,                              // ID:217
}

public enum ShortcutEvent
{
    Min = JudgeOnlineEvent.Max + 1,   // ID:218
    /// <summary>
    /// 按下任意键 在uibase中已经添加
    /// </summary>
    PressAnyKey,                      // ID:219
    Max,                              // ID:220
}

/// <summary>
/// 操作记录列表面板
/// </summary>
public enum HistoryEvent
{
    Min = ShortcutEvent.Max + 1,      // ID:221
    Open,                             // ID:222
    Show,                             // ID:223
    Hide,                             // ID:224
    Max,                              // ID:225
}

/// <summary>
/// 枚举工具类：实现枚举值转名称
/// </summary>
public static class EnumTool
{
    /// <summary>
    /// 预注册项目中所有的事件枚举类型（新增核心配置）
    /// 后续新增枚举，只需在这里添加类型即可
    /// </summary>
    private static readonly List<Type> EventEnumTypes = new List<Type>
    {
        typeof(ModelOperateEvent),
        typeof(LoginEvent),
        typeof(OptionPanelEvent),
        typeof(SpriteTogEvent),
        typeof(ResourcesPanelEvent),
        typeof(BaikeSelectModuleEvent),
        typeof(CoursePanelEvent),
        typeof(ARModuleEvent),
        typeof(PaintEvent),
        typeof(KnowledgeModuleEvent),
        typeof(HyperLinkEvent),
        typeof(IntegrationModuleEvent),
        typeof(SmallFlowModuleEvent),
        typeof(ExercisesModuleEvent),
        typeof(GazeEvent),
        typeof(HierarchyEvent),
        typeof(AdaptiveListEvent),
        typeof(OperationListEvent),
        typeof(StateEvent),
        typeof(NetworkChannelEvent),
        typeof(RoomChannelEvent),
        typeof(MediaChannelEvent),
        typeof(ExamPanelEvent),
        typeof(JudgeOnlineEvent),
        typeof(ShortcutEvent),
        typeof(HistoryEvent)
    };

    /// <summary>
    /// 将枚举实例转换为字符串名称
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>枚举名称字符串</returns>
    public static string GetEnumName(Enum value)
    {
        return Enum.GetName(value.GetType(), value);
    }

    /// <summary>
    /// 泛型方法：根据枚举类型和数值获取枚举名称
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="value">枚举数值</param>
    /// <returns>枚举名称</returns>
    public static string GetEnumNameByValue<T>(int value) where T : Enum
    {
        if (Enum.IsDefined(typeof(T), value))
        {
            return Enum.GetName(typeof(T), value);
        }
        return "Undefined";
    }

    /// <summary>
    /// 【新增核心方法】仅通过数值，遍历所有注册的枚举类型查找匹配项
    /// </summary>
    /// <param name="enumValue">枚举数值</param>
    /// <returns>匹配结果：格式=枚举类型名.枚举值名，无匹配返回Undefined</returns>
    public static string GetEnumNameByUnknownType(int enumValue)
    {
        // 遍历所有预注册的枚举类型
        foreach (Type enumType in EventEnumTypes)
        {
            // 校验数值是否在当前枚举的定义中
            if (Enum.IsDefined(enumType, enumValue))
            {
                string enumName = Enum.GetName(enumType, enumValue);
                // 返回格式：LoginEvent.CheckVersion，可读性更强
                return $"{enumType.Name}.{enumName}";
            }
        }
        // 所有枚举都未匹配到该数值
        return "Undefined";
    }

    /// <summary>
    /// 【扩展方法】仅通过数值获取匹配的枚举类型，无匹配返回null
    /// </summary>
    public static Type GetEnumTypeByValue(int enumValue)
    {
        foreach (Type enumType in EventEnumTypes)
        {
            if (Enum.IsDefined(enumType, enumValue))
            {
                return enumType;
            }
        }
        return null;
    }
}
