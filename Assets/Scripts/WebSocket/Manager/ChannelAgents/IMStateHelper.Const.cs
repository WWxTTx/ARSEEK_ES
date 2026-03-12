using System.Collections.Generic;

public partial class IMStateHelper
{
    /// <summary>
    /// 状态消息类型
    /// </summary>
    public enum MsgStateType
    {
        /// <summary>
        /// 顺序加入opsSend
        /// </summary>
        Default,
        /// <summary>
        /// 保留最新操作
        /// </summary>
        Update,
        /// <summary>
        /// 保留最新操作并且插入到末尾
        /// </summary>
        UpdateAppend,
        /// <summary>
        /// 保留用户最新操作
        /// </summary>
        UserUpdate,
        /// <summary>
        /// 保留用户最新操作并且插入到末尾
        /// </summary>
        UserUpdateAppend,
        /// <summary>
        /// 保留用户互斥操作，界面开关等
        /// </summary>
        UserConflict,
        /// <summary>
        /// 绘图相关操作
        /// </summary>
        Paint,
        /// <summary>
        /// 拆解特殊操作
        /// </summary>
        Dismantling,
        /// <summary>
        /// 不记录
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
    /// 根据类型决定存入状态版本的方式
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
        //拆解百科 （拆分、组合走状态同步 DismantlingBaikeState）
        { (ushort)IntegrationModuleEvent.JumpToSelect, MsgStateType.Dismantling },
        //可变列表切换(选择动画)
        { (ushort)AdaptiveListEvent.Select, MsgStateType.Default },
        //动画百科
        { (ushort)IntegrationModuleEvent.AnimPlay, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AnimValue, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AnimFinish, MsgStateType.UpdateAppend },
        { (ushort)IntegrationModuleEvent.AlphaValue, MsgStateType.UpdateAppend },
        ////操作百科 syncbaikestate TODO
        //{ (ushort)SmallFlowModuleEvent.SelectFlow, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.SelectStep, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.Operate, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.MasterComputerOperate, MsgStateType.Default },
        //{ (ushort)SmallFlowModuleEvent.Input, MsgStateType.Default },
    };

    /// <summary>
    /// 根据类型决定存入状态版本的方式
    /// </summary>
    public static Dictionary<ushort, MsgStateType> MsgTypeMapExam = new Dictionary<ushort, MsgStateType>
    {
        //课程、百科切换
        { (ushort)BaikeSelectModuleEvent.BaikeSelect, MsgStateType.Default },
        //操作百科
        { (ushort)SmallFlowModuleEvent.Operate, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.MasterComputerOperate, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.Input, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.Contact, MsgStateType.Default },
        { (ushort)SmallFlowModuleEvent.OperatingRecordChange, MsgStateType.Default },
        //考核
        { (ushort)ExamPanelEvent.Start, MsgStateType.Update },
        { (ushort)ExamPanelEvent.Submit, MsgStateType.Default },
        { (ushort)ExamPanelEvent.Quit, MsgStateType.Default }
    };

    private static Dictionary<ushort, List<ushort>> MsgConflictMap = new Dictionary<ushort, List<ushort>>
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