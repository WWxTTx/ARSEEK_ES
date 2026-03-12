
namespace UnityFramework.Runtime
{
    /// <summary>
    /// 计算FPS工具类
    /// </summary>
    public class FpsCounter
    {
        /// <summary>
        /// 当前FPS
        /// </summary>
        public float CurrentFps
        {
            get
            {
                return m_CurrentFps;
            }
        }
        /// <summary>
        /// 刷新FPS时间(秒)
        /// </summary>
        public float UpdateInterval
        {
            get
            {
                return m_UpdateInterval;
            }
            set
            {
                if (value <= 0f)
                {
                    Log.Error("Update interval is invalid.");
                    return;
                }

                m_UpdateInterval = value;
                Reset();
            }
        }

        /// <summary>
        /// 刷新FPS时间(秒)
        /// </summary>
        private float m_UpdateInterval;
        /// <summary>
        /// 当前FPS
        /// </summary>
        private float m_CurrentFps;
        /// <summary>
        /// 刷新时间内帧数
        /// </summary>
        private int m_Frames;
        /// <summary>
        /// 刷新时间内帧数对应时间(秒)
        /// </summary>
        private float m_Accumulator;
        /// <summary>
        /// 计时器
        /// </summary>
        private float m_TimeLeft;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="updateInterval">刷新FPS时间(秒)</param>
        public FpsCounter(float updateInterval)
        {
            if (updateInterval <= 0f)
            {
                Log.Error("Update interval is invalid.");
                return;
            }

            m_UpdateInterval = updateInterval;
            Reset();
        }
        /// <summary>
        /// 计算FPS函数，每一帧调用在unity Update中调用
        /// </summary>
        /// <param name="realElapseSeconds">从上一帧到当前帧的时间间隔，以秒为单位</param>
        public void Update(float realElapseSeconds)
        {
            m_Frames++;
            m_Accumulator += realElapseSeconds;
            m_TimeLeft -= realElapseSeconds;

            if (m_TimeLeft <= 0f)
            {
                m_CurrentFps = m_Accumulator > 0f ? m_Frames / m_Accumulator : 0f;
                m_Frames = 0;
                m_Accumulator = 0f;
                m_TimeLeft += m_UpdateInterval;
            }
        }

        /// <summary>
        /// 重置计算所需数据
        /// </summary>
        private void Reset()
        {
            m_CurrentFps = 0f;
            m_Frames = 0;
            m_Accumulator = 0f;
            m_TimeLeft = 0f;
        }
    }
}
