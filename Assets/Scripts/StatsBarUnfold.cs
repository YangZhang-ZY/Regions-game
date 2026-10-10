using System.Collections;
using UnityEngine;

public class StatsBarUnfold : MonoBehaviour
{
    [Tooltip("拖入顶栏 StatsBar。")]
    public RectTransform bar;

    [Tooltip("展开所用秒数。")]
    public float duration = 0.5f;

    public AnimationCurve scaleY = DefaultScaleY();

    Vector3 rememberedScale;
    bool prepared;
    Coroutine unfoldRoutine;

    static AnimationCurve DefaultScaleY()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.72f, 1.08f),
            new Keyframe(1f, 1f));
    }

    void Reset()
    {
        scaleY = DefaultScaleY();
    }

    void OnEnable()
    {
        PrepareCollapsed();
    }

    public void BeginUnfold()
    {
        if (bar == null)
        {
            return;
        }

        PrepareCollapsed();
        if (unfoldRoutine != null)
        {
            StopCoroutine(unfoldRoutine);
            unfoldRoutine = null;
        }

        unfoldRoutine = StartCoroutine(Unfold());
    }

    public void Collapse()
    {
        if (bar == null)
        {
            return;
        }

        PrepareCollapsed();
        if (unfoldRoutine != null)
        {
            StopCoroutine(unfoldRoutine);
            unfoldRoutine = null;
        }

        Vector3 scale = rememberedScale;
        scale.y = 0f;
        bar.localScale = scale;
    }

    void PrepareCollapsed()
    {
        if (prepared || bar == null)
        {
            return;
        }

        rememberedScale = bar.localScale;

        Vector2 pivot = bar.pivot;
        float previousPivotY = pivot.y;
        float deltaPivotY = 1f - previousPivotY;
        pivot.y = 1f;
        bar.pivot = pivot;
        bar.anchoredPosition += new Vector2(0f, deltaPivotY * bar.rect.height * rememberedScale.y);
        Vector3 scale = rememberedScale;
        scale.y = 0f;
        bar.localScale = scale;
        prepared = true;
    }

    IEnumerator Unfold()
    {
        if (duration <= 0f)
        {
            ApplyScaleY(scaleY.Evaluate(1f));
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyScaleY(scaleY.Evaluate(t));
            yield return null;
        }

        ApplyScaleY(scaleY.Evaluate(1f));
    }

    void ApplyScaleY(float curveValue)
    {
        if (bar == null)
        {
            return;
        }

        bar.localScale = new Vector3(
            rememberedScale.x,
            rememberedScale.y * curveValue,
            rememberedScale.z);
    }
}
