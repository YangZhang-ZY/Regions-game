using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public void Begin()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
