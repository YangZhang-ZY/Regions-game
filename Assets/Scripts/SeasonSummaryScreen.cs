using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SeasonSummaryScreen : MonoBehaviour
{
    [Header("Pieces")]
    public CanvasGroup backgroundGroup;
    public CanvasGroup panelGroup;
    public CanvasGroup characterGroup;
    public RectTransform portrait;
    public ScrollRect summaryScroll;
    public RectTransform lineContent;
    public TMP_Text lineTemplate;
    public CanvasGroup buttonsGroup;
    public RectTransform buttonsRoot;
    public GameObject clickCatcher;

    [Header("Timing")]
    [Tooltip("How long the room and summary frame take to fade in.")]
    public float backgroundFadeDuration = 0.6f;

    [Tooltip("How long the owner portrait takes to fade in after the room. It stays in place.")]
    public float characterFadeDuration = 1.1f;

    [Tooltip("Seconds between the start of one summary line and the next.")]
    public float lineInterval = 0.28f;

    [Tooltip("How long each line takes to fade and scale from line start scale to full size.")]
    public float linePopDuration = 0.28f;

    [Tooltip("Scale of a line at the start of its pop.")]
    public float lineStartScale = 0.92f;

    [Tooltip("How long the Restart and Main Menu buttons take to fade and slide in.")]
    public float buttonsDuration = 0.35f;

    [Tooltip("How far below their resting spot the buttons start, in canvas pixels.")]
    public float buttonsSlideDistance = 28f;

    [Header("Text")]
    [Tooltip("Starting size for summary lines. Shrinks if a line would leave the panel.")]
    public float lineFontSize = 22f;

    [Tooltip("Smallest size summary lines may shrink to. Extra lines scroll inside the panel.")]
    public float lineFontMin = 16f;

    [Header("Scenes")]
    public string restartSceneName = "SampleScene";
    public string menuSceneName = "StartScene";

    Vector2 buttonsRest;
    bool waitingForClick;
    Image portraitImage;
    readonly List<RectTransform> lines = new List<RectTransform>();
    readonly List<CanvasGroup> lineGroups = new List<CanvasGroup>();

    void Awake()
    {
        if (portrait != null)
        {
            portraitImage = portrait.GetComponent<Image>();
            SetImageAlpha(portraitImage, 0f);
        }

        SetGroup(characterGroup, 0f, false);

        if (buttonsRoot != null)
        {
            buttonsRest = buttonsRoot.anchoredPosition;
            buttonsRoot.anchoredPosition = buttonsRest + new Vector2(0f, -Mathf.Abs(buttonsSlideDistance));
        }

        SetGroup(backgroundGroup, 0f, false);
        SetGroup(panelGroup, 0f, false);
        SetGroup(buttonsGroup, 0f, false);
        if (clickCatcher != null)
        {
            clickCatcher.SetActive(false);
        }

        if (lineTemplate != null)
        {
            lineTemplate.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        BuildLines();
        StartCoroutine(Play());
    }

    public void Continue()
    {
        if (!waitingForClick)
        {
            return;
        }

        waitingForClick = false;
        if (clickCatcher != null)
        {
            clickCatcher.SetActive(false);
        }
    }

    public void Restart()
    {
        SeasonRunSession.Begin();
        SceneManager.LoadScene(SceneName(restartSceneName, "SampleScene"));
    }

    public void MainMenu()
    {
        SeasonRunSession.Begin();
        SceneManager.LoadScene(SceneName(menuSceneName, "StartScene"));
    }

    IEnumerator Play()
    {
        yield return FadeRoom();
        yield return FadeCharacter();

        waitingForClick = true;
        if (clickCatcher != null)
        {
            clickCatcher.SetActive(true);
        }

        while (waitingForClick)
        {
            yield return null;
        }

        yield return PlayLines();
        yield return ShowButtons();
    }

    IEnumerator FadeRoom()
    {
        float duration = Mathf.Max(0.01f, backgroundFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetAlpha(backgroundGroup, t);
            SetAlpha(panelGroup, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(backgroundGroup, 1f);
        SetAlpha(panelGroup, 1f);
        if (panelGroup != null)
        {
            panelGroup.blocksRaycasts = true;
        }
    }

    IEnumerator FadeCharacter()
    {
        SetImageAlpha(portraitImage, 0f);
        SetAlpha(characterGroup, 1f);
        if (portraitImage == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, characterFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetImageAlpha(portraitImage, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetImageAlpha(portraitImage, 1f);
    }

    IEnumerator PlayLines()
    {
        int count = lines.Count;
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(PopLine(i));
            ScrollToLine(i);
            if (i < count - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, lineInterval));
            }
        }

        if (count > 0)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, linePopDuration));
        }
    }

    IEnumerator PopLine(int index)
    {
        CanvasGroup group = lineGroups[index];
        RectTransform rect = lines[index];
        float duration = Mathf.Max(0.01f, linePopDuration);
        float from = Mathf.Clamp(lineStartScale, 0.01f, 1f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (group != null)
            {
                group.alpha = t;
            }

            if (rect != null)
            {
                float scale = Mathf.Lerp(from, 1f, t);
                rect.localScale = new Vector3(scale, scale, 1f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
        }

        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }
    }

    IEnumerator ShowButtons()
    {
        Vector2 from = buttonsRest + new Vector2(0f, -Mathf.Abs(buttonsSlideDistance));
        if (buttonsRoot != null)
        {
            buttonsRoot.anchoredPosition = from;
        }

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }

        float duration = Mathf.Max(0.01f, buttonsDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetAlpha(buttonsGroup, t);
            if (buttonsRoot != null)
            {
                buttonsRoot.anchoredPosition = Vector2.Lerp(from, buttonsRest, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(buttonsGroup, 1f);
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }

        if (buttonsRoot != null)
        {
            buttonsRoot.anchoredPosition = buttonsRest;
        }
    }

    void BuildLines()
    {
        lines.Clear();
        lineGroups.Clear();
        if (lineTemplate == null || lineContent == null)
        {
            return;
        }

        List<string> texts = LineTexts();
        LayoutElement templateLayout = lineTemplate.GetComponent<LayoutElement>();
        if (templateLayout == null)
        {
            templateLayout = lineTemplate.gameObject.AddComponent<LayoutElement>();
        }

        templateLayout.ignoreLayout = true;
        FitFont(texts);
        for (int i = 0; i < texts.Count; i++)
        {
            GameObject clone = Instantiate(lineTemplate.gameObject, lineContent);
            clone.name = "SummaryLine";
            clone.SetActive(true);

            LayoutElement layout = clone.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.ignoreLayout = false;
            }

            TMP_Text text = clone.GetComponent<TMP_Text>();
            if (text != null)
            {
                bool prose = i >= 4;
                text.text = texts[i];
                text.fontSize = lineTemplate.fontSize;
                text.raycastTarget = false;
                text.textWrappingMode = prose ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                if (prose && layout != null)
                {
                    Vector2 preferred = text.GetPreferredValues(texts[i], TextWidth(), 0f);
                    if (preferred.y > 1f)
                    {
                        layout.preferredHeight = preferred.y;
                    }
                }
            }

            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.localScale = Vector3.one * Mathf.Clamp(lineStartScale, 0.01f, 1f);

            CanvasGroup group = clone.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = clone.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            lines.Add(rect);
            lineGroups.Add(group);
        }

        if (templateLayout != null)
        {
            templateLayout.ignoreLayout = true;
        }

        lineTemplate.gameObject.SetActive(false);
        if (summaryScroll != null)
        {
            summaryScroll.verticalNormalizedPosition = 1f;
        }
    }

    List<string> LineTexts()
    {
        List<string> texts = new List<string>();
        if (!SeasonRunSession.HasRun)
        {
            texts.Add("Performance did not change.");
            texts.Add("Morale did not change.");
            texts.Add("Fans did not change.");
            texts.Add("The club's income did not change.");
            texts.Add("No season was recorded.");
            return texts;
        }

        texts.Add(StatSentence("Performance", SeasonRunSession.Performance - SeasonRunSession.StartPerformance));
        texts.Add(StatSentence("Morale", SeasonRunSession.Morale - SeasonRunSession.StartMorale));
        texts.Add(StatSentence("Fans", SeasonRunSession.Fans - SeasonRunSession.StartFans));
        texts.Add(IncomeSentence(SeasonRunSession.Wealth - SeasonRunSession.StartWealth));
        AddSentences(texts, SeasonRunSession.Outcome);
        return texts;
    }

    static void AddSentences(List<string> texts, string paragraph)
    {
        if (string.IsNullOrWhiteSpace(paragraph))
        {
            return;
        }

        string[] sentences = paragraph.Split(new[] { ". " }, System.StringSplitOptions.None);
        for (int i = 0; i < sentences.Length; i++)
        {
            string sentence = sentences[i].Trim();
            if (sentence.Length == 0)
            {
                continue;
            }

            if (!sentence.EndsWith("."))
            {
                sentence += ".";
            }

            texts.Add(sentence);
        }
    }

    static string StatSentence(string name, int delta)
    {
        if (delta > 0)
        {
            return name + " rose by " + delta.ToString(CultureInfo.InvariantCulture) + ".";
        }

        if (delta < 0)
        {
            return name + " fell by " + (-delta).ToString(CultureInfo.InvariantCulture) + ".";
        }

        return name + " did not change.";
    }

    static string IncomeSentence(long delta)
    {
        if (delta > 0)
        {
            return "The club's income rose by " + TeamStatsDisplay.FormatWealth(delta) + ".";
        }

        if (delta < 0)
        {
            return "The club's income fell by " + TeamStatsDisplay.FormatWealth(-delta) + ".";
        }

        return "The club's income did not change.";
    }

    void FitFont(List<string> texts)
    {
        float size = Mathf.Max(1f, lineFontSize);
        float min = Mathf.Clamp(lineFontMin, 1f, size);
        float width = TextWidth();
        lineTemplate.gameObject.SetActive(true);
        lineTemplate.enableAutoSizing = false;
        lineTemplate.textWrappingMode = TextWrappingModes.NoWrap;
        lineTemplate.overflowMode = TextOverflowModes.Overflow;

        int limited = texts.Count < 4 ? texts.Count : 4;
        while (size > min)
        {
            lineTemplate.fontSize = size;
            bool fits = true;
            for (int i = 0; i < limited; i++)
            {
                Vector2 preferred = lineTemplate.GetPreferredValues(texts[i]);
                if (preferred.x > width)
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
            {
                break;
            }

            size -= 1f;
        }

        lineTemplate.fontSize = Mathf.Max(min, size);
        lineTemplate.gameObject.SetActive(false);
    }

    float TextWidth()
    {
        if (summaryScroll != null && summaryScroll.viewport != null)
        {
            float width = summaryScroll.viewport.rect.width;
            if (width > 1f)
            {
                return width - 8f;
            }
        }

        return 560f;
    }

    void ScrollToLine(int index)
    {
        if (summaryScroll == null || lineContent == null || index < 0 || index >= lines.Count)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(lineContent);
        RectTransform viewport = summaryScroll.viewport;
        if (viewport == null)
        {
            summaryScroll.verticalNormalizedPosition = 1f;
            return;
        }

        float view = viewport.rect.height;
        float content = lineContent.rect.height;
        if (content <= view + 1f)
        {
            summaryScroll.verticalNormalizedPosition = 1f;
            return;
        }

        RectTransform line = lines[index];
        float lineBottom = -line.anchoredPosition.y + line.rect.height;
        float hidden = content - view;
        float target = Mathf.Clamp(lineBottom - view, 0f, hidden);
        summaryScroll.verticalNormalizedPosition = 1f - (target / hidden);
    }

    static string SceneName(string requested, string fallback)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return fallback;
        }

        return requested;
    }

    static void SetGroup(CanvasGroup group, float alpha, bool open)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = open;
        group.blocksRaycasts = open;
    }

    static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
    }

    static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
