using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 日志显示Item
    /// </summary>
    public class LogItem : MonoBehaviour
    {
        public Button btn;
        public Image bg;
        public Image icon;
        public Text info;
        public Text num;

        private GameObject numGO;
        /// <summary>
        /// 最大显示字符数
        /// </summary>
        private int infoMax = 150;

        public void Show(Color32 bgColor, Sprite levelIcon, string time, string msg, int count,
            bool isShowNum = false, Action onClick = null)
        {
            bg.color = bgColor;
            icon.sprite = levelIcon;

            //超过长度的字符串裁切
            if (msg.Length > infoMax)
                msg = msg.Substring(0,infoMax)+"...";

            info.text = "[" + time + "] " + msg;

            if (count > 999)
                num.text = "999+";
            else
                num.text = count.ToString();

            if (numGO == null)
                numGO = num.transform.parent.gameObject;
            numGO.SetActive(isShowNum);
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
