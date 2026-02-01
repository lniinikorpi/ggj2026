using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryProgressionScriptMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Collider triggerCollider;
    public Transform playerTransform;
    [SerializeField] private CanvasGroup fadeCanvas;
    private Coroutine fadeRoutine;
    bool fadeRunning = false;

    void Update()
    {
        if (triggerCollider.bounds.Contains(playerTransform.position) && !fadeRunning)
        {
            print("Player within the trigger area");
            StartCoroutine(FadeCanvasAlphaTo(1, 2));
            fadeRunning = true;
        }
    }
    private System.Collections.IEnumerator FadeCanvasAlphaTo(float targetAlpha, float duration)
    {
        if (fadeCanvas == null) yield break;

        float startAlpha = fadeCanvas.alpha;
        if (duration <= 0f)
        {
            fadeCanvas.alpha = targetAlpha;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerpT = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerpT);
            yield return null;
        }
        fadeCanvas.alpha = targetAlpha;
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
    }
}
