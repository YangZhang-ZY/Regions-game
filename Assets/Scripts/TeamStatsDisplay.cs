using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class TeamStatsDisplay : MonoBehaviour
{
    [Tooltip("球队实力的起始数值。")]
    public int performance = 50;

    [Tooltip("球员士气的起始数值。")]
    public int morale = 50;

    [Tooltip("粉丝影响力的起始数值。")]
    public int fans = 50;

    [Tooltip("俱乐部财富，单位是美元。2400000 会显示成 $2.4M。")]
    public long wealth = 2400000;

    public TextMeshProUGUI performanceText;
    public TextMeshProUGUI moraleText;
    public TextMeshProUGUI fansText;
    public TextMeshProUGUI wealthText;

    [Tooltip("预览里，增加的变化用这个颜色。")]
    public Color plusColor = new Color32(0xE2, 0x3B, 0x3B, 0xFF);

    [Tooltip("预览里，减少的变化用这个颜色。")]
    public Color minusColor = new Color32(0x3D, 0xAB, 0x5A, 0xFF);

    [Tooltip("数值真正变化后，数字放大再收回所用的秒数。")]
    public float flashDuration = 0.28f;

    [Tooltip("闪动最大时，数字相对原来大小的倍数。")]
    public float flashScale = 1.18f;

    int startPerformance;
    int startMorale;
    int startFans;
    long startWealth;

    public int StartingPerformance
    {
        get { return startPerformance; }
    }

    public int StartingMorale
    {
        get { return startMorale; }
    }

    public int StartingFans
    {
        get { return startFans; }
    }

    public long StartingWealth
    {
        get { return startWealth; }
    }

    FlashingLabel performanceLabel;
    FlashingLabel moraleLabel;
    FlashingLabel fansLabel;
    FlashingLabel wealthLabel;

    void Awake()
    {
        RememberStart();
        EnableStatRichText();
        performanceLabel = CreateLabel(performanceText);
        moraleLabel = CreateLabel(moraleText);
        fansLabel = CreateLabel(fansText);
        wealthLabel = CreateLabel(wealthText);
        SetValueAlpha(0f);
    }

    void Start()
    {
        Refresh();
    }

    public void RememberStart()
    {
        startPerformance = performance;
        startMorale = morale;
        startFans = fans;
        startWealth = wealth;
    }

    public void RestoreStart()
    {
        StopFlash();
        performance = startPerformance;
        morale = startMorale;
        fans = startFans;
        wealth = startWealth;
    }

    public void Apply(StatChange change)
    {
        performance += change.performance;
        morale += change.morale;
        fans += change.fans;
        wealth += change.wealth;
        Refresh();
        FlashIfChanged(performanceLabel, change.performance);
        FlashIfChanged(moraleLabel, change.morale);
        FlashIfChanged(fansLabel, change.fans);
        FlashIfChanged(wealthLabel, change.wealth);
    }

    public void SetValueAlpha(float alpha)
    {
        SetTextAlpha(performanceText, alpha);
        SetTextAlpha(moraleText, alpha);
        SetTextAlpha(fansText, alpha);
        SetTextAlpha(wealthText, alpha);
    }

    public void StopFlash()
    {
        StopOne(performanceLabel);
        StopOne(moraleLabel);
        StopOne(fansLabel);
        StopOne(wealthLabel);
    }

    public void Refresh()
    {
        if (performanceText != null)
        {
            performanceText.text = performance.ToString();
        }

        if (moraleText != null)
        {
            moraleText.text = morale.ToString();
        }

        if (fansText != null)
        {
            fansText.text = fans.ToString();
        }

        if (wealthText != null)
        {
            wealthText.text = FormatWealth(wealth);
        }
    }

    public void Preview(StatChange change)
    {
        EnableStatRichText();

        if (performanceText != null)
        {
            performanceText.text = FormatStatPreview(performance, change.performance);
        }

        if (moraleText != null)
        {
            moraleText.text = FormatStatPreview(morale, change.morale);
        }

        if (fansText != null)
        {
            fansText.text = FormatStatPreview(fans, change.fans);
        }

        if (wealthText != null)
        {
            wealthText.text = FormatWealthPreview(wealth, change.wealth);
        }
    }

    void EnableStatRichText()
    {
        EnableRichText(performanceText);
        EnableRichText(moraleText);
        EnableRichText(fansText);
        EnableRichText(wealthText);
    }

    static void EnableRichText(TMP_Text text)
    {
        if (text != null)
        {
            text.richText = true;
        }
    }

    string FormatStatPreview(int current, int delta)
    {
        if (delta == 0)
        {
            return current.ToString();
        }

        string signed = delta > 0
            ? "+" + delta.ToString(CultureInfo.InvariantCulture)
            : delta.ToString(CultureInfo.InvariantCulture);
        return current.ToString() + " " + ColorDelta(signed, delta);
    }

    string FormatWealthPreview(long current, long delta)
    {
        if (delta == 0)
        {
            return FormatWealth(current);
        }

        return FormatWealth(current) + " " + ColorDelta(FormatSignedWealth(delta), delta);
    }

    static string FormatSignedWealth(long dollars)
    {
        if (dollars > 0)
        {
            return "+" + FormatWealth(dollars);
        }

        return FormatWealth(dollars);
    }

    string ColorDelta(string signedDelta, long delta)
    {
        Color color = delta > 0 ? plusColor : minusColor;
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + signedDelta + "</color>";
    }

    public static string FormatWealth(long dollars)
    {
        long abs = dollars < 0 ? -dollars : dollars;
        string sign = dollars < 0 ? "-" : "";

        if (abs >= 1_000_000)
        {
            string millions = (abs / 1_000_000d).ToString("0.0", CultureInfo.InvariantCulture);
            return sign + "$" + millions + "M";
        }

        if (abs >= 1_000)
        {
            string thousands = (abs / 1_000d).ToString("0", CultureInfo.InvariantCulture);
            return sign + "$" + thousands + "K";
        }

        return sign + "$" + abs.ToString(CultureInfo.InvariantCulture);
    }

    void OnDisable()
    {
        StopFlash();
    }

    static FlashingLabel CreateLabel(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return null;
        }

        Color color = text.color;
        color.a = 1f;
        return new FlashingLabel
        {
            text = text,
            restScale = text.rectTransform.localScale,
            restColor = color
        };
    }

    static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.alpha = alpha;
    }

    void FlashIfChanged(FlashingLabel label, long delta)
    {
        if (delta == 0)
        {
            return;
        }

        Flash(label);
    }

    void Flash(FlashingLabel label)
    {
        if (label == null || label.text == null)
        {
            return;
        }

        if (label.routine != null)
        {
            StopCoroutine(label.routine);
            label.routine = null;
        }

        RestoreLabel(label);
        label.routine = StartCoroutine(FlashRoutine(label));
    }

    IEnumerator FlashRoutine(FlashingLabel label)
    {
        float duration = Mathf.Max(0.01f, flashDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            ApplyPunch(label, punch);
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreLabel(label);
        label.routine = null;
    }

    void ApplyPunch(FlashingLabel label, float punch)
    {
        TextMeshProUGUI text = label.text;
        if (text == null)
        {
            return;
        }

        float scale = Mathf.Lerp(1f, flashScale, punch);
        text.rectTransform.localScale = label.restScale * scale;
        Color color = Color.Lerp(label.restColor, Color.white, punch);
        color.a = text.alpha;
        text.color = color;
    }

    void StopOne(FlashingLabel label)
    {
        if (label == null)
        {
            return;
        }

        if (label.routine != null)
        {
            StopCoroutine(label.routine);
            label.routine = null;
        }

        RestoreLabel(label);
    }

    static void RestoreLabel(FlashingLabel label)
    {
        if (label == null || label.text == null)
        {
            return;
        }

        label.text.rectTransform.localScale = label.restScale;
        Color color = label.restColor;
        color.a = label.text.alpha;
        label.text.color = color;
    }

    sealed class FlashingLabel
    {
        public TextMeshProUGUI text;
        public Vector3 restScale;
        public Color restColor;
        public Coroutine routine;
    }
}
