using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StorySceneChanger : MonoBehaviour
{

    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isPlaying = stateInfo.normalizedTime < 1f && !animator.IsInTransition(0);
        if (!isPlaying) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        }
    }
}
