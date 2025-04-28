using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public string levelName;

    public void StartGame()
    {
        SceneManager.LoadScene(levelName);
    }
}
