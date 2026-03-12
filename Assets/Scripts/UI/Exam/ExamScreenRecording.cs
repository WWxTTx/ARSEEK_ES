using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using RenderHeads.Media.AVProMovieCapture;
using FFmpeg;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;


/// <summary>
/// 考试视频录制
/// </summary>
public class ExamScreenRecording : MonoBase, IFFmpegHandler
{
    /// <summary>
    /// 单例（全局访问）
    /// </summary>
    public static ExamScreenRecording Instance;

    [SerializeField] private GameViewEncoder encoder;
    [SerializeField] CaptureFromTexture _movieCapture = null;

    /// <summary>
    /// 当前考核Id
    /// </summary>
    private int mExamId;
    public int ExamId
    {
        get
        {
            return mExamId;
        }
        set
        {
            mExamId = value;
            ClearOtherExamFolders();
        }
    }

    //用于存储本次考核中存储的考核id
    private HashSet<int> examIdList = new HashSet<int>();

    /// <summary>
    /// 本地视频存储路径
    /// </summary>
    private string FileRootPath => $"{ResManager.Instance.examRecordingPath}/{GlobalInfo.account.id}";
    private string FilePath => $"{FileRootPath}/{ExamId}";

    /// <summary>
    /// serverResPath + 考核id  就是后台存储文件夹的文件夹路径
    /// </summary>
    private string serverResPath = "Exam/ClientRecording";

    /// <summary>
    /// 录制模式
    /// false: 百科ID.mp4 切换百科覆盖原有的操作视频
    /// true: 百科ID_timestamp.mp4 切换百科合并视频分段，合并完成后保留输出文件、删除源文件
    /// </summary>
    public bool MergeMode = false;

    /// <summary>
    /// 是否正在写入文件
    /// </summary>
    private bool fileWriting = false;
    public bool FileWriting => fileWriting;

    private string lastFilePrefix;

    /// <summary>
    /// 是否正在合并视频
    /// </summary>
    public bool Merging => startMerge;

    /// <summary>
    /// 是否正在上传文件
    /// </summary>
    private HashSet<string> uploading = new HashSet<string>();

    /// <summary>
    /// 是否完成全部合并、上传任务
    /// </summary>
    public bool TaskCompleted => !startMerge && uploading.Count == 0 && StorageManager.Instance.TaskCompleted;

    private void Awake()
    {
        Instance = this;
        FFmpegParser.Handler = this;
    }

    protected override void InitComponents()
    {
        base.InitComponents();

        if (GlobalInfo.IsHomeowner()) return;

        //start和切换百科功能重复，并且以为调用时间的问题，start会报错，提交这个消息暂未使用
        AddMsg(new ushort[]{
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)ExamPanelEvent.Start,
            //(ushort)ExamPanelEvent.Resume,
            //(ushort)ExamPanelEvent.Pause,
        });

    }

    private void OnEnable()
    {
        if(_movieCapture != null)
            _movieCapture.CompletedFileWritingAction += OnCompleteFinalFileWriting;
    }

    private void OnDisable()
    {
        if (_movieCapture != null)
            _movieCapture.CompletedFileWritingAction -= OnCompleteFinalFileWriting;
    }

    void OnCompleteFinalFileWriting(FileWritingHandler handler)
    {
        fileWriting = false;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        if (GlobalInfo.IsHomeowner()) return;

        switch (msg.msgId)
        {
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
                if (!GlobalInfo.ExamRecording)
                    return;

                //切换考核百科时启动录制
                if (gameObject.activeSelf && encoder != null && _movieCapture != null && GlobalInfo.isExam)
                {
                    foreach (var item in GlobalInfo.currentWikiList)
                    {
                        if (item.id == ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg)
                        {
                            //如果是操作百科
                            if (item.typeId == (int)PediaType.Operation)
                            {
                                //手机热闪退 暂时停用该功能
                                //StartCoroutine(StartCapture(((MsgBrodcastOperate)msg).GetData<MsgInt>().arg));//baikeId
                            }
                            else
                            {
                                StopRecordMovie();
                            }
                        }
                    }
                }
                break;
            case (ushort)ExamPanelEvent.Start:
                //清理之前存储的考核百科
                examIdList.Clear();
                uploading.Clear();
                break;
                //case (ushort)ExamPanelEvent.Pause:
                //    if (encoder != null && _movieCapture != null && GlobalInfo.isExam)
                //    {
                //        //暂停
                //        _movieCapture.PauseCapture();
                //    }
                //    break;
                //case (ushort)ExamPanelEvent.Resume:
                //    if (encoder != null && _movieCapture != null && GlobalInfo.isExam)
                //    {
                //        //继续播放
                //        _movieCapture.ResumeCapture();
                //    }
                //    break;
        }
    }

    /// <summary>
    /// 设置并启动录制
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private IEnumerator StartCapture(int baikeId/*string fileName*/)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => encoder.GetStreamTexture != null && !fileWriting);

        //更新录制图像
        _movieCapture.SetSourceTexture(encoder.GetStreamTexture);
#if UNITY_ANDROID
        _movieCapture.OutputFolder = CaptureBase.OutputPath.RelativeToPeristentData;
#endif
        //设置录制本地文件夹
        _movieCapture.OutputFolderPath = FilePath;
        //if (string.IsNullOrEmpty(fileName))
        //{
        //    fileName = GlobalInfo.currentWiki.id.ToString();
        //}
        //设置存储文件名
        _movieCapture.AppendFilenameTimestamp = MergeMode;
        _movieCapture.FilenamePrefix = baikeId.ToString();
        //设置视频长宽
        _movieCapture.ResolutionDownscaleCustom = new Vector2(encoder.GetStreamTexture.width, encoder.GetStreamTexture.height);

        AddExamId(baikeId/*int.Parse(_movieCapture.FilenamePrefix)*/);
        //开启录制
        _movieCapture.StartCapture();

        fileWriting = true;
        lastFilePrefix = _movieCapture.FilenamePrefix;
    }

    private void Update()
    {
        if (!GlobalInfo.ExamRecording)
            return;

        if (GlobalInfo.IsHomeowner()) return;

        if (GlobalInfo.isExam)
        {
            if (encoder != null && _movieCapture != null && _movieCapture.IsCapturing())
            {
                //不断更新图片
                _movieCapture.SetSourceTexture(encoder.GetStreamTexture);
            }
        }
    }

    /// <summary>
    /// 停止录制 生成视频
    /// </summary>
    /// <returns></returns>
    public void StopRecordMovie()
    {
        if (encoder != null && _movieCapture != null)
        {
            _movieCapture.StopCapture();
            //_movieCapture.CancelCapture();
        }
    }

    /// <summary>
    /// 合并视频
    /// </summary>
    /// <returns></returns>
    public string MergeVideo()
    {
        if (string.IsNullOrEmpty(lastFilePrefix))
            return string.Empty;

        if (MergeMode)
        {
            inputFiles = Directory.EnumerateFiles(FilePath, $"{lastFilePrefix}*.mp4").OrderBy(f => f).Select(f => f.Replace("\\", "/")).ToList();
            if (inputFiles == null || inputFiles.Count == 0)
            {
                return string.Empty;
            }

            // 频繁切换试题时，inputFiles中可能包含正在上传的文件，导致合并失败ffmpeg failure Impossible to open file
            if (inputFiles.Any(f => uploading.Contains(f)))
            {
                return string.Empty;
            }

            if (inputFiles.Count > 1)
            {
                startMerge = true;
                //合并视频
                string mergedVideoPath = $"{FilePath}/{lastFilePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_0merged.mp4";
                MergeVideos(inputFiles, $"{lastFilePrefix}_AppendInputFiles.txt", mergedVideoPath);
                return mergedVideoPath;
            }
            else
            {
                return inputFiles[0];
            }
        }
        else
        {
            return $"{FilePath}/{lastFilePrefix}.mp4";
        }
    }

    /// <summary>
    /// 合并多个视频文件
    /// </summary>
    /// <param name="inputFiles">输入视频文件列表</param>
    /// <param name="appendFile">输入视频文件列表文本文件</param>
    /// <param name="outputFile">输出视频文件路径</param>
    private void MergeVideos(List<string> inputFiles, string appendFile, string outputFile)
    {
        AppendData config = new AppendData();
        inputFiles.Sort();
        foreach (var file in inputFiles)
        {
            config.inputPaths.Add(file);
        }
        config.appendFile = appendFile;
        //Include whitespaces using quoting
        //将outputFile包裹在双引号中，确保路径中包含空格时，路径能够被正确解析
        config.outputPath = $"\"{outputFile}\"";
        FFmpegCommands.AppendFast(config);
    }


    /// <summary>
    /// 上传视频
    /// </summary>
    /// <param name="examid"></param>
    /// <param name="fileName"></param>
    public void Upload(int examid, int baikeId, string filePath, UnityAction<int, string> callback = null)
    {
        if (string.IsNullOrEmpty(filePath))
            return;
        StartCoroutine(UploadFile(examid, baikeId, filePath, callback));
    }

    private IEnumerator UploadFile(int examId, int baikeId, string filePath, UnityAction<int, string> callback)
    {
        uploading.Add(filePath);

        yield return new WaitUntil(() => !startMerge);
        yield return new WaitForEndOfFrame();

        if (!File.Exists(filePath))
        {
            uploading.Remove(filePath);
            callback?.Invoke(baikeId, string.Empty);
            yield break;
        }

        bool result = false;
        //获取STS
        RequestManager.Instance.GetSTS(async (stsInfo) =>
        {
            string objectName = $"{serverResPath}/{examId}/{baikeId}_{GlobalInfo.account.id}.mp4";
            Log.Debug($"{stsInfo.storeType} 开始上传 {filePath} 至 {objectName}");
            result = await StorageManager.Instance.PutObjectAsync(stsInfo, filePath, objectName, null, null, null);
            uploading.Remove(filePath);
        }, errorMessage =>
        {
            Log.Error("STS获取失败:" + errorMessage);
            uploading.Remove(filePath);
        });

        yield return new WaitUntil(() => !uploading.Contains(filePath));
        Log.Debug(result ? $"上传视频文件{filePath}成功" : $"上传视频文件{filePath}失败");
        callback?.Invoke(baikeId, result ? GetVideoObjectName(examId, baikeId, filePath) : string.Empty);
    }

    /// <summary>
    /// 添加已经进行过的考核的百科号，用于之后查找哪些视频需要上传
    /// </summary>
    /// <param name="id"></param>
    public void AddExamId(int id)
    {
        //if (GlobalInfo.IsHomeowner()) return;

        examIdList.Add(id);
    }

    /// <summary>
    /// 获取上传阿里云视频的存储位置
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="baikeId">百科id</param>
    /// <param name="file"></param>
    /// <returns></returns>
    public string GetVideoObjectName(int examId, int baikeId, string file)
    {
        if (GlobalInfo.IsHomeowner()) return string.Empty;

        string str = string.Empty;

        if (examIdList.Contains(baikeId))
        {
            //if (File.Exists($"{FilePath}/{id}.mp4"))
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                str = $"{serverResPath}/{examId}/{baikeId}_{GlobalInfo.account.id}.mp4";
            }
        }

        return str;
    }

    /// <summary>
    /// 清除录制文件
    /// </summary>
    public void ClearFiles()
    {
        if (uploading.Count != 0)
            return;

        try
        {
            if (Directory.Exists(FilePath))
            {
                var files = Directory.GetFiles(FilePath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"清除录制文件异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除非当前考核的录制文件夹
    /// </summary>
    public void ClearOtherExamFolders()
    {
        if (uploading.Count != 0)
            return;

        try
        {
            if (Directory.Exists(FileRootPath))
            {
                var directories = Directory.GetDirectories(FileRootPath);
                foreach (var dir in directories)
                {
                    if (!dir.EndsWith(ExamId.ToString()))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"清除文件夹异常：{ex.Message}");
        }
    }

    #region FFMpeg Handler

    FFmpegHandler defaultHandler = new FFmpegHandler();

    /// <summary>
    /// 合并视频 源文件
    /// </summary>
    List<string> inputFiles = new List<string>();

    /// <summary>
    /// 是否正在合并视频
    /// </summary>
    private bool startMerge;

    public void OnStart()
    {
        startMerge = true;
        defaultHandler.OnStart();
    }

    public void OnProgress(string msg)
    {
        //defaultHandler.OnProgress(msg);
    }

    public void OnFailure(string msg)
    {
        defaultHandler.OnFailure(msg);
    }

    public void OnSuccess(string msg)
    {
        //defaultHandler.OnSuccess(msg);
    }

    public void OnFinish()
    {
        startMerge = false;
        defaultHandler.OnFinish();

        //完成合并 删除源视频文件
        try
        {
            foreach (var file in inputFiles)
            {
                File.Delete(file);
            }
            inputFiles.Clear();
        }
        catch (Exception ex)
        {
            Log.Error($"清除文件异常：{ex.Message}");
        }
    }
    #endregion
}