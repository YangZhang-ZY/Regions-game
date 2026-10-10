using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CardSwip : MonoBehaviour
{
    [Tooltip("左右倾斜的最大角度。转到这个角度后，同方向再移动鼠标，卡片不再移动，也不再旋转。")]
    public float maxRotation = 15f;

    [FormerlySerializedAs("flyThreshold")]
    [Tooltip("到达最大角度时，卡片离开中心的水平距离。位移和旋转在同一时刻到达上限。")]
    public float maxOffset = 220f;

    [Tooltip("卡片跟随鼠标移动和旋转的速度。数值越大跟得越快；只有很大时才会几乎立刻到位。")]
    public float followSpeed = 12f;

    [Tooltip("点击后卡片飞出屏幕的速度。数值越大飞得越快。")]
    public float flySpeed = 8000f;

    [Tooltip("向左或向右确认甩出时播放一次，只播放前 0.9 秒。不指定则不播放。")]
    public AudioClip swipeSound;

    [Tooltip("确认甩出音效音量。")]
    public float swipeSoundVolume = 1f;

    public Action<bool> OnSwiped;
    public Action OnTrackingStarted;

    public float DragAmount { get; private set; }
    public bool IsTracking => tracking;

    const float CenterDeadZone = 0.12f;
    const float SwipeSoundSeconds = 0.9f;

    RectTransform rect;
    Canvas canvas;
    Vector2 homePosition;
    Vector2 followVelocity;
    float followAngleVelocity;
    bool busy;
    bool tracking;
    AudioSource swipeSource;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        homePosition = rect.anchoredPosition;
    }

    void Update()
    {
        if (busy)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        if (!tracking)
        {
            if (mouse.leftButton.wasPressedThisFrame && ScreenPointInsideCard(screenPosition))
            {
                tracking = true;
                OnTrackingStarted?.Invoke();
            }

            return;
        }

        FollowMouse(screenPosition);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryCommit();
        }
    }

    public void StopTracking()
    {
        tracking = false;
    }

    bool ScreenPointInsideCard(Vector2 screenPosition)
    {
        if (rect == null)
        {
            return false;
        }

        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    void FollowMouse(Vector2 screenPosition)
    {
        if (!TryScreenToAnchored(screenPosition, out Vector2 anchored))
        {
            return;
        }

        float limit = OffsetLimit();
        float offset = Mathf.Clamp(anchored.x - homePosition.x, -limit, limit);
        float amount = offset / limit;
        DragAmount = amount;

        Vector2 targetPosition = new Vector2(homePosition.x + offset, homePosition.y);
        float targetAngle = -amount * maxRotation;
        float smoothTime = 1f / Mathf.Max(0.01f, followSpeed);

        rect.anchoredPosition = Vector2.SmoothDamp(
            rect.anchoredPosition,
            targetPosition,
            ref followVelocity,
            smoothTime);

        float currentAngle = rect.localEulerAngles.z;
        if (currentAngle > 180f)
        {
            currentAngle -= 360f;
        }

        float nextAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref followAngleVelocity, smoothTime);
        rect.localRotation = Quaternion.Euler(0f, 0f, nextAngle);
    }

    void TryCommit()
    {
        float offset = rect.anchoredPosition.x - homePosition.x;
        if (Mathf.Abs(offset) < OffsetLimit() * CenterDeadZone)
        {
            return;
        }

        PlaySwipeSound();
        StartCoroutine(Fly(offset > 0f));
    }

    void PlaySwipeSound()
    {
        if (swipeSound == null || swipeSoundVolume <= 0f)
        {
            return;
        }

        if (swipeSource == null)
        {
            swipeSource = gameObject.AddComponent<AudioSource>();
            swipeSource.playOnAwake = false;
            swipeSource.loop = false;
            swipeSource.spatialBlend = 0f;
        }

        swipeSource.Stop();
        swipeSource.clip = swipeSound;
        swipeSource.volume = swipeSoundVolume;
        swipeSource.time = 0f;
        swipeSource.Play();
        swipeSource.SetScheduledEndTime(AudioSettings.dspTime + SwipeSoundSeconds);
    }

    bool TryScreenToAnchored(Vector2 screenPosition, out Vector2 anchored)
    {
        anchored = default;

        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
        {
            return false;
        }

        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect parentRect = parent.rect;
        Vector2 anchor = (rect.anchorMin + rect.anchorMax) * 0.5f;
        Vector2 anchorLocal = new Vector2(
            Mathf.Lerp(parentRect.xMin, parentRect.xMax, anchor.x),
            Mathf.Lerp(parentRect.yMin, parentRect.yMax, anchor.y));

        anchored = localPoint - anchorLocal;
        return true;
    }

    float OffsetLimit()
    {
        return Mathf.Max(0.001f, Mathf.Abs(maxOffset));
    }

    IEnumerator Fly(bool toRight)
    {
        busy = true;
        DragAmount = toRight ? 1f : -1f;
        Vector2 from = rect.anchoredPosition;
        float targetX = toRight ? 1800f : -1800f;
        Vector2 to = new Vector2(targetX, from.y + 40f);
        float speed = Mathf.Max(1f, flySpeed);

        while (Vector2.Distance(rect.anchoredPosition, to) > 0.5f)
        {
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, to, speed * Time.deltaTime);
            yield return null;
        }

        rect.anchoredPosition = to;
        OnSwiped?.Invoke(toRight);
        busy = false;
    }

    public void SetHome(Vector2 position)
    {
        homePosition = position;
    }

    public void ResetToHome()
    {
        StopAllCoroutines();
        busy = false;
        DragAmount = 0f;
        followVelocity = Vector2.zero;
        followAngleVelocity = 0f;

        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = homePosition;
        rect.localRotation = Quaternion.identity;
    }
}
