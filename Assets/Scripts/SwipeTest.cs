using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SwipeTest : MonoBehaviour
{
    public CardSwip card;
    public SeasonDeck deck;
    public TeamStatsDisplay stats;
    public TMP_Text questionText;
    public TMP_Text leftText;
    public TMP_Text rightText;
    public TMP_Text roundText;
    public GameObject endPanel;
    public TMP_Text endTitle;
    public TMP_Text endBody;

    [Header("Season summary thresholds")]
    [Tooltip("Season-summary threshold. Change this in the Inspector for testing.")]
    public int dominantPerformance = 70;

    [Tooltip("Season-summary threshold. Change this in the Inspector for testing.")]
    public int dominantMorale = 70;

    [Tooltip("Season-summary threshold. Change this in the Inspector for testing.")]
    public int dominantFans = 70;

    [Tooltip("Season-summary threshold. Change this in the Inspector for testing.")]
    public long dominantWealth = 1500000;

    const float ChoiceFadeStart = 0.25f;

    [Tooltip("每张牌从左边滑到中间所用的秒数。越小越快。")]
    public float dealDuration = 0.28f;

    [Tooltip("牌从终点左侧多远的地方开始飞入。越大，起点越靠左。")]
    public float dealOffscreenDistance = 1400f;

    [Tooltip("飞入方向，单位是度。0 从右边来，90 从上方来，180 从左边来，135 从左上角来。牌从该方向飞向中心。")]
    public float dealAngle = 180f;

    int index;
    int roundCount = 1;
    bool runOver;
    Coroutine dealRoutine;
    readonly List<GameObject> dealtBacks = new List<GameObject>();
    GameObject staticCardBack;

    void Start()
    {
        if (stats == null)
        {
            stats = GetComponent<TeamStatsDisplay>();
        }

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        if (card != null)
        {
            card.OnSwiped += HandleSwiped;
        }

        runOver = false;
        PlayDeal();
    }

    void Update()
    {
        if (dealRoutine != null)
        {
            return;
        }

        ApplyChoiceFade();
    }

    void OnDestroy()
    {
        if (card != null)
        {
            card.OnSwiped -= HandleSwiped;
        }

        ClearDealtBacks();
    }

    void HandleSwiped(bool choseRight)
    {
        if (runOver)
        {
            return;
        }

        ApplyCurrentEffect(choseRight);

        if (IsRunFailed())
        {
            ShowEnd(false);
            return;
        }

        index++;

        if (deck != null && deck.cards != null && index >= deck.cards.Length)
        {
            ShowEnd(true);
            return;
        }

        roundCount++;

        if (card != null)
        {
            card.ResetToHome();
        }

        Show();
    }

    public void Restart()
    {
        if (stats != null)
        {
            stats.RestoreStart();
            stats.Refresh();
        }

        index = 0;
        roundCount = 1;
        runOver = false;

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        PlayDeal();
    }

    void PlayDeal()
    {
        if (dealRoutine != null)
        {
            StopCoroutine(dealRoutine);
            dealRoutine = null;
        }

        ClearDealtBacks();
        HideStaticCardBack();
        HidePrompt();

        if (card != null)
        {
            card.enabled = false;
            card.ResetToHome();
        }

        if (card == null || DeckCount() <= 0)
        {
            if (card != null && (endPanel == null || !endPanel.activeSelf))
            {
                card.enabled = true;
            }

            Show();
            return;
        }

        dealRoutine = StartCoroutine(Deal());
    }

    IEnumerator Deal()
    {
        RefreshRoundLabel();

        RectTransform cardRect = card.transform as RectTransform;
        Vector2 home = cardRect.anchoredPosition;
        float distance = Mathf.Max(0f, dealOffscreenDistance);
        float radians = dealAngle * Mathf.Deg2Rad;
        Vector2 offscreen = home + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * distance;
        cardRect.anchoredPosition = offscreen;

        Transform parent = cardRect.parent;
        int sibling = cardRect.GetSiblingIndex();
        int backCount = DeckCount() - 1;
        for (int i = 0; i < backCount; i++)
        {
            RectTransform back = SpawnBack(parent, cardRect, offscreen, sibling);
            if (back == null)
            {
                continue;
            }

            sibling++;
            yield return SlideTo(back, offscreen, home);
        }

        yield return SlideTo(cardRect, offscreen, home);

        ClearDealtBacks();
        card.ResetToHome();
        Show();
        if (endPanel == null || !endPanel.activeSelf)
        {
            card.enabled = true;
        }

        dealRoutine = null;
    }

    RectTransform SpawnBack(Transform parent, RectTransform cardRect, Vector2 offscreen, int sibling)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject backObject = new GameObject("DealtBack", typeof(RectTransform));
        backObject.layer = card.gameObject.layer;
        backObject.transform.SetParent(parent, false);

        RectTransform rect = backObject.GetComponent<RectTransform>();
        rect.anchorMin = cardRect.anchorMin;
        rect.anchorMax = cardRect.anchorMax;
        rect.pivot = cardRect.pivot;
        rect.sizeDelta = cardRect.sizeDelta;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = offscreen;
        rect.SetSiblingIndex(sibling);

        Image image = backObject.AddComponent<Image>();
        image.raycastTarget = false;
        Image source = staticCardBack != null ? staticCardBack.GetComponent<Image>() : null;
        if (source == null)
        {
            source = card.GetComponent<Image>();
        }

        if (source != null)
        {
            image.sprite = source.sprite;
            image.color = source.color;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
        }

        dealtBacks.Add(backObject);
        return rect;
    }

    IEnumerator SlideTo(RectTransform rect, Vector2 from, Vector2 to)
    {
        float duration = Mathf.Max(0.01f, dealDuration);
        rect.anchoredPosition = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        rect.anchoredPosition = to;
    }

    void HideStaticCardBack()
    {
        if (staticCardBack == null && card != null && card.transform.parent != null)
        {
            Transform found = card.transform.parent.Find("CardBcak");
            if (found != null)
            {
                staticCardBack = found.gameObject;
            }
        }

        if (staticCardBack != null)
        {
            staticCardBack.SetActive(false);
        }
    }

    void HidePrompt()
    {
        SetChoiceAlpha(questionText, 0f);
        SetChoiceAlpha(leftText, 0f);
        SetChoiceAlpha(rightText, 0f);
    }

    void ClearDealtBacks()
    {
        for (int i = 0; i < dealtBacks.Count; i++)
        {
            if (dealtBacks[i] != null)
            {
                Destroy(dealtBacks[i]);
            }
        }

        dealtBacks.Clear();
    }

    int DeckCount()
    {
        if (deck == null || deck.cards == null)
        {
            return 0;
        }

        return deck.cards.Length;
    }

    bool IsRunFailed()
    {
        if (stats == null)
        {
            return false;
        }

        return stats.wealth <= 0 || stats.performance <= 0 || stats.morale <= 0 || stats.fans <= 0;
    }

    void ShowEnd(bool clearedDeck)
    {
        runOver = true;

        if (card != null)
        {
            card.enabled = false;
        }

        if (endTitle != null)
        {
            endTitle.text = clearedDeck ? "Season complete" : "You failed";
        }

        if (endBody != null)
        {
            endBody.text = BuildSummary(clearedDeck);
        }

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }
    }

    string BuildSummary(bool clearedDeck)
    {
        int performance = stats != null ? stats.performance : 0;
        int morale = stats != null ? stats.morale : 0;
        int fans = stats != null ? stats.fans : 0;
        long wealth = stats != null ? stats.wealth : 0;

        return "Performance " + performance + "\n"
            + "Morale " + morale + "\n"
            + "Fans " + fans + "\n"
            + "Wealth " + TeamStatsDisplay.FormatWealth(wealth) + "\n\n"
            + Verdict(clearedDeck, performance, morale, fans, wealth);
    }

    string Verdict(bool clearedDeck, int performance, int morale, int fans, long wealth)
    {
        if (clearedDeck
            && performance >= dominantPerformance
            && morale >= dominantMorale
            && fans >= dominantFans
            && wealth >= dominantWealth)
        {
            return "A dominant season.";
        }

        if (clearedDeck && performance > 0 && morale > 0 && fans > 0 && wealth > 0)
        {
            return "The club survived the season.";
        }

        if (wealth <= 0)
        {
            return "The club is bankrupt.";
        }

        return CollapseSentence(performance, morale, fans);
    }

    static string CollapseSentence(int performance, int morale, int fans)
    {
        bool performanceFailed = performance <= 0;
        bool moraleFailed = morale <= 0;
        bool fansFailed = fans <= 0;

        string names;
        if (performanceFailed && moraleFailed && fansFailed)
        {
            names = "Performance, Morale, and Fans";
        }
        else if (performanceFailed && moraleFailed)
        {
            names = "Performance and Morale";
        }
        else if (performanceFailed && fansFailed)
        {
            names = "Performance and Fans";
        }
        else if (moraleFailed && fansFailed)
        {
            names = "Morale and Fans";
        }
        else if (performanceFailed)
        {
            names = "Performance";
        }
        else if (moraleFailed)
        {
            names = "Morale";
        }
        else if (fansFailed)
        {
            names = "Fans";
        }
        else
        {
            return "The season collapsed.";
        }

        return "The season collapsed. " + names + " reached zero.";
    }

    void ApplyCurrentEffect(bool choseRight)
    {
        if (stats == null || deck == null || deck.cards == null)
        {
            return;
        }

        if (index < 0 || index >= deck.cards.Length)
        {
            return;
        }

        CardDefinition current = deck.cards[index];
        if (current == null)
        {
            return;
        }

        stats.Apply(choseRight ? current.rightEffect : current.leftEffect);
    }

    void Show()
    {
        if (deck != null && deck.cards != null && deck.cards.Length > 0)
        {
            if (index < 0 || index >= deck.cards.Length)
            {
                index = 0;
            }

            CardDefinition current = deck.cards[index];
            if (current != null)
            {
                if (questionText != null)
                {
                    questionText.text = current.question;
                }

                if (leftText != null)
                {
                    leftText.text = current.leftChoice;
                }

                if (rightText != null)
                {
                    rightText.text = current.rightChoice;
                }
            }
        }

        if (questionText != null)
        {
            questionText.alpha = 1f;
        }

        ApplyChoiceFade();
        RefreshRoundLabel();
    }

    void RefreshRoundLabel()
    {
        if (roundText == null)
        {
            return;
        }

        int total = deck != null && deck.cards != null ? deck.cards.Length : 0;
        roundText.text = "Round " + roundCount + " / " + total;
    }

    void ApplyChoiceFade()
    {
        float amount = card != null ? card.DragAmount : 0f;
        float fade = Mathf.InverseLerp(ChoiceFadeStart, 1f, Mathf.Abs(amount));

        SetChoiceAlpha(leftText, amount < 0f ? fade : 0f);
        SetChoiceAlpha(rightText, amount > 0f ? fade : 0f);
    }

    static void SetChoiceAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.alpha = alpha;
    }
}
