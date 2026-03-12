using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 接口请求数据结构
    /// </summary>
    public class RequestData
    {
        /// <summary>
        /// 请求结果基类
        /// </summary>
        public class ResultBase
        {
            /// <summary>
            ///  是否成功
            /// </summary>
            public string success { get; set; }
            /// <summary>
            /// 结果
            /// </summary>
            public string code { get; set; }
        }

        #region 版本

        /// <summary>
        /// 版本信息
        /// </summary>
        public class Version
        {
            public string addTime { get; set; }
            public string clientId { get; set; }
            public string content { get; set; }
            public VersionDetail[] details { get; set; }
            public string downloadUrl { get; set; }
            public int id { get; set; }
            public bool restart { get; set; }
            public int state { get; set; }
            public string version { get; set; }
        }

        public class VersionDetail
        {
            public string fileName { get; set; }
            public int id { get; set; }
            public string installPath { get; set; }
            public int type { get; set; }
            public int versionId { get; set; }
        }

        #endregion

        #region 用户

        /// <summary>
        /// 用户信息对象类
        /// </summary>
        public class Account
        {
            /// <summary>
            ///  用户ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 用户账号
            /// </summary>
            public string account { get; set; }
            /// <summary>
            /// 手机号码
            /// </summary>
            public string mobile { get; set; }
            /// <summary>
            /// 工号
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string userNo { get; set; }
            /// <summary>
            /// 管理单位
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string userOrgName { get; set; }
            /// <summary>
            /// 昵称
            /// </summary>
            public string nickname;
            /// <summary>
            /// 学校ID
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int schoolId { get; set; }
            /// <summary>
            /// 学校ID
            /// </summary>
            public string schoolName { get; set; }
            /// <summary>
            /// 角色
            /// </summary>
            public string[] role { get; set; }
            /// <summary>
            ///  令牌
            /// </summary>
            public Token token { get; set; }

            /// <summary>
            /// 权限  0是学生; 1是超级管理员、企业管理员、老师,有编辑权限
            /// </summary>
            public int roleType
            {
                get
                {
                    if (admin || businessAdmin || courseManager || teacher)
                        return 1;
                    //if (role.Contains("ROLE_business_admin"))
                    //    return 1;
                    return 0;
                }
            }

            /// <summary>
            /// 角色描述
            /// </summary>
            public string roleDescription
            {
                get
                {
                    if (courseManager) return "备课管理员";
                    if (admin) return "管理员";//超级
                    if (businessAdmin) return "管理员";//企业
                    if (teacher) return "老师";
                    if (student) return "学生";
                    return "学生";
                    //if (role.Contains("ROLE_business_admin"))
                    //    return "企业管理员";
                    //if (role.Contains("ROLE_business_user"))
                    //    return "企业用户";
                    //return "个人用户";
                }
            }
            /// <summary>
            /// 是否开启课程模块
            /// </summary>
            public bool allowCourse;
            /// <summary>
            /// 是否开启协同模块
            /// </summary>
            public bool allowCoordination;
            /// <summary>
            /// 是否开启考核模块
            /// </summary>
            public bool allowExamine;
            #region 用户类型
            /// <summary>
            /// 超级管理员
            /// </summary>
            public bool courseManager;
            /// <summary>
            /// 超级管理员
            /// </summary>
            public bool admin;
            /// <summary>
            /// 企业管理员
            /// </summary>
            public bool businessAdmin;
            /// <summary>
            /// 教师
            /// </summary>
            public bool teacher;
            /// <summary>
            /// 学生
            /// </summary>
            public bool student;
            #endregion
        }

        /// <summary>
        /// 令牌对象类
        /// </summary>
        public class Token
        {
            /// <summary>
            /// 令牌
            /// </summary>
            public string accessToken;
            /// <summary>
            /// 刷新
            /// </summary>
            public string refreshToken;
            /// <summary>
            /// 到期
            /// </summary>
            public int expiresIn;
        }

        /// <summary>
        /// 登录请求类
        /// </summary>
        public class LoginRequest
        {
            /// <summary>
            /// 账号
            /// </summary>
            public string username;
            /// <summary>
            /// 密码
            /// </summary>
            public string password;
        }

        /// <summary>
        /// 注册请求类
        /// </summary>
        public class RegisterRequest
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
            /// <summary>
            /// 昵称
            /// </summary>
            public string nickName { get; set; }
            /// <summary>
            /// 密码
            /// </summary>
            public string password { get; set; }
            /// <summary>
            /// 手机号
            /// </summary>
            public string phoneNumber { get; set; }
        }

        /// <summary>
        /// 忘记密码请求类
        /// </summary>
        public class ForgetPasswordRequest
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
            /// <summary>
            /// 密码
            /// </summary>
            public string password { get; set; }
            /// <summary>
            /// 手机号
            /// </summary>
            public string phoneNumber { get; set; }
        }

        /// <summary>
        /// 验证码请求类
        /// </summary>
        public class CaptchaRequest
        {
            public string phoneNumber { get; set; }
        }

        /// <summary>
        /// 验证码信息
        /// </summary>
        public class Captcha
        {
            public string captcha { get; set; }
            public string phoneNumber { get; set; }
        }
        #endregion

        #region 个人信息
        /// <summary>
        /// 修改个人信息请求类
        /// </summary>
        public class PersonalRequest
        {
            /// <summary>
            /// 真实姓名,TODO服务器字段未与登录返回字段（nickname）统一
            /// </summary>
            public string realName { get; set; }
            /// <summary>
            /// 工号
            /// </summary>
            public string userNo { get; set; }
            /// <summary>
            /// 单位
            /// </summary>
            public string userOrgName { get; set; }
        }
        /// <summary>
        /// 修改密码请求类
        /// </summary>
        public class PersonalPasswordRequest_Old
        {
            /// <summary>
            /// 新密码
            /// </summary>
            public string newPassword { get; set; }
            /// <summary>
            /// 原密码
            /// </summary>
            public string password { get; set; }
        }
        /// <summary>
        /// 修改密码请求类 使用验证码
        /// </summary>
        public class PersonalPasswordRequest_Captcha
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
            /// <summary>
            /// 原密码
            /// </summary>
            public string password { get; set; }
            /// <summary>
            /// 手机号
            /// </summary>
            public string phoneNumber { get; set; }
        }
        /// <summary>
        /// 修改密码请求类 使用验证码
        /// </summary>
        public class PersonalPasswordRequest
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
            /// <summary>
            /// 原密码
            /// </summary>
            public string password { get; set; }
        }
        public class BindPhoneNumberRequest
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
            /// <summary>
            /// 解绑时给参数
            /// </summary>
            public string passport { get; set; }
            /// <summary>
            /// 手机号
            /// </summary>
            public string phoneNumber { get; set; }
        }
        public class ReBindPhoneNumberRequest
        {
            /// <summary>
            /// 验证码
            /// </summary>
            public string captcha { get; set; }
        }
        #endregion

        #region 账号\单位

        /// <summary>
        /// 授权
        /// </summary>
        public class Navigation
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int schoolId { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool isOverview { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int overviewId { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool isAr { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool isLive { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool isMr { get; set; }
            //public bool isPark { get; set; }
            //public bool isProduct { get; set; }
            //public bool isProductOrder { get; set; }
            //public bool isProductSop { get; set; }
            //public bool isProductTicket { get; set; }
            //public bool isQueue { get; set; }
            //public bool isResource { get; set; }
            //public bool isResourceExercise { get; set; }
            //public bool isResourceSimulate { get; set; }
            //public bool isResourceTeach { get; set; }
            //public bool isTraining { get; set; }
            //public int modifyer { get; set; }
            //public int parkId { get; set; }
        }

        /// <summary>
        /// 总览
        /// </summary>
        public class Overview
        {
            public string abPackageUrl { get; set; }
            public string name { get; set; }
            public CourseAnchor[] overviewKey { get; set; }
        }

        /// <summary>
        /// 总览锚点
        /// </summary>
        public class CourseAnchor
        {
            /// <summary>
            /// 课程ID 
            /// </summary>
            public int courseId { get; set; }
            /// <summary>
            /// 锚点ID
            /// </summary>
            public string key { get; set; }
        }

        /// <summary>
        /// 修改用户单位请求类
        /// </summary>
        public class AccountSchoolRequest
        {
            public int accountId;
            public int schoolId;
        }


        public class OrgTheme
        {
            public string appLoginBg { get; set; }
            public string appLoginLogo { get; set; }
            public string domain { get; set; }
            public int orgId { get; set; }
            //public string webFavicon { get; set; }
            //public string webHomeBg { get; set; }
            //public string webLoginBg { get; set; }
            //public string webLoginLogo { get; set; }
        }

        #endregion

        #region 元数据
        /// <summary>
        /// 课程分类
        /// </summary>
        public class TeachCategory
        {
            public string category { get; set; }
            public int id { get; set; }
            public List<TeachCategory> subCategory { get; set; }
        }

        /// <summary>
        /// 课程标签
        /// </summary>
        public class TeachTag
        {
            public int id { get; set; }
            public string tag { get; set; }
        }

        #endregion

        #region 标签管理
        /// <summary>
        /// 标签
        /// </summary>
        public class TagList
        {
            public Paging paging { get; set; }
            public List<Record> records { get; set; }
        }

        public class Record
        {
            /// <summary>
            /// 创建时间
            /// </summary>
            public string createTime { get; set; }
            /// <summary>
            /// 创造者ID
            /// </summary>
            public int creatorId { get; set; }
            /// <summary>
            /// 识别ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 标签名
            /// </summary>
            public string tag { get; set; }
            public string tagType { get; set; }
        }
        #endregion

        #region 课程、百科
        /// <summary>
        /// 课程
        /// </summary>
        public class Course
        {
            /// <summary>
            /// 课程编号
            /// </summary>
            public int id;
            /// <summary>
            /// 课程名称
            /// </summary>
            public string name;
            /// <summary>
            /// 图标url
            /// </summary>
            public string iconPath;
            /// <summary>
            /// 备注
            /// </summary>
            public string remarks;
            /// <summary>
            /// 百科列表
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<Encyclopedia> encyclopediaList;
            /// <summary>
            /// 时长(分钟)
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int duration;
            /// <summary>
            /// 创建者
            /// </summary>
            public int creatorId;
            /// <summary>
            /// 多个标签
            /// </summary>
            public string tags;
            public string tags_readable;
            /// <summary>
            /// 是否上架
            /// </summary>
            public bool online;
        }

        /// <summary>
        /// 累积课程学习时长请求参数
        /// </summary>

        public class SumStudyDurationRequest
        {
            /// <summary>
            /// 课程ID
            /// </summary>
            public int courseId;
            /// <summary>
            /// 时长 秒
            /// </summary>
            public int duration;
        }

        /// <summary>
        /// 课程AB包
        /// </summary>

        public class CourseAB
        {
            /// <summary>
            /// 课程编号
            /// </summary>
            public int id;
            /// <summary>
            /// AB包列表
            /// </summary>
            public List<CourseABPackage> abPackage { get; set; }
        }

        public class CourseABPackage
        {
            /// <summary>
            /// ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 文件名
            /// </summary>
            public string fileName { get; set; }
            /// <summary>
            /// 文件路径
            /// </summary>
            public string filePath { get; set; }
            public int encyclopediaId { get; set; }
            public int type { get; set; }
        }

        /// <summary>
        /// 百科列表信息
        /// </summary>
        public class EncyclopediaInfo
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 百科名称
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 百科封面路径
            /// </summary>
            public string iconPath { get; set; }
            /// <summary>
            /// 百科类型
            /// </summary>
            public int typeId { get; set; }
            /// <summary>
            /// 显示类型文字
            /// </summary>
            public string typeDescription { get; set; }

            ///// <summary>
            ///// AB包信息
            ///// </summary>
            //public List<ABPackage> abPackageList { get; set; }
            ///// <summary>
            ///// 知识点
            ///// </summary>
            //public List<Knowledgepoint> knowledgePointList { get; set; }
            ///// <summary>
            ///// 任务+步骤+分数+标准（考核试卷）
            ///// </summary>
            //public string operations { get; set; }
            ///// <summary>
            ///// 习题题目+选项（考核试卷）
            ///// </summary>
            //public string content { get; set; }
            /// <summary>
            /// 分数
            /// </summary>
            public int score { get; set; }


            ///// <summary>
            ///// 得分点（步数）
            ///// </summary>
            //public int scorePoints { get; set; }
            ///// <summary>
            ///// 百科来源
            ///// </summary>
            //public int origin { get; set; }

            /// <summary>
            /// 用于多个Item Prefab的列表实例化
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                switch (typeId)
                {
                    case (int)PediaType.Picture:
                        return 1;
                    case (int)PediaType.ANV:
                        if (typeDescription.Equals(RequestManager.VID))
                            return 1;
                        return 0;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// 百科
        /// </summary>
        public class Encyclopedia
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 百科名称
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 百科封面路径
            /// </summary>
            public string iconPath { get; set; }
            /// <summary>
            /// 百科类型
            /// </summary>
            public int typeId { get; set; }
            /// <summary>
            /// 百科来源
            /// </summary>
            public int origin { get; set; }
            ///// <summary>
            ///// 超链接
            ///// </summary>
            //public string hyperlink { get; set; }
            /// <summary>
            /// 知识点
            /// </summary>
            public List<Knowledgepoint> knowledgePointList { get; set; }
            ///// <summary>
            ///// 分数
            ///// </summary>
            //public List<int> scores { get; set; }

            public string typeDescription { get; set; }

            public int totalScore { get; set; }

            /// <summary>
            /// 用于多个Item Prefab的列表实例化
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                switch (typeId)
                {
                    case (int)PediaType.Picture:
                    case (int)PediaType.ANV:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// 操作百科
        /// </summary>
        public class EncyclopediaOperation : EncyclopediaModel
        {
            /// <summary>
            /// 是否漫游
            /// </summary>
            public bool hasRole { get; set; }
            /// <summary>
            ///  任务+步骤+分数+标准
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string operations { get; set; }
            /// <summary>
            /// 任务集合
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<Flow> flows;

            public AnswerOp answerOp { get; set; }
        }

        public class Flow
        {
            /// <summary>
            /// 任务id
            /// </summary>
            public string id;
            /// <summary>
            /// 显示任务文本
            /// </summary>
            public string title;
            /// <summary>
            /// 步骤集合
            /// </summary>
            public List<Step> children;
        }
        /// <summary>
        /// 步骤
        /// </summary>
        public class Step
        {
            /// <summary>
            /// 步骤id
            /// </summary>
            public string id;
            /// <summary>
            /// 显示步骤文本
            /// </summary>
            public string title;
            /// <summary>
            /// 分值
            /// </summary>
            public float score;
            /// <summary>
            /// 评分标准
            /// </summary>
            public string standard;
        }
        /// <summary>
        /// 模型百科 拆分动画
        /// </summary>
        public class EncyclopediaModel : Encyclopedia
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ProjectAB data { get; set; }
            /// <summary>
            /// 模型节点
            /// </summary>
            public List<ModelNode> modelNodes { get; set; }
        }

        public class ProjectAB
        {
            /// <summary>
            /// 工程ID
            /// </summary>
            public int projectId { get; set; }
            /// <summary>
            /// AB包信息
            /// </summary>
            public List<ABPackage> abPackageList { get; set; }//abPackage
        }

        /// <summary>
        /// 链接百科 图片音视频文档
        /// </summary>
        public class EncyclopediaLink : Encyclopedia
        {
            /// <summary>
            /// 地址信息
            /// </summary>
            public string data { get; set; }
        }

        /// <summary>
        /// 习题百科
        /// </summary>
        public class EncyclopediaExercise : Encyclopedia
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ExerciseData data { get; set; }

            public AnswerExercise answerExercise { get; set; }
        }


        public class ExerciseData
        {
            public List<string> scores { get; set; }
            public Exercise exercise { get; set; }
        }

        public class Exercise
        {
            public int id { get; set; }
            public string content { get; set; }
            //选择题 1 (单选;多选); 判断题 2 ; 操作题 3
            public int type { get; set; }
        }

        /// <summary>
        /// AB包信息
        /// </summary>
        public class ABPackage
        {
            /// <summary>
            /// ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 文件名
            /// </summary>
            public string fileName { get; set; }
            /// <summary>
            /// 文件路径
            /// </summary>
            public string filePath { get; set; }
        }

        /// <summary>
        /// 百科类型
        /// </summary>
        public enum PediaType
        {
            Unknown,
            /// <summary>
            /// 图片
            /// </summary>
            Picture,
            /// <summary>
            /// 音视频
            /// </summary>
            ANV,
            /// <summary>
            /// 文档
            /// </summary>
            Doc,
            /// <summary>
            /// 拆解
            /// </summary>
            Disassemble,
            /// <summary>
            /// 动画
            /// </summary>
            Animation,
            /// <summary>
            /// 操作
            /// </summary>
            Operation,
            /// <summary>
            /// 习题
            /// </summary>
            Exercise,
            /// <summary>
            /// 空白
            /// </summary>
            Blank
        }

        /// <summary>
        /// 百科来源
        /// </summary>
        public enum PediaOrigin
        {
            Unknown,
            /// <summary>
            /// 工程百科
            /// </summary>
            Project,
            /// <summary>
            /// 定制百科
            /// </summary>
            ABPackage,
            /// <summary>
            /// 分享百科
            /// </summary>
            Link
        }

        /// <summary>
        /// 习题 选择题
        /// </summary>
        public struct ExerciseContent
        {
            public ExerciseQuestion question { get; set; }
            public List<ExerciseAnswer> answers { get; set; }
        }

        /// <summary>
        /// 习题 
        /// </summary>
        public struct JudgementExerciseContent
        {
            public ExerciseQuestion question { get; set; }
            public bool answers { get; set; }
        }

        /// <summary>
        /// 习题题干
        /// </summary>
        public struct ExerciseQuestion
        {
            public string text { get; set; }
            public string image { get; set; }
            public string audio { get; set; }
            public string video { get; set; }

            public ExerciseQuestion(string text)
            {
                this.text = text;
                this.image = string.Empty;
                this.audio = string.Empty;
                this.video = string.Empty;
            }
        }
        /// <summary>
        /// 习题选项
        /// </summary>
        public struct ExerciseAnswer
        {
            public bool right { get; set; }
            public ExerciseQuestion content { get; set; }
        }

        ///// <summary>
        ///// 习题选项
        ///// </summary>
        //public struct ExerciseChoice
        //{
        //    public bool right { get; set; }
        //    public ExerciseContent content { get; set; }
        //}

        #endregion

        #region 知识点
        /// <summary>
        /// 知识点
        /// </summary>
        public class Knowledgepoint
        {
            /// <summary>
            /// 知识点ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 唯一编号
            /// </summary>
            public string uuid { get; set; }
            /// <summary>
            /// 知识点标题 or 文件名
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// 知识点内容 or 文件路径
            /// </summary>
            public string content { get; set; }
            /// <summary>
            /// 所属百科ID
            /// </summary>
            public int encyclopediaId { get; set; }
            /// <summary>
            /// 知识点类型ID
            /// </summary>
            public int typeId { get; set; }
            /// <summary>
            /// 知识点类型 TXT A&V IMG DOC PPT
            /// </summary>
            public string type { get; set; }

            public string docType
            {
                get
                {
                    return FileExtension.Convert(title, type);
                }
            }

            /// <summary>
            /// 用于多个Item Prefab的列表实例化
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                switch (docType)
                {
                    case FileExtension.TXT:
                        return 0;
                    case FileExtension.DOC:
                    case FileExtension.PPT:
                    case FileExtension.XLS:
                    case FileExtension.PDF:
                        return 1;
                    case FileExtension.IMG:
                        return 2;
                    case FileExtension.MP4:
                        return 3;
                    case FileExtension.MP3:
                        return 4;
                    default:
                        return -1;
                }
            }
        }

        /// <summary>
        /// 创建知识点请求类
        /// </summary>
        public class AddKnowledgepointRequest
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int encyclopediaId;
            /// <summary>
            /// 唯一编号
            /// </summary>
            public string uuid;
            /// <summary>
            /// 知识点标题
            /// </summary>
            public string title;
            /// <summary>
            /// 知识点内容
            /// </summary>
            public string content;

            public AddKnowledgepointRequest(int encyclopediaId, string uuid, string title, string content)
            {
                this.encyclopediaId = encyclopediaId;
                this.uuid = uuid;
                this.title = title;
                this.content = content;
            }
        }

        /// <summary>
        /// 修改知识点请求类
        /// </summary>
        public class EditKnowledgepointRequest
        {
            /// <summary>
            /// 知识点ID
            /// </summary>
            public int id;
            /// <summary>
            /// 百科ID
            /// </summary>
            public int encyclopediaId;
            /// <summary>
            /// 唯一编号
            /// </summary>
            public string uuid;
            /// <summary>
            /// 知识点标题
            /// </summary>
            public string title;
            /// <summary>
            /// 知识点内容
            /// </summary>
            public string content;

            public EditKnowledgepointRequest(int id, int encyclopediaId, string uuid, string title, string content)
            {
                this.id = id;
                this.encyclopediaId = encyclopediaId;
                this.uuid = uuid;
                this.title = title;
                this.content = content;
            }
        }

        /// <summary>
        /// 修改知识点顺序请求类
        /// </summary>
        public class SortKnowledgepointRequest
        {
            /// <summary>
            /// 百科内知识点ID 按顺序传
            /// </summary>
            public List<int> idArray;

            public SortKnowledgepointRequest(List<int> idArray)
            {
                this.idArray = idArray;
            }
        }

        /// <summary>
        /// 批量添加超链接（知识点）请求类
        /// </summary>
        public class AddBatchKnowledgepointRequest
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int encyclopediaId;
            /// <summary>
            /// 唯一编号
            /// </summary>
            public string uuid;
            /// <summary>
            /// 课件资源ID
            /// </summary>
            public List<int> idArray;

            public AddBatchKnowledgepointRequest(int encyclopediaId, string uuid, List<int> idArray)
            {
                this.encyclopediaId = encyclopediaId;
                this.uuid = uuid;
                this.idArray = idArray;
            }
        }

        /// <summary>
        /// 替换超链接（知识点）请求类
        /// </summary>
        public class ReplaceKnowledgepointRequest
        {
            /// <summary>
            /// 知识点id
            /// </summary>
            public int id;
            /// <summary>
            /// 课件资源ID
            /// </summary>
            public int resourceId;

            public ReplaceKnowledgepointRequest(int id, int resourceId)
            {
                this.id = id;
                this.resourceId = resourceId;
            }
        }
        #endregion

        #region 模型节点
        /// <summary>
        /// 模型节点编辑请求
        /// </summary>
        public class ModelNode
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int encyclopediaId { get; set; }
            /// <summary>
            /// 名称
            /// </summary>
            public string nodeName { get; set; }
            /// <summary>
            /// UUID
            /// </summary>
            public string uuid { get; set; }
        }
        /// <summary>
        /// 任务和步骤节点编辑请求
        /// </summary>
        public class StepNode
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 名称
            /// </summary>
            public string operations { get; set; }
        }
        #endregion

        #region 课件资源
        /// <summary>
        /// 课件资源
        /// </summary>
        public class Courseware
        {
            public string fileName { get; set; }
            public string filePath { get; set; }
            public int id { get; set; }
            public int schoolId { get; set; }
            public string type { get; set; }

            public string docType
            {
                get
                {
                    return FileExtension.Convert(fileName, type);
                }
            }
        }

        public class FileExtension
        {
            public const string TXT = "TXT";
            public const string IMG = "IMG";
            public const string JPG = "JPG";
            public const string JPEG = "JPEG";
            public const string PNG = "PNG";
            public const string ANV = "A&V";
            public const string MP3 = "MP3";
            public const string OGG = "OGG";
            public const string WAV = "WAV";
            public const string MP4 = "MP4";
            public const string AVI = "AVI";
            public const string PPT = "PPT";
            public const string DOC = "DOC";
            public const string XLS = "XLS";
            public const string PDF = "PDF";

            public static string Convert(string fileName, string type)
            {
                string temp;
                switch (type)
                {
                    case DOC:
                        temp = System.IO.Path.GetExtension(fileName);
                        if (temp.Length > 0)
                        {
                            temp = temp.ToUpper().Substring(1);

                            if (temp.StartsWith(DOC))
                                temp = DOC;

                            if (temp.StartsWith(PPT))
                                temp = PPT;

                            if (temp.StartsWith(XLS))
                                temp = XLS;

                            if (temp.StartsWith(PDF))
                                temp = PDF;
                        }
                        return temp;
                    case ANV:
                        temp = System.IO.Path.GetExtension(fileName);
                        if (temp.Length > 0)
                        {
                            temp = temp.ToUpper().Substring(1);

                            if (temp.StartsWith(MP4) || temp.StartsWith(AVI))
                                temp = MP4;

                            if (temp.StartsWith(MP3) || temp.StartsWith(WAV))
                                temp = MP3;
                        }
                        return temp;
                    default:
                        return type;
                }
            }

            public static string Convert(string fileName)
            {
                string temp = System.IO.Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(temp))
                {
                    temp = temp.ToUpper().Substring(1);

                    if (temp.StartsWith(JPG) || temp.StartsWith(JPEG) || temp.StartsWith(PNG))
                        temp = IMG;

                    if (temp.StartsWith(MP4) || temp.StartsWith(AVI))
                        temp = MP4;

                    if (temp.StartsWith(MP3) || temp.StartsWith(WAV))
                        temp = MP3;

                    if (temp.StartsWith(PDF))
                        temp = PDF;

                    if (temp.StartsWith(DOC))
                        temp = DOC;

                    if (temp.StartsWith(PPT))
                        temp = PPT;

                    if (temp.StartsWith(XLS))
                        temp = XLS;
                }
                return temp;
            }
        }

        public class CoursewareResourceList
        {
            public Paging paging;
            public List<Courseware> records;
        }

        /// <summary>
        /// 新增课件资源信息请求类
        /// </summary>
        public class AddCoursewareResourceRequest
        {
            public string fileName;
            public string filePath;

            public AddCoursewareResourceRequest(string fileName, string filePath)
            {
                this.fileName = fileName;
                this.filePath = filePath;
            }
        }

        /// <summary>
        /// 批量删除课件资源信息请求类
        /// </summary>
        public class BatchCoursewareResourceRequest
        {
            /// <summary>
            /// 资源id
            /// </summary>
            public List<int> idList;

            public BatchCoursewareResourceRequest(List<int> idList)
            {
                this.idList = idList;
            }
        }
        #endregion

        #region 考核
        public class CreateExamRecordRequest
        {
            public int courseId;
            public string name;
            public bool teamWork;
        }

        public class StartExamRecordRequest
        {
            /// <summary>
            /// 考核id
            /// </summary>
            public int examineId;
            /// <summary>
            /// 考核学员列表
            /// </summary>
            public List<ExamRecordMember> examinee;
        }

        public class ExamRecordMember
        {
            /// <summary>
            /// 考核用户id
            /// </summary>
            public int examineeId;
            /// <summary>
            /// 考核用户工号
            /// </summary>
            public string examineeNo;
            /// <summary>
            /// 考核用户显示名称
            /// </summary>
            public string examineeName;
        }

        public class EndExamRequest
        {
            /// <summary>
            /// 考核id
            /// </summary>
            public int examineId;
        }

        public class Examination
        {
            public int examineId { get; set; }
            public string examineName { get; set; }
            public string startTime { get; set; }
            public string endTime { get; set; }
            public bool ended { get; set; }
            /// <summary>
            /// 课程编号
            /// </summary>
            public int id;
            /// <summary>
            /// 课程名称
            /// </summary>
            public string name;
            /// <summary>
            /// 图标url
            /// </summary>
            public string iconPath;
            /// <summary>
            /// 备注
            /// </summary>
            public string remarks;
            /// <summary>
            /// 百科列表
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<Encyclopedia> encyclopediaList;
            /// <summary>
            /// 时长(分钟)
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int duration;
            /// <summary>
            /// 创建者
            /// </summary>
            public int creatorId;
            /// <summary>
            /// 多个标签
            /// </summary>
            public string tags;
            public string tags_readable;
            /// <summary>
            /// 是否上架
            /// </summary>
            public bool online;
        }

        /// <summary>
        /// 考核记录列表
        /// </summary>
        public class HisExamList
        {  /// <summary>
           /// 考核记录列表
           /// </summary>
            public List<HisExam> records;
        }

        /// <summary>
        /// 单条考核记录数据
        /// </summary>
        public class HisExam
        {
            /// <summary>
            /// 考核ID
            /// </summary>
            public int id;
            /// <summary>
            /// 考核名称
            /// </summary>
            public string name;
            /// <summary>
            /// 考核试卷ID
            /// </summary>
            public int courseId;
            /// <summary>
            /// 考核试卷名称
            /// </summary>
            public string courseName;
            /// <summary>
            /// 考核创建时间
            /// </summary>
            public string createTime;
            /// <summary>
            /// 考核时长（分钟）
            /// </summary>
            public int duration;
            /// <summary>
            /// 考核创建者名称
            /// </summary>
            public string creator;
            /// <summary>
            /// 是否阅卷
            /// </summary>
            public bool allChecked;
        }

        /// <summary>
        /// 考核成绩列表
        /// </summary>
        public class ExamPersonnalList
        {
            /// <summary>
            /// 考核成绩列表
            /// </summary>
            public List<ExamResult> records;
        }

        public class ExamResult
        {
            /// <summary>
            /// 考核记录id
            /// </summary>
            public int id;
            /// <summary>
            /// 考生ID
            /// </summary>
            public int examineeId;
            /// <summary>
            /// 考生工号
            /// </summary>
            public string examineeNo;
            /// <summary>
            /// 考生名字
            /// </summary>
            public string examineeName;
            /// <summary>
            /// 考试用时
            /// </summary>
            public int examineTime;
            /// <summary>
            /// 是否结束答题
            /// </summary>
            public bool ended;
            /// <summary>
            /// 得分
            /// </summary>
            public float score;
            /// <summary>
            /// 答案
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string answers;
            /// <summary>
            /// 视频附件
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<Accessory> accessoryList;

            public bool allChecked { get; set; }
        }

        /// <summary>
        /// TODO 待删
        /// </summary>

        public class SubmitExamRecordRequest
        {
            /// <summary>
            /// 附件列表 考核各百科录屏
            /// </summary>
            public List<Accessory> accessoryList;
            /// <summary>
            /// 操作结果记录
            /// </summary>
            public string answers;
            /// <summary>
            /// 考核ID
            /// </summary>
            public int examineId;
        }
    
        /// <summary>
        /// 考核结果
        /// </summary>
        public class Answer
        {
            /// <summary>
            /// 百科ID
            /// </summary>
            public int baikeId { get; set; }
            /// <summary>
            /// 题目
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// 百科类型
            /// </summary>
            public int typeId { get; set; }
        }

        /// <summary>
        /// 操作百科记录结果
        /// </summary>
        public class AnswerOp : Answer
        {
            /// <summary>
            /// 任务+步骤
            /// </summary>
            public List<AnswerFlow> children;
            /// <summary>
            /// 考核记录
            /// </summary>
            public List<ExamineResultOperation> operations { get; set; }
            /// <summary>
            /// 模型状态
            /// </summary>
            public List<ExamineResultModelState> modelStates { get; set; }
        }

        public class AnswerFlow
        {
            /// <summary>
            /// 任务描述
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// 步骤集合
            /// </summary>
            public List<AnswerStep> children;
        }

        public class AnswerStep
        {
            /// <summary>
            /// 步骤描述
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// 标准分
            /// </summary>
            public float score { get; set; }
            /// <summary>
            /// 标准答案(评分标准)
            /// </summary>
            public string standard { get; set; }
            /// <summary>
            /// 得分
            /// </summary>
            public float getScore { get; set; }
        }

        /// <summary>
        /// 习题记录结果
        /// </summary>
        public class AnswerExercise : Answer
        {
            /// <summary>
            /// 标准分
            /// </summary>
            public float score { get; set; }
            /// <summary>
            /// 标准答案(评分标准)
            /// </summary>
            public string standard { get; set; }
            /// <summary>
            /// 记录操作
            /// </summary>
            public string operation { get; set; }
            /// <summary>
            /// 得分
            /// </summary>
            public float getScore { get; set; }
        }
  
        /// <summary>
        /// 阅卷提交的数据，服务器只保存JSON结果，所以每次都要提交完整的结果，如果不是全部完成，allChecked要设为false
        /// </summary>
        public class SubmitCheckPaperRequest
        {
            /// <summary>
            /// 是否全部完成
            /// </summary>
            public bool allChecked;
            /// <summary>
            /// 阅卷结果
            /// </summary>
            public string answers;
            /// <summary>
            /// 正确步骤数
            /// todo:暂未正常使用
            /// </summary>
            public int correct;
            /// <summary>
            /// 考核id
            /// </summary>
            public int id;
            /// <summary>
            /// 得分
            /// </summary>
            public float score;
            /// <summary>
            /// 错误步骤数字
            /// todo:暂未正常使用
            ///</summary>
            public int wrong;
        }


        public class ExamineResultRequest
        {
            public int examineId { get; set; }
            public int encyclopediaId { get; set; }
            /// <summary>
            /// 操作题
            /// </summary>
            public ExamineResultOperation[] operations { get; set; }
            /// <summary>
            /// 模型状态
            /// </summary>
            public ExamineResultModelState[] modelStates { get; set; }
        }


        public class ExamineResultOperation
        {
            public int index { get; set; }
            public string userNo { get; set; }
            public string userName { get; set; }
            /// <summary>
            /// 操作
            /// </summary>
            public string msg { get; set; }
            /// <summary>
            /// 习题
            /// </summary>
            public string operation { get; set; }

            public string createTime { get; set; }
            public int type { get; set; }
        }


        public class ExamineResultModelState
        {
            public int index { get; set; }
            public string id { get; set; }
            public string optionName { get; set; }
            public string uiTargetModelEulerZ { get; set; }
        }

        public class ExamineResultAccessoryRequest
        {
            /// <summary>
            /// 考核ID
            /// </summary>
            public int examineId;
            /// <summary>
            /// 附件列表 考核各百科录屏
            /// </summary>
            public List<Accessory> accessoryList;
        }

        /// <summary>
        /// 附件
        /// </summary>
        public class Accessory
        {
            public int encyclopediaId;
            public string filePath;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? examineeId;
        }

        #endregion

        #region 语音数据
        public class SpeechData
        {
            // 0-未生成语音，1-已生成语音
            public int audioStatus { get; set; }
            public string audioUrl { get; set; }
            public int encyclopediaId { get; set; }
            public int id { get; set; }
            public string tipType { get; set; }
            public TipType Type()
            {
                switch (tipType)
                {
                    case stepName:
                        return TipType.StepName;
                    case stepComplete:
                        return TipType.StepComplete;
                    case tips:
                        return TipType.Tips;
                    default:
                        return TipType.StepName;
                }
            }
            public int sort { get; set; }
            public string stepId { get; set; }
            public string text { get; set; }
            public string unicode { get; set; }
            #region 
            public int top { get; set; } = 120;
            public int left { get; set; } = 320;
            public int width { get; set; } = 400;
            public int height { get; set; } = 300;
            #endregion

        }

        private const string stepName = "步骤名称";
        private const string stepComplete = "操作完成";
        private const string tips = "tips";

        public enum TipType
        {
            StepName = 1,
            StepComplete,
            Tips
        }
        #endregion

        #region 其他
        /// <summary>
        /// 分页信息
        /// </summary>
        public class Paging
        {
            /// <summary>
            /// 当前页
            /// </summary>
            public int page { get; set; }
            /// <summary>
            /// 页大小
            /// </summary>
            public int pagesize { get; set; }
            /// <summary>
            /// 总条数
            /// </summary>
            public int total { get; set; }
        }
        public class OSSConfig
        {
            public string accessKeyId { get; set; }
            public string accessKeySecret { get; set; }
            public string bucket { get; set; }
            public string endpoint { get; set; }
            public string securityToken { get; set; }
        }

        public class StsBase
        {
            public enum StoreType
            {
                AliyunOSS,
                Minio
            }

            public string storeType { get; set; }

            public StoreType StorageType { get; set; }
        }

        public class AliyunOSSStsInfo : StsBase
        {
            public string securityToken { get; set; }
            public string expiration { get; set; }
            public string accessKeySecret { get; set; }
            public string accessKeyId { get; set; }
            public string bucket { get; set; }
            public string endpoint { get; set; }
        }

        public class MinioStsInfo : StsBase
        {
            public string accessKey { get; set; }
            public string secretKey { get; set; }
            public string sessionToken { get; set; }
            public string host { get; set; }
            public int port { get; set; }
            public string region { get; set; }
            public string bucket { get; set; }
        }
        #endregion
    }
}