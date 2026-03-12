using Vuplex.WebView;
using Vuplex.WebView.Demos;
using UnityFramework.Runtime;

public class WebBaikeData : UIData
{
    public string url;
    public WebBaikeData(string url)
    {
        this.url = url;
    }
}

/// <summary>
/// 网页百科模块
/// </summary>
public class WebModule : UIModuleBase
{
    private CanvasWebViewPrefab canvasWebViewPrefab;
    private HardwareKeyboardListener _hardwareKeyboardListener;

    private string url;

    public override void Open(UIData uiData = null)
    {
        base.Open();
        if (uiData != null)
        {
            WebBaikeData data = uiData as WebBaikeData;
            url = data.url;
            WebViewLoad();
        }
    }

    //IEnumerator WebViewLoad()
    async void WebViewLoad()
    {
        if (canvasWebViewPrefab == null)
            canvasWebViewPrefab = GetComponentInChildren<CanvasWebViewPrefab>(true);

        _setUpHardwareKeyboard();

        await canvasWebViewPrefab.WaitUntilInitialized();

        canvasWebViewPrefab.DragMode = DragMode.DragWithinPage;
        canvasWebViewPrefab.WebView.LoadUrl(url);
        //canvasWebViewPrefab.WebView.SetResolution(2);//控制缩放比例

        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
        canvasWebViewPrefab.WebView.LoadProgressChanged += (object sender, ProgressChangedEventArgs e) =>
        { 
            UIManager.Instance.CloseUI<LoadingPanel>(); 
        };
    }

    void _setUpHardwareKeyboard()
    {
        // Send keys from the hardware (USB or Bluetooth) keyboard to the webview.
        // Use separate KeyDown() and KeyUp() methods if the webview supports
        // it, otherwise just use IWebView.SendKey().
        // https://developer.vuplex.com/webview/IWithKeyDownAndUp
        _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
        _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
            var webViewWithKeyDown = canvasWebViewPrefab.WebView as IWithKeyDownAndUp;
            if (webViewWithKeyDown != null)
            {
                webViewWithKeyDown.KeyDown(eventArgs.Value, eventArgs.Modifiers);
            }
            else
            {
                canvasWebViewPrefab.WebView.SendKey(eventArgs.Value);
            }
        };
        _hardwareKeyboardListener.KeyUpReceived += (sender, eventArgs) => {
            var webViewWithKeyUp = canvasWebViewPrefab.WebView as IWithKeyDownAndUp;
            webViewWithKeyUp?.KeyUp(eventArgs.Value, eventArgs.Modifiers);
        };
    }
}
