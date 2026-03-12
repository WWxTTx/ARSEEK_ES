using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class InputFieldModule : MonoBehaviour
{
    private void Awake()
    {
        bool isOpenClear = false;
        bool isSelected = false;

        InputField_LinkMode inputField = GetComponentInChildren<InputField_LinkMode>();
        {
            Button clearButton = this.GetComponentByChildName<Button>("ClearButton");
            {
                clearButton.onClick.AddListener(() =>
                {
                    inputField.text = string.Empty;
                    inputField.onEndEdit.Invoke(string.Empty);
                    clearButton.gameObject.SetActive(false);
                });

                inputField.onValueChanged.AddListener(content =>
                {
                    clearButton.image.SetAlpha(1);
                    //if (isOpenClear) clearButton.gameObject.SetActive(content.Length > 0);
                    //isOpenClear = content.Length > 0;
                    if(isSelected) clearButton.gameObject.SetActive(content.Length > 0);
                });
            }

            EventTrigger Touch = inputField.GetComponent<EventTrigger>() ?? inputField.gameObject.AddComponent<EventTrigger>();
            {
                Touch.AddEvent(EventTriggerType.Select, arg =>
                {
                    isSelected = true;
                    clearButton.image.SetAlpha(1);
                    //clearButton.gameObject.SetActive(isOpenClear);
                    clearButton.gameObject.SetActive(inputField.text.Length > 0);
                });

                //if (!isAndroid)
                Touch.AddEvent(EventTriggerType.Deselect, arg =>
                {
                    isSelected = false;
                    clearButton.image.SetAlpha(0);
                    this.WaitTime(0.2f, () => { if (clearButton != null) clearButton.gameObject.SetActive(false); });
                });
            }

            Toggle PasswordToggle = this.GetComponentByChildName<Toggle>("PasswordToggle");
            {
                PasswordToggle?.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        inputField.inputType = InputField.InputType.Password;
                        //inputField.contentType = InputField.ContentType.Password;
                    }
                    else
                    {
                        inputField.inputType = InputField.InputType.Standard;
                        //inputField.contentType = InputField.ContentType.Custom;
                    }
                    inputField.RefreshLabel();
                });
            }
        }
    }
}