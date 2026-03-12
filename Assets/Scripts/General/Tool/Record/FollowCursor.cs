using UnityEngine;
using UnityEngine.UI;

public class FollowCursor : MonoBehaviour
{
    //[SerializeField] Camera camera;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        rectTransform.anchoredPosition = Input.mousePosition - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f);
    }
}