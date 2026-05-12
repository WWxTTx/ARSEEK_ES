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
    Min = IntegrationModuleEvent.Max + 1,    // ID:101
    /// <summary>
    /// 左侧ui活动
    /// </summary>
    LeftFlex,                                // ID:102
    /// <summary>
    /// 右侧ui活动
    /// </summary>
    RightFlex,                               // ID:103
    /// <summary>
    /// 选择任务
    /// </summary>
    SelectFlow,                              // ID:104
    /// <summary>
    /// 选择步骤
    /// </summary>
    SelectStep,                              // ID:105
    /// <summary>
    /// 选择下一步骤
    /// </summary>
    NextStep,                                // ID:106
    /// <summary>
    /// 选择工具
    /// </summary>
    SelectTool,                              // ID:107
    /// <summary>
    /// 切换工具箱道具
    /// </summary>
    ChangeProp,                              // ID:108
    /// <summary>
    /// 聚焦对象变化 修改操作权限占用
    /// </summary>
    FocusChanged,                            // ID:109
    /// <summary>
    /// 观察
    /// </summary>
    ClickObj,                                // ID:110
    Look2D,                                  // ID:111
    /// <summary>
    /// 操作
    /// </summary>
    Operate,                                 // ID:112
    /// <summary>
    /// 上位机选中
    /// </summary>
    MasterComputerSelect,                    // ID:113
    /// <summary>
    /// 上位机操作
    /// </summary>
    MasterComputerOperate,                   // ID:114
    /// <summary>
    /// 输入操作
    /// </summary>
    Input,                                   // ID:115
    /// <summary>
    /// 联系操作
    /// </summary>
    Contact,                                 // ID:116
    /// <summary>
    /// 操作开始执行
    /// </summary>
    StartExecute,                            // ID:117
    /// <summary>
    /// 同步关闭弹窗
    /// </summary>
    ClousePop,                               // ID:118
    /// <summary>
    /// 操作执行完成
    /// </summary>
    CompleteExecute,                         // ID:119
    /// <summary>
    /// 步骤完成
    /// </summary>
    CompleteStep,                            // ID:120
    /// <summary>
    /// 当前任务全部步骤完成
    /// </summary>
    CompleteAll,                             // ID:121
    /// <summary>
    /// 关闭监控
    /// </summary>
    HideMonitor,                             // ID:122
    UIHighlight,                             // ID:123
    /// <summary>
    /// 操作记录
    /// </summary>
    OperatingRecord,                         // ID:124
    /// <summary>
    /// 输入操作记录
    /// </summary>
    OperatingRecordInput,                    // ID:125
    /// <summary>
    /// 修改输入操作记录
    /// </summary>
    OperatingRecordChange,                   // ID:126
    /// <summary>
    /// 清除记录
    /// </summary>
    OperatingRecordClear,                    // ID:127
    /// <summary>
    /// 小地图最大化
    /// </summary>
    MaxMap,                                  // ID:128
    /// <summary>
    /// 输入操作记录
    /// </summary>
    SelectInput,                             // ID:129
    /// <summary>
    /// 联系
    /// </summary>
    SelectContact,                           // ID:130
    /// <summary>
    /// 执行2D操作
    /// </summary>
    Operate2D,                               // ID:131
    ShowUIOperation,                         // ID:132
    /// <summary>
    /// 道具状态
    /// </summary>
    OpState,                                 // ID:133
    /// <summary>
    /// 关闭相机操作
    /// </summary>
    CloseCameraOperation,                    // ID:134
    /// <summary>
    /// 打开相机操作
    /// </summary>
    OpenCameraOperation,                     // ID:135
    /// <summary>
    /// 视角引导
    /// </summary>
    Guide,                                   // ID:136
    /// <summary>
    /// 系统记录
    /// </summary>
    SystemRecord,                            // ID:137
    /// <summary>
    /// 工具栏显示
    /// </summary>
    ShowTool,                                // ID:138
    /// <summary>
    /// 释放操作权限
    /// </summary>
    ReleasePermission,                       // ID:139
    /// <summary>
    /// TSQ_TsqXsp 按钮事件同步
    /// </summary>
    SynchronizationTsq,                      // ID:140
    /// <summary>
    /// LCU_mlfsjs 按钮事件同步
    /// </summary>
    SynchronizationLcu,                      // ID:141
    /// <summary>
    /// LC_Zlqzz 按钮事件同步
    /// </summary>
    SynchronizationZlqzz,                    // ID:142
    Max,                                     // ID:143
}

/// <summary>
/// 习题百科
/// </summary>
public enum ExercisesModuleEvent
{
    Min = SmallFlowModuleEvent.Max + 1,  // ID:144
    /// <summary>
    /// 选择答案
    /// </summary>
    ChooseAnswer,                        // ID:145
    /// <summary>
    /// 查看答案
    /// </summary>
    ConfirmAnswer,                       // ID:146
    /// <summary>
    /// 查看图片
    /// </summary>
    OpenAnswerImg,                       // ID:147
    CloseAnswerImg,                      // ID:148
    /// <summary>
    /// 查看视频
    /// </summary>
    OpenAnswerVideo,                     // ID:149
    CloseAnswerVideo,                    // ID:150
    Max,                                 // ID:151
}

/// <summary>
/// 角色同步
/// </summary>
public enum GazeEvent
{
    Min = ExercisesModuleEvent.Max + 1,  // ID:152
    SyncCamera,                          // ID:153
    UserPose,                            // ID:154
    Max,                                 // ID:155
}

/// <summary>
/// 模型层级面板
/// </summary>
public enum HierarchyEvent
{
    Min = GazeEvent.Max + 1,             // ID:156
    Hide,                                // ID:157
    /// <summary>
    /// 展开节点
    /// </summary>
    Expand,                              // ID:158
    /// <summary>
    /// 收起节点
    /// </summary>
    Collapse,                            // ID:159
    /// <summary>
    /// 点击节点
    /// </summary>
    Click,                               // ID:160
    /// <summary>
    /// 更新节点课件资料提示
    /// </summary>
    UpdateAttachment,                    // ID:161
    Interactable,                        // ID:162
    Max,                                 // ID:163
}

/// <summary>
/// 动画列表面板
/// </summary>
public enum AdaptiveListEvent
{
    Min = HierarchyEvent.Max + 1,        // ID:163
    Hide,                                // ID:164
    Select,                              // ID:165
    SelectWithoutNotify,                 // ID:166
    Max,                                 // ID:167
}

/// <summary>
/// 操作列表面板
/// </summary>
public enum OperationListEvent
{
    Min = AdaptiveListEvent.Max + 1,     // ID:168
    Open,                                // ID:169
    Show,                                // ID:170
    Hide,                                // ID:171
    Max,                                 // ID:172
}

/// <summary>
/// 协同状态同步
/// </summary>
public enum StateEvent
{
    Min = OperationListEvent.Max + 1,    // ID:173
    /// <summary>
    /// 同步版本准备操作
    /// </summary>
    PreSyncVersion,                      // ID:174
    Max,                                 // ID:175
}

/// <summary>
/// 网络通道
/// </summary>
public enum NetworkChannelEvent
{
    Min = StateEvent.Max + 1,            // ID:176
    Open,                                // ID:177
    Closed,                              // ID:178
    Error,                               // ID:179
    HeartMiss,                           // ID:180
    Max,                                 // ID:181
}

/// <summary>
/// 房间通道
/// </summary>
public enum RoomChannelEvent
{
    Min = NetworkChannelEvent.Max + 1,     // ID:182
    UpdateRoomList,                       // ID:183
    JoinRoomSuccess,                      // ID:184
    JoinRoomFail,                         // ID:185
    LeaveRoom,                            // ID:186
    OtherJoin,                            // ID:187
    OtherLeave,                           // ID:188
    StartMainScreen,                      // ID:189
    UpdateMainScreen,                     // ID:190
    UpdateControl,                        // ID:191
    TalkState,                            // ID:192
    UpdateMemberList,                     // ID:193
    LiveRoomMemberModuleClose,            // ID:194
    LiveRoomMemberModuleShow,             // ID:195
    LiveRoomSettingModuleClose,           // ID:196
    /// <summary>
    /// 更新房间信息
    /// </summary>
    RoomInfo,                             // ID:197
    /// <summary>
    /// 房间解散
    /// </summary>
    RoomClose,                            // ID:198
    /// <summary>
    /// 成员断连（可能异常退出，后续可能重进）
    /// </summary>
    OtherDisconnect,                      // ID:199
    Max,                                  // ID:200
}

/// <summary>
/// 音视频通道
/// </summary>
public enum MediaChannelEvent
{
    Min = RoomChannelEvent.Max + 1,       // ID:201
    AddView,                              // ID:202
    RemoveView,                           // ID:203
    ClearView,                            // ID:204
    MicError,                             // ID:205
    MicOnAir,                             // ID:206
    Max,                                  // ID:207
}

public enum ExamPanelEvent
{
    Min = MediaChannelEvent.Max + 1,      // ID:208
    ExerciseScore,                        // ID:209
    /// <summary>
    /// 考核状态
    /// </summary>
    Start,                                // ID:210
    Resume,                               // ID:211
    Stop,                                 // ID:212
    /// <summary>
    /// 房主计时结束
    /// </summary>
    Timeout,                              // ID:213
    /// <summary>
    /// 本地计时结束
    /// </summary>
    LocalTimeout,                         // ID:214
    /// <summary>
    /// 确保结束考核后清空状态消息
    /// </summary>
    Flush,                                // ID:215
    /// <summary>
    /// 提交成绩
    /// </summary>
    Submit,                               // ID:216
    /// <summary>
    /// 退出房间
    /// </summary>
    Quit,                                 // ID:217
    Max,                                  // ID:218
}

/// <summary>
/// 直播答题消息
/// </summary>
public enum JudgeOnlineEvent
{
    Min = ExamPanelEvent.Max + 1,         // ID:219
    Start,                                // ID:220
    Answer,                               // ID:221
    Complete,                             // ID:222
    End,                                  // ID:223
    Max,                                  // ID:224
}

public enum ShortcutEvent
{
    Min = JudgeOnlineEvent.Max + 1,       // ID:225
    /// <summary>
    /// 按下任意键 在uibase中已经添加
    /// </summary>
    PressAnyKey,                          // ID:226
    Max,                                  // ID:227
}

/// <summary>
/// 操作记录列表面板
/// </summary>
public enum HistoryEvent
{
    Min = ShortcutEvent.Max + 1,          // ID:228
    Open,                                 // ID:229
    Show,                                 // ID:230
    Hide,                                 // ID:231
    Max,                                  // ID:232
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
