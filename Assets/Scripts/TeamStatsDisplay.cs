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

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (performanceText != null)
        {
            performanceText.text = "Performance " + performance;
        }

        if (moraleText != null)
        {
            moraleText.text = "Morale " + morale;
        }

        if (fansText != null)
        {
            fansText.text = "Fans " + fans;
        }

        if (wealthText != null)
        {
            wealthText.text = "Wealth " + FormatWealth(wealth);
        }
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
}
