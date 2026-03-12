using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class InputFieldScaler : MonoBehaviour, ILayoutElement
{
    private Text textComponent { get { return GetComponent<InputField>().textComponent; } }
    public TextGenerationSettings GetTextGenerationSettings(Vector2 extents)
    {
        var settings = textComponent.GetGenerationSettings(extents);
        settings.generateOutOfBounds = true;
        return settings;
    }
    private RectTransform m_Rect;
    private RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }
    public void OnValueChanged(string v)
    {
        if (!fixedWidth)
        {
            rectTransform.SetSizeWithCurrentAnchors(0, LayoutUtility.GetPreferredWidth(m_Rect));
        }
        rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, LayoutUtility.GetPreferredHeight(m_Rect));
    }
    void OnEnable()
    {
        inputField.onValueChanged.AddListener(OnValueChanged);
    }
    void OnDisable() { }
    private Vector2 originalSize;
    private InputField _inputField;
    public InputField inputField { get { return _inputField ?? (_inputField = GetComponent<InputField>()); } }
    private float _offsetHeight;
    public float offsetHeight
    {
        get
        {
            if (_offsetHeight == 0)
                _offsetHeight = generatorForLayout.GetPreferredHeight(text, GetTextGenerationSettings(Vector2.zero)) / textComponent.pixelsPerUnit;
            return _offsetHeight;
        }
    }
    private float _offsetTextComponentLeftRingt;
    public float offsetTextComponentLeftRingt
    {
        get
        {
            if (_offsetTextComponentLeftRingt == 0)
                _offsetTextComponentLeftRingt = Mathf.Abs(rectTransform.rect.xMin - textComponent.rectTransform.rect.xMin) + Mathf.Abs(rectTransform.rect.xMax - textComponent.rectTransform.rect.xMax);
            return _offsetTextComponentLeftRingt;
        }
    }
    protected void Awake()
    {
        originalSize = GetComponent<RectTransform>().sizeDelta;
        inputField.lineType = fixedWidth ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
        rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, LayoutUtility.GetPreferredHeight(m_Rect));
    }
    private string text { get { return GetComponent<InputField>().text; } }
    private TextGenerator _generatorForLayout;
    public TextGenerator generatorForLayout { get { return _generatorForLayout ?? (_generatorForLayout = new TextGenerator()); } }

    public virtual void CalculateLayoutInputHorizontal() { }
    public virtual void CalculateLayoutInputVertical() { }
    public virtual float minWidth { get { return -1; } }
    public virtual float preferredWidth
    {
        get
        {
            if (fixedWidth)
            {
                return originalSize.x;
            }
            else
            {
                return Mathf.Max(originalSize.x, generatorForLayout.GetPreferredWidth(text, GetTextGenerationSettings(Vector2.zero)) / textComponent.pixelsPerUnit + offsetTextComponentLeftRingt);
            }
        }
    }
    public virtual float flexibleWidth { get { return -1; } }
    public virtual float minHeight { get { return -1; } }
    public virtual float preferredHeight
    {
        get
        {
            if (fixedHeight)
            {
                return originalSize.y;
            }
            else
            {
                return generatorForLayout.GetPreferredHeight(text, GetTextGenerationSettings(new Vector2(textComponent.GetPixelAdjustedRect().size.x, 0.0f))) / textComponent.pixelsPerUnit + offsetHeight;
            }
        }
    }
    public virtual float flexibleHeight { get { return -1; } }
    /// <summary>
    /// 是否保持宽度不变
    /// </summary>
    [HideInInspector]
    public bool fixedWidth = true;
    /// <summary>
    /// 是否保持高度不变
    /// </summary>
    [HideInInspector]
    public bool fixedHeight = false;
    private int priority = 1; //提高Layout计算优先级，要比InputField大 这里设为1
    public virtual int layoutPriority { get { return priority; } }
}


