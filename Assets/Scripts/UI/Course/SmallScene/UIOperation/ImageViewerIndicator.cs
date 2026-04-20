using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// ͼƬ������ʾ
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ImageViewerIndicator : MonoBehaviour
{
    [SerializeField] private float _fadeTime = 0.5f;
    [SerializeField] private float _showAlpha = 1f;
    [SerializeField] private float _hideAlpha = 0f;
    [SerializeField] private float _showTime = 3f;

    [SerializeField] private float _deltaThreshold = 0.001f;
    public float DeltaThreshold { get { return _deltaThreshold; } }

    private CanvasGroup _canvasGroup;
    private Text _text;
    private bool _currentShow = true;

    private string formatter = "{0:N0}%";
    private string maxMsg = "�ѷŵ����";
    private string minMsg = "��������С";

    private float _minScale;
    private float _maxScale;

    private bool initialized = false;

    private string SHOW_ID = "show";
    private string HIDE_ID = "hide";

    private void InitComponents()
    {
        if (initialized)
            return;
        _canvasGroup = GetComponent<CanvasGroup>();
        _text = GetComponentInChildren<Text>(true);
//#if UNITY_ANDROID
//        _text.fontSize *= 2;
//#endif
        initialized = true;
    }

    public void SetScaleRange(float min, float max)
    {
        InitComponents();
        _minScale = min;
        _maxScale = max;
    }

    public void Show(float scale = 0)
    {
        string msg = string.Empty;
        if (scale == _maxScale)
            msg = maxMsg;
        else if (scale == _minScale)
            msg = minMsg;
        else
            msg = string.Format(formatter, (1 + scale) * 100f);

        if (_currentShow)
        {
            _text.text = msg;
            return;
        }

        DOTween.Kill(SHOW_ID);
        DOTween.Kill(HIDE_ID);

        _currentShow = true;
        _text.text = msg;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(_canvasGroup.DOFade(_showAlpha, _fadeTime));
        sequence.AppendInterval(_showTime);
        sequence.AppendCallback(Hide);
        sequence.SetId(SHOW_ID);
    }

    public void Show(string msg)
    {
        DOTween.Kill(SHOW_ID);
        DOTween.Kill(HIDE_ID);

        _currentShow = true;
        _text.text = msg;

        //if (isActiveAndEnabled)
        RebuildLayout(this.GetCancellationTokenOnDestroy()).Forget();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(_canvasGroup.DOFade(_showAlpha, _fadeTime));
        sequence.AppendInterval(_showTime);
        sequence.AppendCallback(Hide);
        sequence.SetId(SHOW_ID);
    }

    public void Hide()
    {
        DOTween.Kill(SHOW_ID);
        DOTween.Kill(HIDE_ID);

        _currentShow = false;
        _text.text = string.Empty;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(_canvasGroup.DOFade(_hideAlpha, _fadeTime));
        sequence.SetId(HIDE_ID);
    }

    private async UniTaskVoid RebuildLayout(System.Threading.CancellationToken ct)
    {
        await UniTask.WaitForEndOfFrame(this);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_text.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_text.transform.parent.GetComponent<RectTransform>());
    }
}