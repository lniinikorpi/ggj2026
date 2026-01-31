using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RandomBailText : MonoBehaviour
{

    [ContextMenu("Debug Trigger: General Bail")]
    public void DebugBail() => TriggerBail(false);

    [ContextMenu("Debug Trigger: Water Hazard")]
    public void DebugWater() => TriggerBail(true);
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI displayLib;
    [SerializeField] private RectTransform textRect; // Drag the Text's RectTransform here

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private Color waterColor = new Color(0.2f, 0.6f, 1f); // Blueish
    [SerializeField] private Color failColor = Color.red;

    private List<string> waterLines = new List<string> { "Watch that current...", "Water....BAAAD!!!", "All wet.", "No swimming.", "You're hosed!", "You big drip!", "I'm drowning!", "No swimming", "Shark Attack!", "Jaws of death!", "Splash", "You smell like a wet dog!", "Thank you, come again.", "How about an ice cold drink?" };
    private List<string> generalLines = new List<string> { "Watch that last step!", "Whoops.", "Well, it WAS a nice trick...", "Not the smartest move!","Young dog, no tricks..." };

    private Coroutine activeRoutine;

    void Start()
    {
        textRect.localScale = Vector3.zero;
    }

    // Call this and pass 'true' if they hit water, 'false' if it's just a bail
    public void TriggerBail(bool isWater)
    {
        // 1. Setup Content & Color
        if (isWater)
        {
            displayLib.text = waterLines[Random.Range(0, waterLines.Count)];
            displayLib.color = waterColor;
        }
        else
        {
            displayLib.text = generalLines[Random.Range(0, generalLines.Count)];
            displayLib.color = failColor;
        }

        // 2. Trigger the Pop Animation
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {

        // --- POP IN EFFECT ---
        float elapsed = 0;
        float popTime = 0.15f;
        while (elapsed < popTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / popTime); // Overshoot slightly
            textRect.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }
        textRect.localScale = Vector3.one; // Settle at 1.0

        // --- HOLD ---
        yield return new WaitForSeconds(displayDuration);

        // --- FADE OUT ---
        elapsed = 0;
        float fadeTime = 0.3f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            displayLib.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        textRect.localScale = Vector3.zero;
        displayLib.alpha = 1f; // Reset for next time
    }
}