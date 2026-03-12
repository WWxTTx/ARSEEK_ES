using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UI.Tables;
using UnityEngine.Serialization;
using TMPro;

namespace UI.Dates
{
    public class DatePicker_DayHeader : MonoBehaviour
    {
#pragma warning disable
        [SerializeField, FormerlySerializedAs("HeaderText"), HideInInspector]
        private Text oldHeaderText;
#pragma warning restore

        [SerializeField]
        private TextMeshProUGUI m_TMPHeaderText;

        public TextMeshProUGUI TMPHeaderText
        {
            get
            {
                if (m_TMPHeaderText == null)
                {
                    if (oldHeaderText == null)
                    {
                        Debug.LogError("[DatePicker] An error ocurred while upgrading to TextMesh Pro - unable to locate original text element for a day header.");
                        return null;
                    }

                    m_TMPHeaderText = DatePicker_TextMeshProUtilities.ReplaceTextElementWithTextMeshPro(oldHeaderText);

                    m_TMPHeaderText.enableAutoSizing = true;
                    m_TMPHeaderText.fontSizeMin = 2;
                    m_TMPHeaderText.fontSizeMax = 16;
                }

                return m_TMPHeaderText;
            }
        }

        [SerializeField]
        private Text m_HeaderText;

        public Text HeaderText
        {
            get
            {
                if (m_HeaderText == null)
                {
                    //if (oldHeaderText == null)
                    //{
                    //    Debug.LogError("[DatePicker] An error ocurred while upgrading to TextMesh Pro - unable to locate original text element for a day header.");
                    //    return null;
                    //}

                    m_HeaderText = GetComponentInChildren<Text>();
                }

                return m_HeaderText;
            }
        }

        public TableCell Cell;
    }
}
