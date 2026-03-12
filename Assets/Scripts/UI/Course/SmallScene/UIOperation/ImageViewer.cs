using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 图片缩放、拖拽
/// </summary>
public class ImageViewer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _image = null;

    [SerializeField] private ImageViewerIndicator _indicator = null;

    [Header("拖拽")]
    [SerializeField] private float _speedD = 25;
    [SerializeField] protected float _dumpingD = 3f;

    [Header("缩放")]
    [SerializeField] private float _speedZ = 25f;
    [SerializeField] private float _dumpingZ = 4f;
    [SerializeField] private float _minScale = 0f;
    [SerializeField] private float _maxScale = 5f;

    [SerializeField] private bool _rescaleOnDisable;

    private RectTransform _transform;
    private RectTransform RectTransform => _transform == null ? _transform = GetComponent<RectTransform>() : _transform;

    private RectTransform _imageTransform;
    private RectTransform ImageTransform => _imageTransform == null ? _imageTransform = _image.GetComponent<RectTransform>() : _imageTransform;

    private Vector2? _defaultImageSize;

    private Vector2 _deltaPosition = Vector2.zero;
    private float _delta;
    private float _scale;

    private Vector2 oldPos1;
    private Vector2 oldPos2;
    private bool isTouchBegin;

    private bool isEnter;

    private Vector2 ViewerSize
    {
        get
        {
            var rect = _transform.rect;
            return new Vector2(rect.width, rect.height);
        }
    }

    private Vector2 ImageSize
    {
        get
        {
            var rect = _imageTransform.rect;
            return new Vector2(rect.width, rect.height);
        }
    }

    private void Awake()
    {
        _transform = GetComponent<RectTransform>();
        _imageTransform = _image.GetComponent<RectTransform>();
        if (_indicator)
        {
            _indicator.gameObject.SetActive(true);
            _indicator.SetScaleRange(_minScale, _maxScale);
            _indicator.Hide();
        }
    }

    private void OnEnable()
    {
        if (_indicator)
        {
            _indicator.Show("左下角按钮控制放大/缩小");
        }
    }

    private void OnDisable()
    {
        if (_indicator)
        {
            _indicator.Hide();
        }

        if (_rescaleOnDisable)
        {
            RescalePhoto(_image.sprite);
            _deltaPosition = Vector2.zero;
            _delta = 0f;
            _scale = 0f;
        }
    }


    private void Update()
    {
        _deltaPosition = Vector2.Lerp(_deltaPosition, Vector2.zero, Time.deltaTime * _dumpingD);
        if (_deltaPosition.magnitude < 0.1f)
            _deltaPosition = Vector2.zero;
        ApplyInput(_deltaPosition);

#if UNITY_ANDROID && !UNITY_EDITOR
        var delta = 0f;
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                oldPos1 = touch1.position; oldPos2 = touch2.position;
                isTouchBegin = true;
                return;
            }

            if (isTouchBegin && touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                if (Vector3.Dot(touch1.position - oldPos1, touch2.position - oldPos2) < 0)
                {
                    float oldDistance = Vector2.Distance(oldPos1, oldPos2);
                    float newDistance = Vector2.Distance(touch1.position, touch2.position);
                    //距离差，为正表示放大手势， 为负表示缩小手势  
                    delta = (newDistance - oldDistance) * 0.004f;
                }
                oldPos1 = touch1.position;
                oldPos2 = touch2.position;
            }
        }
#else
        var delta = 0f;
        if(isEnter)
            delta = Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * _speedZ;
#endif

        _delta = Mathf.Lerp(_delta, delta, Time.deltaTime * _dumpingZ);
        _scale = Mathf.Clamp(_scale + _delta, _minScale, _maxScale);
        Zoom(_scale);

        if (_indicator && Mathf.Abs(_delta) >= _indicator.DeltaThreshold)
        {
            _indicator.Show(_scale);
        }
    }

    private void Zoom(float value)
    {
        if (_defaultImageSize == null)
            return;

        _imageTransform.sizeDelta = (Vector2)(_defaultImageSize + _defaultImageSize * value);
    }


    public void ApplyInput(Vector2 deltaPosition)
    {
        Vector2 newPosition = _imageTransform.localPosition;

        if (ImageSize.x > ViewerSize.x)
        {
            newPosition.x += deltaPosition.x;

            if ((newPosition.x - ImageSize.x / 2) > -ViewerSize.x / 2)
                newPosition.x = -ViewerSize.x / 2 + ImageSize.x / 2;

            if ((newPosition.x + ImageSize.x / 2) < ViewerSize.x / 2)
                newPosition.x = ViewerSize.x / 2 - ImageSize.x / 2;
        }
        else
            newPosition.x = 0;

        if (ImageSize.y > ViewerSize.y)
        {
            newPosition.y += deltaPosition.y;

            if ((newPosition.y - ImageSize.y / 2) > -ViewerSize.y / 2)
                newPosition.y = -ViewerSize.y / 2 + ImageSize.y / 2;

            if ((newPosition.y + ImageSize.y / 2) < ViewerSize.y / 2)
                newPosition.y = ViewerSize.y / 2 - ImageSize.y / 2;
        }
        else
            newPosition.y = 0;

        _imageTransform.localPosition = (Vector3)newPosition;
    }

    private void RescalePhoto(Sprite sprite)
    {
        var viewerSize = ViewerSize;
        var spriteSize = new Vector2(sprite.rect.width, sprite.rect.height);

        var viewerAspect = viewerSize.x / viewerSize.y;
        var spriteAspect = spriteSize.x / spriteSize.y;

        if (spriteAspect > viewerAspect)
        {
            var relation = viewerSize.x / sprite.texture.width;
            _imageTransform.sizeDelta = new Vector2(viewerSize.x, relation * spriteSize.y);
        }
        else
        {
            var relate = viewerSize.y / sprite.texture.height;
            _imageTransform.sizeDelta = new Vector2(relate * spriteSize.x, viewerSize.y);
        }

        _defaultImageSize = _imageTransform.sizeDelta;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            _deltaPosition = Vector2.zero;
        else
            _deltaPosition = eventData.delta * Time.deltaTime * _speedD;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void RecoverScale()
    {
        _imageTransform.sizeDelta = new Vector2(_image.sprite.texture.width / _image.sprite.texture.height * ViewerSize.y, ViewerSize.y);
        RescalePhoto(_image.sprite);

        Zoom(_scale);
        //if (_indicator)
        //{
        //    _indicator.Show(_scale);
        //}
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isEnter = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isEnter = false;
    }

    /// <summary>
    /// 安卓版放大按钮调用
    /// </summary>
    /// <param name="delta">缩放增量，默认0.5</param>
    public void ZoomIn(float delta = 0.5f)
    {
        _scale = Mathf.Clamp(_scale + delta, _minScale, _maxScale);
        Zoom(_scale);
        if (_indicator)
            _indicator.Show(_scale);
    }

    /// <summary>
    /// 安卓版缩小按钮调用
    /// </summary>
    /// <param name="delta">缩放增量，默认0.5</param>
    public void ZoomOut(float delta = 0.5f)
    {
        _scale = Mathf.Clamp(_scale - delta, _minScale, _maxScale);
        Zoom(_scale);
        if (_indicator)
            _indicator.Show(_scale);
    }

    /// <summary>
    /// 重置缩放和位置到初始状态
    /// </summary>
    public void ResetScaleAndPosition()
    {
        _scale = 0f;
        _delta = 0f;
        _deltaPosition = Vector2.zero;

        if (_image != null && _image.sprite != null)
        {
            _imageTransform.sizeDelta = new Vector2(_image.sprite.texture.width / _image.sprite.texture.height * ViewerSize.y, ViewerSize.y);
            RescalePhoto(_image.sprite);
        }
        _imageTransform.localPosition = Vector3.zero;
        Zoom(0f);
    }

    /// <summary>
    /// 设置图片
    /// </summary>
    /// <param name="sprite">新的图片</param>
    public void SetImage(Sprite sprite)
    {
        if (sprite == null) return;

        _image.sprite = sprite;
        _image.preserveAspect = true;

        // 重新计算尺寸
        _imageTransform.sizeDelta = new Vector2(sprite.texture.width / sprite.texture.height * ViewerSize.y, ViewerSize.y);
        RescalePhoto(sprite);

        // 重置缩放和位置
        ResetScaleAndPosition();
    }
}