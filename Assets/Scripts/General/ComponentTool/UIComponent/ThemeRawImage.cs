using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;

public enum ThemeType
{
    AppLoginBg,
    AppLoginLogo,
}

[RequireComponent(typeof(AspectRatioFitter))]
public class ThemeRawImage : MonoBehaviour
{
    private RawImage rawImage;
    private AspectRatioFitter aspectRatioFitter;

    [SerializeField]
    private ThemeType themeType;

    private bool updated = false;
    public bool Updated
    {
        get { return updated; }
    }

    public void UpdateElement()
    {
        updated = false;
        
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }
        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = GetComponent<AspectRatioFitter>();
        }
        rawImage.SetAlpha(0);

        var imgPath = string.Empty;
        switch (themeType) 
        {
            case ThemeType.AppLoginBg:
                imgPath = PlayerPrefs.GetString(GlobalInfo.appLoginBgCacheKey);
                break;
            case ThemeType.AppLoginLogo:
                imgPath = PlayerPrefs.GetString(GlobalInfo.appLoginLogoCacheKey);
                break;
        }        
        if (string.IsNullOrEmpty(imgPath))
        {
            updated = true;
            return;
        }

        ResManager.Instance.LoadCoverImage(themeType.ToString(), ResManager.Instance.OSSDownLoadPath + imgPath, false, (texture) =>
        {
            if (texture != null)
            {
                rawImage.texture = texture;
                switch (aspectRatioFitter.aspectMode)
                {
                    case AspectRatioFitter.AspectMode.None:
                        rawImage.GetComponent<RectTransform>().sizeDelta = new Vector2(texture.width, texture.height);
                        break;
                    case AspectRatioFitter.AspectMode.EnvelopeParent:
                        aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
                        break;
                }
                rawImage.SetAlpha(1);
            }
            updated = true;
        });
    }
}