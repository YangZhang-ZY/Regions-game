using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("指针停在按钮上时的缩放。1 是原来的大小。")]
    public float hoverScale = 1.08f;

    [Tooltip("放大和文字变色所用的秒数。")]
    public float duration = 0.12f;

    [Tooltip("指针进入按钮时播放一次。不指定则不播放。")]
    public AudioClip hoverSound;

    [Tooltip("悬停音效音量。")]
    public float hoverSoundVolume = 1f;

    [Tooltip("点击按钮时播放一次。不指定则不播放。")]
    public AudioClip clickSound;

    [Tooltip("点击音效音量。")]
    public float clickSoundVolume = 1f;

    static readonly Color HoverGold = new Color(226f / 255f, 197f / 255f, 107f / 255f, 1f);

    Vector3 restScale;
    Color restColor;
    TMP_Text label;
    float blend;
    bool hovering;
    AudioSource uiSource;

    void OnEnable()
    {
        restScale = transform.localScale;
        blend = 0f;
        hovering = false;
        label = GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            restColor = label.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        PlayUiClip(hoverSound, hoverSoundVolume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        PlayUiClip(clickSound, clickSoundVolume);
    }

    void PlayUiClip(AudioClip clip, float volume)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
        }

        uiSource.PlayOneShot(clip, volume);
    }

    void Update()
    {
        float target = hovering ? 1f : 0f;
        if (duration <= 0.0001f)
        {
            blend = target;
        }
        else
        {
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime / duration);
        }

        transform.localScale = Vector3.LerpUnclamped(restScale, restScale * hoverScale, blend);

        if (label == null)
        {
            return;
        }

        Color hoverColor = HoverGold;
        hoverColor.a = restColor.a;
        label.color = Color.LerpUnclamped(restColor, hoverColor, blend);
    }
}
