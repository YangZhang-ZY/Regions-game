using System.Collections.Generic;

public static class SeasonRunSession
{
    static readonly List<string> choiceTexts = new List<string>();
    static readonly List<bool> choseRight = new List<bool>();

    public static bool HasRun { get; private set; }
    public static int StartPerformance { get; private set; }
    public static int StartMorale { get; private set; }
    public static int StartFans { get; private set; }
    public static long StartWealth { get; private set; }
    public static int Performance { get; private set; }
    public static int Morale { get; private set; }
    public static int Fans { get; private set; }
    public static long Wealth { get; private set; }
    public static string Outcome { get; private set; }

    public static int ChoiceCount
    {
        get { return choiceTexts.Count; }
    }

    public static void Begin()
    {
        HasRun = false;
        StartPerformance = 0;
        StartMorale = 0;
        StartFans = 0;
        StartWealth = 0;
        Performance = 0;
        Morale = 0;
        Fans = 0;
        Wealth = 0;
        Outcome = null;
        choiceTexts.Clear();
        choseRight.Clear();
    }

    public static void RecordChoice(string choiceText, bool right)
    {
        if (string.IsNullOrWhiteSpace(choiceText))
        {
            choiceText = "Choice";
        }

        choiceText = choiceText.Replace('\r', ' ').Replace('\n', ' ').Trim();
        choiceTexts.Add(choiceText);
        choseRight.Add(right);
    }

    public static string ChoiceText(int index)
    {
        return choiceTexts[index];
    }

    public static bool ChoiceWasRight(int index)
    {
        return choseRight[index];
    }

    public static void Commit(
        int startPerformance,
        int startMorale,
        int startFans,
        long startWealth,
        int performance,
        int morale,
        int fans,
        long wealth,
        string outcome)
    {
        StartPerformance = startPerformance;
        StartMorale = startMorale;
        StartFans = startFans;
        StartWealth = startWealth;
        Performance = performance;
        Morale = morale;
        Fans = fans;
        Wealth = wealth;
        if (string.IsNullOrWhiteSpace(outcome))
        {
            outcome = "The season ended.";
        }

        Outcome = outcome.Replace('\r', ' ').Replace('\n', ' ').Trim();
        HasRun = true;
    }
}
