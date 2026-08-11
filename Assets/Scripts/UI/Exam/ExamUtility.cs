using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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

    public Dictionary<int, int> ExamineeRecords
    {
        get
        {
            return examineeRecords;
        }
    }

    /// <summary>
    /// 初始化提交缓存，考生由房主在开始重连时传递，房主从 InitExamRecord 响应获取
    /// </summary>
    /// <param name="records">examineeId → recordId 映射</param>
    public void InitSubmitCache(Dictionary<int, int> records)
    {
        submitCache.Clear();
        examineeRecords.Clear();

        if (records == null || records.Count == 0)
            return;

        submitCache = records.ToDictionary(kvp => kvp.Key, kvp => false);
        examineeRecords = records;
    }

    /// <summary>
    /// 从服务端考核列表更新考生记录，保留已有的提交状态（IM消息积累的提交状态优先级高于API）。
    /// examineeRecords 由服务端数据覆盖（recordId 以服务端为准）。
    /// </summary>
    /// <param name="records">服务端返回的考核记录列表</param>
    public void InitSubmitCacheWithStatus(List<RequestData.ExamResult> records)
    {
        examineeRecords.Clear();

        if (records == null || records.Count == 0)
            return;

        foreach (var r in records)
        {
            examineeRecords[r.examineeId] = r.id;
            if (!submitCache.ContainsKey(r.examineeId))
                submitCache[r.examineeId] = r.examineTime > 0;
        }
    }

    /// <summary>
    /// 单人考核由各自的recordId
    /// 多人公用一个
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public int GetUserRecordId(int userId)
    {
        if (GlobalInfo.courseMode == CourseMode.Exam)
        {
            if (examineeRecords.TryGetValue(userId, out int recordId))
                return recordId;
        }
        else
            return examineeRecords.Values.ToArray()[0];
        
        return 0;
    }

    /// <summary>
    /// 检查指定用户是否已提交考核
    /// </summary>
    public bool HasSubmitted(int userId)
    {
        return submitCache.TryGetValue(userId, out bool submitted) && submitted;
    }

    /// <summary>
    /// 是否全员提交。
    /// 单人考核以 examineeRecords 为准等待掉线考生重连提交；
    /// 其余情况沿用房间成员列表判断，成员空了即结束。
    /// </summary>
    public bool AllSubmit()
    {
        if (!GlobalInfo.IsGroupMode())
        {
            if (examineeRecords.Count > 0)
            {
                foreach (var examineeId in examineeRecords.Keys)
                {
                    if (!HasSubmitted(examineeId))
                        return false;
                }
                return true;
            }
            // examineeRecords 尚未填充（异步回调未完成），不可断定全员提交
            return false;
        }

        return submitCache.Count == 0 || !submitCache.Values.Contains(false);
    }

    /// <summary>
    /// 更新考核成员提交状态
    /// </summary>
    public void UpdateSubmitCache(int userId)
    {
        if (GlobalInfo.IsGroupMode())
        {
            List<int> k = submitCache.Keys.ToList();
            for (int i = 0; i < k.Count; i++)
            {
                submitCache[k[i]] = true;
            }
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
        examineeRecords.Clear();
    }

    /// <summary>
    /// 未提交考生ID列表，用于日志与UI显示
    /// </summary>
    public List<int> GetPendingExaminees()
    {
        return examineeRecords.Keys.Where(id => !HasSubmitted(id)).ToList();
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
        // 跳过空操作者的记录（如 RefreshOpHistory 步骤跳转时的历史记录重建）
        if (record != null && string.IsNullOrEmpty(record.userNo))
        {
            Log.Debug($"跳过空操作者记录提交");
            return;
        }

        bool isCurrentUser = record != null && record.userNo.Equals(GlobalInfo.account.userNo);

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

            // 所有操作都添加到队列，确保上传数据与UI显示同步
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

        // 仅上传当前用户的操作分数，避免其他用户的操作分数被错误上传到本机记录
        float uploadScore = isCurrentUser ? (record != null ? record.score : 0) : 0;
        int uploadStepIndex = isCurrentUser ? (record != null ? record.totalStepIndex : -1) : -1;

        //自动提交新增的操作
        SubmitExamineResult_Operation(examId, uploadScore, baikeId, modelStates, () =>
        {
        }, (code, msg) =>
        {
            Log.Error($"考核ID:{examId}, 百科ID：{baikeId} 保存考核记录失败：{msg}");
        }, uploadStepIndex);
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
    public void SubmitExamineResult_Exercise(int examId, int baikeId, string operation, string msg, float score, UnityAction success, UnityAction<int, string> failure)
    {
        try
        {
            var operations = new List<ExamineResultOperation>
            {
                new ExamineResultOperation()
                {
                    index = 0,
                    userNo = GlobalInfo.account.userNo,
                    userName = GlobalInfo.account.nickname,
                    msg = msg,
                    operation = operation,
                    score = score
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
        public Dictionary<int, int> examineeRecords;
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

    public Dictionary<int, int> GetHostExamExamineeRecords(string roomUuid)
    {
        var examHistory = GetExamHistory();
        if (examHistory != null && examHistory.TryGetValue(GlobalInfo.account.id, out var roomExams) && roomExams.TryGetValue(roomUuid, out var data))
            return data.examineeRecords;
        return null;
    }

    public void SetHostExamCache(string roomUuid, int examId, DateTime? endTime = null, Dictionary<int, int> examineeRecords = null)
    {
        var examHistory = GetExamHistory() ?? new Dictionary<int, Dictionary<string, ExamCacheData>>();
        var entry = new ExamCacheData { examId = examId, endTime = endTime?.ToString("o"), examineeRecords = examineeRecords };

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

    #region 参与者考核房间缓存
    /// <summary>
    /// 参与者进入考核时缓存 examId、endTime 和 examineeRecords，用于异常退出后自动重连
    /// </summary>
    public void SetParticipantExamCache(string roomUuid, int examId, DateTime endTime, Dictionary<int, int> examineeRecords = null)
    {
        SetHostExamCache(roomUuid, examId, endTime, examineeRecords);
    }

    /// <summary>
    /// 获取参与者缓存的考核ID
    /// </summary>
    public int GetParticipantExamId(string roomUuid)
    {
        return GetHostExamCache(roomUuid);
    }

    /// <summary>
    /// 获取参与者缓存的考核结束时间
    /// </summary>
    public DateTime? GetParticipantExamEndTime(string roomUuid)
    {
        return GetHostExamEndTime(roomUuid);
    }

    /// <summary>
    /// 获取参与者缓存的考生记录映射
    /// </summary>
    public Dictionary<int, int> GetParticipantExamExamineeRecords(string roomUuid)
    {
        var examHistory = GetExamHistory();
        if (examHistory != null && examHistory.TryGetValue(GlobalInfo.account.id, out var roomExams) && roomExams.TryGetValue(roomUuid, out var data))
            return data.examineeRecords;
        return null;
    }

    /// <summary>
    /// 删除参与者考核缓存（考核正常结束时调用）
    /// </summary>
    public void DeleteParticipantExamCache(string roomUuid)
    {
        DeleteHostExamCache(roomUuid);
    }
    #endregion

    #region 习题答案缓存
    /// <summary>
    /// 习题答案缓存（考核模式下切换百科后还原已选答案）
    /// key: wikiId, value: 已选答案索引列表
    /// </summary>
    private Dictionary<int, List<int>> exerciseAnswerCache = new Dictionary<int, List<int>>();

    public void SetExerciseAnswer(int wikiId, List<int> answers)
    {
        exerciseAnswerCache[wikiId] = new List<int>(answers);
    }

    public List<int> GetExerciseAnswer(int wikiId)
    {
        exerciseAnswerCache.TryGetValue(wikiId, out var answers);
        return answers;
    }

    public void ClearExerciseAnswer(int wikiId)
    {
        exerciseAnswerCache.Remove(wikiId);
    }

    public void ClearAllExerciseAnswers()
    {
        exerciseAnswerCache.Clear();
    }
    #endregion
}