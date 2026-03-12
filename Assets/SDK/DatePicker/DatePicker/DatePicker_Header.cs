using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UI.Tables;
using TMPro;

namespace UI.Dates
{
    public class DatePicker_Header : MonoBehaviour
    {
        #pragma warning disable
        [SerializeField, FormerlySerializedAs("HeaderText")]
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
                        Debug.LogError($"[DatePicker] An error ocurred while upgrading to TextMesh Pro - unable to locate original header text element. {transform.name}");
                        return null;
                    }

                    m_TMPHeaderText = DatePicker_TextMeshProUtilities.ReplaceTextElementWithTextMeshPro(oldHeaderText);
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
                    if (oldHeaderText == null)
                    {
                        Debug.LogError($"[DatePicker] An error ocurred while upgrading to TextMesh Pro - unable to locate original header text element. {transform.name}");
                        return null;
                    }
                }

                return m_HeaderText;
            }
        }

        public DatePicker_Button PreviousMonthButton;
        public DatePicker_Button NextMonthButton;
        public DatePicker_Button PreviousYearButton;
        public DatePicker_Button NextYearButton;
        public Image Background;
        public TableRow Row;
        public TableLayout TableLayout;
    }
}
