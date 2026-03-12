using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace UnityFramework.Runtime
{
    public class UIProfilerModule : UIModuleBase
    {
        /// <summary>
        /// FPS
        /// </summary>
        public Text fps;
        /// <summary>
        /// Unity 内部分配器分配的总内存
        /// </summary>
        public Text TotalAllocatedMemory;
        /// <summary>
        /// Unity 保留的总内存
        /// </summary>
        public Text TotalReservedMemory;
        /// <summary>
        /// 当 Unity 需要分配内存时，Unity 会在池中分配内存以供使用。此函数返回这些池中未使用的内存量。
        /// </summary>
        public Text TotalUnusedReservedMemory;

        /// <summary>
        /// 计时器
        /// </summary>
        private float timer;
        /// <summary>
        /// 性能参数刷新间隔
        /// </summary>
        private float updateInterval = 0.5f;
        /// <summary>
        /// FPS计算工具
        /// </summary>
        private FpsCounter fpsCounter;

        public override void Open(UIData uiData = null)
        {
            base.Open(uiData);
            timer = updateInterval;
            fpsCounter = new FpsCounter(updateInterval);
        }

        void Update()
        {
            fpsCounter.Update(Time.unscaledDeltaTime);

            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0;
                Refresh();
            }
        }

        void Refresh()
        {
            //渲染
            fps.text = "FPS   " + fpsCounter.CurrentFps;
            //内存
            TotalAllocatedMemory.text = "Total Allocated Memory       " + GetByteLengthString(Profiler.GetTotalAllocatedMemoryLong());
            TotalReservedMemory.text = "Total Reserved Memory        " + GetByteLengthString(Profiler.GetTotalReservedMemoryLong());
            TotalUnusedReservedMemory.text = "Total Unused Reserved Memory " + GetByteLengthString(Profiler.GetTotalUnusedReservedMemoryLong());
        }

        protected static string GetByteLengthString(long byteLength)
        {
            if (byteLength < 1024L) // 2 ^ 10
            {
                return string.Format("{0} Bytes", byteLength.ToString());
            }

            if (byteLength < 1048576L) // 2 ^ 20
            {
                return string.Format("{0} KB", (byteLength / 1024f).ToString("F2"));
            }

            if (byteLength < 1073741824L) // 2 ^ 30
            {
                return string.Format("{0} MB", (byteLength / 1048576f).ToString("F2"));
            }

            if (byteLength < 1099511627776L) // 2 ^ 40
            {
                return string.Format("{0} GB", (byteLength / 1073741824f).ToString("F2"));
            }

            if (byteLength < 1125899906842624L) // 2 ^ 50
            {
                return string.Format("{0} TB", (byteLength / 1099511627776f).ToString("F2"));
            }

            if (byteLength < 1152921504606846976L) // 2 ^ 60
            {
                return string.Format("{0} PB", (byteLength / 1125899906842624f).ToString("F2"));
            }

            return string.Format("{0} EB", (byteLength / 1152921504606846976f).ToString("F2"));
        }
    }
}
