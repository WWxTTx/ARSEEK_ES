using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 不显示状态的特殊模型名称配置
/// </summary>
[CreateAssetMenu(fileName = "HiddenStatusModelNames", menuName = "Config/HiddenStatusModelNames")]
public class HiddenStatusModelNames : ScriptableObject
{
    [Tooltip("包含这些关键词的模型名称不显示状态信息")]
    public List<string> keywords = new List<string> { "开度给定" };

    /// <summary>
    /// 检查模型名称是否应该隐藏状态
    /// </summary>
    public bool ShouldHideStatus(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return false;

        return keywords.Any(keyword => modelName.Contains(keyword));
    }
}
