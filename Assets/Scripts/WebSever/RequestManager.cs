using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using static UnityFramework.Runtime.RequestData;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// web服务器请求管理类
    /// </summary>
    [RequireComponent(typeof(RequestBase))]
    public class RequestManager : Singleton<RequestManager>
    {
        private RequestBase requestBase;
        protected override void InstanceAwake()
        {
            requestBase = GetComponent<RequestBase>();
        }

        #region 版本管理
        /// <summary>
        /// 获取最新版本
        /// </summary>
        /// <param name="nickName"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetLatestVersion(int state, UnityAction<Version> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("获取最新版本", RequestType.GET, $"{ApiData.LatestVersion}?state={state}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="nickName"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetVersionInfo(string version, UnityAction<Version> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("获取版本信息", RequestType.GET, $"{ApiData.VersionInfo}?version={version}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        #endregion

        #region 平台登录管理
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="accountStr"></param>
        /// <param name="pwdStr"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void Login(string accountStr, string pwdStr, UnityAction<Account, string> successCallBack, UnityAction<int, string> failureCallBack)
        {
            if (GlobalInfo.isOffLine)
            {
                string result = ConfigXML.GetData(ConfigType.Cache, DtataType.LocalSever, RequestBase.GetNewUrl(ApiData.Login));

                this.WaitTime(0.01f, () =>
                {
                    Log.Debug("LocalServer返回数据字符串：<color=green>" + result + "</color>");
                    if (string.IsNullOrEmpty(result))
                        failureCallBack.Invoke(0, "请先正常登录后下载资源");
                    else
                        GetRequest<Account>(true, result, account => successCallBack.Invoke(account, result), failureCallBack);
                });
            }
            else
            {
                LoginRequest loginRequest = new LoginRequest()
                {
                    username = accountStr,
                    password = Encryption.MD5Encrypt(pwdStr)
                };
                string json = JsonTool.Serializable(loginRequest);
                requestBase.TryRequest_List("登录", RequestType.POST, ApiData.Login, json, (request, message) =>
                {
                    GetRequest<Account>(request, message, account => successCallBack.Invoke(account, message), failureCallBack);
                }, false);
            }
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="nickName"></param>
        /// <param name="password"></param>
        /// <param name="captcha"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void Register(string phoneNumber, string nickName, string password, string captcha, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            RegisterRequest registerRequest = new RegisterRequest()
            {
                captcha = captcha,
                nickName = nickName,
                password = Encryption.MD5Encrypt(password),
                phoneNumber = phoneNumber
            };
            string json = JsonTool.Serializable(registerRequest);
            requestBase.TryRequest_List("注册", RequestType.POST, ApiData.Register, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 注册验证码
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void RegisterCaptcha(string phoneNumber, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            CaptchaRequest captchaRequest = new CaptchaRequest()
            {
                phoneNumber = phoneNumber
            };
            string json = JsonTool.Serializable(captchaRequest);
            requestBase.TryRequest_List("注册验证码", RequestType.POST, ApiData.RegisterCaptcha, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 忘记密码
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="password"></param>
        /// <param name="captcha"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ForgetPassword(string phoneNumber, string password, string captcha, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            ForgetPasswordRequest forgetPasswordRequest = new ForgetPasswordRequest()
            {
                captcha = captcha,
                password = Encryption.MD5Encrypt(password),
                phoneNumber = phoneNumber
            };
            string json = JsonTool.Serializable(forgetPasswordRequest);
            requestBase.TryRequest_List("忘记密码", RequestType.POST, ApiData.ForgetPassword, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 忘记密码验证码
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ForgetPwdCaptcha(string phoneNumber, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            CaptchaRequest captchaRequest = new CaptchaRequest()
            {
                phoneNumber = phoneNumber
            };
            string json = JsonTool.Serializable(captchaRequest);
            requestBase.TryRequest_List("忘记密码验证码", RequestType.POST, ApiData.ForgetCaptcha, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void RefreshToken(string refreshToken, UnityAction<Token> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("刷新token", RequestType.POST, ApiData.NewRefreshToken, refreshToken, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        #endregion

        #region 个人信息管理
        /// <summary>
        /// 修改个人信息（昵称）
        /// </summary>
        /// <param name="nickName"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangeNickName(string nickName, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            PersonalRequest personalRequest = new PersonalRequest() 
            { 
                realName = nickName,
                userNo = GlobalInfo.account.userNo,
                userOrgName = GlobalInfo.account.userOrgName
            };
            string json = JsonTool.Serializable(personalRequest);
            requestBase.TryRequest_List("修改个人信息", RequestType.PUT, ApiData.Profile, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 修改个人信息（工号）
        /// </summary>
        /// <param name="nickName"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangeUserNo(string userNo, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            PersonalRequest personalRequest = new PersonalRequest()
            {
                realName = GlobalInfo.account.nickname,
                userNo = string.IsNullOrEmpty(userNo) ? null : userNo,
                userOrgName = GlobalInfo.account.userOrgName
            };
            requestBase.TryRequest_List("修改工号", RequestType.PUT, ApiData.Profile, JsonTool.Serializable(personalRequest), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 修改个人信息（单位）
        /// </summary>
        /// <param name="nickName"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangeUserOrg(string userOrg, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            PersonalRequest personalRequest = new PersonalRequest()
            {
                realName = GlobalInfo.account.nickname,
                userNo = GlobalInfo.account.userNo,
                userOrgName = userOrg
            };
            requestBase.TryRequest_List("修改单位", RequestType.PUT, ApiData.Profile, JsonTool.Serializable(personalRequest), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 旧密码修改密码
        /// </summary>
        /// <param name="newPassword"></param>
        /// <param name="password"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangePassword(string newPassword, string password, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            PersonalPasswordRequest_Old personalPwdRequest = new PersonalPasswordRequest_Old()
            {
                newPassword = Encryption.MD5Encrypt(newPassword),
                password = Encryption.MD5Encrypt(password)
            };
            string json = JsonTool.Serializable(personalPwdRequest);
            requestBase.TryRequest_List("修改密码", RequestType.PUT, ApiData.ProfilePassword, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 验证码修改密码 
        /// </summary>
        /// <param name="captcha"></param>
        /// <param name="password"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangePassword_Captcha(string captcha, string password, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            PersonalPasswordRequest personalPwdRequest = new PersonalPasswordRequest()
            {
                captcha = captcha,
                password = Encryption.MD5Encrypt(password),
            };
            string json = JsonTool.Serializable(personalPwdRequest);
            requestBase.TryRequest_List("修改密码", RequestType.POST, ApiData.PasswordChange, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 验证码解绑定手机号
        /// </summary>
        /// <param name="captcha"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ReBindPhoneNumber(string captcha, UnityAction<string> successCallBack, UnityAction<int, string> failureCallBack)
        {
            ReBindPhoneNumberRequest personalPwdRequest = new ReBindPhoneNumberRequest()
            {
                captcha = captcha
            };
            string json = JsonTool.Serializable(personalPwdRequest);
            requestBase.TryRequest_List("解绑手机", RequestType.POST, ApiData.Captcha_BindPhoneNumber_Old, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        /// <summary>
        /// 验证码绑定手机号
        /// </summary>
        /// <param name="captcha"></param>
        /// <param name="passport"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void BindPhoneNumber(string captcha, string passport, string phoneNumber, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            BindPhoneNumberRequest personalPwdRequest = new BindPhoneNumberRequest()
            {
                captcha = captcha,
                passport = passport,
                phoneNumber = phoneNumber
            };
            string json = JsonTool.Serializable(personalPwdRequest);
            requestBase.TryRequest_List("绑定手机", RequestType.POST, ApiData.AccountBind, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        #endregion

        #region 验证码
        /// <summary>
        /// 这个接口很特殊 验证码分为四种
        /// 重绑手机 新手机
        /// 重绑手机 旧手机
        /// 修改密码
        /// 忘记密码
        /// 我不知道为何要独立出来
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="path">ApiData.Captcha</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetCaptcha(string phoneNumber, string path, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            string url;
            if (string.IsNullOrEmpty(phoneNumber))
                url = path;
            else
                url = $"{path}?phoneNumber={phoneNumber}";

            requestBase.TryRequest_List("获取验证码", RequestType.GET, url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }
        #endregion

        #region 账号\单位管理
        /// <summary>
        /// 获取首页授权
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        //public void GetNavigation(UnityAction<Navigation> successCallBack, UnityAction<string> failureCallBack)
        //{
        //    requestBase.TryRequest_List("获取首页授权", RequestType.GET, ApiData.Navigation, string.Empty, (result, message) =>
        //    {
        //        GetRequest(result, message, successCallBack, failureCallBack);
        //    });
        //}

        /// <summary>
        /// 获取总览
        /// </summary>
        /// <param name="overviewId"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetOverview(int overviewId, UnityAction<Overview> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取总览模型", RequestType.GET, ApiData.Overview + "/" + overviewId, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        /// <summary>
        /// 加入单位(二维码)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void JoinOrg(string url, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("加入单位", RequestType.GET, url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, true);
        }
        /// <summary>
        /// 加入单位(邀请码)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void JoinOrgInviteCode(string inviteCode, UnityAction<int> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("凭邀请码加入单位", RequestType.GET, ApiData.InviteCode + "/" + inviteCode, string.Empty, (result, message) =>
             {
                 GetRequest(result, message, successCallBack, failureCallBack);
             }, true);
        }

        /// <summary>
        /// 获取单位UI主题
        /// </summary>
        /// <param name="overviewId"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetOrgTheme(UnityAction<OrgTheme> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取UI主题", RequestType.GET, ApiData.Theme, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            });
        }

        #endregion

        #region 元数据
        /// <summary>
        /// 获取课程分类列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetTeachCategoryList(UnityAction<List<TeachCategory>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取课程分类列表", RequestType.GET, ApiData.TeachCategory, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取课程标签列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetTeachTagList(UnityAction<List<TeachTag>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取课程标签列表", RequestType.GET, ApiData.TeachTag, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 标签管理
        /// <summary>
        /// 获取课程标签列表
        /// </summary>
        /// <param name="type">1-课程标签， 3-考核课程标签</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetTagList(int type, UnityAction<TagList> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取标签列表", RequestType.GET, ApiData.Tag + "?page=1&pageSize=500&type=" + type, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 课程管理
        /// <summary>
        /// 获取课程列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetCourseList(UnityAction<List<Course>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取课程列表", RequestType.GET, ApiData.CourseListForClient, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取课程AB包列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetCourseABPackageList(UnityAction<List<CourseAB>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取课程AB包列表", RequestType.GET, ApiData.CourseABPackageList, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        public static string PIC = "图片";
        public static string AUD = "音频";
        public static string VID = "视频";
        public static string ANV = "音视频";
        public static string WORD = "WORD";
        public static string ASSEM = "拆分";
        public static string ANIM = "动画";
        public static string OP = "操作";
        public static string CHOICE = "单选题";
        public static string MULCHOICE = "多选题";
        public static string JUDGE = "判断题";

        /// <summary>
        /// 获取课程
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetCourse(int courseId, UnityAction<Course> successCallBack, UnityAction<string> failureCallBack)
        {
            string Url = ApiData.Course + "/" + courseId;
            requestBase.TryRequest_List("获取课程", RequestType.GET, Url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, (string data) =>
                {
                    Course course = new Course();
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        course.id = jObject["id"].Value<int>();
                        course.name = jObject["name"].Value<string>();
                        course.iconPath = jObject["iconPath"].Value<string>();
                        course.remarks = jObject["remarks"]?.Value<string>();
                        //course.teachCategory = jObject["teachCategory"].Value<string>();
                        //course.teachTag = jObject["teachTag"].Value<string>();
                        //course.teachTagId = jObject["teachTagId"].Value<int>();

                        course.creatorId = jObject["creatorId"].Value<int>();
                        course.online = jObject["online"].Value<bool>();

                        List<Encyclopedia> encyclopediaList = new List<Encyclopedia>();
                        foreach (JToken token in jObject["encyclopediaList"])
                        {
                            Encyclopedia encyclopedia = new Encyclopedia();
                            encyclopedia.id = token["id"].Value<int>();
                            encyclopedia.name = token["name"].Value<string>();
                            encyclopedia.typeId = token["typeId"].Value<int>();
                            encyclopedia.iconPath = token["iconPath"]?.Value<string>();
                            switch (encyclopedia.typeId)
                            {
                                case (int)PediaType.Picture:
                                    //确保图片百科封面图显示图片内容
                                    encyclopedia.iconPath = token["data"].Value<string>();
                                    encyclopedia.typeDescription = PIC;
                                    break;
                                case (int)PediaType.ANV:
                                    string content = token["data"].Value<string>();
                                    string fileExtension = FileExtension.Convert(content);
                                    switch (fileExtension)
                                    {
                                        case FileExtension.MP3:
                                            encyclopedia.typeDescription = AUD;
                                            break;
                                        case FileExtension.MP4:
                                            //获取视频第一帧作为封面图
                                            encyclopedia.iconPath = content;
                                            encyclopedia.typeDescription = VID;
                                            break;
                                        default:
                                            encyclopedia.typeDescription = ANV;
                                            break;
                                    }
                                    break;
                                case (int)PediaType.Doc:
                                    string docExtension = FileExtension.Convert(token["data"].Value<string>());
                                    switch (docExtension)
                                    {
                                        case FileExtension.DOC:
                                            encyclopedia.typeDescription = WORD;
                                            break;
                                        default:
                                            encyclopedia.typeDescription = docExtension;
                                            break;
                                    }
                                    break;
                                case (int)PediaType.Disassemble:
                                    encyclopedia.typeDescription = ASSEM;
                                    break;
                                case (int)PediaType.Animation:
                                    encyclopedia.typeDescription = ANIM;
                                    break;
                                case (int)PediaType.Operation:
                                    encyclopedia.typeDescription = OP;
                                    break;
                                case (int)PediaType.Exercise:
                                    int exerciseType = token["data"]["exercise"]["type"].Value<int>();
                                    switch (exerciseType)
                                    {
                                        case 1:
                                            ExerciseContent exerciseContent = JsonTool.DeSerializable<ExerciseContent>(token["data"]["exercise"]["content"].Value<string>());
                                            bool multipleChoice = exerciseContent.answers.FindAll(a => a.right == true).Count > 1;
                                            encyclopedia.typeDescription = multipleChoice ? MULCHOICE : CHOICE;
                                            break;
                                        case 2:
                                            encyclopedia.typeDescription = JUDGE;
                                            break;
                                    }
                                    break;
                            }
                            encyclopediaList.Add(encyclopedia);
                        }
                        course.encyclopediaList = encyclopediaList;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析课程Json失败 {ex.Message}");
                    }
                    successCallBack.Invoke(course);
                }, failureCallBack);
            });
        }

        /// <summary>
        /// 累积课程学习时长
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="durationSecs"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void AddCourseStudyDuration(int courseId, int durationSecs, UnityAction successCallBack, UnityAction<string> failureCallBack)
        {
            SumStudyDurationRequest sumStudyDuration = new SumStudyDurationRequest
            {
                courseId = courseId,
                duration = durationSecs
            };
            string json = JsonTool.Serializable(sumStudyDuration);
            requestBase.TryRequest_List("累积课程学习时长", RequestType.POST, ApiData.CourseStudyDuration, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 百科管理
        /// <summary>
        /// 获取百科
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetEncyclopedia(int encyclopediaId, UnityAction<Encyclopedia, Answer> successCallBack, UnityAction<string> failureCallBack)
        {
            string Url = ApiData.Encyclopedia + "/" + encyclopediaId;
            requestBase.TryRequest_List("获取百科", RequestType.GET, Url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, (string data) =>
                {
                    Encyclopedia encyclopedia = null;
                    Answer answer = null;
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        int typeId = jObject["typeId"].Value<int>();
                        if (jObject["origin"] != null && jObject["origin"].Value<int>() == (int)PediaOrigin.Link)
                        {
                            EncyclopediaLink encyclopediaLink = JsonTool.DeSerializable<EncyclopediaLink>(data);
                            encyclopedia = encyclopediaLink;
                        }
                        else
                        {
                            switch (typeId)
                            {
                                case (int)PediaType.Picture:
                                case (int)PediaType.ANV:
                                case (int)PediaType.Doc:
                                    EncyclopediaLink encyclopediaLink = JsonTool.DeSerializable<EncyclopediaLink>(data);
                                    encyclopedia = encyclopediaLink;
                                    break;
                                case (int)PediaType.Disassemble:
                                case (int)PediaType.Animation:
                                    EncyclopediaModel encyclopediaModel = JsonTool.DeSerializable<EncyclopediaModel>(data);
                                    encyclopedia = encyclopediaModel;
                                    break;
                                case (int)PediaType.Operation:
                                    EncyclopediaOperation encyclopediaOperation = JsonTool.DeSerializable<EncyclopediaOperation>(data);
                                    if (!string.IsNullOrEmpty(encyclopediaOperation.operations))
                                        encyclopediaOperation.flows = JsonTool.DeSerializable<List<Flow>>(encyclopediaOperation.operations);
                                    encyclopedia = encyclopediaOperation;

                                    //操作步骤列表数据
                                    if (encyclopediaOperation.flows == null || encyclopediaOperation.flows.Count <= 0)
                                        Log.Warning("数据库没有步骤列表数据！");
                                    else
                                    {
                                        AnswerOp answerOp = new AnswerOp();
                                        answerOp.title = encyclopediaOperation.name;
                                        answerOp.typeId = encyclopediaOperation.typeId;
                                        answerOp.children = new List<AnswerFlow>();
                                        foreach (var flow in encyclopediaOperation.flows)
                                        {
                                            AnswerFlow answerFlow = new AnswerFlow();
                                            answerFlow.title = flow.title;
                                            answerFlow.children = new List<AnswerStep>();
                                            foreach (var step in flow.children)
                                            {
                                                AnswerStep answerStep = new AnswerStep();
                                                answerStep.title = step.title;
                                                answerStep.score = step.score;
                                                answerStep.standard = step.standard;
                                                //answerStep.operation = step.operation;
                                                //answerStep.getScore = step.title;
                                                //answerStep.state = step.title;
                                                answerFlow.children.Add(answerStep);
                                            }
                                            answerOp.children.Add(answerFlow);
                                        }
                                        answer = answerOp;
                                    }
                                    break;
                                case (int)PediaType.Exercise:
                                    EncyclopediaExercise encyclopediaExercise = JsonTool.DeSerializable<EncyclopediaExercise>(data);
                                    encyclopedia = encyclopediaExercise;

                                    //习题数据
                                    AnswerExercise answerExercise = new AnswerExercise();
                                    answerExercise.title = encyclopediaExercise.name;
                                    answerExercise.typeId = encyclopediaExercise.typeId;

                                    if (encyclopediaExercise.data.scores != null && encyclopediaExercise.data.scores.Count > 0)
                                        answerExercise.score = int.Parse(encyclopediaExercise.data.scores[0]);
                                    else
                                        Log.Error($"百科{encyclopediaExercise.id}习题未配置分数");

                                    if (encyclopediaExercise.data.exercise == null)
                                        Log.Error($"百科{encyclopediaExercise.id}习题未配置题干和选项");
                                    else
                                        switch (encyclopediaExercise.data.exercise.type)
                                        {
                                            case 1://选择题(单选;多选)
                                                ExerciseContent exerciseContent = JsonTool.DeSerializable<ExerciseContent>(encyclopediaExercise.data.exercise.content);
                                                for (int i = 0; i < exerciseContent.answers.Count; i++)
                                                {
                                                    if (exerciseContent.answers[i].right)
                                                    {
                                                        if (string.IsNullOrEmpty(answerExercise.standard))
                                                            answerExercise.standard = ((char)('A' + i)).ToString();
                                                        else
                                                            answerExercise.standard += ((char)('A' + i)).ToString();
                                                    }
                                                }
                                                break;
                                            case 2://判断题
                                                JudgementExerciseContent exerciseContent1 = JsonTool.DeSerializable<JudgementExerciseContent>(encyclopediaExercise.data.exercise.content);
                                                if (exerciseContent1.answers)
                                                    answerExercise.standard = "正确";
                                                else
                                                    answerExercise.standard = "错误";
                                                break;
                                            case 3://操作题
                                                break;
                                            default:
                                                Log.Error("未处理题型");
                                                break;
                                        }

                                    answer = answerExercise;
                                    break;
                            }
                            if (encyclopedia != null)
                                encyclopedia.typeId = typeId;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析百科Json失败 {ex.Message}");
                    }
                    successCallBack.Invoke(encyclopedia, answer);
                }, failureCallBack);
            });
        }
        #endregion

        #region 知识点管理
        /// <summary>
        /// 获取知识点
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetKnowledgepointList(int encyclopediaId, UnityAction<List<Knowledgepoint>> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("获取知识点", RequestType.GET, $"{ApiData.KnowledgeList}?encyclopediaId={encyclopediaId}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取模型知识点
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="uuid">模型uuid</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetKnowledgepointsByUUID(int encyclopediaId, string uuid, UnityAction<List<Knowledgepoint>> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("获取模型知识点", RequestType.GET, $"{ApiData.KnowledgeList}?encyclopediaId={encyclopediaId}&uuid={uuid}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 添加知识点
        /// </summary>
        /// <param name="knowledgepoint"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void AddKnowledgepoint(AddKnowledgepointRequest knowledgepoint, UnityAction<Knowledgepoint> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("创建知识点", RequestType.POST, ApiData.KnowledgePoint, JsonTool.Serializable(knowledgepoint), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 批量添加超链接（知识点）
        /// </summary>
        /// <param name="knowledgepoint"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void AddBatchKnowledgepoint(AddBatchKnowledgepointRequest batchKnowledgepoints, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("批量添加知识点", RequestType.POST, ApiData.KnowledgePointBatch, JsonTool.Serializable(batchKnowledgepoints), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }


        /// <summary>
        /// 替换超链接（知识点）
        /// </summary>
        /// <param name="knowledgepoint"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ReplaceKnowledgepoint(ReplaceKnowledgepointRequest replaceKnowledgepoint, UnityAction<Knowledgepoint> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("替换知识点资源", RequestType.PUT, ApiData.KnowledgePointResource, JsonTool.Serializable(replaceKnowledgepoint), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 修改知识点
        /// </summary>
        /// <param name="knowledgepoint"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void EditKnowledgepoint(EditKnowledgepointRequest knowledgepoint, UnityAction<Knowledgepoint> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("修改知识点", RequestType.PUT, ApiData.KnowledgePoint, JsonTool.Serializable(knowledgepoint), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 删除知识点
        /// </summary>
        /// <param name="id">知识点ID</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void DeleteKnowledgepoint(int id, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            string Url = ApiData.KnowledgePoint + "/" + id;
            requestBase.TryRequest_List("删除知识点", RequestType.DELETE, Url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 修改知识点顺序
        /// </summary>
        /// <param name="id">知识点ID</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void SortKnowledgepoint(List<int> ids, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            SortKnowledgepointRequest sortKnowledgepoint = new SortKnowledgepointRequest(ids);
            requestBase.TryRequest_List("修改知识点顺序", RequestType.PUT, ApiData.KnowledgeSort, JsonTool.Serializable(sortKnowledgepoint), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 模型节点管理
        /// <summary>
        /// 修改模型节点名称
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="uuid">模型节点id</param>
        /// <param name="nodeName">节点名称</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangeModelNodeName(int encyclopediaId, string uuid, string nodeName, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            ModelNode modelNodeRequest = new ModelNode()
            {
                encyclopediaId = encyclopediaId,
                uuid = uuid,
                nodeName = nodeName
            };
            string json = JsonTool.Serializable(modelNodeRequest);
            requestBase.TryRequest_List("修改模型节点名称", RequestType.PUT, ApiData.ModelNode, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        /// <summary>
        /// 修改模拟操作步骤列表节点名称
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="operations">所有任务和步骤</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void ChangeStepNodeName(int encyclopediaId, string operations, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            StepNode modelNodeRequest = new StepNode()
            {
                id = encyclopediaId,
                operations = operations
            };
            string json = JsonTool.Serializable(modelNodeRequest);
            requestBase.TryRequest_List("修改步骤列表节点名称", RequestType.PUT, ApiData.StepNode, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 课件管理
        /// <summary>
        /// 获取课件资源列表
        /// </summary>
        /// <param name="encyclopediaId">百科id</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetCoursewareList(int encyclopediaId, int page, int pagesize, UnityAction<CoursewareResourceList> successCallBack, UnityAction<int, string> failureCallBack)
        {
            requestBase.TryRequest_List("获取课件资源列表", RequestType.GET, $"{ApiData.CoursewareList}?encyclopediaId={encyclopediaId}&page={page}&pagesize={pagesize}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 新增课件资源信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        public void AddCoursewareResource(string fileName, string filePath, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            AddCoursewareResourceRequest addCoursewareResource = new AddCoursewareResourceRequest(fileName, filePath);
            requestBase.TryRequest_List("新增课件资源信息", RequestType.POST, ApiData.AddCoursewareResource, JsonTool.Serializable(addCoursewareResource), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 批量删除课件资源信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        public void DeleteCoursewareResourceBatch(List<int> ids, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            BatchCoursewareResourceRequest batchCoursewareResource = new BatchCoursewareResourceRequest(ids);
            requestBase.TryRequest_List("批量删除课件资源信息", RequestType.POST, ApiData.DeleteCoursewareResourceBatch, JsonTool.Serializable(batchCoursewareResource), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }
        #endregion

        #region 考核管理
        /// <summary>
        /// 获取考核试卷列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamList(UnityAction<List<Course>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取考核试卷列表", RequestType.GET, ApiData.ExamListForClient, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, true);
        }

        /// <summary>
        /// 获取考核试卷
        /// </summary>
        /// <param name="courseId">课程ID</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamPaper(int courseId, UnityAction<Course> successCallBack, UnityAction<string> failureCallBack)
        {
            string Url = ApiData.ExamPaper + "?id=" + courseId;

            requestBase.TryRequest_List("获取考核试卷", RequestType.GET, Url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, (string data) =>
                {
                    Course exam = new Course();
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        exam.id = jObject["id"].Value<int>();
                        exam.name = jObject["name"].Value<string>();
                        exam.iconPath = jObject["iconPath"].Value<string>();
                        exam.remarks = jObject["remarks"]?.Value<string>();
                        exam.duration = jObject["duration"].Value<int>();
                        exam.tags = jObject["tags"].Value<string>();

                        List<Encyclopedia> encyclopediaInfos = new List<Encyclopedia>();
                        int pediaType;
                        foreach (JToken token in jObject["encyclopediaList"])
                        {
                            pediaType = token["typeId"].Value<int>();

                            if (pediaType == 6 || pediaType == 7)//模拟操作或者习题百科
                            {
                                Encyclopedia encyclopedia = new Encyclopedia();
                                encyclopedia.id = token["id"].Value<int>();
                                encyclopedia.name = token["name"].Value<string>();
                                encyclopedia.iconPath = token["iconPath"]?.Value<string>();
                                encyclopedia.typeId = pediaType;

                                //习题百科数据
                                if (token["data"]["exercise"] != null)
                                {
                                    int exerciseType = token["data"]["exercise"]["type"].Value<int>();
                                    switch (exerciseType)
                                    {
                                        case 1:
                                            ExerciseContent exerciseContent = JsonTool.DeSerializable<ExerciseContent>(token["data"]["exercise"]["content"].Value<string>());
                                            bool multipleChoice = exerciseContent.answers.FindAll(a => a.right == true).Count > 1;
                                            encyclopedia.typeDescription = multipleChoice ? "多选题" : "单选题";
                                            break;
                                        case 2:
                                            encyclopedia.typeDescription = "判断题";
                                            break;
                                    }

                                    foreach (JToken scoreToken in token["data"]["scores"])
                                    {
                                        encyclopedia.totalScore += int.Parse(scoreToken.Value<string>());
                                    }
                                }

                                //操作百科数据
                                if (token["data"]["abPackageList"] != null)
                                {
                                    encyclopedia.typeDescription = "操作题";
                                    var operations = token["data"]["operations"]?.Value<string>();
                                    if(!string.IsNullOrEmpty(operations))
                                    {
                                        List<Flow> flows = JsonTool.DeSerializable<List<Flow>>(operations);
                                        foreach (var flow in flows)
                                        {
                                            //todo
                                            foreach (var step in flow.children)
                                                encyclopedia.totalScore += (int)step.score;
                                        }
                                    }                               
                                }
                                encyclopediaInfos.Add(encyclopedia);
                            }
                        }
                        exam.encyclopediaList = encyclopediaInfos;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析考核Json失败 {ex.Message}");
                    }
                    successCallBack.Invoke(exam);
                }, failureCallBack);
            });
        }

        /// <summary>
        /// 获取考试信息
        /// </summary>
        /// <param name="examId">考核ID</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamination(int examId, UnityAction<Examination> successCallBack, UnityAction<string> failureCallBack)
        {
            string Url = ApiData.Examination + "?examinationId=" + examId;

            requestBase.TryRequest_List("获取考试信息", RequestType.GET, Url, string.Empty, (result, message) =>
            {
                GetRequest(result, message, (string data) =>
                {
                    Examination exam = new Examination();
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        exam = jObject.ToObject<Examination>();

                        List<Encyclopedia> encyclopediaList = new List<Encyclopedia>();
                        int pediaType;
                        JArray jArray = (JArray)jObject["encyclopediaList"];
                        foreach (var jPedia in jArray)
                        {
                            pediaType = jPedia["typeId"].Value<int>();
                            //模拟操作
                            if (pediaType == 6)
                            {
                                EncyclopediaOperation encyclopediaOperation = jPedia.ToObject<EncyclopediaOperation>();
                                encyclopediaOperation.typeDescription = "操作题";
                                if (!string.IsNullOrEmpty(encyclopediaOperation.operations))
                                {
                                    List<Flow> flows = JsonTool.DeSerializable<List<Flow>>(encyclopediaOperation.operations);
                                    encyclopediaOperation.flows = flows;
                                    foreach (var flow in flows)
                                    {
                                        foreach (var step in flow.children)
                                            encyclopediaOperation.totalScore += (int)step.score;
                                    }
                                    if (encyclopediaOperation.flows != null && encyclopediaOperation.flows.Count > 0)
                                    {
                                        AnswerOp answerOp = new AnswerOp
                                        {
                                            baikeId = encyclopediaOperation.id,
                                            title = encyclopediaOperation.name,
                                            typeId = encyclopediaOperation.typeId,
                                            children = new List<AnswerFlow>()
                                        };
                                        foreach (var flow in encyclopediaOperation.flows)
                                        {
                                            AnswerFlow answerFlow = new AnswerFlow
                                            {
                                                title = flow.title,
                                                children = new List<AnswerStep>()
                                            };
                                            foreach (var step in flow.children)
                                            {
                                                AnswerStep answerStep = new AnswerStep
                                                {
                                                    title = step.title,
                                                    score = step.score,
                                                    standard = step.standard
                                                };
                                                answerFlow.children.Add(answerStep);
                                            }
                                            answerOp.children.Add(answerFlow);
                                        }
                                        encyclopediaOperation.answerOp = answerOp;
                                    }
                                }
                                encyclopediaList.Add(encyclopediaOperation);
                            }
                            //习题百科
                            else if (pediaType == 7)
                            {
                                EncyclopediaExercise encyclopediaExercise = jPedia.ToObject<EncyclopediaExercise>();

                                AnswerExercise answerExercise = new AnswerExercise
                                {
                                    baikeId = encyclopediaExercise.id,
                                    title = encyclopediaExercise.name,
                                    typeId = encyclopediaExercise.typeId,
                                    score = encyclopediaExercise.data.scores != null && encyclopediaExercise.data.scores.Count > 0 ? int.Parse(encyclopediaExercise.data.scores[0]) : 0
                                };

                                if (jPedia["data"]["exercise"] != null)
                                {
                                    int exerciseType = jPedia["data"]["exercise"]["type"].Value<int>();
                                    switch (exerciseType)
                                    {
                                        case 1:
                                            ExerciseContent exerciseContent = JsonTool.DeSerializable<ExerciseContent>(jPedia["data"]["exercise"]["content"].Value<string>());
                                            bool multipleChoice = exerciseContent.answers.FindAll(a => a.right == true).Count > 1;
                                            encyclopediaExercise.typeDescription = multipleChoice ? "多选题" : "单选题";
                                            for (int i = 0; i < exerciseContent.answers.Count; i++)
                                            {
                                                if (exerciseContent.answers[i].right)
                                                {
                                                    if (string.IsNullOrEmpty(answerExercise.standard))
                                                        answerExercise.standard = ((char)('A' + i)).ToString();
                                                    else
                                                        answerExercise.standard += ((char)('A' + i)).ToString();
                                                }
                                            }
                                            break;
                                        case 2:
                                            encyclopediaExercise.typeDescription = "判断题";
                                            JudgementExerciseContent exerciseContent1 = JsonTool.DeSerializable<JudgementExerciseContent>(jPedia["data"]["exercise"]["content"].Value<string>());
                                            if (exerciseContent1.answers)
                                                answerExercise.standard = "正确";
                                            else
                                                answerExercise.standard = "错误";
                                            break;
                                    }
                                    foreach (JToken scoreToken in jPedia["data"]["scores"])
                                    {
                                        encyclopediaExercise.totalScore += int.Parse(scoreToken.Value<string>());
                                    }
                                    encyclopediaExercise.answerExercise = answerExercise;
                                }
                                encyclopediaList.Add(encyclopediaExercise);
                            }
                        }
                        exam.encyclopediaList = encyclopediaList;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析考核Json失败 {ex.Message}");
                    }
                    successCallBack.Invoke(exam);
                }, failureCallBack);
            });
        }

        /// <summary>
        /// 获取考核AB包列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamABPackageList(UnityAction<List<CourseAB>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取考核AB包列表", RequestType.GET, ApiData.ExamABPackageList, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 创建一个考核记录 也就是开始考核
        /// </summary>
        /// <param name="courseId">课程ID</param>
        /// <param name="name"></param>
        /// <param name="teamWork">是否为小组考核</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void CreateExamRecord(int courseId, string name, bool teamWork, UnityAction<int> successCallBack, UnityAction<string> failureCallBack)
        {
            var loginRequest = new CreateExamRecordRequest()
            {
                courseId = courseId,
                name = name,
                teamWork = teamWork
            };
            string json = JsonTool.Serializable(loginRequest);
            requestBase.TryRequest_List("创建考核记录", RequestType.POST, ApiData.Examination, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 初始化考核成员成绩
        /// </summary>
        /// <param name="data"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void InitExamRecord(StartExamRecordRequest data, UnityAction successCallBack, UnityAction<string> failureCallBack)
        {
            string json = JsonTool.Serializable(data);
            requestBase.TryRequest_List("初始化考核记录", RequestType.POST, ApiData.InitExamRecord, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取考核记录列表
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamRecordList(UnityAction<HisExamList> successCallBack, UnityAction<string> failureCallBack)
        {
            string temp = $"page=1&pageSize={int.MaxValue}";
            requestBase.TryRequest_List("获取考核记录列表", RequestType.GET, $"{ApiData.GetExamRecordList}?{temp}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取考核成员成绩列表
        /// </summary>
        /// <param name="id">考核id</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamResultList(int id, UnityAction<ExamPersonnalList> successCallBack, UnityAction<string> failureCallBack)
        {
            string temp = $"page=1&pageSize={int.MaxValue}";
            requestBase.TryRequest_List("获取考核成员成绩列表", RequestType.GET, $"{ApiData.GetExamResultList}?examineId={id}&{temp}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 保存考核结果(操作题)
        /// </summary>
        /// <param name="examId"></param>
        /// <param name="baikeId"></param>
        /// <param name="operations"></param>
        /// <param name="modelStates"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void SubmitExamineResult_Operation(int examId, int baikeId, ExamineResultOperation[] operations, ExamineResultModelState[] modelStates, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            ExamineResultRequest requestData = new ExamineResultRequest()
            {
                examineId = examId,
                encyclopediaId = baikeId,
                operations = operations,
                modelStates = modelStates
            };
            requestBase.TryRequest_List("保存考核结果（操作）", RequestType.POST, ApiData.ExamineResult, JsonTool.Serializable(requestData), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 保存考核结果（习题等）
        /// </summary>
        /// <param name="examId"></param>
        /// <param name="baikeId"></param>
        /// <param name="operation"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void SubmitExamineResult_Excercise(int examId, int baikeId, ExamineResultOperation[] operations, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            ExamineResultRequest requestData = new ExamineResultRequest()
            {
                examineId = examId,
                encyclopediaId = baikeId,
                operations = operations
            };
            requestBase.TryRequest_List("保存考核结果（习题）", RequestType.POST, ApiData.ExamineResult, JsonTool.Serializable(requestData), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取考试结果V2
        /// </summary>
        /// <param name="examId"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamineResult(int examId, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("取得考试结果", RequestType.GET, $"{ApiData.ExamineResult}?examineId={examId}", string.Empty, (result, message) =>
            {
                ExamineResultGetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取考试结果V2 （个人考核）
        /// </summary>
        /// <param name="examId"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetExamineResultByRecordId(int recordId, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("取得考试结果", RequestType.GET, $"{ApiData.ExamineResult}?id={recordId}", string.Empty, (result, message) =>
            {
                ExamineResultGetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 提交考核结果附件
        /// </summary>
        /// <param name="examId"></param>
        /// <param name="accessoryList"></param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void SubmitExamAccessory(int examId, List<Accessory> accessoryList, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            ExamineResultAccessoryRequest requestData = new ExamineResultAccessoryRequest
            {
                examineId = examId,
                accessoryList = accessoryList
            };
            requestBase.TryRequest_List("提交考核结果附件", RequestType.POST, ApiData.ExamineResultAccessory, JsonTool.Serializable(requestData), (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 考核结束答题
        /// </summary>
        /// <param name="examId">考核id</param>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void EndExam(int examId, UnityAction successCallBack, UnityAction<string> failureCallBack)
        {
            EndExamRequest endExamRequest = new EndExamRequest()
            {
                examineId = examId
            };
            string json = JsonTool.Serializable(endExamRequest);
            requestBase.TryRequest_List("考核结束答题", RequestType.POST, ApiData.ExamEnd, json, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        private void ExamineResultGetRequest(bool result, string message, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        {
            GetRequest(result, message, (ExamResult ExamResult) =>
            {
                if (string.IsNullOrEmpty(ExamResult.answers))
                {
                    successCallBack?.Invoke(ExamResult.id, null, ExamResult.accessoryList);
                    return;
                }

                List<Answer> answers = new List<Answer>();
                var jarray = JArray.Parse(ExamResult.answers);

                foreach (var jObject in jarray)
                {
                    if (jObject == null || !jObject.HasValues)
                        continue;

                    switch (jObject["typeId"].Value<int>())
                    {
                        case (int)PediaType.Exercise:
                            AnswerExercise AnswerExercise = JsonTool.DeSerializable<AnswerExercise>(jObject.ToString());
                            answers.Add(AnswerExercise);
                            break;
                        case (int)PediaType.Operation:
                            AnswerOp AnswerOp = JsonTool.DeSerializable<AnswerOp>(jObject.ToString());
                            answers.Add(AnswerOp);
                            break;
                        default:
                            break;
                    }
                }

                successCallBack?.Invoke(ExamResult.id, answers, ExamResult.accessoryList);

            }, failureCallBack);
        }

        #region 旧接口
        ///// <summary>
        ///// 提交考核记录
        ///// </summary>
        ///// <param name="examId">考核ID</param>
        ///// <param name="answers">考核记录</param>
        ///// <param name="successCallBack"></param>
        ///// <param name="failureCallBack"></param>
        //public void SubmitExamRecord(int examId, string answers, List<Accessory> accessoryList, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        //{
        //    SubmitExamRecordRequest requestData = new SubmitExamRecordRequest();
        //    requestData.answers = answers;
        //    requestData.accessoryList = accessoryList;
        //    requestData.examineId = examId;
        //    requestBase.TryRequest_List("提交考核记录", RequestType.POST, ApiData.SubmitExamRecord, JsonTool.Serializable(requestData), (result, message) =>
        //    {
        //        GetRequest(result, message, successCallBack, failureCallBack);
        //    }, false);
        //}

        ///// <summary>
        ///// 提交阅卷记录
        ///// </summary>
        ///// <param name="allChecked"></param>
        ///// <param name="answers"></param>
        ///// <param name="correct"></param>
        ///// <param name="id"></param>
        ///// <param name="score"></param>
        ///// <param name="wrong"></param>
        ///// <param name="successCallBack"></param>
        ///// <param name="failureCallBack"></param>
        //public void SubmitCheckPaper(bool allChecked, string answers, int correct, int id, float score, int wrong, UnityAction successCallBack, UnityAction<string> failureCallBack)
        //{
        //    SubmitCheckPaperRequest requestData = new SubmitCheckPaperRequest();
        //    requestData.allChecked = allChecked;
        //    requestData.answers = answers;
        //    requestData.correct = correct;
        //    requestData.id = id;
        //    requestData.score = score;
        //    requestData.wrong = wrong;

        //    requestBase.TryRequest_List("提交考核记录", RequestType.POST, ApiData.SubmitCheckPaper, JsonTool.Serializable(requestData), (result, message) =>
        //    {
        //        GetRequest(result, message, successCallBack, failureCallBack);
        //    }, false);
        //}

        ///// <summary>
        ///// 取得考试结果
        ///// </summary>
        ///// <param name="examId">考核记录id</param>
        ///// <param name="successCallBack"></param>
        ///// <param name="failureCallBack"></param>
        //public void GetExamRecord(int examId, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        //{
        //    requestBase.TryRequest_List("取得考试结果", RequestType.GET, $"{ApiData.GetExamRecord}?examineId={examId}", string.Empty, (result, message) =>
        //    {
        //        ExamineResultGetRequest(result, message, successCallBack, failureCallBack);
        //    }, false);
        //}
        //public void GetExamRecordByRecordId(int recordId, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        //{
        //    requestBase.TryRequest_List("取得考试结果", RequestType.GET, $"{ApiData.GetExamRecord}?id={recordId}", string.Empty, (result, message) =>
        //    {
        //        ExamineResultGetRequest(result, message, successCallBack, failureCallBack);
        //    }, false);
        //}
        ///// <summary>
        ///// 取得个人考试结果
        ///// </summary>
        ///// <param name="examId"></param>
        ///// <param name="successCallBack"></param>
        ///// <param name="failureCallBack"></param>
        //public void GetExamRecordPersonal(int examId, UnityAction<int, List<Answer>, List<Accessory>> successCallBack, UnityAction<string> failureCallBack)
        //{
        //    requestBase.TryRequest_List("取得个人考试结果", RequestType.GET, $"{ApiData.GetExamRecordPersonal}?examineId={examId}", string.Empty, (result, message) =>
        //    {
        //        ExamineResultGetRequest(result, message, successCallBack, failureCallBack);
        //    }, false);
        //}
        #endregion
        #endregion

        #region 其他
        /// <summary>
        /// 获取系统时间
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetServerTime(UnityAction<string> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取系统时间", RequestType.GET, ApiData.ServerTime, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取OSS Config
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetOSS(UnityAction<OSSConfig> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取阿里云认证", RequestType.GET, ApiData.OSS, string.Empty, (result, message) =>
            {
                GetRequest(result, message, successCallBack, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取STS
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetSTS(UnityAction<StsBase> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取STS", RequestType.GET, ApiData.STS, string.Empty, (result, message) =>
            {
                //GetRequest(result, message, successCallBack, failureCallBack);
                GetRequest(result, message, (string data) =>
                {
                    StsBase sts = null;
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        string storeType = jObject["storeType"].Value<string>();
                        switch (storeType)
                        {
                            case "minio":
                                MinioStsInfo minio = JsonTool.DeSerializable<MinioStsInfo>(data);
                                sts = minio;
                                sts.StorageType = StsBase.StoreType.Minio;
                                break;
                            case "aliyun":
                                AliyunOSSStsInfo aliyun = JsonTool.DeSerializable<AliyunOSSStsInfo>(data);
                                sts = aliyun;
                                sts.StorageType = StsBase.StoreType.AliyunOSS;
                                break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析STS失败 {ex.Message}");
                    }
                    successCallBack.Invoke(sts);
                }, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 获取工程打包状态
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void GetProjectStatus(int projectId, UnityAction<int> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest_List("获取工程打包状态", RequestType.GET, $"{ApiData.Project}?id={projectId}", string.Empty, (result, message) =>
            {
                GetRequest(result, message, (data) =>
                {
                    int status = 0;
                    try
                    {
                        JObject jObject = JObject.Parse(data);
                        status = jObject["file"]["fileStatus"].Value<int>();
                        successCallBack.Invoke(status);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"解析工程详情Json失败 {ex.Message}");
                        failureCallBack.Invoke(ex.Message);
                    }
                }, failureCallBack);
            }, false);
        }

        /// <summary>
        /// 日志上传
        /// </summary>
        /// <param name="successCallBack"></param>
        /// <param name="failureCallBack"></param>
        public void PostLog(string filepath, UnityAction successCallBack = null, UnityAction<string> failureCallBack = null)//byte[] logFile,
        {
            if (!System.IO.File.Exists(filepath))
                return;

            List<UnityEngine.Networking.IMultipartFormSection> form = new List<UnityEngine.Networking.IMultipartFormSection>();
            if (GlobalInfo.account != null)
                form.Add(new UnityEngine.Networking.MultipartFormDataSection("accountId", GlobalInfo.account.id.ToString()));
            else
                form.Add(new UnityEngine.Networking.MultipartFormDataSection("accountId", "-1"));

            form.Add(new UnityEngine.Networking.MultipartFormDataSection("client", ApiData.ClientID));

            form.Add(new UnityEngine.Networking.MultipartFormFileSection("logFile", System.IO.File.ReadAllBytes(filepath), "log.text", "multipart/form-data"));

            StartCoroutine(Request("日志上传", ApiData.UpLog, form));
        }
        System.Collections.IEnumerator Request(string requestName, string url, List<UnityEngine.Networking.IMultipartFormSection> form)
        {
            // 解析发送请求需要的数据
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, form))
            {
                request.SetRequestHeader("x-client", ApiData.ClientID);
                request.SetRequestHeader("x-device", ApiData.DeviceID);
                request.timeout = 60;//30

                yield return request.SendWebRequest();

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Log.Warning(requestName + "出错:\r\n请求url:" + request.url + "\r\n错误提示:" + request.error + "\r\n服务器返回:" + request.downloadHandler.text);
                }
                else
                {
                    string result = request.downloadHandler.text;
                    Log.Info(requestName + "返回结果:\r\n<color=green>" + result + "</color>");
                }
            }
        }
        #endregion

        #region 语音数据
        public void GetSpeechList(int encyclopediaId, UnityAction<List<SpeechData>> successCallBack, UnityAction<string> failureCallBack)
        {
            requestBase.TryRequest("获取百科语音数据列表", RequestType.GET, $"{ApiData.GetSpeechList}?encyclopediaId={encyclopediaId}", string.Empty, (result, message) =>
            {
                GetRequest<List<SpeechData>>(result, message, data =>
                {
                    successCallBack.Invoke(data);
                }, failureCallBack);
            });
        }
        #endregion

        #region 工具
        /// <summary>
        /// 处理请求回调,成功回调不带参数
        /// </summary>
        /// <param name="requestIsSuccess">请求是否成功</param>
        /// <param name="requestData">请求的数据</param>
        /// <param name="successCallBack">成功回调</param>
        /// <param name="failureCallBack">失败回调</param>
        private void GetRequest(bool requestIsSuccess, string requestData, UnityAction successCallBack, UnityAction<string> failureCallBack)
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke();
                else
                    failureCallBack.Invoke(GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(requestData);
        }

        private void GetRequest(bool requestIsSuccess, string requestData, UnityAction successCallBack, UnityAction<int, string> failureCallBack)
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke();
                else
                    failureCallBack.Invoke(GetCode(jObject), GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(0, "无网络连接");
        }

        /// <summary>
        /// 处理请求回调,成功回调带string参数
        /// </summary>
        /// <param name="requestIsSuccess">请求是否成功</param>
        /// <param name="requestData">请求的数据</param>
        /// <param name="successCallBack">成功回调</param>
        /// <param name="failureCallBack">失败回调</param>
        private void GetRequest(bool requestIsSuccess, string requestData, UnityAction<string> successCallBack, UnityAction<string> failureCallBack)
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData(jObject));
                else
                    failureCallBack.Invoke(GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(requestData);
        }

        private void GetRequest(bool requestIsSuccess, string requestData, UnityAction<string> successCallBack, UnityAction<int, string> failureCallBack)
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData(jObject));
                else
                    failureCallBack.Invoke(GetCode(jObject), GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(0, "无网络连接");
        }

        /// <summary>
        /// 处理请求回调,成功回调带泛型参数
        /// </summary>
        /// <typeparam name="T">成功回调携带的参数</typeparam>
        /// <param name="requestIsSuccess">请求是否成功</param>
        /// <param name="requestData">请求的数据</param>
        /// <param name="successCallBack">成功回调</param>
        /// <param name="failureCallBack">失败回调</param>
        private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<string> failureCallBack) where T : class
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData<T>(jObject));
                else
                    failureCallBack.Invoke(GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(requestData);
        }

        private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<int, string> failureCallBack) where T : class
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData<T>(jObject));
                else
                    failureCallBack.Invoke(GetCode(jObject), GetMessage(jObject));
                //ResponseErrorHandler(jObject, failureCallBack);
            }
            else
                failureCallBack.Invoke(0, "无网络连接");
        }

        /// <summary>
        /// 处理请求回调,成功回调带泛型参数
        /// </summary>
        /// <param name="requestIsSuccess">请求是否成功</param>
        /// <param name="requestData">请求的数据</param>
        /// <param name="successCallBack">成功回调</param>
        /// <param name="failureCallBack">失败回调</param>
        /// <param name="temp">用于强行区分两种GetRequest</param>
        private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<string> failureCallBack, bool temp = false) where T : struct
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData<T>(jObject));
                else
                    failureCallBack.Invoke(GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(requestData);
        }

        private void GetRequest<T>(bool requestIsSuccess, string requestData, UnityAction<T> successCallBack, UnityAction<int, string> failureCallBack, bool temp = false) where T : struct
        {
            if (requestIsSuccess)
            {
                var jObject = JObject.Parse(requestData);

                if (IsSuccess(jObject))
                    successCallBack.Invoke(GetData<T>(jObject));
                else
                    failureCallBack.Invoke(GetCode(jObject), GetMessage(jObject));
            }
            else
                failureCallBack.Invoke(0, "无网络连接");
        }


        private void ResponseErrorHandler(JObject jObject, UnityAction<int, string> failureCallBack)
        {
            int code = GetCode(jObject);
            switch ((ResponseCode)code)
            {
                case ResponseCode.Unauthorized:
                    failureCallBack.Invoke(code, "请求未授权");
                    break;
                case ResponseCode.Forbidden:
                    failureCallBack.Invoke(code, "服务器拒绝访问");
                    break;
                case ResponseCode.NotFound:
                    failureCallBack.Invoke(code, "服务器无法找到请求的资源");
                    break;
                case ResponseCode.BadRequest:
                default:
                    failureCallBack.Invoke(code, GetMessage(jObject));
                    break;
            }
        }

        /// <summary>
        /// 检查是否成功 开出来用于统一管理 应对后台变动
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private bool IsSuccess(JObject jObject)
        {
            return jObject["success"].Value<bool>();
        }

        /// <summary>
        /// 获取错误码 开出来用于统一管理 应对后台变动
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private int GetCode(JObject jObject)
        {
            return int.Parse(jObject["code"].Value<string>().Substring(1));
        }

        /// <summary>
        /// 获取请求回执信息 开出来用于统一管理 应对后台变动
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private string GetMessage(JObject jObject)
        {
            return jObject["message"]?.ToString();
        }

        /// <summary>
        /// 仅获取data的json 不获取多余的东西 开出来用于统一管理 应对后台变动
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private string GetData(JObject jObject)
        {
            return jObject["data"].ToString();
        }

        /// <summary>
        /// 仅获取data的json 不获取多余的东西 开出来用于统一管理 应对后台变动
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private T GetData<T>(JObject jObject)
        {
            if (jObject["data"] == null)
                return default;
            return JsonTool.DeSerializable<T>(jObject["data"].ToString());
        }

        internal enum ResponseCode
        {
            /// <summary>
            /// 未知问题
            /// </summary>
            Unknow = -1,
            /// <summary>
            /// 网络错误
            /// </summary>
            InternalError = 0,
            /// <summary>
            /// 请求成功，服务器已成功处理了请求并返回了相应的信息。
            /// </summary>
            OK = 200,
            /// <summary>
            /// 已创建，服务器成功地创建了新资源。
            /// </summary>
            Created = 201,
            /// <summary>
            /// 无内容，服务器成功处理了请求，但没有返回任何内容。
            /// </summary>
            NoContent = 204,
            /// <summary>
            /// 永久重定向，请求的资源已被永久移动到新位置。
            /// </summary>
            MovedPermanently = 301,
            /// <summary>
            /// 临时重定向，请求的资源已被临时移动到新位置。
            /// </summary>
            Found = 302,
            /// <summary>
            /// 未修改，客户端请求的资源未被修改，服务器返回此状态码时，不会返回资源的内容。
            /// </summary>
            NotModifie = 304,
            /// <summary>
            /// 错误请求，服务器无法理解客户端发送的请求。
            /// </summary>
            BadRequest = 400,
            /// <summary>
            /// 未授权，请求未经授权，需要客户端提供身份验证信息。
            /// </summary>
            Unauthorized = 401,
            /// <summary>
            /// 禁止访问，服务器拒绝请求。
            /// </summary>
            Forbidden = 403,
            /// <summary>
            /// 未找到，服务器无法找到请求的资源。
            /// </summary>
            NotFound = 404,
            /// <summary>
            /// 内部服务器错误，服务器遇到错误，无法完成请求。
            /// </summary>
            InternalServerError = 500,
            /// <summary>
            /// 服务不可用，服务器暂时无法处理请求。
            /// </summary>
            ServiceUnavailable = 503
        }
        #endregion
    }
}