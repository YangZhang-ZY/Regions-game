using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public void Begin()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Quit()
    {
        StartCoroutine(QuitAfterClick());
    }

    IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.9f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
