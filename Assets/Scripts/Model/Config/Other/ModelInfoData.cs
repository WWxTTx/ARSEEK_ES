using UnityEngine;

/// <summary>
/// 道具类型
/// </summary>
[System.Serializable]
public enum PropType
{
    /// <summary>
    /// 锚点
    /// </summary>
    Anchor,
    /// <summary>
    /// 通用道具
    /// </summary>
    Operate,
    /// <summary>
    /// 背包道具
    /// </summary>
    BackPack,
    /// <summary>
    /// 校准器
    /// </summary>
    Calibrator,
    /// <summary>
    /// 动画
    /// </summary>
    Animation,
    /// <summary>
    /// 地图Size
    /// </summary>
    Map,
    /// <summary>
    /// 背包原件
    /// </summary>
    BackPack_Original,
    /// <summary>
    /// 自由触发
    /// 不需要配置到流程中，不计入操作记录，不判断正确性
    /// </summary>
    Free,
    /// <summary>
    /// 上位机
    /// </summary>
    MasterComputer,
    /// <summary>
    /// 全局视角
    /// </summary>
    GlobalPerspective,
    /// <summary>
    /// 自动触发
    /// </summary>
    Auto,
    /// <summary>
    /// 安全工器具
    /// </summary>
    SafetyTool,
    /// <summary>
    /// 图纸
    /// </summary>
    Schematics
}

/// <summary>
/// 触发方式
/// </summary>
[System.Serializable]
public enum InteractMode
{
    /// <summary>
    /// 无交互
    /// </summary>
    None,
    /// <summary>
    /// 点击
    /// </summary>
    Click,
    /// <summary>
    /// 开关
    /// </summary>
    Switch,
    /// <summary>
    /// 范围
    /// </summary>
    Range,
    /// <summary>
    /// 看
    /// </summary>
    Look,
    /// <summary>
    /// 菜单
    /// </summary>
    Menu,
    /// <summary>
    /// 对准
    /// </summary>
    Aim,
    /// <summary>
    /// 操作UI
    /// </summary>
    OpUI,
    /// <summary>
    /// 菜单选择UI
    /// </summary>
    ListUI,
    /// <summary>
    /// 2D点击
    /// </summary>
    Click2D,
    /// <summary>
    /// 2D下拉选择 (开关、挡位等)
    /// </summary>
    Menu2D
}

[System.Serializable]
public class ModelInfoDataBase
{
    /// <summary>
    /// 显示信息
    /// </summary>
    //public string Info;

    /// <summary>
    /// 触发方式
    /// </summary>
    public InteractMode InteractMode = InteractMode.Click;
    [SerializeReference]
    public InteractDataBase interactData;
}

public class ModelInfo_BackPack : ModelInfoDataBase
{
    /// <summary>
    /// 道具图标
    /// </summary>
    public Sprite Icon;
    /// <summary>
    /// 道具选中显示图片
    /// </summary>
    public Sprite Icon2D;
    /// <summary>
    /// 是否初始在背包内
    /// </summary>
    public bool InBackPack;
    /// <summary>
    /// 抓取位置
    /// </summary>
    public Transform GrabPointTrans;
}

public class ModelInfo_BackPackOriginal : ModelInfoDataBase
{
    /// <summary>
    /// 原件图标
    /// </summary>
    public Sprite Icon;
    /// <summary>
    /// 道具选中显示图片
    /// </summary>
    public Sprite Icon2D;
    /// <summary>
    /// 原件数量
    /// </summary>
    public int num;
    /// <summary>
    /// 是否初始在背包内
    /// </summary>
    public bool InBackPack;
    /// <summary>
    /// 抓取位置
    /// </summary>
    public Transform GrabPointTrans;
}

public class ModelInfo_SafetyTool : ModelInfoDataBase
{
    /// <summary>
    /// 道具图标
    /// </summary>
    public Sprite Icon;
    /// <summary>
    /// 道具穿戴图片
    /// </summary>
    public Sprite Icon2D;
    /// <summary>
    /// 是否初始在背包内
    /// </summary>
    public bool InBackPack;
}

[System.Serializable]
public class InteractDataBase { }

public class OpUIData : InteractDataBase
{
    /// <summary>
    /// 操作UI
    /// </summary>
    public Transform content;
    /// <summary>
    /// 目标物体
    /// </summary>
    public Transform targetObject;
}