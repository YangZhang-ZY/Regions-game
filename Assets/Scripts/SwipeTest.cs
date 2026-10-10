using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SwipeTest : MonoBehaviour
{
    public CardSwip card;
    public SeasonDeck deck;
    public TeamStatsDisplay stats;
    public TMP_Text questionText;

    [Tooltip("玩家点下卡牌后，问题文字淡入所需的秒数。下一题换上时也用这个时间。")]
    public float questionFadeDuration = 0.5f;

    public TMP_Text leftText;

    [Tooltip("向左拖动卡牌时与左侧选项一起淡入，回到中间时消失。")]
    public Image leftChoiceImage;

    [Tooltip("向右拖动卡牌时与右侧选项一起淡入，回到中间时消失。")]
    public Image rightChoiceImage;

    public TMP_Text rightText;
    public Image cardImage;
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
    const string CourtBackgroundSpriteGuid = "7d7dea9e553960a40895d46cd0990d66";
    const string SeasonSummaryScene = "SeasonSummary";

    [Tooltip("每张牌从左边滑到中间所用的秒数。越小越快。")]
    public float dealDuration = 0.28f;

    [Tooltip("牌从终点左侧多远的地方开始飞入。越大，起点越靠左。")]
    public float dealOffscreenDistance = 1400f;

    [Tooltip("飞入方向，单位是度。0 从右边来，90 从上方来，180 从左边来，135 从左上角来。牌从该方向飞向中心。")]
    public float dealAngle = 180f;

    [Tooltip("牌堆中每一层相对下一层的偏移，单位是界面像素。X 向右，Y 向上，下层牌会从左下方露出一圈边。")]
    public Vector2 stackOffset = new Vector2(10f, 8f);

    [Tooltip("每张牌开始滑入时播放一次。不指定则不播放。")]
    public AudioClip dealSound;

    [Tooltip("发牌音效音量。")]
    public float dealSoundVolume = 0.7f;

    int index;
    int roundCount = 1;
    int statPreviewSide;
    bool runOver;
    Coroutine dealRoutine;
    Coroutine questionFadeRoutine;
    Coroutine statsRevealRoutine;
    StatsBarUnfold statsBar;
    bool statsRevealed;
    bool statsVisible;
    readonly List<RectTransform> dealtBacks = new List<RectTransform>();
    RectTransform deckStack;
    Vector2 baseHome;
    bool baseHomeCaptured;
    GameObject staticCardBack;
    Sprite defaultCardSprite;
    Color defaultCardColor;
    bool defaultCardVisualCaptured;

    void Awake()
    {
        CaptureDefaultCardVisual();
        if (stats == null)
        {
            stats = GetComponent<TeamStatsDisplay>();
        }

        EnsureStatsBar();
        SetStatsAlpha(0f);
    }

    void Start()
    {
        SeasonRunSession.Begin();

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
            card.OnTrackingStarted += HandleTrackingStarted;
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
            card.OnTrackingStarted -= HandleTrackingStarted;
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
        RemoveTopBack();
        PlacePlayableCard();
        Show();
    }

    public void Restart()
    {
        if (stats != null)
        {
            stats.RestoreStart();
            stats.Refresh();
        }

        SeasonRunSession.Begin();
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
        DestroyLeftoverStacks();
        HideStaticCardBack();
        HidePrompt();

        if (card != null)
        {
            card.StopTracking();
            card.enabled = false;
            card.ResetToHome();
            CaptureBaseHome();
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
        CaptureBaseHome();
        float distance = Mathf.Max(0f, dealOffscreenDistance);
        float radians = dealAngle * Mathf.Deg2Rad;
        Vector2 slide = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * distance;
        int topLayer = Mathf.Max(0, DeckCount() - 1);
        slide.y += DealOffscreenRise(cardRect, slide.y, topLayer);
        cardRect.anchoredPosition = StackRest(topLayer) + slide;

        Transform stack = EnsureDeckStack(cardRect);
        int backCount = DeckCount() - 1;
        for (int i = 0; i < backCount; i++)
        {
            int layer = dealtBacks.Count;
            Vector2 rest = StackRest(layer);
            RectTransform back = SpawnBack(stack, cardRect, rest + slide, i);
            if (back == null)
            {
                continue;
            }

            yield return SlideTo(back, rest + slide, rest);
        }

        Vector2 top = StackRest(dealtBacks.Count);
        yield return SlideTo(cardRect, top + slide, top);

        PlacePlayableCard();
        PlaceDeckStack();
        Show();
        if (endPanel == null || !endPanel.activeSelf)
        {
            card.enabled = true;
        }

        dealRoutine = null;
    }

    float DealOffscreenRise(RectTransform cardRect, float slideY, int topLayer)
    {
        float cardHeight = Mathf.Abs(cardRect.sizeDelta.y);
        if (cardHeight < 1f)
        {
            cardHeight = Mathf.Abs(cardRect.rect.height);
        }

        RectTransform parent = cardRect.parent as RectTransform;
        float parentHeight = parent != null ? Mathf.Abs(parent.rect.height) : 0f;
        float rise = parentHeight * 0.5f + cardHeight;
        if (parent == null)
        {
            return rise;
        }

        float restY = baseHome.y + Mathf.Min(0f, stackOffset.y) * topLayer;
        float anchorY = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, (cardRect.anchorMin.y + cardRect.anchorMax.y) * 0.5f);
        float neededSlideY = parent.rect.yMax - anchorY - restY + cardHeight * cardRect.pivot.y;
        return Mathf.Max(rise, neededSlideY - slideY);
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
        rect.SetAsLastSibling();

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

        dealtBacks.Add(rect);
        return rect;
    }

    IEnumerator SlideTo(RectTransform rect, Vector2 from, Vector2 to)
    {
        PlayDealSound();
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

    void PlayDealSound()
    {
        if (dealSound == null || dealSoundVolume <= 0f)
        {
            return;
        }

        Vector3 position = Vector3.zero;
        if (Camera.main != null)
        {
            position = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(dealSound, position, dealSoundVolume);
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

    void HandleTrackingStarted()
    {
        BeginQuestionFade();
    }

    void HidePrompt()
    {
        StopQuestionFade();
        StopStatsReveal();
        statsRevealed = false;
        statsVisible = false;
        SetChoiceAlpha(questionText, 0f);
        SetChoiceAlpha(leftText, 0f);
        SetChoiceAlpha(leftChoiceImage, 0f);
        SetChoiceAlpha(rightText, 0f);
        SetChoiceAlpha(rightChoiceImage, 0f);
        if (stats != null)
        {
            stats.StopFlash();
        }

        SetStatsAlpha(0f);
        CollapseStatsBar();
        EndStatPreview();
    }

    void CaptureBaseHome()
    {
        if (baseHomeCaptured || card == null)
        {
            return;
        }

        RectTransform cardRect = card.transform as RectTransform;
        if (cardRect == null)
        {
            return;
        }

        baseHome = cardRect.anchoredPosition;
        baseHomeCaptured = true;
    }

    Vector2 StackRest(int layer)
    {
        return baseHome + stackOffset * layer;
    }

    void PlacePlayableCard()
    {
        if (card == null || !baseHomeCaptured)
        {
            return;
        }

        card.SetHome(StackRest(dealtBacks.Count));
        card.ResetToHome();
    }

    void RemoveTopBack()
    {
        if (dealtBacks.Count == 0)
        {
            return;
        }

        int topIndex = dealtBacks.Count - 1;
        RectTransform top = dealtBacks[topIndex];
        dealtBacks.RemoveAt(topIndex);
        if (top != null)
        {
            Destroy(top.gameObject);
        }
    }

    void ClearDealtBacks()
    {
        for (int i = 0; i < dealtBacks.Count; i++)
        {
            if (dealtBacks[i] != null)
            {
                DestroyImmediate(dealtBacks[i].gameObject);
            }
        }

        dealtBacks.Clear();
        if (deckStack != null)
        {
            DestroyImmediate(deckStack.gameObject);
            deckStack = null;
        }
    }

    void DestroyLeftoverStacks()
    {
        if (card == null)
        {
            return;
        }

        Transform parent = card.transform.parent;
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name == "DealtBack" || child.name == "DeckStack")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        dealtBacks.Clear();
        deckStack = null;
    }

    RectTransform EnsureDeckStack(RectTransform cardRect)
    {
        if (deckStack == null)
        {
            GameObject stackObject = new GameObject("DeckStack", typeof(RectTransform));
            stackObject.layer = card.gameObject.layer;
            stackObject.transform.SetParent(cardRect.parent, false);
            deckStack = stackObject.GetComponent<RectTransform>();
            deckStack.anchorMin = Vector2.zero;
            deckStack.anchorMax = Vector2.one;
            deckStack.offsetMin = Vector2.zero;
            deckStack.offsetMax = Vector2.zero;
            deckStack.pivot = new Vector2(0.5f, 0.5f);
        }

        PlaceDeckStack();
        return deckStack;
    }

    void PlaceDeckStack()
    {
        if (deckStack == null)
        {
            return;
        }

        Transform background = FindCourtBackground(deckStack.parent);
        if (background == null)
        {
            deckStack.SetAsFirstSibling();
            return;
        }

        int stackIndex = deckStack.GetSiblingIndex();
        int target = background.GetSiblingIndex() + 1;
        if (stackIndex < target)
        {
            target--;
        }

        if (card != null)
        {
            int cardIndex = card.transform.GetSiblingIndex();
            int lastIndexBeforeCard = stackIndex < cardIndex ? cardIndex - 1 : cardIndex;
            if (target > lastIndexBeforeCard)
            {
                target = lastIndexBeforeCard;
            }
        }

        if (target < 0)
        {
            target = 0;
        }

        deckStack.SetSiblingIndex(target);
    }

    Transform FindCourtBackground(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        Transform namedBackground = null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == deckStack)
            {
                continue;
            }

            if (UsesCourtBackgroundSprite(child))
            {
                return child;
            }

            if (namedBackground == null && child.name.Equals("Background", System.StringComparison.OrdinalIgnoreCase))
            {
                namedBackground = child;
            }
        }

        return namedBackground;
    }

    static bool UsesCourtBackgroundSprite(Transform child)
    {
        Image image = child.GetComponent<Image>();
        if (image == null || image.sprite == null)
        {
            return false;
        }

#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(image.sprite);
        if (!string.IsNullOrEmpty(assetPath)
            && UnityEditor.AssetDatabase.AssetPathToGUID(assetPath) == CourtBackgroundSpriteGuid)
        {
            return true;
        }
#endif

        return image.sprite.name == "GameBackground";
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
        EndStatPreview();

        if (card != null)
        {
            card.enabled = false;
        }

        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        int performance = stats != null ? stats.performance : 0;
        int morale = stats != null ? stats.morale : 0;
        int fans = stats != null ? stats.fans : 0;
        long wealth = stats != null ? stats.wealth : 0;
        int startPerformance = stats != null ? stats.StartingPerformance : 0;
        int startMorale = stats != null ? stats.StartingMorale : 0;
        int startFans = stats != null ? stats.StartingFans : 0;
        long startWealth = stats != null ? stats.StartingWealth : 0;
        SeasonRunSession.Commit(
            startPerformance,
            startMorale,
            startFans,
            startWealth,
            performance,
            morale,
            fans,
            wealth,
            Verdict(
                clearedDeck,
                performance,
                morale,
                fans,
                wealth,
                startPerformance,
                startMorale,
                startFans,
                startWealth));
        SceneManager.LoadScene(SeasonSummaryScene);
    }

    string Verdict(
        bool clearedDeck,
        int performance,
        int morale,
        int fans,
        long wealth,
        int startPerformance,
        int startMorale,
        int startFans,
        long startWealth)
    {
        bool wealthFailed = wealth <= 0;
        bool performanceFailed = performance <= 0;
        bool moraleFailed = morale <= 0;
        bool fansFailed = fans <= 0;
        if (wealthFailed || performanceFailed || moraleFailed || fansFailed)
        {
            return FailureParagraph(wealthFailed, performanceFailed, moraleFailed, fansFailed);
        }

        if (clearedDeck
            && performance >= dominantPerformance
            && morale >= dominantMorale
            && fans >= dominantFans
            && wealth >= dominantWealth)
        {
            return "This was a dominant season. The club finished the year in command of its results, its locker room, its public, and its books. It leaves the campaign as a side that set the standard.";
        }

        if (clearedDeck)
        {
            return SurvivedParagraph(
                performance - startPerformance,
                morale - startMorale,
                fans - startFans,
                wealth - startWealth);
        }

        return "The season ended before the deck was finished.";
    }

    static string FailureParagraph(bool wealthFailed, bool performanceFailed, bool moraleFailed, bool fansFailed)
    {
        int count = (wealthFailed ? 1 : 0)
            + (performanceFailed ? 1 : 0)
            + (moraleFailed ? 1 : 0)
            + (fansFailed ? 1 : 0);
        if (count == 1)
        {
            if (wealthFailed)
            {
                return "The books no longer balance. Income has run out, and the club is bankrupt. It is closing.";
            }

            if (performanceFailed)
            {
                return "Results on the floor have fallen apart. The season came undone on the pitch, and the club could not put the campaign back together. There is no run of results left to save it.";
            }

            if (moraleFailed)
            {
                return "The locker room has given up. The players are done with the plan, and they will not see it through. The season ends with that room.";
            }

            return "The crowd has left. The club has lost its public, and the stands have emptied. There is no one left to play the rest of this season for.";
        }

        List<string> picture = new List<string>();
        List<string> cost = new List<string>();
        if (performanceFailed)
        {
            picture.Add("results on the floor have fallen apart");
            cost.Add("the season has come apart");
        }

        if (moraleFailed)
        {
            picture.Add("the locker room has given up");
            cost.Add("the players are done with the plan");
        }

        if (fansFailed)
        {
            picture.Add("the crowd has left");
            cost.Add("the club has lost its public");
        }

        if (wealthFailed)
        {
            picture.Add("the books no longer balance");
            cost.Add("income has run out");
        }

        string paragraph = Sentence(picture) + " " + Sentence(cost);
        if (wealthFailed)
        {
            paragraph += " The club is bankrupt and closing.";
        }

        return paragraph;
    }

    static string SurvivedParagraph(int performanceDelta, int moraleDelta, int fansDelta, long wealthDelta)
    {
        bool performanceUp = performanceDelta > 0;
        bool performanceDown = performanceDelta < 0;
        bool moraleUp = moraleDelta > 0;
        bool moraleDown = moraleDelta < 0;
        bool fansUp = fansDelta > 0;
        bool fansDown = fansDelta < 0;
        bool wealthUp = wealthDelta > 0;
        bool wealthDown = wealthDelta < 0;
        bool anyUp = performanceUp || moraleUp || fansUp || wealthUp;
        bool anyDown = performanceDown || moraleDown || fansDown || wealthDown;

        if (!anyUp && !anyDown)
        {
            return "The club got through the year. It leaves the season as it began, a stable club with nothing gained and nothing given away.";
        }

        if (!anyDown)
        {
            return "The club got through the year. It leaves the season stronger than it began, stable, and better set than the side that started the campaign.";
        }

        string worn = DownShape(performanceDown, moraleDown, fansDown, wealthDown);
        if (!anyUp)
        {
            return "The club got through the year. It leaves the season " + worn + ", still standing, with a harder year behind it.";
        }

        string gained = UpShape(performanceUp, moraleUp, fansUp, wealthUp);
        return "The club got through the year. It leaves the season " + gained + ", and " + worn + " where the year took something. It is intact, and it is not the same club that started.";
    }

    static string UpShape(bool performanceUp, bool moraleUp, bool fansUp, bool wealthUp)
    {
        List<string> parts = new List<string>();
        if (wealthUp)
        {
            parts.Add("richer");
        }

        if (fansUp)
        {
            parts.Add("better followed");
        }

        if (performanceUp)
        {
            parts.Add("sharper in its results");
        }

        if (moraleUp)
        {
            parts.Add("steadier in the room");
        }

        return JoinAnd(parts);
    }

    static string DownShape(bool performanceDown, bool moraleDown, bool fansDown, bool wealthDown)
    {
        List<string> parts = new List<string>();
        if (performanceDown && moraleDown)
        {
            parts.Add("bruised on the field and in the room");
        }
        else if (performanceDown)
        {
            parts.Add("bruised on the field");
        }
        else if (moraleDown)
        {
            parts.Add("bruised in the room");
        }

        if (fansDown)
        {
            parts.Add("quieter");
        }

        if (wealthDown)
        {
            parts.Add("poorer");
        }

        return JoinAnd(parts);
    }

    static string Sentence(List<string> parts)
    {
        return Capitalize(JoinAnd(parts)) + ".";
    }

    static string JoinAnd(List<string> parts)
    {
        if (parts == null || parts.Count == 0)
        {
            return "";
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        if (parts.Count == 2)
        {
            return parts[0] + " and " + parts[1];
        }

        string text = parts[0];
        for (int i = 1; i < parts.Count - 1; i++)
        {
            text += ", " + parts[i];
        }

        return text + ", and " + parts[parts.Count - 1];
    }

    static string Capitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.Length == 1)
        {
            return text.ToUpperInvariant();
        }

        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    void ApplyCurrentEffect(bool choseRight)
    {
        EndStatPreview();

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

        string choiceText = choseRight ? current.rightChoice : current.leftChoice;
        if (string.IsNullOrWhiteSpace(choiceText))
        {
            choiceText = current.question;
        }

        SeasonRunSession.RecordChoice(choiceText, choseRight);
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

                ApplyCardArt(current);
            }
        }

        if (questionText != null)
        {
            questionText.alpha = 0f;
        }

        if (card != null && card.IsTracking)
        {
            BeginQuestionFade();
        }
        else
        {
            StopQuestionFade();
        }

        ApplyChoiceFade();
        RefreshRoundLabel();
    }

    void BeginQuestionFade()
    {
        BeginStatsReveal();
        StopQuestionFade();

        if (questionText == null)
        {
            return;
        }

        questionFadeRoutine = StartCoroutine(FadeQuestionIn());
    }

    void StopQuestionFade()
    {
        if (questionFadeRoutine == null)
        {
            return;
        }

        StopCoroutine(questionFadeRoutine);
        questionFadeRoutine = null;
    }

    IEnumerator FadeQuestionIn()
    {
        float duration = Mathf.Max(0.01f, questionFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (questionText != null)
            {
                questionText.alpha = Mathf.Clamp01(elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (questionText != null)
        {
            questionText.alpha = 1f;
        }

        questionFadeRoutine = null;
    }

    void BeginStatsReveal()
    {
        if (statsRevealed)
        {
            return;
        }

        statsRevealed = true;
        statsVisible = false;
        EnsureStatsBar();
        if (statsBar != null)
        {
            statsBar.BeginUnfold();
        }

        StopStatsReveal();
        statsRevealRoutine = StartCoroutine(FadeStatsIn());
    }

    void StopStatsReveal()
    {
        if (statsRevealRoutine == null)
        {
            return;
        }

        StopCoroutine(statsRevealRoutine);
        statsRevealRoutine = null;
    }

    IEnumerator FadeStatsIn()
    {
        float duration = Mathf.Max(0.01f, questionFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetStatsAlpha(Mathf.Clamp01(elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetStatsAlpha(1f);
        statsVisible = true;
        statsRevealRoutine = null;
    }

    void SetStatsAlpha(float alpha)
    {
        if (stats != null)
        {
            stats.SetValueAlpha(alpha);
        }

        SetChoiceAlpha(roundText, alpha);
    }

    void EnsureStatsBar()
    {
        if (statsBar != null)
        {
            return;
        }

        statsBar = GetComponent<StatsBarUnfold>();
        if (statsBar == null)
        {
            statsBar = FindFirstObjectByType<StatsBarUnfold>();
        }
    }

    void CollapseStatsBar()
    {
        EnsureStatsBar();
        if (statsBar != null)
        {
            statsBar.Collapse();
        }
    }

    void CaptureDefaultCardVisual()
    {
        if (defaultCardVisualCaptured)
        {
            return;
        }

        Image image = cardImage;
        if (image == null && card != null)
        {
            image = card.GetComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        defaultCardSprite = image.sprite;
        defaultCardColor = image.color;
        defaultCardVisualCaptured = true;
    }

    void ApplyCardArt(CardDefinition current)
    {
        if (cardImage == null && card != null)
        {
            cardImage = card.GetComponent<Image>();
        }

        if (!defaultCardVisualCaptured)
        {
            CaptureDefaultCardVisual();
        }

        if (cardImage == null || current == null)
        {
            return;
        }

        if (current.art != null)
        {
            cardImage.sprite = current.art;
            cardImage.color = Color.white;
        }
        else if (defaultCardVisualCaptured)
        {
            cardImage.sprite = defaultCardSprite;
            cardImage.color = defaultCardColor;
        }
    }

    void RefreshRoundLabel()
    {
        if (roundText == null)
        {
            return;
        }

        if (deck != null && deck.cards != null)
        {
            roundText.text = roundCount + " / " + deck.cards.Length;
        }
        else
        {
            roundText.text = roundCount.ToString();
        }
    }

    void ApplyChoiceFade()
    {
        float amount = card != null ? card.DragAmount : 0f;
        float fade = Mathf.InverseLerp(ChoiceFadeStart, 1f, Mathf.Abs(amount));

        float leftAlpha = amount < 0f ? fade : 0f;
        float rightAlpha = amount > 0f ? fade : 0f;
        SetChoiceAlpha(leftText, leftAlpha);
        SetChoiceAlpha(leftChoiceImage, leftAlpha);
        SetChoiceAlpha(rightText, rightAlpha);
        SetChoiceAlpha(rightChoiceImage, rightAlpha);
        ApplyStatPreview(amount, fade);
    }

    void ApplyStatPreview(float amount, float fade)
    {
        if (stats == null || !statsVisible)
        {
            EndStatPreview();
            return;
        }

        int side = 0;
        if (!runOver && fade > 0f)
        {
            if (amount < 0f)
            {
                side = -1;
            }
            else if (amount > 0f)
            {
                side = 1;
            }
        }

        if (side == 0)
        {
            EndStatPreview();
            return;
        }

        if (side == statPreviewSide)
        {
            return;
        }

        if (deck == null || deck.cards == null || index < 0 || index >= deck.cards.Length)
        {
            EndStatPreview();
            return;
        }

        CardDefinition current = deck.cards[index];
        if (current == null)
        {
            EndStatPreview();
            return;
        }

        statPreviewSide = side;
        stats.Preview(side < 0 ? current.leftEffect : current.rightEffect);
    }

    void EndStatPreview()
    {
        if (statPreviewSide == 0)
        {
            return;
        }

        statPreviewSide = 0;
        if (stats != null)
        {
            stats.Refresh();
        }
    }

    static void SetChoiceAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.alpha = alpha;
    }

    static void SetChoiceAlpha(Image image, float alpha)
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
