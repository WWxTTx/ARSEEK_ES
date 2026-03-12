using System;
using UnityEngine.Events;

/// <summary>
/// 基础行为脚本接口
/// </summary>
public interface IBaseBehaviour
{
    //是否要执行完才继续执行下一个联动步骤
    public bool UseCallback(int step);
    /// <summary>
    /// 顺序执行
    /// </summary>
    /// <param name="callback"></param>
    void Execute(int step = 0, UnityAction callback = null);

    /// <summary>
    /// 切换到指定步骤的最终状态
    /// </summary>
    /// <param name="step">目标步骤</param>
    void SetFinalState();

    Type GetStatusEnumType();
}
