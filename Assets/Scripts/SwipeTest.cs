using UnityEngine;

public class SwipeTest : MonoBehaviour
{
    public CardSwip card;

    void Start()
    {
        if (card != null)
        {
            card.OnSwiped += HandleSwiped;
        }
    }

    void OnDestroy()
    {
        if (card != null)
        {
            card.OnSwiped -= HandleSwiped;
        }
    }

    void HandleSwiped(bool choseRight)
    {
        Debug.Log(choseRight ? "选择了右边" : "选择了左边");

        if (card != null)
        {
            card.ResetToHome();
        }
    }
}
