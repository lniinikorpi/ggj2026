using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizeUIController : MonoBehaviour
{
    public List<SkinnedMeshRenderer> maskRenderers;
    public List<Button> maskButtons;
    public Transform dogTransform;
    public Animator dogAnimator;

    private void Start()
    {
        dogAnimator.Play("DoggoRig|Doggo_Idle_Stand");
        SaveData save = SaveSystem.LoadGame();

        // Unlocking works in reverse:
        // - Leaderboard index 0 (best) => all masks unlocked
        // - Leaderboard index 1 => all except the last mask unlocked
        // - etc.
        // If the player has no placement yet, only the first mask should be available.
        int rawLeaderboardIndex = save.playerLeaderboardIndex;

        bool hasPlayerPlacement = rawLeaderboardIndex >= 0;
        if (hasPlayerPlacement && save.highScores != null && save.highScores.highScores != null)
        {
            // Back-compat: older saves may have `playerLeaderboardIndex = 0` by default even when the player
            // never set a time. If the save contains only baseline entries ("TIME n"), treat as no placement.
            bool anyNonBaseline = false;
            for (int i = 0; i < save.highScores.highScores.Count; i++)
            {
                var e = save.highScores.highScores[i];
                if (e != null && !string.IsNullOrEmpty(e.playerName) && !e.playerName.StartsWith("TIME ", StringComparison.OrdinalIgnoreCase))
                {
                    anyNonBaseline = true;
                    break;
                }
            }

            if (!anyNonBaseline)
                hasPlayerPlacement = false;
        }

        int maxUnlockedIndex;
        if (!hasPlayerPlacement)
        {
            maxUnlockedIndex = 0;
        }
        else
        {
            int countMinusOne = Mathf.Max(0, maskButtons.Count - 1);
            maxUnlockedIndex = Mathf.Clamp(countMinusOne - rawLeaderboardIndex, 0, countMinusOne);
        }

        for (int i = 0; i < maskButtons.Count; i++)
        {
            bool unlocked = i <= maxUnlockedIndex;
            maskButtons[i].interactable = unlocked;
            maskButtons[i].GetComponent<CustomizeUIButton>().EnableButton(unlocked);
        }

        int selectedMask = Mathf.Clamp(save.selectedMaskMaterialIndex, 0, Mathf.Max(0, maxUnlockedIndex));

        if (maskButtons.Count > 0)
            maskButtons[selectedMask].onClick.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        dogTransform.eulerAngles += new Vector3(0, 30, 0) * Time.deltaTime;
    }
}
