using System.Collections.Generic;

public partial class IMStateHelper
{
    /// <summary>
    /// 状态信息类型
    /// </summary>
    public enum MsgStateType
    {
        /// <summary>
        /// 顺序调用opsSend
        /// </summary>
        Default,
        /// <summary>
        /// 服务端更新
        /// </summary>
        Update,
        /// <summary>
        /// 服务端更新且追加到末尾
        /// </summary>
        UpdateAppend,
        /// <summary>
        /// 本地用户更新
        /// </summary>
        UserUpdate,
        /// <summary>
        /// 本地用户更新且追加到末尾
        /// </summary>
        UserUpdateAppend,
        /// <summary>
        /// 本地用户冲突，强制重载
        /// </summary>
        UserConflict,
        /// <summary>
        /// 绘图相关操作
        /// </summary>
        Paint,
        /// <summary>
        /// 百科拆解状态
        /// </summary>
        Dismantling,
        /// <summary>
        /// 空事件
        /// </summary>
        NoneState,
    }

    private static List<ushort> MsgTruncate = new List<ushort>()
    {
        (ushort)CoursePanelEvent.SwitchResource,
        (ushort)BaikeSelectModuleEvent.BaikeSelect
    };
    private static List<ushort> MsgTruncateIndependent = new List<ushort>()
    {
        (ushort)CoursePanelEvent.SwitchResource,
        (ushort)PaintEvent.PaintArea
    };

    private static List<ushort> MsgTruncateExam = new List<ushort>()
    {
        (ushort)ExamPanelEvent.Stop,
        (ushort)ExamPanelEvent.Timeout,
        (ushort)ExamPanelEvent.Flush
    };

    private static List<ushort> MsgTruncateIndependentExam = new List<ushort>()
    {
        //(ushort)ExamPanelEvent.Start
    };

    private static List<ushort> MsgTruncateExamGroup = new List<ushort>()
    {
        (ushort)BaikeSelectModuleEvent.BaikeSelect
    };

    private static List<ushort> MsgTruncateIndependentExamGroup = new List<ushort>()
    {
        (ushort)ExamPanelEvent.Start
    };

    /// <summary>
    /// 消息类型定义状态版本的方式
    /// </summary>
    public static Dictionary<ushort, MsgStateType> MsgTypeMap = new Dictionary<ushort, MsgStateType>
    {
        //画笔
        { (ushort)PaintEvent.SyncPaint, MsgStateType.Paint },
        { (ushort)PaintEvent.SyncUndo, MsgStateType.Paint },
        { (ushort)PaintEvent.SyncReset, MsgStateType.Paint },
        //课程、百科切换
        { (ushort)CoursePanelEvent.SwitchResource, MsgStateType.Update },
        { (ushort)BaikeSelectModuleEvent.BaikeSelect, MsgStateType.Update },
        //百科 跳转帧动画状态同步 DismantlingBaikeState）
        { (ushort)IntegrationModuleEvent.JumpToSelect, MsgStateType.Dismantling },
        //自适应列表切换(选择动画)
        { (ushort)AdaptiveListEvent.Select, MsgStateType.Default },
        //集成百科
        { (ushort)IntegrationModuleEvent.AnimPlay, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AnimValue, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AnimFinish, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AlphaValue, MsgStateType.UpdateAppend },
        ////集成百科 syncbaikestate TODO
        { (ushort)SmallFlowModuleEvent.SelectFlow, MsgStateType.Update },
        { (ushort)SmallFlowModuleEvent.SelectStep, MsgStateType.Update },
        //{ (ushort)SmallFlowModuleEvent.Operate, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.MasterComputerOperate, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.Input, MsgStateType.Default },
    };

    /// <summary>
    /// 消息类型定义状态版本的方式
    /// </summary>
    public static Dictionary<ushort, MsgStateType> MsgTypeMapExam = new Dictionary<ushort, MsgStateType>
    {
        //课程、百科切换
        { (ushort)BaikeSelectModuleEvent.BaikeSelect, MsgStateType.Default },
        //集成百科
        { (ushort)SmallFlowModuleEvent.Operate, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.MasterComputerOperate, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.Input, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.Contact, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.OperatingRecordChange, MsgStateType.Default },
        //考试
        { (ushort)ExamPanelEvent.Start, MsgStateType.Update },
        { (ushort)ExamPanelEvent.Submit, MsgStateType.Default },
        { (ushort)ExamPanelEvent.Quit, MsgStateType.Default }
    };

    private static Dictionary<ushort, List<ushort>> MsgMap = new Dictionary<ushort, List<ushort>>
    {
        //{ (ushort)IntegrationModuleEvent.UnCheck,
        //    new List<ushort>()
        //    {               
        //        (ushort)IntegrationModuleEvent.Check,
        //        (ushort)IntegrationModuleEvent.AnimSelect,
        //        (ushort)IntegrationModuleEvent.AnimPlay,
        //        (ushort)IntegrationModuleEvent.AnimValue,
        //        (ushort)IntegrationModuleEvent.AlphaValue,
        //    }
        //}
    };
}
