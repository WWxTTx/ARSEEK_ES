
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

namespace UnityFramework.Runtime
{
    public class UIData
    {

    }
    /// <summary>
    /// UI对象基类
    /// </summary>
    public class UIBase : MonoBase
    {
        [HideInInspector]
        public bool isAndroid
        {
            get
            {
#if UNITY_STANDALONE
                return false;
#elif UNITY_ANDROID || UNITY_IOS
                return true;
#endif
            }
        }
        protected override void InitComponents()
        {
            base.InitComponents();
        }

        /// <summary>
        /// 刷新所有的布局控制器 用于解决布局更新不及时的问题
        /// </summary>
        public void RefreshLayouGroup()
        {
            foreach (var component in GetComponentsInChildren<LayoutGroup>())
            {
                LayoutRebuilder.MarkLayoutForRebuild(component.GetComponent<RectTransform>());
            }
        }

        #region 框架通用函数
        /// <summary>
        /// 能否从当前panel退出登录
        /// </summary>
        protected virtual bool CanLogout { get { return false; } }
        /// <summary>
        /// 是否可重复
        /// </summary>
        public virtual bool Repeatable { get { return false; } }

        /// <summary>
        /// 进场动画时长
        /// </summary>
        protected virtual float joinAnimePlayTime { get { return 0.5f; } }
        public float JoinAnimePlayTime { get { return joinAnimePlayTime * GlobalInfo.uiAnimRatio; } }
        /// <summary>
        /// 退场动画时长
        /// </summary>
        protected virtual float exitAnimePlayTime { get { return 0.4f; } }
        public float ExitAnimePlayTime { get { return exitAnimePlayTime * GlobalInfo.uiAnimRatio; } }
        /// <summary>
        /// 动效是否遮罩
        /// </summary>
        protected virtual bool UIMask { get { return true; } }

        /// <summary>
        /// 进场动画序列
        /// </summary>
        protected Sequence JoinSequence;
        /// <summary>
        /// 退场动画序列
        /// </summary>
        protected Sequence ExitSequence;

        /// <summary>
        /// 检查是否重复
        /// </summary>
        public virtual bool CheckIsDuplicated(UIData uiData = null)
        {
            return false;
        }

        /// <summary>
        /// 创建(实例化)时调用
        /// </summary>
        public virtual void Open(UIData uiData = null)
        {
            //AddButtonSE();
            AddMsg((ushort)ShortcutEvent.PressAnyKey);
        }

        /// <summary>
        /// 界面显示时调用
        /// </summary>
        public virtual void Show(UIData uiData = null)
        {
            if (JoinSequence != null)
                JoinSequence.Kill();
            if (ExitSequence != null)
                ExitSequence.Kill();

            JoinSequence = DOTween.Sequence();
            JoinSequence.SetAutoKill(false);
            JoinSequence.Pause();

            gameObject.SetActive(true);
            if (UIMask)
                SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
            JoinAnim(() =>
            {
                if (UIMask)
                    SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
            });
        }

        /// <summary>
        /// 界面隐藏时调用
        /// </summary>
        public virtual void Hide(UIData uiData = null, UnityAction callback = null)
        {
            if (JoinSequence != null)
                JoinSequence.Kill();
            if (ExitSequence != null)
                ExitSequence.Kill();

            ExitSequence = DOTween.Sequence();
            ExitSequence.SetAutoKill(false);
            ExitSequence.Pause();

            if (UIMask)
                SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
            ExitAnim(() =>
            {
                if (UIMask)
                    SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
                gameObject.SetActive(false);
                callback?.Invoke();
            });
        }

        /// <summary>
        /// 添加按钮音效
        /// </summary>
        protected virtual void AddButtonSE()
        {
            foreach (var button in GetComponentsInChildren<Button>())
            {
                button.onClick.AddListener(() => SoundManager.Instance.PlayEffect("ButtonClick"));
            }
        }

        /// <summary>
        /// 销毁时调用
        /// </summary>
        public virtual void Close(UIData uiData = null, UnityAction callback = null)
        {
            if (JoinSequence != null)
                JoinSequence.Kill();
            if (ExitSequence != null)
                ExitSequence.Kill();

            ExitSequence = DOTween.Sequence();
            ExitSequence.SetAutoKill(false);
            ExitSequence.Pause();

            if (UIMask)
                SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
            ExitAnim(() =>
            {
                if (UIMask)
                    SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
                callback?.Invoke();
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// 进场动画
        /// </summary>
        /// <param name="callback">回调</param>
        public virtual void JoinAnim(UnityAction callback)
        {
            JoinSequence.OnComplete(() =>
            {
                callback?.Invoke();
            });
            JoinSequence.Restart();
        }

        /// <summary>
        /// 退场动画
        /// </summary>
        /// <param name="callback">回调</param>
        public virtual void ExitAnim(UnityAction callback)
        {
            ExitSequence.OnComplete(() =>
            {
                callback?.Invoke();
            });
            ExitSequence.Restart();
        }
        #endregion
    }
}