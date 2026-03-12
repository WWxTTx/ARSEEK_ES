using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ËÑË÷¿òÄ£¿é
/// </summary>
public class SearchModule : MonoBehaviour
{
    private InputField_LinkMode inputField;
    private Button clearButton;

    public UnityEvent<string> OnSearch = new UnityEvent<string>();

    public string Text
    {
        get
        {
            return inputField.text;
        }
        set
        {
            inputField.text = value;
        }
    }

    private void Awake()
    {
        inputField = GetComponentInChildren<InputField_LinkMode>();
        clearButton = this.GetComponentByChildName<Button>("ClearButton");

        inputField.onEndEdit.AddListener((value) =>
        {
            if(string.IsNullOrEmpty(value.Replace(" ","")))
            {
                inputField.text = string.Empty;
                //return;
            }
            OnSearch?.Invoke(value);
        });
        inputField.onValueChanged.AddListener(content =>
        {
            clearButton.gameObject.SetActive(content.Length > 0);
        });

        clearButton.onClick.AddListener(() =>
        {
            inputField.text = string.Empty;
            OnSearch?.Invoke(string.Empty);
        });
    }
}
