using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 模型操作
/// </summary>
[System.Serializable]
public class ModelOperation : MonoBehaviour
{
    private ModelInfo modelInfo;
    public string ID
    {
        get
        {
            try
            {
                if (modelInfo == null)
                    modelInfo = GetComponent<ModelInfo>();

                if (modelInfo == null)
                    return string.Empty;
                return modelInfo.ID;
            }
            catch {  return string.Empty; }
        }
    }
    public string Name
    {
        get
        {
            if (modelInfo == null)
                modelInfo = GetComponent<ModelInfo>();

            if (modelInfo == null)
                return string.Empty;
            return modelInfo.Name;
        }
    }

    public bool HasFocusMode
    {
        get
        {
            return operations.Count > 0 && operations.Any(o => o.name.Equals(SmallFlowCtrl.focusFlag));
        }
    }
    /// <summary>
    /// 初始状态
    /// </summary>
    public string initState;
    /// <summary>
    /// 当前状态
    /// </summary>
    public string currentState;
    /// <summary>
    /// 操作表现列表
    /// </summary>
    public List<OperationBase> operations = new List<OperationBase>();

    public Dictionary<string, OperationBase> GetOperations()
    {
        var dic = new Dictionary<string, OperationBase>();
        {
            if (operations.Count > 0)
            {
                foreach (var operation in operations)
                    if (!dic.ContainsKey(operation.name))
                        dic.Add(operation.name, operation);
            }
            else
                dic.Add("无", null);

            return dic;
        }
    }
}