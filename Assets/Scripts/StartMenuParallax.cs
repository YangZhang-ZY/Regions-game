using UnityEngine;
using UnityEngine.InputSystem;

public class StartMenuParallax : MonoBehaviour
{
    [Tooltip("背景。鼠标移动时它比按钮偏得更多，像镜头轻轻跟着指针。")]
    public RectTransform background;

    [Tooltip("前景按钮。偏移比背景小，方便点中。")]
    public RectTransform[] foreground;

    [Tooltip("背景离开记住的位置的最大距离，单位是界面像素。")]
    public float backgroundOffset = 22f;

    [Tooltip("前景按钮离开记住的位置的最大距离，单位是界面像素。")]
    public float foregroundOffset = 8f;

    [Tooltip("跟上鼠标的速度。数值越大跟得越快。")]
    public float smoothSpeed = 8f;

    Vector2 backgroundHome;
    Vector2 backgroundVelocity;
    bool backgroundReady;
    Vector2[] foregroundHomes;
    Vector2[] foregroundVelocities;

    void OnEnable()
    {
        backgroundVelocity = Vector2.zero;
        backgroundReady = false;

        if (background != null)
        {
            backgroundHome = background.anchoredPosition;
            backgroundReady = true;
        }

        if (foreground == null)
        {
            foregroundHomes = null;
            foregroundVelocities = null;
            return;
        }

        foregroundHomes = new Vector2[foreground.Length];
        foregroundVelocities = new Vector2[foreground.Length];
        for (int i = 0; i < foreground.Length; i++)
        {
            if (foreground[i] == null)
            {
                continue;
            }

            foregroundHomes[i] = foreground[i].anchoredPosition;
        }
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float width = Screen.width;
        float height = Screen.height;
        if (width <= 1f || height <= 1f)
        {
            return;
        }

        Vector2 screen = mouse.position.ReadValue();
        float nx = Mathf.Clamp((screen.x / width - 0.5f) * 2f, -1f, 1f);
        float ny = Mathf.Clamp((screen.y / height - 0.5f) * 2f, -1f, 1f);
        Vector2 look = new Vector2(-nx, -ny);
        float smoothTime = 1f / Mathf.Max(0.01f, smoothSpeed);

        if (background != null && backgroundReady)
        {
            Vector2 target = backgroundHome + look * backgroundOffset;
            background.anchoredPosition = Vector2.SmoothDamp(
                background.anchoredPosition,
                target,
                ref backgroundVelocity,
                smoothTime);
        }

        if (foreground == null || foregroundHomes == null || foregroundVelocities == null)
        {
            return;
        }

        int count = foreground.Length;
        if (foregroundHomes.Length < count)
        {
            count = foregroundHomes.Length;
        }

        if (foregroundVelocities.Length < count)
        {
            count = foregroundVelocities.Length;
        }

        for (int i = 0; i < count; i++)
        {
            if (foreground[i] == null)
            {
                continue;
            }

            Vector2 target = foregroundHomes[i] + look * foregroundOffset;
            foreground[i].anchoredPosition = Vector2.SmoothDamp(
                foreground[i].anchoredPosition,
                target,
                ref foregroundVelocities[i],
                smoothTime);
        }
    }
}
