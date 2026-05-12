using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UISmallSceneOperationHistory;
using static UnityFramework.Runtime.RequestData;


public class ExamUtility : Singleton<ExamUtility>
{
    /// <summary>
    /// 记录房间成员的提交状态
    /// </summary>
    private Dictionary<int, bool> submitCache = new Dictionary<int, bool>();

    /// <summary>
    /// 参与考核成员的考试结果ID
    /// </summary>
    private Dictionary<int, int> examineeRecords = new Dictionary<int, int>();


    public List<int> Examinees
    {
        get
        {
            if (submitCache == null || submitCache.Count == 0)
                return null;
            return submitCache.Keys.ToList();
        }
    }

    public Dictionary<int, int> ExamineeRecords
    {
        get
        {
            return examineeRecords;
        }
    }

    public void InitSubmitCache(int examId, UnityAction success, UnityAction<string> failure)
    {
        RequestManager.Instance.GetExamResultList(examId, (list) =>
        {
            submitCache.Clear();
            examineeRecords.Clear();

            if (GlobalInfo.IsGroupMode())
            {
                submitCache.Add(0, false);
            }
            else
            {
                //获取指定ID考核的未结束答题的成员列表
                submitCache = list.records.Select(r => r).Where(r => !r.ended).ToDictionary(kvp => kvp.examineeId, kvp => false);
                examineeRecords = list.records.ToDictionary(kvp => kvp.examineeId, kvp => kvp.id);
            }
            success?.Invoke();
        }, (error) =>
        {
            Log.Error($"获取考核[{examId}]成员列表失败：{error}");
            failure?.Invoke(error);
        });
    }

    public void InitSubmitCache(Dictionary<int, int> records)
    {
        submitCache.Clear();
        examineeRecords.Clear();

        if (GlobalInfo.IsGroupMode())
        {
            submitCache.Add(0, false);
        }
        else
        {
            submitCache = records.ToDictionary(kvp => kvp.Key, kvp => false);
            examineeRecords = records;
        }
    }

    public int GetUserRecordId(int userId)
    {
        if (examineeRecords.TryGetValue(userId, out int recordId))
            return recordId;
        return -1;
    }

    /// <summary>
    /// 是否全员提交
    /// 房间内无其他成员视为全员提交
    /// </summary>
    /// <returns></returns>
    public bool AllSubmit()
    {
        return submitCache.Count == 0 || !submitCache.Values.Contains(false);
    }

    /// <summary>
    /// 更新考核成员提交状态
    /// </summary>
    public void UpdateSubmitCache(int userId)
    {
        if (GlobalInfo.IsGroupMode())
        {
            submitCache[0] = true;
        }
        else
        {
            if (submitCache.ContainsKey(userId))
                submitCache[userId] = true;
            else
                Log.Warning($"缓存中没有这个人 提交成绩者ID:{userId} 当前缓存{JsonTool.Serializable(submitCache)}");
        }
    }

    public void ClearSubmitCache()
    {
        submitCache.Clear();
    }

    /// <summary>
    /// 操作百科考核记录
    /// </summary>
    private Dictionary<int, Queue<ExamineResultOperation>> PediaOperationRecords = new Dictionary<int, Queue<ExamineResultOperation>>();

    /// <summary>
    /// 等待提交完成
    /// </summary>
    /// <param name="index"></param>
    /// <param name="record"></param>
    /// <param name="operateMsg"></param>
    public void EnqueueOperation(int examId, int baikeId, OpRecordData record, ExamineResultModelState[] modelStates)
    {
        if (record != null)
        {
            var operation = new ExamineResultOperation()
            {
                index = record.index,
                userNo = record.userNo,
                userName = record.userName,
                msg = record.msg,
                type = record.type,
                createTime = GlobalInfo.ServerTimeFormat,
                score = record.score,
                totalStepIndex = record.totalStepIndex,
            };

            if (PediaOperationRecords.ContainsKey(baikeId))
            {
                PediaOperationRecords[baikeId].Enqueue(operation);
            }
            else
            {
                Queue<ExamineResultOperation> operations = new Queue<ExamineResultOperation>();
                operations.Enqueue(operation);
                PediaOperationRecords.Add(baikeId, operations);
            }
        }

        //自动提交新增的操作
        SubmitExamineResult_Operation(examId, record != null ? record.score : 0, baikeId, modelStates, () =>
        {
        }, (code, msg) =>
        {
            Log.Error($"考核ID:{examId}, 百科ID：{baikeId} 保存考核记录失败：{msg}");
        }, record != null ? record.totalStepIndex : -1);
    }


    /// <summary>
    /// 保存操作百科考核记录
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="baikeId"></param>
    /// <param name="modelStates"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    /// <param name="totalStepIndex">扁平步骤索引，用于上传得分</param>
    public void SubmitExamineResult_Operation(int examId, float score, int baikeId, ExamineResultModelState[] modelStates, UnityAction success, UnityAction<int, string> failure, int totalStepIndex = -1)
    {
        ExamineResultOperation[] operations = null;
        try
        {
            if (PediaOperationRecords.ContainsKey(baikeId))
            {
               operations = PediaOperationRecords[baikeId].ToArray();
            }
            else
            {
                operations = new ExamineResultOperation[0];
            }

            RequestManager.Instance.SubmitExamineResult_Operation(examId, score, baikeId, operations, modelStates, () =>
            {
                if(PediaOperationRecords.ContainsKey(baikeId))
                    PediaOperationRecords[baikeId].Clear();
                success?.Invoke();
            }, (errorCode, errorMsg) =>
            {
                failure.Invoke(errorCode, errorMsg);
            }, totalStepIndex);
        }
        catch (Exception e)
        {
            Log.Error($"提交考核记录异常：{e}");
            failure?.Invoke(-1, e.Message);
        }
    }

    /// <summary>
    /// 保存习题百科考核记录
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="baikeId"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    public void SubmitExamineResult_Exercise(int examId, int baikeId, string operation, UnityAction success, UnityAction<int, string> failure)
    {
        try
        {
            var operations = new List<ExamineResultOperation>
            {
                new ExamineResultOperation()
                {
                    index = 0,
                    operation = operation,
                }
            };
            RequestManager.Instance.SubmitExamineResult_Excercise(examId, baikeId, operations.ToArray(), () =>
            {
                success?.Invoke();
            }, (errorCode, errorMsg) =>
            {
                failure.Invoke(errorCode, errorMsg);
            });
        }
        catch (Exception e)
        {
            Log.Error($"提交习题考核记录异常：{e}");
            failure?.Invoke(-1, e.Message);
        }
    }

    /// <summary>
    /// 取得考核记录
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    public void GetExamineResult(int examId, UnityAction<int, List<Answer>, List<Accessory>> success, UnityAction<string> failure)
    {
        RequestManager.Instance.GetExamineResult(examId, (id, answers, accessories) =>
        {
            success?.Invoke(id, answers, accessories);
        }, (error) =>
        {
            failure?.Invoke($"{error}, 考核ID:{examId}");
        });
    }

    /// <summary>
    /// 取得个人考核记录
    /// </summary>
    /// <param name="recordId"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    public void GetExamineResultByRecordId(int recordId, UnityAction<int, List<Answer>, List<Accessory>> success, UnityAction<string> failure)
    {
        RequestManager.Instance.GetExamineResultByRecordId(recordId, (id, answers, accessories) =>
        {
            success?.Invoke(id, answers, accessories);
        }, (error) =>
        {
            failure?.Invoke($"{error}, 考试结果ID:{recordId}");
        });
    }

    #region 房主考核房间缓存
    [Serializable]
    private class ExamCacheData
    {
        public int examId;
        public string endTime;
    }

    private Dictionary<int, Dictionary<string, ExamCacheData>> GetExamHistory()
    {
        try
        {
            return JsonTool.DeSerializable<Dictionary<int, Dictionary<string, ExamCacheData>>>(PlayerPrefs.GetString(GlobalInfo.lastExamId));
        }
        catch
        {
            return null;
        }
    }

    private void SaveExamHistory(Dictionary<int, Dictionary<string, ExamCacheData>> examHistory)
    {
        PlayerPrefs.SetString(GlobalInfo.lastExamId, JsonTool.Serializable(examHistory));
    }

    public int GetHostExamCache(string roomUuid)
    {
        var examHistory = GetExamHistory();
        if (examHistory != null && examHistory.TryGetValue(GlobalInfo.account.id, out var roomExams) && roomExams.TryGetValue(roomUuid, out var data))
            return data.examId;
        return -1;
    }

    public DateTime? GetHostExamEndTime(string roomUuid)
    {
        var examHistory = GetExamHistory();
        if (examHistory != null && examHistory.TryGetValue(GlobalInfo.account.id, out var roomExams) && roomExams.TryGetValue(roomUuid, out var data))
        {
            if (!string.IsNullOrEmpty(data.endTime))
                return DateTime.Parse(data.endTime);
        }
        return null;
    }

    public void SetHostExamCache(string roomUuid, int examId, DateTime? endTime = null)
    {
        var examHistory = GetExamHistory() ?? new Dictionary<int, Dictionary<string, ExamCacheData>>();
        var entry = new ExamCacheData { examId = examId, endTime = endTime?.ToString("o") };

        if (!examHistory.ContainsKey(GlobalInfo.account.id))
        {
            examHistory.Add(GlobalInfo.account.id, new Dictionary<string, ExamCacheData>() { { roomUuid, entry } });
        }
        else
        {
            if (examHistory[GlobalInfo.account.id].ContainsKey(roomUuid))
                examHistory[GlobalInfo.account.id][roomUuid] = entry;
            else
                examHistory[GlobalInfo.account.id].Add(roomUuid, entry);
        }
        SaveExamHistory(examHistory);
    }

    public void DeleteHostExamCache(string roomUuid)
    {
        var examHistory = GetExamHistory();
        if (examHistory == null)
            return;

        if (examHistory.ContainsKey(GlobalInfo.account.id))
        {
            if (examHistory[GlobalInfo.account.id].ContainsKey(roomUuid))
                examHistory[GlobalInfo.account.id].Remove(roomUuid);

            if (examHistory[GlobalInfo.account.id] == null || examHistory[GlobalInfo.account.id].Count == 0)
                examHistory.Remove(GlobalInfo.account.id);
        }
        SaveExamHistory(examHistory);
    }
    #endregion
}