using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityFramework.Runtime;

/// <summary>
/// 考核缓存工具类
/// </summary>
public class ExamUtils1
{
    /// <summary>
    /// 初始化考核数据
    /// </summary>
    /// <param name="key">缓存key</param>
    /// <param name="msgExamStart"></param>
    /// <param name="examData"></param>
    public static void InitExamData(string key, int examId, DateTime startTime, DateTime endTime, ref ExamData examData)
    {
        var cache = LoadCachedData(key);
        if (cache.ContainsKey(examId))
        {
            examData = cache[examId];
        }
        else
        {
            examData = new ExamData();
            {
                examData.examId = examId;
                foreach (var encyclopedia in GlobalInfo.currentWikiList)
                {
                    //这里有问题 所有的typeid都是7 但操作题应该是6 所以这么判断
                    examData.state.Add(encyclopedia.id, new ExamEncyclopediaInfo()
                    {
                        id = encyclopedia.id,
                        type = encyclopedia.typeDescription == "操作题" ? 6 : 7
                    });
                }
            }
        }
        examData.startTime = startTime;
        examData.endTime = endTime;
        SaveExamData(key, examData);
    }

    public static void InitExamData(string key, int examId, ref ExamData examData)
    {
        var cache = LoadCachedData(key);
        if (cache.ContainsKey(examId))
        {
            examData = cache[examId];
        }
    }

    /// <summary>
    /// 保存考核数据
    /// </summary>
    /// <param name="key"></param>
    public static void SaveExamData(string key, ExamData examData)
    {
        var cache = LoadCachedData(key);
        if (cache.ContainsKey(examData.examId))
            cache[examData.examId] = examData;
        else
            cache.Add(examData.examId, examData);

        PlayerPrefs.SetString(key, JsonTool.Serializable(cache));
    }

    /// <summary>
    /// 读取缓存的json
    /// </summary>
    /// <returns></returns>
    private static Dictionary<int, ExamData> LoadCachedData(string key)
    {
        var cache = new Dictionary<int, ExamData>();
        try
        {
            cache = JsonTool.DeSerializable<Dictionary<int, ExamData>>(PlayerPrefs.GetString(key));
            if (cache == null)
            {
                cache = new Dictionary<int, ExamData>();
            }
        }
        catch (Exception e)
        {
            cache = new Dictionary<int, ExamData>();
            Debug.LogError($"缓存转换失败\n原文:{PlayerPrefs.GetString(key)}\n原因{e}");
        }

        var time = GlobalInfo.ServerTime;
        foreach (var item in cache.Where(value => value.Value.endTime < time).ToList())
        {
            cache.Remove(item.Key);
        }
        return cache;
    }

    /// <summary>
    /// 加载本地配置分值
    /// </summary>
    /// <returns></returns>
    public static IEnumerator LoadTempScore(UnityAction<Dictionary<int, List<int>>> callback = null)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{Application.streamingAssetsPath}/TempJson.json"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    callback?.Invoke(JsonTool.DeSerializable<Dictionary<int, List<int>>>(request.downloadHandler.text));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }

    public class ExamData
    {
        /// <summary>
        /// 考核记录Id
        /// </summary>
        public int examId;
        /// <summary>
        /// 是否已提交
        /// </summary>
        public bool submit;
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime startTime;
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime endTime;
        /// <summary>
        /// 考核用时(秒) 小组以教师端时间为准
        /// </summary>
        public int duration;

        /// <summary>
        /// 百科列表
        /// </summary>
        public Dictionary<int, ExamEncyclopediaInfo> state = new Dictionary<int, ExamEncyclopediaInfo>();
    }

    public class ExamEncyclopediaInfo
    {
        /// <summary>
        /// 百科ID
        /// </summary>
        public int id;
        /// <summary>
        /// 百科类型
        /// </summary>
        public int type;

        /// <summary>
        /// 小场景成绩
        /// </summary>
        public Dictionary<string, ScoreDetail> score = new Dictionary<string, ScoreDetail>();
        /// <summary>
        /// 获取成绩
        /// </summary>
        /// <returns></returns>
        public int GetScore()
        {
            return score.Sum(kvp => (kvp.Value.correct ?? false) ? kvp.Value.value : 0);
        }
        /// <summary>
        /// 获取正确数量
        /// </summary>
        /// <returns></returns>
        public int GetCorrect()
        {
            return score.Where(kvp => kvp.Value.correct ?? false).Count();
        }
        /// <summary>
        /// 获取错误数量
        /// </summary>
        /// <returns></returns>
        public int GetWrong()
        {
            return score.Where(kvp => !kvp.Value.correct ?? true).Count();
        }

        /// <summary>
        /// 消息Json
        /// </summary>
        public List<MsgJson> msgJson = new List<MsgJson>();
        /// <summary>
        /// 同步消息
        /// </summary>
        public void SyncMsg()
        {
            MsgBase msgBase;

            foreach (var msg in msgJson)
            {
                msgBase = (MsgBase)Newtonsoft.Json.JsonConvert.DeserializeObject(msg.json, msg.type);
                Log.Debug($"同步消息 {msgBase.msgId} {msg.json}");
                FormMsgManager.Instance.SendMsg(msgBase);
            }
        }
        /// <summary>
        /// 同步消息顺序执行
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IEnumerator _syncMsg(UnityAction callback)
        {
            if (msgJson.Count == 0)
            {
                callback?.Invoke();
                yield break;
            }

            MsgBase msgBase;
            WaitForSeconds waitForSeconds = new WaitForSeconds(0.02f);

            for (int i = 0; i < msgJson.Count; i++)
            {
                msgBase = (MsgBase)Newtonsoft.Json.JsonConvert.DeserializeObject(msgJson[i].json, msgJson[i].type);
                FormMsgManager.Instance.SendMsg(msgBase);

                yield return waitForSeconds;
                Log.Debug($"同步消息 {msgBase.msgId} {msgJson[i].json}");

                if (i >= msgJson.Count - 1)
                    callback?.Invoke();
            }
        }

        public void AddMsg(MsgBase msgBase)
        {
            msgJson.Add(new MsgJson()
            {
                type = msgBase.GetType(),
                json = JsonTool.Serializable(msgBase)
            });
        }

        public class MsgJson
        {
            public Type type;
            public string json;
        }

    }

    /// <summary>
    /// 成绩详情
    /// </summary>
    public class ScoreDetail
    {
        /// <summary>
        /// 成绩值
        /// </summary>
        public int value;
        /// <summary>
        /// 是否答对
        /// </summary>
        public bool? correct;
        public ScoreDetail(int value)
        {
            this.value = value;
            correct = null;
        }
    }
}