using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private Button skipTutorialButton;

    private void Start()
    {
        SaveData save = SaveSystem.LoadGame();
        if (skipTutorialButton != null)
        {
            skipTutorialButton.gameObject.SetActive(save.tutorialCompleted);
            skipTutorialButton.interactable = save.tutorialCompleted;
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    }

    public void QuitGame()
    {
        Application.Quit(); 
    }

    public void SkipTutorial()
    {
        SceneManager.LoadScene("DogCustomize");
    }

}
