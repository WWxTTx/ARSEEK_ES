using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 课程模式类别
/// </summary>
public enum CourseMode
{
    /// <summary>
    /// 培训模式（默认）
    /// </summary>
    Training = 0,
    /// <summary>
    /// 联机 单人操作 直播
    /// </summary>
    Livebroadcast = 1,
    /// <summary>
    /// 联机 多人操作 协同
    /// </summary>
    Collaboration = 2,
    /// <summary>
    /// 考核模式
    /// </summary>
    Exam = 3,
    /// <summary>
    /// 多人考核模式
    /// </summary>
    OnlineExam = 4
}

public class GlobalInfo
{
    #region 常量
    public const string commonVersion = "commonVersion";
    public const string commonAccount = "commonAccount";
    public const string accountCacheKey = "account";
    public const string passwordCacheKey = "password";
    public const string qualityCacheKey = "quality";
    public const string themeCacheKey = "theme";
    public const string appLoginBgCacheKey = "appLoginBg";
    public const string appLoginLogoCacheKey = "appLoginLogo";
    public const string isFirstRunCacheKey = "is_first_run";
    public const string savePasswordKey = "savePassword";
    public const string fileSavePathCacheKey = "fileSavePath";
    /// <summary>
    /// 协同房间缓存
    /// </summary>
    public const string lastSynergiaRoomId = "synergiaRoomId";
    /// <summary>
    /// 房主考核记录Id缓存
    /// </summary>
    public const string lastExamId = "examId";
    /// <summary>
    /// 验证码请求间隔缓存key
    /// </summary>
    public const string codeSpanKey = "codeSpanKey";
    /// <summary>
    /// 音量
    /// </summary>
    public const string volumeCacheKey = "volume";
    /// <summary>
    /// 优先输入设备 有就用 没有旧默认
    /// </summary>
    public const string inputDeviceCacheKey = "inputDevice";
    /// <summary>
    /// 优先输出设备 有就用 没有旧默认
    /// </summary>
    public const string outputDeviceCacheKey = "outputDevice";
    /// <summary>
    /// 角色移动速度
    /// </summary>
    public const string moveSpeedCacheKey = "moveSpeed";
    /// <summary>
    /// 角色旋转速度
    /// </summary>
    public const string rotateSpeedCacheKey = "rotateSpeed";
    /// <summary>
    /// 基础移动速度
    /// </summary>
    public const float baseMoveSpeed = 5f;
    /// <summary>
    /// 基础旋转速度
    /// </summary>
    public const float baseRotateSpeed = 40f;
    /// <summary>
    /// 速度系数默认值（滑动条中间值对应的系数）
    /// </summary>
    public const float defaultSpeedCoefficient = 1f;
    /// <summary>
    /// 课程语音模式
    /// </summary>
    public const string courseVoice = "CourseVoice";
    #endregion

    #region 课程模式
    /// <summary>
    /// 当前课程模式
    /// </summary>
    public static CourseMode courseMode = CourseMode.Training;
    /// <summary>
    /// 是否正在新建场景
    /// </summary>
    public static bool CreatedMode = false;
    /// <summary>
    /// 判断是否是考核模式（Exam 或 OnlineExam）
    /// </summary>
    public static bool IsExamMode()
    {
        return courseMode == CourseMode.Exam || courseMode == CourseMode.OnlineExam;
    }

    /// <summary>
    /// 判断是否是直播
    /// </summary>
    public static bool IsLiveMode()
    {
        return courseMode == CourseMode.Livebroadcast;
    }

    /// <summary>
    /// 设置课程模式（统一入口，同时更新 isExam 和 isLive）
    /// </summary>
    public static void SetCourseMode(CourseMode mode)
    {
        courseMode = mode;
        isExam = (mode == CourseMode.Exam || mode == CourseMode.OnlineExam);

        // 根据模式设置语音模式
        UpdateSpeechMode();
    }

    /// <summary>
    /// 根据当前课程模式更新语音模式
    /// 考核模式无语音提示，其他模式根据用户设置
    /// </summary>
    public static void UpdateSpeechMode()
    {
        if (SpeechManager.Instance == null)
            return;

        //非培训模式无语音提示
        if (courseMode != CourseMode.Training)
        {
            SpeechManager.Instance.SpeechMode = false;
        }
        else
        {
            // 其他模式根据用户设置
            if (PlayerPrefs.GetInt(courseVoice) == 0)
            {
                SpeechManager.Instance.SpeechMode = false;
            }
            else if (PlayerPrefs.GetInt(courseVoice) == 1)
            {
                SpeechManager.Instance.SpeechMode = true;
            }
        }
    }
    #endregion

    #region 服务器时间及计时相关
    private static System.DateTime _serverTime = System.DateTime.Now;
    private static float _startOffset;
    /// <summary>
    /// 服务器当前时间，防止修改本地时间
    /// </summary>
    public static System.DateTime ServerTime
    {
        get
        {
            return _serverTime.AddSeconds(Time.realtimeSinceStartup - _startOffset);
        }
    }

    public static string ServerTimeFormat
    {
        get
        {
            return ServerTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public static void InitServerTime()
    {
        RequestManager.Instance.GetServerTime(time =>
        {
            _serverTime = System.Convert.ToDateTime(time);
            _startOffset = Time.realtimeSinceStartup;
        }, error =>
        {
            _serverTime = System.DateTime.Now;
            Log.Error($"获取服务器时间失败，暂时用本地时间代替! \n错误信息为{error}");
        });
    }
    #endregion

    /// <summary>
    /// UI进出场动画时间系数
    /// </summary>
    public static float uiAnimRatio = 1f;

    public static int CanvasWidth;
    public static int CanvasHeight;

    ///// <summary>
    ///// 授权信息
    ///// </summary>
    //public static int overview_id;

    /// <summary>
    /// 光标状态
    /// </summary>
    public static CursorLockMode CursorLockMode;

    /// <summary>
    /// 当前是否有弹窗显示
    /// </summary>
    public static bool ShowPopup
    {
        get
        {
            if (_showPopup > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        set
        {
            if (value)
            {
                _showPopup++;
            }
            else
            {
                _showPopup--;
            }

            if (_showPopup < 0)
            {
                Debug.LogError("就不应该能等于负数,检查相关设置!");
                _showPopup = 0;
            }
        }
    }
    /// <summary>
    /// 当前是否有多个弹窗
    /// </summary>
    public static bool MultiplePopup
    {
        get
        {
            return _showPopup > 1;
        }
    }
    private static int _showPopup = 0;
    /// <summary>
    /// 是否显示转场
    /// </summary>
    public static bool ShowTransition;
    /// <summary>
    /// 是否是离线模式
    /// </summary>
    public static bool isOffLine = false;
    /// <summary>
    /// 用户数据
    /// </summary>
    public static RequestData.Account account;
    /// <summary>
    /// 是否课编辑用户信息
    /// </summary>
    public static bool canEditUserInfo = true;
    public static bool waitExam;

    /// <summary>
    /// 操作完成提示默认显示时长
    /// </summary>
    public static float TipShowTime = 2.5f;

    #region AR

    /// <summary>
    /// 是否是AR模式
    /// </summary>
    public static bool isAR = false;
    /// <summary>
    /// 是否开启平面检测
    /// </summary>
    public static bool isARTracking = false;
    #endregion

    #region 课程
    ///// <summary>
    ///// 课程字典，key：课程包名；value：课程集合
    ///// </summary>
    //public static Dictionary<string, List<Course>> courseDic = new Dictionary<string, List<Course>>();
    /// <summary>
    /// Key课程编号 value课程 用于快速索引
    /// </summary>
    public static Dictionary<int, Course> courseDicExists = new Dictionary<int, Course>();
    /// <summary>
    /// Key课程编号 value课程AB包 用于快速索引
    /// </summary>
    public static Dictionary<int, List<CourseABPackage>> courseABDic = new Dictionary<int, List<CourseABPackage>>();
    /// <summary>
    /// Key考核编号 value课程AB包 用于快速索引
    /// </summary>
    public static Dictionary<int, List<CourseABPackage>> examABDic = new Dictionary<int, List<CourseABPackage>>();

    /// <summary>
    /// 当前课程
    /// </summary>
    public static Course currentCourseInfo;
    /// <summary>
    /// 当前课程ID 直播模式需要
    /// </summary>
    public static int currentCourseID;
    /// <summary>
    /// 当前课程百科列表
    /// </summary>
    public static List<Encyclopedia> currentWikiList;
    /// <summary>
    /// 当前百科
    /// </summary>
    public static Encyclopedia currentWiki;
    /// <summary>
    /// 当前百科类型 状态同步需要
    /// </summary>
    public static BaikeType currentBaikeType;
    /// <summary>
    /// 当前百科是否漫游
    /// </summary>
    public static bool hasRole;

    /// <summary>
    /// 百科模型节点名称缓存 key:uuid
    /// </summary>
    public static Dictionary<string, string> currentWikiNames = new Dictionary<string, string>();
    /// <summary>
    /// 百科知识点缓存 key:uuid
    /// </summary>
    public static Dictionary<string, List<Knowledgepoint>> currentWikiKnowledges = new Dictionary<string, List<Knowledgepoint>>();

    /// <summary>
    /// 模型是否单独显示
    /// </summary>
    public static bool InSingleMode;

    /// <summary>
    /// 是否在标注模式
    /// </summary>
    public static bool InPaintMode;

    /// <summary>
    /// 是否在课件编辑模式
    /// </summary>
    public static bool InEditMode;
    #endregion

    public static void SaveCourseInfo(Course course)
    {
        currentCourseInfo = course;
    }

    public static void SaveExaminationInfo(Examination examination)
    {
        currentCourseInfo = new Course() { id = examination.id, name = examination.name, duration = examination.duration };
    }

    public static void SaveCourseInfo(List<Course> allCourses)
    {
        //courseDic.Clear();
        courseDicExists.Clear();
        allCourses.ForEach(course =>
        {
            courseDicExists.Add(course.id, course);
        });
    }

    public static void SaveCourseABInfo(List<CourseAB> allCourseABs)
    {
        courseABDic.Clear();

        foreach (CourseAB courseAB in allCourseABs)
        {
            Dictionary<int, CourseABPackage> courseABs = new Dictionary<int, CourseABPackage>();

            if (courseAB.abPackage != null)
            {
                //只保留最新ab包信息
                foreach (CourseABPackage ab in courseAB.abPackage)
                {
                    if (courseABs.ContainsKey(ab.encyclopediaId))
                        courseABs[ab.encyclopediaId] = ab;
                    else
                        courseABs.Add(ab.encyclopediaId, ab);
                }
            }
            courseABDic.Add(courseAB.id, courseABs.Values.ToList());
        }
    }

    public static void SaveExamABInfo(List<CourseAB> allCourseABs)
    {
        examABDic.Clear();

        foreach (CourseAB courseAB in allCourseABs)
        {
            Dictionary<int, CourseABPackage> courseABs = new Dictionary<int, CourseABPackage>();

            if (courseAB.abPackage != null)
            {
                //只保留最新ab包信息
                foreach (CourseABPackage ab in courseAB.abPackage)
                {
                    if (courseABs.ContainsKey(ab.encyclopediaId))
                        courseABs[ab.encyclopediaId] = ab;
                    else
                        courseABs.Add(ab.encyclopediaId, ab);
                }
            }
            examABDic.Add(courseAB.id, courseABs.Values.ToList());
        }
    }

    #region 考核
    /// <summary>
    /// 是否考核
    /// </summary>
    public static bool isExam = false;

    /// <summary>
    /// 是否启用步骤列表功能
    /// </summary>
    public static bool EnableFlow = false;

    /// <summary>
    /// 是否开启考核录制
    /// 250627 暂时移除功能
    /// </summary>
    public static bool ExamRecording = false;

    #endregion

    #region 协同
    private static float _playTimeRatio = 1f;
    /// <summary>
    /// 操作表现执行时间系数
    /// </summary>
    public static float playTimeRatio
    {
        get { return _playTimeRatio; }

        set 
        {
            if (value > 0f && (NetworkManager.Instance.IsIMSyncCachedState || NetworkManager.Instance.IsIMSyncState))
                return;
            _playTimeRatio = value;
        }
    }
    /// <summary>
    /// 房间列表刷新间隔
    /// </summary>
    public static int roomListRefreshTime = 10;

    /// <summary>
    /// 是否直播
    /// </summary>
    public static bool isLive = false;
    /// <summary>
    /// 当前房间信息
    /// </summary>
    public static RoomInfoModel roomInfo;
    /// <summary>
    /// 当前被设置为主画面的用户ID
    /// </summary>
    public static int mainScreenId = -1;
    /// <summary>
    /// 当前拥有操作权的用户IDs
    /// </summary>
    public static HashSet<int> controllerIds = new HashSet<int>();

    /// <summary>
    /// 当前房间全员禁言状态
    /// </summary>
    public static bool isAllTalk = false;

    /// <summary>
    /// 操作版本
    /// </summary>
    public static int version = 0;

    /// <summary>
    /// 是否是房主
    /// </summary>
    /// <returns></returns>
    public static bool IsHomeowner()
    {
        if (roomInfo == null || account == null)
            return false;
        return roomInfo.creatorId == account.id;
    }

    /// <summary>
    /// 是否是小组模式
    /// </summary>
    /// <returns></returns>
    public static bool IsGroupMode()
    {
        if (roomInfo == null)
            return false;
        return roomInfo.ExamType == (int)ExamRoomType.Group;
    }

    /// <summary>
    /// 是否是主画面
    /// </summary>
    /// <returns></returns>
    public static bool IsMainScreen()
    {
        if (roomInfo == null || account == null)
            return false;
        return roomInfo.RoomType != 0 && mainScreenId == account.id;
    }

    /// <summary>
    /// 用户是否是操作者
    /// </summary>
    /// <returns></returns>
    public static bool IsOperator()
    {
        if (roomInfo == null || account == null)
            return false;
        if (IsExamMode() || courseMode == CourseMode.Collaboration)
            return true;
        return controllerIds.Contains(account.id);
    }


    /// <summary>
    /// 是否是操作者
    /// </summary>
    /// <returns></returns>
    public static bool IsUserOperator(int userId)
    {
        if (roomInfo == null || account == null)
            return false;
        
        //多人协同 以及单人多人考核必然获得操作权限
        if (IsExamMode() || courseMode == CourseMode.Collaboration)
            return true;
        return controllerIds.Contains(userId);
    }

    /// <summary>
    /// 是否需要处理操作消息
    /// </summary>
    /// <param name="sendUserId"></param>
    /// <param name="force">无权限成员是否执行来自主画面的操作(eg.音视频操作)，默认接收主屏分享数据，不需要接收同步操作数据</param>
    /// <returns></returns>
    public static bool ShouldProcess(int sendUserId, bool force = false)
    {
        if (IsOperator() && sendUserId == account.id)
            return true;

        return !IsOperator() && sendUserId == mainScreenId && force;
    }

    /// <summary>
    /// 是否直播答题
    /// </summary>
    public static bool isJudgeOnline = false;
    #endregion

    public static string DefaultStepHintSuccess = "完成提示";

    public static string OpUILayer = "OpUI";
}