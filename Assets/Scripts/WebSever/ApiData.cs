using System;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// web服务器api地址及api需要的常用信息管理类
    /// </summary>
    public class ApiData
    {
        /// <summary>
        /// 开发版或正式版，0测试版  1正式版
        /// </summary>
        public static int state;
        /// <summary>
        /// 基础接口前缀
        /// </summary>
        private static string baseAddress = "http://139.155.5.87:9981/api/base/";
        /// <summary>
        /// 用户相关接口前缀
        /// </summary>
        private static string userAddress = "http://139.155.5.87:9981/api/user/";
        /// <summary>
        /// 其他接口前缀
        /// </summary>
        private static string contentAddress = "http://139.155.5.87:9981/api/content/";
        /// <summary>
        /// 日志接口前缀
        /// </summary>
        private static string logAddress = "http://139.155.5.87:9981/api/ops/log/";
        /// <summary>
        /// 客户端ID
        /// </summary>
        public static string ClientID
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    return "arim.pc";//test
                }
                else if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    return "arim.pc";
                }
                else if (Application.platform == RuntimePlatform.Android)
                {
                    return "arim.android";
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return "arim.ios";
                }

                return "arim.pc";
            }
        }
        /// <summary>
        /// 客户端设备ID
        /// </summary>
        public static string DeviceID
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    return "1";
                }
                else if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    return "1";
                }
                else if (Application.platform == RuntimePlatform.Android)
                {
                    return "2";
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return "3";
                }

                return "windows";
            }

        }

        /// <summary>
        /// 登录成功后的令牌 会在Post时 用到
        /// </summary>
        public static string AccessToken = "";

        /// <summary>
        /// 接口获取服务器接口配置并初始化
        /// </summary>
        /// <param name="index_apiUrl">0:本地，1测试外网，2外网</param>
        /// <param name="index_state">0测试版，1正式版</param>
        public static void Init(int index_apiUrl, int index_state)
        {
            state = index_state;

            //设置内网或外网地址
            switch (index_apiUrl)
            {
                case 1://测试外网
                    baseAddress = "http://139.155.5.87:9981/api/base/";
                    userAddress = "http://139.155.5.87:9981/api/user/";
                    contentAddress = "http://139.155.5.87:9981/api/content/";
                    ServiceApiData.rtc_api_url = "https://test.api.arseek.cn/v5/api/rtc/";
                    logAddress = "http://139.155.5.87:9981/api/ops/log/";
                    break;
                case 2://正式外网
                    baseAddress = "https://api.arseek.cn/3d/api/res/api/base/";
                    userAddress = "https://api.arseek.cn/3d/api/res/api/user/";
                    contentAddress = "https://api.arseek.cn/3d/api/res/api/content/";
                    ServiceApiData.rtc_api_url = "https://api.arseek.cn/v5/api/rtc/";
                    logAddress = "https://api.arseek.cn/3d/api/res/api/ops/log/";
                    break;
                case 3://华能测试外网
                    baseAddress = "http://hn-test.arseek.cn/api/base/";
                    userAddress = "http://hn-test.arseek.cn/api/user/";
                    contentAddress = "http://hn-test.arseek.cn/api/content/";
                    ServiceApiData.rtc_api_url = "https://test.api.arseek.cn/v5/api/rtc/";
                    ServiceApiData.exam_rtc_api_url = "http://139.155.5.87:21345/colla/rtc/";// "http://139.155.246.138:12345/colla/rtc/";//10.0.1.125:12345
                    logAddress = "http://hn-test.arseek.cn/api/ops/log/";
                    break;
                case 4://外出局域网，可修改配置文件，修改IP
                       //加载配置文件
                    try
                    {
                        string xmlPath = Application.streamingAssetsPath + "/IPAddress.xml"; // 请确保路径正确  
                        string xmlFile = File.ReadAllText(xmlPath);
                        string xmlData = xmlFile;

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlData);

                        XmlNodeList settingNodes = xmlDoc.SelectNodes("/Config/IPAddress");
                        XmlNodeList settingNodes1 = xmlDoc.SelectNodes("/Config/ServiceExamIPAddress");

                        string ipAddress = settingNodes[0].Attributes["value"].Value;
                        string serviceIpAddress = settingNodes1[0].Attributes["value"].Value;

                        baseAddress = ipAddress + "api/base/";
                        userAddress = ipAddress + "api/user/";
                        contentAddress = ipAddress + "api/content/";
                        ServiceApiData.exam_rtc_api_url = serviceIpAddress + "colla/rtc/";
                        logAddress = ipAddress + "api/ops/log/";
                    }
                    catch (Exception ex)
                    {
                        Log.Error("加载IP配置文件失败：" + ex);

                        //会使用华能测试外网的ip数据
                        ServiceApiData.exam_rtc_api_url = "http://139.155.5.87:21345/colla/rtc/";
                    }
                    break;
                default:
                    ServiceApiData.rtc_api_url = "https://test.api.arseek.cn/v5/api/rtc/";
                    break;
            }
        }

        #region 版本管理
        /// <summary>
        /// 获取最新版本
        /// </summary>
        public static string LatestVersion { get { return contentAddress + "version/latest"; } }
        /// <summary>
        /// 获取版本信息
        /// </summary>
        public static string VersionInfo { get { return contentAddress + "version"; } }
        #endregion

        #region 平台登录管理
        /// <summary>
        /// 登录
        /// </summary>
        public static string Login { get { return userAddress + "login"; } }
        /// <summary>
        /// 注册
        /// </summary>
        public static string Register { get { return userAddress + "register"; } }
        /// <summary>
        /// 注册验证码
        /// </summary>
        public static string RegisterCaptcha { get { return userAddress + "register/captcha"; } }
        /// <summary>
        /// 忘记密码
        /// </summary>
        public static string ForgetPassword { get { return userAddress + "forget/password"; } }
        /// <summary>
        /// 忘记密码验证码
        /// </summary>
        public static string ForgetCaptcha { get { return userAddress + "forget/captcha"; } }
        /// <summary>
        /// 刷新token
        /// </summary>
        public static string NewRefreshToken { get { return userAddress + "refresh_token"; } }
        #endregion

        #region 个人信息管理
        /// <summary>
        /// 修改个人信息
        /// </summary>
        public static string Profile { get { return userAddress + "profile"; } }
        /// <summary>
        /// 修改密码
        /// </summary>
        public static string ProfilePassword { get { return userAddress + "profile/password"; } }
        #endregion

        #region 验证码
        /// <summary>
        /// 绑定手机新手机的验证码
        /// </summary>
        public static string Captcha_BindPhoneNumber_New { get { return userAddress + "account/bind/captcha"; } }
        /// <summary>
        /// 重新绑定手机的验证码(新手机号)
        /// </summary>
        public static string AccountBind { get { return userAddress + "account/bind"; } }
        /// <summary>
        /// 绑定手机旧手机的验证码
        /// </summary>
        public static string Captcha_BindPhoneNumber_Old { get { return userAddress + "account/rebind/captcha"; } }
        /// <summary>
        /// 修改密码的验证码
        /// </summary>
        public static string Captcha_ChangePassword { get { return userAddress + "password/change/captcha"; } }
        /// <summary>
        /// 修改密码
        /// </summary>
        public static string PasswordReset { get { return userAddress + "password/reset"; } }
        /// <summary>
        /// 修改密码
        /// </summary>
        public static string PasswordChange { get { return userAddress + "password/change"; } }
        /// <summary>
        /// 忘记密码的验证码
        /// </summary>
        public static string Captcha_ForgetPassword { get { return userAddress + "password/forget/captcha"; } }
        #endregion

        #region 账号\单位管理
        /// <summary>
        /// 凭邀请码加入单位
        /// </summary>
        public static string InviteCode { get { return userAddress + "org/invitee"; } }
        /// <summary>
        /// 首页授权
        /// </summary>
        public static string Navigation { get { return userAddress + "navigation"; } }
        /// <summary>
        /// 获取总览
        /// </summary>
        public static string Overview { get { return contentAddress + "overview"; } }
        /// <summary>
        /// 获取UI主题
        /// </summary>
        public static string Theme { get { return contentAddress + "client/theme"; } }
        #endregion

        #region 元数据
        /// <summary>
        /// 获取课程分类列表
        /// </summary>
        public static string TeachCategory { get { return contentAddress + "meta/teach_category/list"; } }
        /// <summary>
        /// 获取课程标签列表(已弃用)
        /// </summary>
        public static string TeachTag { get { return contentAddress + "meta/teach_tag/list"; } }
        #endregion

        #region 标签管理
        /// <summary>
        /// 获取标签列表
        /// </summary>
        public static string Tag { get { return contentAddress + "tag/list"; } }
        #endregion

        #region 课程管理
        /// <summary>
        /// 获取课程列表
        /// </summary>
        public static string CourseListForClient { get { return contentAddress + "course/list_for_client"; } }
        /// <summary>
        /// 获取课程AB包列表
        /// </summary>
        public static string CourseABPackageList { get { return contentAddress + "course/ab_package/list"; } }
        /// <summary>
        /// 获取课程
        /// </summary>
        public static string Course { get { return contentAddress + "course"; } }
        /// <summary>
        /// 获取课程习题列表
        /// </summary>
        public static string CourseExercise { get { return contentAddress + "course/exercise"; } }
        /// <summary>
        /// 累计学习时长
        /// </summary>
        public static string CourseStudyDuration { get { return contentAddress + "course/study/duration"; } }
        #endregion

        #region 百科管理
        /// <summary>
        /// 获取百科
        /// </summary>
        public static string Encyclopedia { get { return contentAddress + "encyclopedia"; } }

        #region 语音数据
        /// <summary>
        /// 获取语音数据列表
        /// </summary>
        public static string GetSpeechList { get { return contentAddress + "encyclopedia/speech/list"; } }
        #endregion
        #endregion

        #region 模型节点管理
        /// <summary>
        /// 修改模型节点名称
        /// </summary>
        public static string ModelNode { get { return contentAddress + "model_node"; } }
        /// <summary>
        /// 修改步骤列表节点名称
        /// </summary>
        public static string StepNode { get { return contentAddress + "encyclopedia/operations"; } }
        #endregion

        #region 知识点管理
        /// <summary>
        /// 获取知识点列表
        /// </summary>
        public static string KnowledgeList { get { return contentAddress + "knowledge_point/list"; } }
        /// <summary>
        /// 知识点 创建 修改
        /// </summary>
        public static string KnowledgePoint { get { return contentAddress + "knowledge_point"; } }
        /// <summary>
        /// 批量添加知识点
        /// </summary>
        public static string KnowledgePointBatch { get { return contentAddress + "knowledge_point/batch"; } }
        /// <summary>
        /// 修改知识点资源
        /// </summary>
        public static string KnowledgePointResource { get { return contentAddress + "knowledge_point/resource"; } }
        /// <summary>
        /// 修改知识点顺序
        /// </summary>
        public static string KnowledgeSort { get { return contentAddress + "knowledge_point/sort"; } }
        #endregion

        #region 课件管理
        /// <summary>
        /// 获取课件资源列表
        /// </summary>
        public static string CoursewareList { get { return contentAddress + "courseware/resource/list"; } }
        /// <summary>
        /// 新增课件资源信息
        /// </summary>
        public static string AddCoursewareResource { get { return contentAddress + "courseware/resource"; } }
        /// <summary>
        /// 批量删除课件资源信息
        /// </summary>
        public static string DeleteCoursewareResourceBatch { get { return contentAddress + "courseware/resource/batch"; } }
        #endregion

        #region 考核管理
        /// <summary>
        /// 获取考核列表
        /// </summary>
        public static string ExamListForClient { get { return contentAddress + "examine/list_for_client"; } }
        /// <summary>
        /// 获取考核AB包列表
        /// </summary>
        public static string ExamABPackageList { get { return contentAddress + "examine/ab_package/list"; } }
        /// <summary>
        /// 获取考核试卷
        /// </summary>
        public static string ExamPaper { get { return contentAddress + "examine/paper"; } }
        /// <summary>
        /// 按照考生人数初始化考核记录
        /// </summary>
        public static string InitExamRecord { get { return contentAddress + "examine/start"; } }
        /// <summary>
        /// 考试信息
        /// POST 创建考核记录
        /// GET 获取考试信息
        /// </summary>
        public static string Examination { get { return contentAddress + "examine/examination"; } }
        /// <summary>
        /// 获取考核记录列表
        /// </summary>
        public static string GetExamRecordList { get { return contentAddress + "examine/examination/list"; } }
        /// <summary>
        /// 考核结束答题
        /// </summary>
        public static string ExamEnd { get { return contentAddress + "examine/end"; } }
        /// <summary>
        /// GET  取得考核结果
        /// POST 保存考核结果
        /// </summary>
        public static string ExamineResult { get { return contentAddress + "examine/result/v2"; } }
        /// <summary>
        /// 取得考核成绩列表
        /// </summary>
        public static string GetExamResultList { get { return contentAddress + "examine/result/list"; } }
        /// <summary>
        /// 取得个人考试结果
        /// </summary>
        public static string GetExamRecordPersonal { get { return contentAddress + "examine/result/personal"; } }    
        /// <summary>
        /// 保存考核结果附件
        /// </summary>
        public static string ExamineResultAccessory { get { return contentAddress + "examine/result/accessory"; } }

        #region 旧接口
        ///// <summary>
        ///// 提交考核记录
        ///// </summary>
        //public static string SubmitExamRecord { get { return contentAddress + "examine/result"; } }
        ///// <summary>
        ///// 取得考试结果
        ///// </summary>
        //public static string GetExamRecord { get { return contentAddress + "examine/result"; } }
        ///// <summary>
        ///// 提交阅卷数据
        ///// </summary>
        //public static string SubmitCheckPaper { get { return contentAddress + "examine/result/check"; } }
        #endregion
        #endregion

        #region 其他
        /// <summary>
        /// 获取系统时间
        /// </summary>
        public static string ServerTime { get { return baseAddress + "server/time"; } }
        /// <summary>
        /// 获取阿里云认证
        /// </summary>
        public static string OSS { get { return contentAddress + "oss/sts"; } }
        /// <summary>
        /// 获取sts
        /// </summary>
        public static string STS { get { return contentAddress + "storage/sts"; } }
        /// <summary>
        /// 获取sts对象
        /// </summary>
        public static string STSObject { get { return contentAddress + "storage/object/fetch"; } }
        /// <summary>
        /// 查看sts对象
        /// </summary>
        public static string STSObjectView { get { return contentAddress + "storage/view"; } }
        /// <summary>
        /// 工程详情
        /// </summary>
        public static string Project { get { return contentAddress + "project"; } }
        /// <summary>
        /// 日志上传
        /// </summary>
        public static string UpLog { get { return logAddress + "client/upload"; } }
        #endregion
    }
}