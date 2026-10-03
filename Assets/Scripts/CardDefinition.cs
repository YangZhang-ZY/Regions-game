using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Basketball/Card")]
public class CardDefinition : ScriptableObject
{
    public string title = "Event";
    public Sprite art;

    [TextArea(2, 4)]
    public string question;

    public string leftChoice = "Left";
    public string rightChoice = "Right";

    public StatChange leftEffect;
    public StatChange rightEffect;

    [TextArea(2, 4)]
    public string leftResult;

    [TextArea(2, 4)]
    public string rightResult;
}

[System.Serializable]
public struct StatChange
{
    public int performance;
    public int morale;
    public int fans;

    [Tooltip("美元。-500000 表示减少五十万。")]
    public int wealth;
}
