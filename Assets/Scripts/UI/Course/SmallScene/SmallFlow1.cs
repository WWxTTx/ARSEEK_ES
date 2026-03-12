using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NavigationPoint
{
    public string Name;
    public Transform Point;
}

[System.Serializable]
public class SmallStepState
{
    /// <summary>
    /// 操作对象
    /// </summary>
    [Tooltip("操作对象")]
    public ModelOperation operation;
    /// <summary>
    /// 操作选项
    /// </summary>
    [Tooltip("操作选项")]
    public string optionName;

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}

[System.Serializable]
public class SmallStepSequenceState
{
    /// <summary>
    /// 操作对象
    /// </summary>
    [Tooltip("操作对象")]
    public ModelOperation operation;
    /// <summary>
    /// 操作选项
    /// </summary>
    [Tooltip("操作选项")]
    public string optionName;

    [Tooltip("是否等待当前行为执行完毕")]
    public bool useCallback;

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}


[System.Serializable]
public class SmallOp1
{
    /// <summary>
    /// 操作对象
    /// </summary>
    [Tooltip("操作对象")]
    public ModelOperation operation;
    /// <summary>
    /// 操作选项
    /// </summary>
    [Tooltip("操作选项")]
    public string optionName;
    /// <summary>
    /// 需选择道具
    /// </summary>
    [Tooltip("需选择道具")]
    public ModelInfo prop;
    /// <summary>
    /// 操作联动，TODO 待迁移数据后删除
    /// </summary>
    [Tooltip("操作联动")]
    public List<SmallStepSequenceState> actions = new List<SmallStepSequenceState>();

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}
public class SmallOpEqualityComparer : IEqualityComparer<SmallOp1>
{
    public bool Equals(SmallOp1 x, SmallOp1 y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;

        return x.operation == y.operation && x.optionName.Equals(y.optionName) && x.prop == y.prop;
    }

    public int GetHashCode(SmallOp1 obj)
    {
        return obj.ToString().GetHashCode();
    }
}

[System.Serializable]
public class SmallStep1
{
    /// <summary>
    /// 唯一ID跟知识点关联
    /// </summary>
    [Tooltip("步骤ID")]
    public string ID;
    /// <summary>
    /// 跟知识点关联点，VR使用
    /// </summary>
    [Tooltip("知识点显示位置")]
    public Transform knowledgeShowPoint;
    /// <summary>
    /// 跟知识点关联点，VR使用
    /// </summary>
    [Tooltip("知识点指向位置")]
    public Transform knowledgeRefPoint;
    /// <summary>
    /// 提示
    /// </summary>
    [Tooltip("提示")]
    public string hint = "提示";
    /// <summary>
    /// 完成提示,TODO暂时保留，后续不使用再删除
    /// </summary>
    [Tooltip("完成提示")]
    public string hint_success = "完成提示";
    /// <summary>
    /// 初始视角
    /// </summary>
    [Tooltip("初始视角")]
    public List<SmallStepState> initState = new List<SmallStepState>();
    /// <summary>
    /// 道具状态限制
    /// </summary>
    [Tooltip("道具状态限制")]
    public List<SmallStepState> conditions = new List<SmallStepState>();
    /// <summary>
    /// 并列操作集合
    /// </summary>
    [Tooltip("并列操作集合")]
    public List<SmallOp1> ops = new List<SmallOp1>();


#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}

public class SmallFlow1 : MonoBehaviour
{
    /// <summary>
    /// 唯一ID
    /// </summary>
    [Tooltip("任务ID")]
    public string ID;
    /// <summary>
    /// 任务名称
    /// </summary>
    [Tooltip("任务名称")]
    public string flowName;
  
    /// <summary>
    /// 标准操作步骤集合
    /// </summary>
    [Tooltip("标准操作流程")]
    public List<SmallStep1> steps = new List<SmallStep1>();
}