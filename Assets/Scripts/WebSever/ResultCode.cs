public class ResultCode
{
    //用于复制

    private enum Base
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
    }

    /// <summary>
    /// 登录请求结果码
    /// </summary>
    public enum Login
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 用户未注册
        /// </summary>
        User_Not_Registered = temp.User_Not_Registered,
        /// <summary>
        /// 用户已被禁用
        /// </summary>
        User_Disabled = temp.User_Disabled,
        /// <summary>
        /// 用户已经过期
        /// </summary>
        User_Expired = temp.User_Expired,
        /// <summary>
        /// 用户密码错误
        /// </summary>
        User_Password_Error = temp.User_Password_Error,
        /// <summary>
        /// 密码错误次数过多 连续错误5次，5分钟后尝试
        /// </summary>
        Password_Error_Max = temp.Password_Error_Max,
        /// <summary>
        /// 获取令牌失败
        /// </summary>
        Get_Token_Error = temp.Get_Token_Error
    }
    /// <summary>
    /// 检查手机号是否占用
    /// </summary>
    public enum CheckPhonenumber
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 手机号已被使用 手机号已经被其他用户使用
        /// </summary>
        Mobile_Used = temp.Mobile_Used,
    }
    /// <summary>
    /// 注册
    /// </summary>
    public enum Register
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 添加失败
        /// </summary>
        Insert_Failed = temp.Insert_Failed,
        /// <summary>
        /// 手机号已被使用 手机号已经被其他用户使用
        /// </summary>
        Mobile_Used = temp.Mobile_Used,
        /// <summary>
        /// 无效手机号
        /// </summary>
        Invalid_Mobile = temp.Invalid_Mobile,
        /// <summary>
        /// 无效或过期验证码
        /// </summary>
        Invalid_SMS_Code = temp.Invalid_SMS_Code,
        /// <summary>
        /// 短信验证码错误
        /// </summary>
        SMS_Code_Error = temp.SMS_Code_Error,
    }
    /// <summary>
    /// 短信验证错误
    /// </summary>
    public enum SMSCode
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 短信额度达到每日上限
        /// </summary>
        HighFrequency_DAY_LIMIT = temp.HighFrequency_DAY_LIMIT,
        /// <summary>
        /// 停机，短信余额不足
        /// </summary>
        HighFrequency_OUT = temp.HighFrequency_OUT,
        /// <summary>
        /// 非法手机号
        /// </summary>
        HighFrequency_MOBILE = temp.HighFrequency_MOBILE,
        /// <summary>
        ///  短信发送频率超限 1分钟1条 1小时5条 1天10条
        /// </summary>
        HighFrequency_LIMIT_CONTROL = temp.HighFrequency_LIMIT_CONTROL,
    }
    /// <summary>
    /// 忘记密码
    /// </summary>
    public enum ForgetPassword
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 更新失败	
        /// </summary>
        Update_Failed = temp.Update_Failed,
        /// <summary>
        /// 用户未注册
        /// </summary>
        User_Not_Registered = temp.User_Not_Registered,
        /// <summary>
        /// 获取令牌失败
        /// </summary>
        Get_Token_Error = temp.Get_Token_Error,
        /// <summary>
        /// 无效手机号	
        /// </summary>
        Invalid_Mobile = temp.Invalid_Mobile,
        /// <summary>
        /// 无效或过期验证码
        /// </summary>
        Invalid_SMS_Code = temp.Invalid_SMS_Code,
        /// <summary>
        /// 短信验证码错误
        /// </summary>
        SMS_Code_Error = temp.SMS_Code_Error,
    }
    /// <summary>
    /// 修改密码
    /// </summary>
    public enum ChangePassword
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 更新失败	
        /// </summary>
        Update_Failed = temp.Update_Failed,
        /// <summary>
        /// 用户未注册
        /// </summary>
        User_Not_Registered = temp.User_Not_Registered,
        /// <summary>
        /// 用户密码错误
        /// </summary>
        User_Password_Error = temp.User_Password_Error,
        /// <summary>
        /// 密码错误次数过多	
        /// </summary>
        Password_Error_Max = temp.Password_Error_Max,
    }
    public enum VerifyCompany
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 授权信息错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 已加入此学校，无需重复操作
        /// </summary>
        Device_SchoolAuth_Exit = temp.Device_SchoolAuth_Exit,
        /// <summary>
        /// 授权已经过期
        /// </summary>
        Auth_Expired = temp.Auth_Expired,
        /// <summary>
        /// 刷新令牌失败
        /// </summary>
        Refresh_Token_Faile = temp.Refresh_Token_Faile,
    }
    public enum JoinCompany
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 过期请求
        /// </summary>
        Expired_Request = temp.Expired_Request,
        /// <summary>
        /// 更新失败
        /// </summary>
        Update_Failed = temp.Update_Failed,
    }
    public enum KnowledgeGet
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
    }
    public enum KnowledgeAdd
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 长度超限
        /// </summary>
        OverLength = temp.OverLength,
        /// <summary>
        /// 添加失败
        /// </summary>
        Insert_Failed = temp.Insert_Failed,
    }
    public enum KnowledgeEditor
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 资源不存在或已删除
        /// </summary>
        Teach_ResourceNoExist = temp.Teach_ResourceNoExist,
        /// <summary>
        /// 更新失败
        /// </summary>
        Update_Failed = temp.Update_Failed,
    }
    public enum KnowledgeDelet
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = temp.Successful,
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = temp.InternetError,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = temp.Service_Exception,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = temp.Invalid_Parameter,
        /// <summary>
        /// 资源不存在或已删除
        /// </summary>
        Teach_ResourceNoExist = temp.Teach_ResourceNoExist,
        /// <summary>
        /// 更新失败
        /// </summary>
        Delete_Failed = MsgEnum.Delete_Failed,
    }
    /// <summary>
    /// 当前用到的所有枚举 用于之后替换
    /// </summary>
    private enum temp
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        InternetError = 0,
        /// <summary>
        /// 成功
        /// </summary>
        Successful = 1000,
        /// <summary>
        /// 字符超出长度
        /// </summary>
        OverLength=-34,
        /// <summary>
        /// 服务异常
        /// </summary>
        Service_Exception = -1000,
        /// <summary>
        /// 过期请求
        /// </summary>
        Expired_Request = -1006,
        /// <summary>
        /// 请求参数错误
        /// </summary>
        Invalid_Parameter = -1100,
        /// <summary>
        /// 添加失败
        /// </summary>
        Insert_Failed = -1102,
        /// <summary>
        /// 更新失败	
        /// </summary>
        Update_Failed = -1103,
        /// <summary>
        /// 删除失败
        /// </summary>
        Delete_Failed=-1104,
        /// <summary>
        /// 短信额度达到每日上限
        /// </summary>
        HighFrequency_DAY_LIMIT = -1107,
        /// <summary>
        /// 停机，短信余额不足
        /// </summary>
        HighFrequency_OUT = -1108,
        /// <summary>
        /// 非法手机号
        /// </summary>
        HighFrequency_MOBILE = -1109,
        /// <summary>
        ///  短信发送频率超限 1分钟1条 1小时5条 1天10条
        /// </summary>
        HighFrequency_LIMIT_CONTROL = -1110,
        /// <summary>
        /// 已加入此学校，无需重复操作
        /// </summary>
        Device_SchoolAuth_Exit = -1156,
        /// <summary>
        /// 授权已经过期
        /// </summary>
        Auth_Expired = -1157,
        /// <summary>
        /// 获取令牌失败
        /// </summary>
        Get_Token_Error = -2100,
        /// <summary>
        /// 刷新令牌失败
        /// </summary>
        Refresh_Token_Faile = -2101,
        /// <summary>
        /// 用户未注册
        /// </summary>
        User_Not_Registered = -4000,
        /// <summary>
        /// 手机号已被使用 手机号已经被其他用户使用
        /// </summary>
        Mobile_Used = -4001,
        /// <summary>
        /// 用户已被禁用
        /// </summary>
        User_Disabled = -4005,
        /// <summary>
        /// 用户已经过期
        /// </summary>
        User_Expired = -4006,
        /// <summary>
        /// 用户密码错误
        /// </summary>
        User_Password_Error = -4100,
        /// <summary>
        /// 密码错误次数过多
        /// </summary>
        Password_Error_Max = -4101,
        /// <summary>
        /// 资源不存在或已删除
        /// </summary>
        Teach_ResourceNoExist=-6602,
        /// <summary>
        /// 无效手机号
        /// </summary>
        Invalid_Mobile = -7001,
        /// <summary>
        /// 无效或过期验证码
        /// </summary>
        Invalid_SMS_Code = -7002,
        /// <summary>
        /// 短信验证码错误
        /// </summary>
        SMS_Code_Error = -7003,
    }


    /// <summary>
    /// 后台的所有枚举
    /// </summary>
    public enum MsgEnum
    {
        /// <summary>
        /// 成功
        /// </summary>
        Successful = 1000,
        /// <summary>
        /// 服务异常  500
        /// </summary>
        Service_Exception = -1000,

        #region 公共错误 400
        /// <summary>
        /// 头部缺少Client信息或Client错误
        /// </summary>

        Invalid_Headers_Client = -1001,

        /// <summary>
        /// 头部缺少Sign信息或Sign错误
        /// </summary>

        Invalid_Headers_Sign = -1002,
        /// <summary>
        /// 头部缺少TimeStamp或TimeStamp错误
        /// </summary>

        Invalid_Headers_TimeStamp = -1003,
        /// <summary>
        /// 头部缺少Nonce信息或Nonce错误
        /// </summary>

        Invalid_Headers_Nonce = -1004,
        /// <summary>
        /// 无效签名
        /// </summary>

        Invalid_Signature = -1005,
        /// <summary>
        /// 过期请求
        /// </summary>

        Expired_Request = -1006,
        /// <summary>
        /// 重发请求
        /// </summary>

        Resend_Request = -1007,
        /// <summary>
        /// 拒绝访问
        /// </summary>

        Forbidden = -1008,
        #endregion

        #region  身份授权  401        
        /// <summary>
        /// 无效身份
        /// </summary>

        Invalid_Identity = -2001,
        /// <summary>
        /// 身份过期
        /// </summary>

        Expired_Identity = -2002,
        /// <summary>
        /// 获取令牌失败
        /// </summary>

        Get_Token_Error = -2100,
        /// <summary>
        /// 刷新令牌有误
        /// </summary>

        Refresh_Token_Wrong = -2101,
        /// <summary>
        /// 刷新令牌失败
        /// </summary>

        Refresh_Token_Faile = -2101,
        /// <summary>
        /// 访问权限不足
        /// </summary>

        Insufficient_Access_Rights = -2102,
        #endregion

        #region  用户服务
        /// <summary>
        /// 用户未注册
        /// </summary>

        User_Not_Registered = -4000,
        /// <summary>
        /// 未注册AR希课平台，暂不提供绑定服务
        /// </summary>

        User_NotBindAppletUser = -4002,
        /// <summary>
        /// 手机号已被其他用户绑定
        /// </summary>

        Mobile_Used = -4001,
        /// <summary>
        /// 用户已被禁用
        /// </summary>

        User_Disabled = -4005,
        /// <summary>
        /// 用户已经过期
        /// </summary>

        User_Expired = -4006,
        /// <summary>
        /// 用户权限不足
        /// </summary>

        User_Permissions_Insufficient = -4007,

        /// <summary>
        /// 登录失败
        /// </summary>

        Login_Error = -4008,
        /// <summary>
        /// 用户异地登陆
        /// </summary>

        User_Remote_Login = -4009,
        /// <summary>
        /// 用户密码错误
        /// </summary>

        User_Password_Error = -4100,
        /// <summary>
        /// 密码错误超过5次，锁定账号5分钟
        /// </summary>

        Password_Error_Max = -4101,
        /// <summary>
        /// 密码需要加密
        /// </summary>

        Password_Not_MD5 = -4102,
        /// <summary>
        /// 手机号重复绑定
        /// </summary>

        Mobile_Repeat_Bind = -4201,
        /// <summary>
        /// 用户已在其他学校
        /// </summary>
        User_JoinSchool = -4202,
        #endregion

        #region  通知服务        
        /// <summary>
        /// 无效手机号
        /// </summary>

        Invalid_Mobile = -7001,
        /// <summary>
        /// 无效或过期验证码
        /// </summary>

        Invalid_SMS_Code = -7002,
        /// <summary>
        /// 短信验证码错误
        /// </summary>

        SMS_Code_Error = -7003,

        #endregion

        InvalidIdentity = -1,
        /// <summary>
        /// 身份过期
        /// </summary>

        IdentityOverdue = -2,
        /// <summary>
        /// 无效参数
        /// </summary>

        Invalid_Parameter = -1100,
        /// <summary>
        /// 查询失败
        /// </summary>

        Query_Failed = -1101,
        /// <summary>
        /// 添加失败
        /// </summary>

        Insert_Failed = -1102,
        /// <summary>
        /// 更新失败
        /// </summary>

        Update_Failed = -1103,
        /// <summary>
        /// 删除失败
        /// </summary>

        Delete_Failed = -1104,
        /// <summary>
        /// 重复录入
        /// </summary>

        Duplicate_Entry = -1105,
        /// <summary>
        /// 数据为空
        /// </summary>

        Data_Null = -1106,
        /// <summary>
        /// 发送失败
        /// </summary>

        Send_Failed = -1200,


        /// <summary>
        /// 无效参数
        /// </summary>

        InvalidParameter = -3,
        /// <summary>
        /// 无效分享码
        /// </summary>

        InvalidShareKey = -4,
        /// <summary>
        /// 无效授权
        /// </summary>

        InvalidAuthor = -5,
        /// <summary>
        /// 频率过高
        /// </summary>

        HighFrequency = -6,
        /// <summary>
        /// 频率过高
        /// </summary>

        HighFrequency1 = -6,
        /// <summary>
        /// 触日发送限额
        /// </summary>

        HighFrequency_DAY_LIMIT = -1107,
        /// <summary>
        /// 停机，短信余额不足
        /// </summary>

        HighFrequency_OUT = -1108,
        /// <summary>
        /// 非法手机号
        /// </summary>

        HighFrequency_MOBILE = -1109,
        /// <summary>
        ///  短信发送频率超限
        /// </summary>

        HighFrequency_LIMIT_CONTROL = -1110,

        /// <summary>
        /// ARSeek未激活
        /// </summary>

        ARSeek_Not_Active = -1301,
        /// <summary>
        /// ARSeek激活已过期
        /// </summary>

        ARSeek_Active_Expired = -1302,
        /// <summary>
        /// ARSeek激活已注销
        /// </summary>

        ARSeek_Active_Cancelled = -1303,
        /// <summary>
        /// ARSeek激活码错误
        /// </summary>

        ARSeek_Cdkey_Error = -1304,
        /// <summary>
        /// ARSeek激活码与设备不匹配
        /// </summary>

        ARSeek_Device_Error = -1305,

        /// <summary>
        /// 验证码错误
        /// </summary>

        VerificationCodeError = -7,
        /// <summary>
        /// 验证码错误或已过期
        /// </summary>

        InvalidVerificationCode = -8,
        /// <summary>
        /// 设备激活码无效，请重新获取
        /// </summary>

        DeviceCode = -9,
        /// <summary>
        /// 无效的设备
        /// </summary>

        InvalidDevice = -10,
        /// <summary>
        /// 注册码码无效
        /// </summary>

        InvalidCDKey = -11,
        /// <summary>
        /// 注册码已经被使用
        /// </summary>

        CDKeyUsed = -12,
        /// <summary>
        /// 最大数量限制
        /// </summary>

        MaxLimit = -13,
        /// <summary>
        /// 该账户未注册
        /// </summary>

        InvalidAccount = -14,
        /// <summary>
        /// 用户名已被使用
        /// </summary>

        AccountExisted = -15,
        /// <summary>
        /// 学校已存在
        /// </summary>

        SchoolExisted = -16,
        /// <summary>
        /// 账号限制IP地址登录
        /// </summary>

        LimitedIP = -17,
        /// <summary>
        /// 设备与账号不匹配
        /// </summary>

        DAMismatch = -18,
        /// <summary>
        /// 禁用的账号
        /// </summary>

        DisabledAccount = -19,
        /// <summary>
        /// 过期的设备
        /// </summary>

        ExpiredDevice = -20,
        /// <summary>
        /// 禁用的设备
        /// </summary>

        DisabledDevice = -21,
        /// <summary>
        /// 无效的学校
        /// </summary>

        InvalidSchool = -22,
        /// <summary>
        /// 禁用的学校
        /// </summary>

        DisabledSchool = -23,
        /// <summary>
        /// 过期的学校
        /// </summary>

        OverdueSchool = -100,


        /// <summary>
        /// 无数据
        /// </summary>

        NoData = -24,
        /// <summary>
        /// 内容为空
        /// </summary>

        ContentIsEmpty = -25,
        /// <summary>
        /// 已存在相同数据
        /// </summary>

        Existed = -26,
        /// <summary>
        /// 唯一的
        /// </summary>

        Only = -27,
        /// <summary>
        /// 执行失败
        /// </summary>

        ExecutionFaile = -28,
        /// <summary>
        /// 课程化错误
        /// </summary>

        SerializerError = -29,
        /// <summary>
        /// 重复添加
        /// </summary>

        DuplicateEntry = -30,
        /// <summary>
        /// 无权操作
        /// </summary>

        NoOperate = -31,
        /// <summary>
        /// 非科华人员，无权操作
        /// </summary>

        NoSchoolOperate = -32,
        /// <summary>
        /// 不支持该格式
        /// </summary>

        FormalError = -33,
        /// <summary>
        /// 输入字符超出长度
        /// </summary>

        OverLength = -34,
        #region 工业服务
        /// <summary>
        /// 该课件不存在或已删除
        /// </summary>

        Arim_CourseWareNoExist = -9001,
        /// <summary>
        /// 关键字搜索超出长度(10字内)！
        /// </summary>

        Arim_SearchOverLength = -9002,
        /// <summary>
        /// 标签尚在使用，不能删除！
        /// </summary>

        Arim_TagIsUse = -9003,
        /// <summary>
        /// 该资源尚在使用，不可删除！
        /// </summary>

        Arim_ResIsUse = -9004,
        /// <summary>
        /// 该操作票不存在或以删除
        /// </summary>

        Arim_TicketNoExist = -9005,
        /// <summary>
        /// 该工序卡不存在或以删除
        /// </summary>

        Arim_ProcessNoExist = -9006,
        /// <summary>
        /// 该项目不存在或以删除
        /// </summary>

        Arim_ProjectNoExist = -9006,
        /// <summary>
        /// 还未提交操作票
        /// </summary>

        Arim_NoCheckTicket = -9007,
        /// <summary>
        /// 操作票还未审核通过，暂不能操作
        /// </summary>

        Arim_TicketNoPass = -9008,
        /// <summary>
        /// 流程出错
        /// </summary>

        Arim_PathError = -9009,
        /// <summary>
        /// 操作票内容尚未完成，不能提交任务！
        /// </summary>

        Arim_TicketItemNoAllCheck = -9010,
        /// <summary>
        /// 等待审核中，请勿重复提交！
        /// </summary>

        Arim_Submit_Exit = -9011,
        /// <summary>
        /// 此权限只能由项目组班长操作！
        /// </summary>

        Arim_NoClassLeader = -9012,
        /// <summary>
        ///该状态暂不允许删除
        /// </summary>

        Arim_StateNoDelete = -9013,
        /// <summary>
        /// 现状态不支持该操作！
        /// </summary>

        Arim_StateNoOperate = -9014,
        /// <summary>
        ///该角色已存在，是否确认修改！
        /// </summary>

        Arim_TeamUserRoleExit = -9015,
        /// <summary>
        ///项目编号已存在！
        /// </summary>

        Arim_ProjectNumExit = -9016,
        /// <summary>
        ///该操作票已被领取！
        /// </summary>

        Arim_WuFangTicketIsGet = -9017,

        #endregion

        #region 百科服务
        /// <summary>
        /// 百科名称超出长度
        /// </summary>

        Seek_OverLength = -86,
        /// <summary>
        /// 缺少百科资源上传配置参数
        /// </summary>

        Seek_IniJsonIsNull = -87,
        /// <summary>
        /// 搜索关键字最好在2-10字间
        /// </summary>

        Seek_SearchNoNorm = -4040,
        /// <summary>
        /// 未有进行课程
        /// </summary>

        Upload_NoLesson = -4041,
        /// <summary>
        /// 百科已删除或不存在
        /// </summary>

        Seek_EncyclopediaNoExist = -4042,
        #endregion

        #region 用户服务
        /// <summary>
        /// 请传入客户端id
        /// </summary>

        User_ClientIdIsNull = -88,
        /// <summary>
        /// 账户名应在最6-18字间
        /// </summary>

        User_AccountOverLength = -89,
        /// <summary>
        /// 密码长度应在最6-18字间
        /// </summary>

        User_AccountPasswordOverLength = -90,
        /// <summary>
        /// 用户姓名应在2-20字
        /// </summary>

        User_AccountRealNameLength = -91,
        /// <summary>
        /// 添加账号已存在该学校
        /// </summary>

        User_AccountExistSchool = -92,
        /// <summary>
        /// 无效的手机号
        /// </summary>

        User_InvalidMobilePhone = -93,
        /// <summary>
        /// 请输入有效合同号
        /// </summary>

        User_InvalidContractid = -94,
        /// <summary>
        /// 请认真填写地址
        /// </summary>

        User_InvalidAddress = -95,
        /// <summary>
        /// 手机未注册
        /// </summary>

        User_MobileUnRegistered = -96,
        /// <summary>
        /// 手机号已被使用
        /// </summary>

        User_MobilePhoneExisted = -97,
        /// <summary>
        /// 学校名无效
        /// </summary>

        User_SchoolInvalidParameter = -98,
        /// <summary>
        /// 请选择学校
        /// </summary>

        User_SchoolIdInvalidParameter = -99,
        /// <summary>
        /// 请输入搜索学校关键字
        /// </summary>

        User_SearchSchoolNull = -100,
        /// <summary>
        /// 请仔细核对学校是否和签约学校名一致，如有问题请联系管理员
        /// </summary>

        User_SchoolUnLike = -101,
        /// <summary>
        /// 当前位置与签约单位地址不符，请保持通讯定位畅通，如有问题请联系管理员
        /// </summary>

        User_SchoolNoLocal = -102,
        /// <summary>
        /// 账户超出学校可开账户数量！请联系管理员确认
        /// </summary>

        User_OverSchoolAccountCount = -103,
        /// <summary>
        /// 过期时间无效
        /// </summary>

        User_expirestime = -2203,

        #endregion

        #region 美术资源
        /// <summary>
        ///资源名超出长度
        /// </summary>

        Art_NameOverLength = -35,
        /// <summary>
        ///缺少类型参数
        /// </summary>

        Art_TypeEmpty = -36,
        /// <summary>
        ///登录账号 无此操作权限
        /// </summary>

        Art_AccountNoPower = -37,
        /// <summary>
        ///登录账号角色 无此操作权限
        /// </summary>

        Art_AccountRoleNoPower = -38,
        /// <summary>
        ///指派人不能为空
        /// </summary>

        Art_Current_HandlerNull = -39,
        /// <summary>
        ///缺必填基础参数
        /// </summary>

        Art_ParamsNull = -40,
        /// <summary>
        ///此状态下 不允许删除需求
        /// </summary>

        Art_StateNoDelete = -41,
        #endregion

        #region 微信相关信息
        /// <summary>
        ///微信用户未找到
        /// </summary>

        WeChat_User_Not_Found = -42,
        /// <summary>
        /// 微信账号未绑定希课账户
        /// </summary>

        WeChat_Not_Bound = -43,
        /// <summary>
        /// 微信登录尚未同意
        /// </summary>

        WeChat_Login_Not_Agreed = -44,
        /// <summary>
        /// 微信登录信息已使用
        /// </summary>

        WeChat_Login_State_Used = -45,
        /// <summary>
        /// 微信登录失败
        /// </summary>

        WeChat_Login_Failed = -46,
        /// <summary>
        /// “自定义登录态”无效
        /// </summary>

        WeChat_CustomStatus_No = -6201,
        /// <summary>
        /// 未绑定用户，请先绑定
        /// </summary>

        WeChat_OpenidNoAccount = -6000,
        #endregion

        #region 授权服务
        /// <summary>
        /// 设备授权成功
        /// </summary>

        Device_Authorization_Successful = 1011,
        /// <summary>
        /// 设备添加失败
        /// </summary>

        Device_Add_Faile = -47,
        /// <summary>
        /// 无效参数，机器码无效
        /// </summary>

        Device_DC_Invalid = -48,
        /// <summary>
        /// 无效参数，二维码无效
        /// </summary>

        Device_QRCodeInvalid = -49,
        /// <summary>
        /// 无效参数，请检查机器授权码
        /// </summary>

        Device_AC_Invalid = -50,
        /// <summary>
        /// 设备未经授权
        /// </summary>

        Device_Not_Authorized = -51,
        /// <summary>
        /// 激活二维码已失效
        /// </summary>

        Device_ExpiredQRCode = -52,
        /// <summary>
        /// 该设备已生成激活码，请用激活工具操作
        /// </summary>

        Device_Authorization_Waiting = -53,
        /// <summary>
        /// 设备超出学校可开设备数量！请联系管理员确认
        /// </summary>

        Device_OverSchoolDeviceCount = -54,
        /// <summary>
        /// 是否确定加入新学校
        /// </summary>

        Auth_JoinNewSchool = -1154,
        /// <summary>
        /// 无效授权信息
        /// </summary>

        Device_key_Invalid = -1155,
        /// <summary>
        /// 已加入此学校，无重复操作
        /// </summary>

        Device_SchoolAuth_Exit = -1156,
        /// <summary>
        /// 授权已经过期
        /// </summary>

        Auth_Expired = -1157,
        #endregion

        #region 授权服务  H5
        /// <summary>
        /// 设备激活成功 H5
        /// </summary>

        Device_ActivatedSuccessful = 1010,
        /// <summary>
        /// 设备激活失败 H5
        /// </summary>

        Device_ActivatedFail = -55,
        /// <summary>
        /// 设备所属的学校不能为空，请注意参数
        /// </summary>

        Device_SchoolEmpty = -56,
        /// <summary>
        /// 学校地址信息获取失败，请确定通讯定位畅通
        /// </summary>

        Device_SchoolAddressEmpty = -57,

        #endregion

        #region 课程

        Course_Unavailable = -5001,

        Subject_Permissions_Insufficient = -5002,

        Course_Permissions_Insufficient = -5003,
        /// <summary>
        /// 过期课程缓存，请重新获取课程列表
        /// </summary>

        ExpiredCourseIds = -58,
        /// <summary>
        /// 暂未取得该课程的使用权限
        /// </summary>

        InvalidAuthorCourseId = -60,
        /// <summary>
        /// 课程名称超过限制长度
        /// </summary>

        Course_NameOverLength = -61,
        /// <summary>
        /// 请输入有效过期时间
        /// </summary>

        Course_ExpiresTimeNull = -62,
        /// <summary>
        /// 安装教室输入无效
        /// </summary>

        Course_ClassRoomNull = -63,
        /// <summary>
        /// 此课程标签名已经存在
        /// </summary>

        Dictionary_TagNameExist = -64,
        /// <summary>
        /// 课程包名称超过限制长度
        /// </summary>

        Package_NameOverLength = -65,
        /// <summary>
        /// 此课程包名已经存在
        /// </summary>

        Package_NameExist = -66,
        /// <summary>
        /// 输入搜索关键字无效
        /// </summary>

        Package_SearchPackageNull = -67,
        /// <summary>
        /// 已存在相同版本号
        /// </summary>

        VersionExisted = -200,
        /// <summary>
        /// 用户未有进行课程
        /// </summary>

        Course_NoAccout = -7100,
        /// <summary>
        /// 此账号不支持分享到平台
        /// </summary>

        Course_NoShareToSeek = -7101,
        #endregion

        #region OA服务
        /// <summary>
        /// 分类的 值不允许删除
        /// </summary>
        Classify_NoDeleteOperate = -68,
        /// <summary>
        /// 此分类id不存在，请确认
        /// </summary>

        Classify_NoExiteId = -69,
        /// <summary>
        /// 最多只能选择10个标签哦~
        /// </summary>

        Message_MaxTagCount = -70,
        /// <summary>
        /// 缺少基础参数
        /// </summary>

        Task_BasicsNull = -71,
        /// <summary>
        /// 无数据
        /// </summary>

        Task_Null = -72,
        /// <summary>
        /// 任务名过长
        /// </summary>

        Task_NameOverLength = -73,
        /// <summary>
        /// 建议不超过500字
        /// </summary>

        Task_IdeaOverLength = -74,
        /// <summary>
        /// 登录账号无权操作
        /// </summary>

        Task_NoPower = -75,
        /// <summary>
        /// 该状态或该账号 不支持修改
        /// </summary>

        Task_NoUpdate = -76,
        /// <summary>
        /// 选择时间有误
        /// </summary>

        Burn_PlanDate = -1170,
        /// <summary>
        /// 描述不超过100字哦
        /// </summary>

        Burn_FuntionLenghtOver = -1171,
        /// <summary>
        /// 课程id不能为空
        /// </summary>

        Burn_CourseIdIsNull = -1172,
        /// <summary>
        /// 课程已发布，无需重复操作
        /// </summary>

        Course_ShareOver = -1173,
        /// <summary>
        /// 该课程已引用此百科！
        /// </summary>

        Course_ExistSameWare = -1174,


        #endregion

        #region 上传服务
        /// <summary>
        /// 用户角色为空
        /// </summary>

        Upload_RoleNull = -77,
        /// <summary>
        /// 上传数为空，请重试
        /// </summary>

        Upload_NoData = -78,
        /// <summary>
        /// 上传分类参数为空
        /// </summary>

        Upload_CategoryEmpty = -79,
        /// <summary>
        /// 只能单个上传哦
        /// </summary>

        Upload_AllowOne = -80,
        /// <summary>
        /// 上传文件过大
        /// </summary>

        Upload_FileTooBig = -81,
        /// <summary>
        /// 上传图片过大
        /// </summary>

        Upload_ImgTooBig = -82,
        /// <summary>
        /// 上传视频过大
        /// </summary>

        Upload_VideoTooBig = -83,
        /// <summary>
        /// 该类型不允许上传！
        /// </summary>

        Upload_NoOperate = -84,
        /// <summary>
        /// 图片只支持png格式哟！
        /// </summary>

        Upload_LimitPng = -85,
        /// <summary>
        /// 压缩文件超过5M，请联系管理员
        /// </summary>

        Upload_ZipTooBig = -86,
        /// <summary>
        /// 只支持zip、rar格式哟！
        /// </summary>

        Upload_LimitZipOrRar = -4106,
        /// <summary>
        /// 最多上传20张图片哦
        /// </summary>

        Upload_CountOver = -4088,
        #endregion

        #region 官网服务
        /// <summary>
        /// 表单内容为必填，请认真填写
        /// </summary>

        Kotien_ContactFormEmpty = -104,
        /// <summary>
        /// 你已经提交过多次信息了哦！
        /// </summary>

        Kotien_IP_Used = -105,
        /// <summary>
        /// 缺少必要参数
        /// </summary>

        Kotien_ParameterEmpty = -106,
        /// <summary>
        /// 操作频繁，有问题可以直接联系我们哦！
        /// </summary>

        Kotien_AddContactFormLoser = -107,
        /// <summary>
        /// 长度不规范哦，不要调皮哈
        /// </summary>

        Kotien_LenghtOver = -108,
        /// <summary>
        /// 标签头标题最多100字
        /// </summary>

        Kotien_HeadTitleLenghtOver = -109,
        /// <summary>
        /// seo描述最多1000字
        /// </summary>

        Kotien_Meta_DescriptionLenghtOver = -110,
        /// <summary>
        /// seo关键字最多500字
        /// </summary>

        Kotien_Meta_KeyWordsLenghtOver = -111,

        #endregion

        #region 直播
        /// <summary>
        /// 直播已经关闭
        /// </summary>

        LiveClosed = -112,
        /// <summary>
        /// 已开播，不能取消直播！
        /// </summary>

        Live_NoCancelRoom = -6202,
        /// <summary>
        /// 非主播账号，无权操作！
        /// </summary>

        Live_NoPower = -6203,
        /// <summary>
        /// 您已收藏房间，无需重复操作
        /// </summary>

        Live_RepeatColletion = -6204,
        /// <summary>
        /// 您未收藏此房间，请刷新重试
        /// </summary>

        Live_NeverColletion = -6205,
        /// <summary>
        /// 重复添加笔记
        /// </summary>

        Live_RepeatNote = -6206,
        #endregion

        #region 教研服务
        /// <summary>
        ///组名已存在
        /// </summary>

        Teach_TeachNameExist = -6600,
        /// <summary>
        /// 小组不存在或已删除
        /// </summary>

        Teach_TeamNoExist = -6604,
        /// <summary>
        /// 无权解散小组！
        /// </summary>

        Teach_NoPowerDissolveTeam = -6605,
        /// <summary>
        ///搜索关键字超出长度
        /// </summary>

        Teach_SearchNameNull = -6601,
        /// <summary>
        ///关注成功
        /// </summary>

        Teach_AddFollowerOk = 6602,
        /// <summary>
        ///已取消关注
        /// </summary>

        Teach_DeleteFollowerOk = 6603,
        /// <summary>
        ///您已是小组成员了哦
        /// </summary>

        Teach_IsTeamMember = -6606,
        /// <summary>
        ///您还不是小组成员哦！
        /// </summary>

        Teach_NoIsTeamMember = -6607,
        /// <summary>
        ///组长才有此操作权哦！
        /// </summary>

        Teach_NoCreateNoPower = -6608,
        /// <summary>
        ///缺少学科参数！
        /// </summary>

        Teach_NoSubject = -6609,
        /// <summary>
        ///自己不能关注自己哦！
        /// </summary>

        Teach_MeNoFansMe = -6610,
        /// <summary>
        ///选择时间不符合规范哦！
        /// </summary>

        Teach_DateTimeAbnormal = -6611,
        /// <summary>
        ///您已是活动成员了哦
        /// </summary>

        Teach_IsActivityMember = -6612,
        /// <summary>
        ///您还不是活动成员哦！
        /// </summary>

        Teach_NoIsActivityMember = -6613,
        /// <summary>
        /// 此操作仅活动期间开放
        /// </summary>

        Teach_NoOpenTime = -6614,
        /// <summary>
        /// 该环节已开始，暂不支持编辑
        /// </summary>

        Teach_OverStartTimeNoUpdate = -6615,
        /// <summary>
        /// 该活动不存在或已解散
        /// </summary>

        Teach_ActivityNoExist = -6616,
        /// <summary>
        /// 该环节不存在或已删除
        /// </summary>

        Teach_ActivityLinkNoExist = -6617,
        /// <summary>
        /// 邀请码已过期
        /// </summary>

        Teach_InviteOverNow = -6618,
        /// <summary>
        /// 邀请码不存在或已失效
        /// </summary>

        Teach_InviteCodeOver = -6619,
        /// <summary>
        /// 资源不存在或已删除！
        /// </summary>

        Teach_ResourceNoExist = -6620,
        #endregion

        #region 订单服务
        /// <summary>
        /// 账号无权操作审核
        /// </summary>

        Order_NoPower = -10000,
        /// <summary>
        /// 该状态暂不能操作
        /// </summary>

        Order_NoPowerState = -10001,
        /// <summary>
        ///目前只支持市场人员、管理员创建噢！
        /// </summary>

        Order_MarketCreateOrder = -10002,
        /// <summary>
        ///请认真填写合同号
        /// </summary>

        Order_ContractNumberNoOK = -10003,
        /// <summary>
        ///此订单还未完成，不支持续单操作！
        /// </summary>

        Order_OrderUnFinishNoKeep = -10004,
        /// <summary>
        ///尚有未完成的续单，暂不能新增续单！
        /// </summary>

        Order_KeepOrderUnFinishNoKeep = -10005,
        /// <summary>
        /// 分配课程包和安装数 必填其一
        /// </summary>

        Order_PackgeIds = -10010,
        /// <summary>
        /// 请填写安装数量
        /// </summary>

        Order_NoInstallCount = -10011,
        /// <summary>
        /// 出库单已存在，请认真核对订单!
        /// </summary>

        Order_DeliveryExit = -10012,
        /// <summary>
        /// 财务审核还未通过，请认真核对订单!
        /// </summary>

        Order_CWcheckNoPass = -10012,

        #endregion

        #region 新备课服务
        /// <summary>
        /// 官方账号不能在此创建课程！
        /// </summary>

        Plan_NoCreateCourse = -20000,
        /// <summary>
        /// 该课程未被分享！
        /// </summary>

        Plan_CourseNoShare = -20001,
        /// <summary>
        /// 该课程未被授权单位！
        /// </summary>

        Plan_CourseNoAuthSchool = -20002,
        /// <summary>
        /// 原始课程暂停分享！
        /// </summary>

        Plan_OldCourseNoShare = -20003,
        /// <summary>
        /// 原始课程未被授权单位！
        /// </summary>

        Plan_OldCourseNoAuthSchool = -20004,
        /// <summary>
        /// 原始课程不存在或已删除！
        /// </summary>

        Plan_OldCourseIsNull = -20005,
        /// <summary>
        /// 账号已创建过此课程！
        /// </summary>

        Plan_AccountIsCreateCourse = -20006,
        #endregion
    }
}
