using UnityEngine;

/// <summary>
/// 模型基础数据
/// </summary>
public class ModelInfo : MonoBehaviour
{
    /// <summary>
    /// 唯一ID
    /// </summary>
    public string ID;
    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name;
    /// <summary>
    /// 编号
    /// </summary>
    public string Code;
    /// <summary>
    /// 道具类型
    /// </summary>
    public PropType PropType = PropType.Operate;
    /// <summary>
    /// 详细数据
    /// </summary>
    [SerializeReference]
    public ModelInfoDataBase InfoData;
}