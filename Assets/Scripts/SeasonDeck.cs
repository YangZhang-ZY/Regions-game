using UnityEngine;

[CreateAssetMenu(fileName = "SeasonDeck", menuName = "Basketball/Season Deck")]
public class SeasonDeck : ScriptableObject
{
    public CardDefinition[] cards;
}
